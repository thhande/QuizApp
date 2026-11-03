using QuizApp.Application.Models.QuizEditor;
using QuizApp.Domain.Enums;

namespace QuizApp.Application.Services.QuizEditor;

public sealed class QuizEditorDraftValidator
{
    public IReadOnlyList<string> Validate(QuizEditorDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = new List<string>();

        if (draft.Id == Guid.Empty)
            errors.Add("Quiz ID is invalid.");

        if (string.IsNullOrWhiteSpace(draft.Title))
            errors.Add("Quiz title is required.");

        if (draft.DurationSeconds <= 0)
            errors.Add("Quiz duration must be greater than zero.");

        if (draft.Questions.Count == 0)
            errors.Add("A quiz must contain at least one question.");

        var questionIds = new HashSet<Guid>();

        for (var i = 0; i < draft.Questions.Count; i++)
        {
            var question = draft.Questions[i];
            var questionNumber = i + 1;

            if (question.Id == Guid.Empty)
                errors.Add($"Question {questionNumber}: ID is invalid.");

            if (!questionIds.Add(question.Id))
                errors.Add($"Question {questionNumber}: duplicate question ID.");

            ValidateQuestion(question, questionNumber, errors);
        }

        return errors;
    }

    private static void ValidateQuestion(
        QuestionDraft question,
        int questionNumber,
        List<string> errors)
    {
        var prefix = $"Question {questionNumber}";

        if (string.IsNullOrWhiteSpace(question.Content))
            errors.Add($"{prefix}: content is required.");

        if (question.Points < 0)
            errors.Add($"{prefix}: points cannot be negative.");

        if (question.Type is not QuestionType.SingleChoice
            and not QuestionType.MultipleChoice)
        {
            errors.Add($"{prefix}: unsupported question type.");
        }

        if (question.Options.Count < 2)
            errors.Add($"{prefix}: at least two options are required.");

        var optionIds = new HashSet<Guid>();

        foreach (var option in question.Options)
        {
            if (option.Id == Guid.Empty)
                errors.Add($"{prefix}: an option has an invalid ID.");

            if (!optionIds.Add(option.Id))
                errors.Add($"{prefix}: duplicate option ID.");

            if (string.IsNullOrWhiteSpace(option.Content))
                errors.Add($"{prefix}: option content is required.");
        }

        var correctCount = question.Options.Count(option => option.IsCorrect);

        if (correctCount == 0)
            errors.Add($"{prefix}: at least one correct option is required.");

        if (question.Type == QuestionType.SingleChoice && correctCount > 1)
            errors.Add($"{prefix}: single-choice questions must have exactly one correct option.");
    }
}