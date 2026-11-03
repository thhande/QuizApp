using Microsoft.Data.Sqlite;
using QuizApp.Infrastructure.Persistance.Sqlite;

namespace QuizApp.Infrastructure.Tests.Persistence.Sqlite;

public class DatabaseInitializerTests
{
    private static string CreateTempDatabasePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString(),
            "test.db");
    }

    [Fact]
    public async Task InitializeAsync_CreatesAllTables()
    {
        // Arrange
        var databasePath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(databasePath);
            var initializer = new DatabaseInitializer(factory);

            // Act
            await initializer.InitializeAsync();

            // Assert
            await using var connection =
                await factory.OpenConnectionAsync();

            var tables = await GetTableNamesAsync(connection);

            Assert.Contains("QuizAttempts", tables);
            Assert.Contains("Answers", tables);
            Assert.Contains("SelectedOptions", tables);
            Assert.Contains("QuizResults", tables);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task InitializeAsync_SetsSchemaVersionToOne()
    {
        // Arrange
        var databasePath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(databasePath);
            var initializer = new DatabaseInitializer(factory);

            // Act
            await initializer.InitializeAsync();

            // Assert
            await using var connection =
                await factory.OpenConnectionAsync();

            await using var command = connection.CreateCommand();

            command.CommandText = "PRAGMA user_version;";

            var version = Convert.ToInt32(
                await command.ExecuteScalarAsync());

            Assert.Equal(1, version);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_DoesNotLoseData()
    {
        // Arrange
        var databasePath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(databasePath);
            var initializer = new DatabaseInitializer(factory);

            await initializer.InitializeAsync();

            var attemptId = Guid.NewGuid();

            await using (var connection =
                await factory.OpenConnectionAsync())
            {
                await using var command = connection.CreateCommand();

                command.CommandText = """
                    INSERT INTO QuizAttempts
                    (
                        Id,
                        QuizId,
                        ExamContentHash,
                        StartedAtUtc,
                        DeadlineUtc,
                        Status
                    )
                    VALUES
                    (
                        $id,
                        $quizId,
                        $hash,
                        $startedAt,
                        $deadline,
                        0
                    );
                    """;

                command.Parameters.AddWithValue(
                    "$id", attemptId.ToString());

                command.Parameters.AddWithValue(
                    "$quizId", Guid.NewGuid().ToString());

                command.Parameters.AddWithValue(
                    "$hash", "test-hash");

                command.Parameters.AddWithValue(
                    "$startedAt", DateTime.UtcNow.ToString("O"));

                command.Parameters.AddWithValue(
                    "$deadline", DateTime.UtcNow.AddMinutes(30).ToString("O"));

                await command.ExecuteNonQueryAsync();
            }

            // Act
            await initializer.InitializeAsync();

            // Assert
            await using (var connection =
                await factory.OpenConnectionAsync())
            {
                await using var command = connection.CreateCommand();

                command.CommandText = """
                    SELECT COUNT(*)
                    FROM QuizAttempts
                    WHERE Id = $id;
                    """;

                command.Parameters.AddWithValue(
                    "$id", attemptId.ToString());

                var count = Convert.ToInt32(
                    await command.ExecuteScalarAsync());

                Assert.Equal(1, count);
            }
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task ForeignKey_DeleteAttempt_CascadesToAnswers()
    {
        // Arrange
        var databasePath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(databasePath);
            var initializer = new DatabaseInitializer(factory);

            await initializer.InitializeAsync();

            var attemptId = Guid.NewGuid();
            var questionId = Guid.NewGuid();

            await using var connection =
                await factory.OpenConnectionAsync();

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    INSERT INTO QuizAttempts
                    (
                        Id,
                        QuizId,
                        ExamContentHash,
                        StartedAtUtc,
                        DeadlineUtc,
                        Status
                    )
                    VALUES
                    (
                        $id,
                        $quizId,
                        $hash,
                        $startedAt,
                        $deadline,
                        0
                    );

                    INSERT INTO Answers
                    (
                        AttemptId,
                        QuestionId,
                        AnsweredAtUtc
                    )
                    VALUES
                    (
                        $attemptId,
                        $questionId,
                        $answeredAt
                    );
                    """;

                command.Parameters.AddWithValue(
                    "$id", attemptId.ToString());

                command.Parameters.AddWithValue(
                    "$quizId", Guid.NewGuid().ToString());

                command.Parameters.AddWithValue(
                    "$hash", "test-hash");

                command.Parameters.AddWithValue(
                    "$startedAt", DateTime.UtcNow.ToString("O"));

                command.Parameters.AddWithValue(
                    "$deadline", DateTime.UtcNow.AddMinutes(30).ToString("O"));

                command.Parameters.AddWithValue(
                    "$attemptId", attemptId.ToString());

                command.Parameters.AddWithValue(
                    "$questionId", questionId.ToString());

                command.Parameters.AddWithValue(
                    "$answeredAt", DateTime.UtcNow.ToString("O"));

                await command.ExecuteNonQueryAsync();
            }

            // Act
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    DELETE FROM QuizAttempts
                    WHERE Id = $id;
                    """;

                command.Parameters.AddWithValue(
                    "$id", attemptId.ToString());

                await command.ExecuteNonQueryAsync();
            }

            // Assert
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT COUNT(*)
                    FROM Answers
                    WHERE AttemptId = $id;
                    """;

                command.Parameters.AddWithValue(
                    "$id", attemptId.ToString());

                var count = Convert.ToInt32(
                    await command.ExecuteScalarAsync());

                Assert.Equal(0, count);
            }
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static async Task<HashSet<string>> GetTableNamesAsync(
        SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table';
            """;

        await using var reader = await command.ExecuteReaderAsync();

        var tables = new HashSet<string>();

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static void DeleteDatabase(string databasePath)
    {
        // Close idle SQLite connections retained by the pool.
        SqliteConnection.ClearAllPools();

        var directory = Path.GetDirectoryName(databasePath);

        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}