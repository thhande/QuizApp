using QuizApp.Application.Models.QuizEditor;
using QuizApp.Application.Services.QuizEditor;
using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;

namespace QuizApp.Application.Tests.Services.QuizEditor;

public class QuizEditorDraftMapperTests
{
    private readonly QuizEditorDraftMapper _mapper =
        new(new QuizEditorDraftValidator());

    [Fact]
    public void ToDraft_ValidQuiz_PreservesAllData()
    {
        var quizId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var correctOptionId = Guid.NewGuid();
        var incorrectOptionId = Guid.NewGuid();

        var quiz = new QuizDefinition
        {
            Id = quizId,
            Title = "Programming",
            DurationSeconds = 1800,
            Questions =
            [
                new Question
                {
                    Id = questionId,
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
                            Id = incorrectOptionId,
                            Content = "Operating system"
                        }
                    ],
                    CorrectOptionIds = [correctOptionId]
                }
            ]
        };

        var draft = _mapper.ToDraft(quiz);

        Assert.Equal(quizId, draft.Id);
        Assert.Equal("Programming", draft.Title);
        Assert.Equal(1800, draft.DurationSeconds);

        var question = Assert.Single(draft.Questions);

        Assert.Equal(questionId, question.Id);
        Assert.Equal(5, question.Points);
        Assert.Equal("What is OOP?", question.Content);

        Assert.True(question.Options[0].IsCorrect);
        Assert.False(question.Options[1].IsCorrect);
    }

    [Fact]
    public void ToDomain_ValidDraft_PreservesAllData()
    {
        var draft = new QuizEditorDraft
        {
            Title = "Programming",
            DurationSeconds = 1800,
            Questions =
            [
                new QuestionDraft
                {
                    Content = "What is OOP?",
                    Type = QuestionType.SingleChoice,
                    Points = 5,
                    Options =
                    [
                        new OptionDraft
                        {
                            Content = "Object-oriented programming",
                            IsCorrect = true
                        },
                        new OptionDraft
                        {
                            Content = "Operating system"
                        }
                    ]
                }
            ]
        };

        var quiz = _mapper.ToDomain(draft);

        Assert.Equal(draft.Id, quiz.Id);
        Assert.Equal(draft.Title, quiz.Title);
        Assert.Equal(1800, quiz.DurationSeconds);

        var question = Assert.Single(quiz.Questions);

        Assert.Equal(draft.Questions[0].Id, question.Id);
        Assert.Equal(5, question.Points);
        Assert.Single(question.CorrectOptionIds);

        Assert.Equal(
            draft.Questions[0].Options[0].Id,
            question.CorrectOptionIds[0]);
    }

    [Fact]
    public void ToDomain_InvalidDraft_ThrowsException()
    {
        var draft = new QuizEditorDraft
        {
            Title = "",
            DurationSeconds = 0
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => _mapper.ToDomain(draft));

        Assert.Contains("title is required", exception.Message);
        Assert.Contains("duration", exception.Message);
    }

    [Fact]
    public void RoundTrip_PreservesCorrectOptionRelationships()
    {
        var quiz = new QuizDefinition
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            DurationSeconds = 600,
            Questions =
            [
                new Question
                {
                    Id = Guid.NewGuid(),
                    Content = "Select the correct answers",
                    Type = QuestionType.MultipleChoice,
                    Points = 10,
                    Options =
                    [
                        new QuestionOption
                        {
                            Id = Guid.NewGuid(),
                            Content = "A"
                        },
                        new QuestionOption
                        {
                            Id = Guid.NewGuid(),
                            Content = "B"
                        },
                        new QuestionOption
                        {
                            Id = Guid.NewGuid(),
                            Content = "C"
                        }
                    ],
                    CorrectOptionIds = []
                }
            ]
        };

        var originalQuestion = quiz.Questions[0];

        var draft = _mapper.ToDraft(quiz);

        draft.Questions[0].Options[0].IsCorrect = true;
        draft.Questions[0].Options[2].IsCorrect = true;

        var convertedQuiz = _mapper.ToDomain(draft);
        var convertedQuestion = convertedQuiz.Questions[0];

        Assert.Equal(
            new[]
            {
                originalQuestion.Options[0].Id,
                originalQuestion.Options[2].Id
            },
            convertedQuestion.CorrectOptionIds);
    }
}