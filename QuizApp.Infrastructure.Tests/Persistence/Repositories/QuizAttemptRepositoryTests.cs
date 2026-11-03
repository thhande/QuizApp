using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;
using QuizApp.Infrastructure.Persistance.Sqlite;
using QuizApp.Infrastructure.Persistence.Repositories;

namespace QuizApp.Infrastructure.Tests.Persistence.Repositories;

public class QuizAttemptRepositoryTests
{
    private static string CreateTempDatabasePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString(),
            "test.db");
    }

    private static QuizAttempt CreateAttempt()
    {
        var now = DateTime.UtcNow;

        return new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = Guid.NewGuid(),
            ExamContentHash = "test-content-hash",
            StartedAtUtc = now,
            DeadlineUtc = now.AddMinutes(30),
            SubmittedAtUtc = null,
            Status = AttemptStatus.InProgress,
            Answers = []
        };
    }

    private static async Task<(
        SqliteConnectionFactory Factory,
        QuizAttemptRepository Repository)> CreateRepositoryAsync(
        string databasePath)
    {
        var factory = new SqliteConnectionFactory(databasePath);

        var initializer = new DatabaseInitializer(factory);

        await initializer.InitializeAsync();

        var repository = new QuizAttemptRepository(factory);

        return (factory, repository);
    }

    private static void DeleteDatabase(string databasePath)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        var directory = Path.GetDirectoryName(databasePath);

        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsCorrectAttempt()
    {
        // Arrange
        var databasePath = CreateTempDatabasePath();

        try
        {
            var (_, repository) =
                await CreateRepositoryAsync(databasePath);

            var attempt = CreateAttempt();

            // Act
            await repository.AddAsync(attempt);

            var result = await repository.GetByIdAsync(attempt.Id);

            // Assert
            Assert.NotNull(result);

            Assert.Equal(attempt.Id, result.Id);
            Assert.Equal(attempt.QuizId, result.QuizId);
            Assert.Equal(
                attempt.ExamContentHash,
                result.ExamContentHash);

            Assert.Equal(
                attempt.StartedAtUtc,
                result.StartedAtUtc);

            Assert.Equal(
                attempt.DeadlineUtc,
                result.DeadlineUtc);

            Assert.Equal(
                attempt.Status,
                result.Status);

            Assert.Empty(result.Answers);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task SaveAnswerAsync_ThenGetById_RestoresAnswer()
    {
        // Arrange
        var databasePath = CreateTempDatabasePath();

        try
        {
            var (_, repository) =
                await CreateRepositoryAsync(databasePath);

            var attempt = CreateAttempt();

            await repository.AddAsync(attempt);

            var questionId = Guid.NewGuid();
            var option1Id = Guid.NewGuid();
            var option2Id = Guid.NewGuid();

            var answer = new Answer
            {
                QuestionId = questionId,
                AnsweredAtUtc = DateTime.UtcNow,
                SelectedOptionIds = [option1Id, option2Id]
            };

            // Act
            await repository.SaveAnswerAsync(
                attempt.Id,
                answer);

            var result = await repository.GetByIdAsync(attempt.Id);

            // Assert
            Assert.NotNull(result);

            var restoredAnswer = Assert.Single(result.Answers);

            Assert.Equal(questionId, restoredAnswer.QuestionId);

            Assert.Equal(
                answer.AnsweredAtUtc,
                restoredAnswer.AnsweredAtUtc);

            Assert.Equal(
                2,
                restoredAnswer.SelectedOptionIds.Count);

            Assert.Contains(
                option1Id,
                restoredAnswer.SelectedOptionIds);

            Assert.Contains(
                option2Id,
                restoredAnswer.SelectedOptionIds);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task SaveAnswerAsync_UpdatesExistingAnswer()
    {
        // Arrange
        var databasePath = CreateTempDatabasePath();

        try
        {
            var (_, repository) =
                await CreateRepositoryAsync(databasePath);

            var attempt = CreateAttempt();

            await repository.AddAsync(attempt);

            var questionId = Guid.NewGuid();

            var oldOptionId = Guid.NewGuid();
            var newOptionId = Guid.NewGuid();

            var firstAnswer = new Answer
            {
                QuestionId = questionId,
                AnsweredAtUtc = DateTime.UtcNow.AddMinutes(-1),
                SelectedOptionIds = [oldOptionId]
            };

            var secondAnswer = new Answer
            {
                QuestionId = questionId,
                AnsweredAtUtc = DateTime.UtcNow,
                SelectedOptionIds = [newOptionId]
            };

            // Act
            await repository.SaveAnswerAsync(
                attempt.Id,
                firstAnswer);

            await repository.SaveAnswerAsync(
                attempt.Id,
                secondAnswer);

            var result = await repository.GetByIdAsync(attempt.Id);

            // Assert
            Assert.NotNull(result);

            var restoredAnswer = Assert.Single(result.Answers);

            Assert.Equal(
                secondAnswer.AnsweredAtUtc,
                restoredAnswer.AnsweredAtUtc);

            Assert.Single(restoredAnswer.SelectedOptionIds);

            Assert.Contains(
                newOptionId,
                restoredAnswer.SelectedOptionIds);

            Assert.DoesNotContain(
                oldOptionId,
                restoredAnswer.SelectedOptionIds);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentAttempt_ReturnsNull()
    {
        // Arrange
        var databasePath = CreateTempDatabasePath();

        try
        {
            var (_, repository) =
                await CreateRepositoryAsync(databasePath);

            // Act
            var result = await repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }
}