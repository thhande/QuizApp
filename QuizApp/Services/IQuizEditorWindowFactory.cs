using QuizApp.Application.Models.QuizEditor;

namespace QuizApp.Desktop.Services;

public interface IQuizEditorWindowFactory
{
    void Show(QuizEditorDraft draft);
}