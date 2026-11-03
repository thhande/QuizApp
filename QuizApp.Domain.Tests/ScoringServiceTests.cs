using QuizApp.Domain.Models;
using QuizApp.Domain.Services;

namespace QuizApp.Domain.Tests;

public class ScoringServiceTests
{
    private readonly ScoringService _scoringService = new();

    [Fact]
    public void Calculate_AllCorrectAnswers_ShouldReturnFullScore()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var optionA = Guid.NewGuid();
        var optionB = Guid.NewGuid();

        var quiz = new QuizDefinition
        {
            Id = Guid.NewGuid(),
            Title = "Test Quiz",
            DurationSeconds = 600,
            Questions = new List<Question>
            {
                new()
                {
                    Id = questionId,
                    Content = "Select the correct options",
                    Points = 10,
                    CorrectOptionIds = new List<Guid>
                    {
                        optionA, optionB
                    }
                }
            }
        };

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            Answers = new List<Answer>
            {
                new()
                {
                    QuestionId = questionId,
                    SelectedOptionIds = new List<Guid>
                    {
                        optionB, optionA
                    }
                }
            }
        };

        // Act
        var result = _scoringService.Calculate(quiz, attempt);

        // Assert
        Assert.Equal(1, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.Equal(10, result.EarnedPoints);
        Assert.Equal(10, result.MaximumPoints);
        Assert.Equal(100, result.ScorePercentage);
    }

    [Fact]
    public void Calculate_PartialSelection_ShouldReturnZeroPoints()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var optionA = Guid.NewGuid();
        var optionB = Guid.NewGuid();

        var quiz = new QuizDefinition
        {
            Id = Guid.NewGuid(),
            Questions = new List<Question>
            {
                new()
                {
                    Id = questionId,
                    Points = 10,
                    CorrectOptionIds = new List<Guid>
                    {
                        optionA, optionB
                    }
                }
            }
        };

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            Answers = new List<Answer>
            {
                new()
                {
                    QuestionId = questionId,
                    SelectedOptionIds = new List<Guid>
                    {
                        optionA
                    }
                }
            }
        };

        // Act
        var result = _scoringService.Calculate(quiz, attempt);

        // Assert
        Assert.Equal(0, result.CorrectAnswers);
        Assert.Equal(0, result.EarnedPoints);
        Assert.Equal(0, result.ScorePercentage);
    }

    [Fact]
    public void Calculate_UnansweredQuestion_ShouldReturnZeroPoints()
    {
        // Arrange
        var quiz = new QuizDefinition
        {
            Id = Guid.NewGuid(),
            Questions = new List<Question>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Points = 5
                }
            }
        };

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            Answers = new List<Answer>()
        };

        // Act
        var result = _scoringService.Calculate(quiz, attempt);

        // Assert
        Assert.Equal(0, result.CorrectAnswers);
        Assert.Equal(0, result.EarnedPoints);
        Assert.Equal(5, result.MaximumPoints);
        Assert.Equal(0, result.ScorePercentage);
    }
}