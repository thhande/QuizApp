using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;
using QuizApp.Infrastructure.FileFormats.QuizPack;

namespace QuizApp.Infrastructure.Tests.FileFormats.QuizPack;

public class QuizPackWriterTests
{
    [Fact]
    public async Task WriteAsync_ThenReadAsync_PreservesQuizData()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.quizpack");

        try
        {
            var quiz = CreateQuiz();

            var writer = new QuizPackWriter();
            var reader = new QuizPackReader();

            await writer.WriteAsync(quiz, filePath);

            Assert.True(File.Exists(filePath));

            var loadedQuiz = await reader.ReadAsync(filePath);

            Assert.Equal(quiz.Id, loadedQuiz.Id);
            Assert.Equal(quiz.Title, loadedQuiz.Title);
            Assert.Equal(quiz.DurationSeconds, loadedQuiz.DurationSeconds);

            var originalQuestion = Assert.Single(quiz.Questions);
            var loadedQuestion = Assert.Single(loadedQuiz.Questions);

            Assert.Equal(originalQuestion.Id, loadedQuestion.Id);
            Assert.Equal(originalQuestion.Content, loadedQuestion.Content);
            Assert.Equal(originalQuestion.Type, loadedQuestion.Type);
            Assert.Equal(originalQuestion.Points, loadedQuestion.Points);

            Assert.Equal(
                originalQuestion.CorrectOptionIds,
                loadedQuestion.CorrectOptionIds);

            Assert.Equal(
                originalQuestion.Options.Select(x => x.Id),
                loadedQuestion.Options.Select(x => x.Id));

            Assert.Equal(
                originalQuestion.Options.Select(x => x.Content),
                loadedQuestion.Options.Select(x => x.Content));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public async Task WriteAsync_WithImageReference_ThrowsNotSupportedException()
    {
        var quiz = CreateQuiz();

        var question = quiz.Questions[0];

        quiz = new QuizDefinition
        {
            Id = quiz.Id,
            Title = quiz.Title,
            DurationSeconds = quiz.DurationSeconds,
            Questions =
            [
                new Question
                {
                    Id = question.Id,
                    Content = question.Content,
                    Type = question.Type,
                    Points = question.Points,
                    ImagePath = "assets/images/question.png",
                    Options = question.Options,
                    CorrectOptionIds = question.CorrectOptionIds
                }
            ]
        };

        var writer = new QuizPackWriter();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => writer.WriteAsync(
                quiz,
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.quizpack")));
    }

    private static QuizDefinition CreateQuiz()
    {
        var correctOptionId = Guid.NewGuid();

        return new QuizDefinition
        {
            Id = Guid.NewGuid(),
            Title = "Programming Fundamentals",
            DurationSeconds = 1800,
            Questions =
            [
                new Question
                {
                    Id = Guid.NewGuid(),
                    Content = "What is OOP?",
                    Type = QuestionType.SingleChoice,
                    Points = 5,
                    Options =
                    [
                        new QuestionOption
                        {
                            Id = correctOptionId,
                            Content = "Object-oriented programming"
                        },
                        new QuestionOption
                        {
                            Id = Guid.NewGuid(),
                            Content = "Operating system"
                        }
                    ],
                    CorrectOptionIds = [correctOptionId]
                }
            ]
        };
    }
}