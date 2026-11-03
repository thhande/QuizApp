using QuizApp.Domain.Enums;
using QuizApp.Infrastructure.FileFormats.QuizPack;
using QuizApp.Infrastructure.Tests.Helpers;

namespace QuizApp.Infrastructure.Tests;

public class QuizPackReaderTests
{
    private readonly QuizPackReader _reader = new();

    private static string CreateTempFilePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.quizpack");
    }

    [Fact]
    public async Task ReadAsync_ValidPackage_ReturnsQuizDefinition()
    {
        // Arrange
        var filePath = CreateTempFilePath();

        try
        {
            QuizPackTestFactory.CreatePackage(
                filePath,
                imagePath: "assets/images/question.png");

            // Act
            var quiz = await _reader.ReadAsync(filePath);

            // Assert
            Assert.Equal(QuizPackTestFactory.ExamId, quiz.Id);
            Assert.Equal("Test Quiz", quiz.Title);
            Assert.Equal(1800, quiz.DurationSeconds);

            var question = Assert.Single(quiz.Questions);

            Assert.Equal(
                QuizPackTestFactory.QuestionId,
                question.Id);

            Assert.Equal(
                QuestionType.SingleChoice,
                question.Type);

            Assert.Equal(2, question.Points);
            Assert.Equal(2, question.Options.Count);

            Assert.Equal(
                "assets/images/question.png",
                question.ImagePath);

            Assert.Contains(
                QuizPackTestFactory.Option1Id,
                question.CorrectOptionIds);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadAsync_MismatchedExamId_ThrowsException()
    {
        // Arrange
        var filePath = CreateTempFilePath();

        try
        {
            QuizPackTestFactory.CreatePackage(
                filePath,
                documentId: Guid.NewGuid());

            // Act
            var exception = await Assert.ThrowsAsync<QuizPackReadException>(
                () => _reader.ReadAsync(filePath));

            // Assert
            Assert.Contains("does not match", exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadAsync_UnsupportedSchemaVersion_ThrowsException()
    {
        // Arrange
        var filePath = CreateTempFilePath();

        try
        {
            QuizPackTestFactory.CreatePackage(
                filePath,
                schemaVersion: 999);

            // Act
            var exception = await Assert.ThrowsAsync<QuizPackReadException>(
                () => _reader.ReadAsync(filePath));

            // Assert
            Assert.Contains("schema version", exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadAsync_MissingImageEntry_ThrowsException()
    {
        // Arrange
        var filePath = CreateTempFilePath();

        try
        {
            QuizPackTestFactory.CreatePackage(
                filePath,
                imagePath: "assets/images/question.png",
                includeImage: false);

            // Act
            var exception = await Assert.ThrowsAsync<QuizPackReadException>(
                () => _reader.ReadAsync(filePath));

            // Assert
            Assert.Contains("was not found", exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadAsync_PathTraversalImage_ThrowsException()
    {
        // Arrange
        var filePath = CreateTempFilePath();

        try
        {
            QuizPackTestFactory.CreatePackage(
                filePath,
                imagePath: "../../outside.png");

            // Act
            var exception = await Assert.ThrowsAsync<QuizPackReadException>(
                () => _reader.ReadAsync(filePath));

            // Assert
            Assert.Contains("Invalid archive path", exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = CreateTempFilePath();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _reader.ReadAsync(filePath));
    }
}