using CommunityToolkit.Mvvm.ComponentModel;
using QuizApp.Application.Models.QuizEditor;

namespace QuizApp.Desktop.ViewModels.Editor;

public partial class EditorOptionViewModel : ObservableObject
{
    private readonly OptionDraft _draft;

    public Guid Id => _draft.Id;

    [ObservableProperty]
    private string content;

    [ObservableProperty]
    private bool isCorrect;

    public EditorOptionViewModel(OptionDraft draft)
    {
        _draft = draft;

        content = draft.Content;
        isCorrect = draft.IsCorrect;
    }

    partial void OnContentChanged(string value)
    {
        _draft.Content = value;
    }

    partial void OnIsCorrectChanged(bool value)
    {
        _draft.IsCorrect = value;
    }

    public OptionDraft ToDraft()
    {
        return new OptionDraft
        {
            Id = Id,
            Content = Content,
            IsCorrect = IsCorrect
        };
    }
}