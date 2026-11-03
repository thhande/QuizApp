using QuizApp.Domain.Enums;

namespace QuizApp.Application.Models.QuizEditor;

public sealed class QuestionDraft
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Content { get; set; } = string.Empty;

    public QuestionType Type { get; set; } = QuestionType.SingleChoice;

    public int Points { get; set; } = 1;

    public string? ImagePath { get; set; }

    public List<OptionDraft> Options { get; set; } = [];
}