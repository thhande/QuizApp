namespace QuizApp.Application.Models.QuizEditor;

public sealed class QuizEditorDraft
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public int DurationSeconds { get; set; } = 1800;

    public List<QuestionDraft> Questions { get; set; } = [];
}