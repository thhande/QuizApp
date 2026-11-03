namespace QuizApp.Application.Models.QuizEditor;

public sealed class OptionDraft
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Content { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}