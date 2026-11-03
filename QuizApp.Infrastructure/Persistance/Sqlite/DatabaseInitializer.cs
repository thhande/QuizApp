using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace QuizApp.Infrastructure.Persistance.Sqlite
{
    public class DatabaseInitializer
    {
        private const int CurrentSchemaVersion = 1;

        private readonly SqliteConnectionFactory _connectionFactory;

        public DatabaseInitializer(
            SqliteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InitializeAsync(
            CancellationToken cancellationToken = default)
        {
            await using var connection =
                await _connectionFactory.OpenConnectionAsync(
                    cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            var currentVersion = await GetSchemaVersionAsync(
            connection,
            (SqliteTransaction)transaction,
            cancellationToken);

            if (currentVersion > CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Database schema version {currentVersion} is newer " +
                    $"than the supported version {CurrentSchemaVersion}.");
            }

            if (currentVersion == 0)
            {
                await CreateSchemaV1Async(
                    connection,
                     (SqliteTransaction)transaction,
                    cancellationToken);

                await SetSchemaVersionAsync(
                    connection,
                    (SqliteTransaction)transaction,
                    CurrentSchemaVersion,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        private static async Task<int> GetSchemaVersionAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = "PRAGMA user_version;";

            var result = await command.ExecuteScalarAsync(
                cancellationToken);

            return Convert.ToInt32(result);
        }

        private static async Task SetSchemaVersionAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int version,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = $"PRAGMA user_version = {version};";

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task CreateSchemaV1Async(
            SqliteConnection connection,
            SqliteTransaction transaction,
            CancellationToken cancellationToken)
        {
            const string sql = """
            CREATE TABLE QuizAttempts
            (
                Id TEXT NOT NULL PRIMARY KEY,
                QuizId TEXT NOT NULL,
                ExamContentHash TEXT NOT NULL,
                StartedAtUtc TEXT NOT NULL,
                DeadlineUtc TEXT NOT NULL,
                SubmittedAtUtc TEXT NULL,
                Status INTEGER NOT NULL
                    CHECK (Status IN (0, 1, 2))
            );

            CREATE INDEX IX_QuizAttempts_QuizId
                ON QuizAttempts (QuizId);

            CREATE INDEX IX_QuizAttempts_Status
                ON QuizAttempts (Status);


            CREATE TABLE Answers
            (
                AttemptId TEXT NOT NULL,
                QuestionId TEXT NOT NULL,
                AnsweredAtUtc TEXT NOT NULL,

                PRIMARY KEY (AttemptId, QuestionId),

                FOREIGN KEY (AttemptId)
                    REFERENCES QuizAttempts (Id)
                    ON DELETE CASCADE
            );


            CREATE TABLE SelectedOptions
            (
                AttemptId TEXT NOT NULL,
                QuestionId TEXT NOT NULL,
                OptionId TEXT NOT NULL,

                PRIMARY KEY (
                    AttemptId,
                    QuestionId,
                    OptionId
                ),

                FOREIGN KEY (AttemptId, QuestionId)
                    REFERENCES Answers (AttemptId, QuestionId)
                    ON DELETE CASCADE
            );


            CREATE TABLE QuizResults
            (
                AttemptId TEXT NOT NULL PRIMARY KEY,
                TotalQuestions INTEGER NOT NULL,
                CorrectAnswers INTEGER NOT NULL,
                EarnedPoints REAL NOT NULL,
                MaximumPoints REAL NOT NULL,
                ScorePercentage REAL NOT NULL,

                FOREIGN KEY (AttemptId)
                    REFERENCES QuizAttempts (Id)
                    ON DELETE CASCADE
            );
            """;

            await using var command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = sql;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
