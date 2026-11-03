
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuizApp.Desktop.ViewModels.Questions;

public partial class QuestionOptionViewModel : ObservableObject
{
    public Guid Id { get; }

    public string Content { get; }

    [ObservableProperty]
    private bool _isSelected;

    public QuestionOptionViewModel(
        Guid id,
        string content,
        bool isSelected = false)
    {
        Id = id;
        Content = content;
        IsSelected = isSelected;
    }
}
