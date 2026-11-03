using QuizApp.Application.Models.QuizEditor;
using QuizApp.Domain.Models;

namespace QuizApp.Application.Services.QuizEditor;

public sealed class QuizEditorDraftMapper
{
    private readonly QuizEditorDraftValidator _validator;

    public QuizEditorDraftMapper(QuizEditorDraftValidator validator)
    {
        _validator = validator
            ?? throw new ArgumentNullException(nameof(validator));
    }

    public QuizEditorDraft ToDraft(QuizDefinition quiz)
    {
        ArgumentNullException.ThrowIfNull(quiz);

        return new QuizEditorDraft
        {
            Id = quiz.Id,
            Title = quiz.Title,
            DurationSeconds = quiz.DurationSeconds,

            Questions = quiz.Questions
                .Select(question =>
                {
                    var correctOptionIds = question.CorrectOptionIds.ToHashSet();

                    return new QuestionDraft
                    {
                        Id = question.Id,
                        Content = question.Content,
                        Type = question.Type,
                        Points = question.Points,
                        ImagePath = question.ImagePath,

                        Options = question.Options
                            .Select(option => new OptionDraft
                            {
                                Id = option.Id,
                                Content = option.Content,
                                IsCorrect = correctOptionIds.Contains(option.Id)
                            })
                            .ToList()
                    };
                })
                .ToList()
        };
    }

    public QuizDefinition ToDomain(QuizEditorDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = _validator.Validate(draft);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Cannot convert an invalid quiz draft:\n" +
                string.Join("\n", errors));
        }

        return new QuizDefinition
        {
            Id = draft.Id,
            Title = draft.Title.Trim(),
            DurationSeconds = draft.DurationSeconds,

            Questions = draft.Questions
                .Select(question => new Question
                {
                    Id = question.Id,
                    Content = question.Content.Trim(),
                    Type = question.Type,
                    Points = question.Points,
                    ImagePath = question.ImagePath,

                    Options = question.Options
                        .Select(option => new QuestionOption
                        {
                            Id = option.Id,
                            Content = option.Content.Trim()
                        })
                        .ToList(),

                    CorrectOptionIds = question.Options
                        .Where(option => option.IsCorrect)
                        .Select(option => option.Id)
                        .ToList()
                })
                .ToList()
        };
    }
}