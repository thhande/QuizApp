using QuizApp.Application.Models.QuizEditor;
using QuizApp.Application.Services.QuizEditor;
using QuizApp.Domain.Enums;

namespace QuizApp.Application.Tests.Services.QuizEditor;

public class QuizEditorDraftValidatorTests
{
    private readonly QuizEditorDraftValidator _validator = new();

    private static QuizEditorDraft CreateValidDraft()
    {
        var question = new QuestionDraft
        {
            Content = "What is OOP?",
            Type = QuestionType.SingleChoice,
            Points = 5,
            Options =
            [
                new OptionDraft { Content = "Object-oriented programming", IsCorrect = true },
                new OptionDraft { Content = "Operating system" }
            ]
        };

        return new QuizEditorDraft
        {
            Title = "Programming Fundamentals",
            DurationSeconds = 1800,
            Questions = [question]
        };
    }

    [Fact]
    public void Validate_ValidDraft_ReturnsNoErrors()
    {
        var draft = CreateValidDraft();

        var errors = _validator.Validate(draft);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptyTitle_ReturnsError()
    {
        var draft = CreateValidDraft();
        draft.Title = " ";

        var errors = _validator.Validate(draft);

        Assert.Contains(errors, e => e.Contains("title"));
    }

    [Fact]
    public void Validate_InvalidDuration_ReturnsError()
    {
        var draft = CreateValidDraft();
        draft.DurationSeconds = 0;

        var errors = _validator.Validate(draft);

        Assert.Contains(errors, e => e.Contains("duration"));
    }

    [Fact]
    public void Validate_NoQuestions_ReturnsError()
    {
        var draft = CreateValidDraft();
        draft.Questions.Clear();

        var errors = _validator.Validate(draft);

        Assert.Contains(errors, e => e.Contains("at least one question"));
    }

    [Fact]
    public void Validate_SingleChoiceWithMultipleCorrectOptions_ReturnsError()
    {
        var draft = CreateValidDraft();
        draft.Questions[0].Options[1].IsCorrect = true;

        var errors = _validator.Validate(draft);

        Assert.Contains(errors, e => e.Contains("exactly one correct option"));
    }

    [Fact]
    public void Validate_MultipleChoiceWithMultipleCorrectOptions_IsValid()
    {
        var draft = CreateValidDraft();
        var question = draft.Questions[0];

        question.Type = QuestionType.MultipleChoice;
        question.Options[1].IsCorrect = true;

        var errors = _validator.Validate(draft);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_QuestionWithNoCorrectOption_ReturnsError()
    {
        var draft = CreateValidDraft();
        draft.Questions[0].Options[0].IsCorrect = false;

        var errors = _validator.Validate(draft);

        Assert.Contains(errors, e => e.Contains("at least one correct option"));
    }

    [Fact]
    public void Validate_DuplicateQuestionIds_ReturnsError()
    {
        var draft = CreateValidDraft();
        var firstQuestion = draft.Questions[0];

        var secondQuestion = new QuestionDraft
        {
            Id = firstQuestion.Id,
            Content = "Another question",
            Options =
            [
                new OptionDraft { Content = "A", IsCorrect = true },
                new OptionDraft { Content = "B" }
            ]
        };

        draft.Questions.Add(secondQuestion);

        var errors = _validator.Validate(draft);

        Assert.Contains(errors, e => e.Contains("duplicate question ID"));
    }
}