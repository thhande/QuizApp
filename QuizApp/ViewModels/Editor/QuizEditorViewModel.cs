using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizApp.Application.Models.QuizEditor;

namespace QuizApp.Desktop.ViewModels.Editor;

public partial class QuizEditorViewModel : ObservableObject
{
    private readonly QuizEditorDraft _draft;

    public Guid Id => _draft.Id;

    public ObservableCollection<EditorQuestionViewModel> Questions { get; }

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private int durationSeconds;

    [ObservableProperty]
    private EditorQuestionViewModel? selectedQuestion;

    public QuizEditorViewModel(QuizEditorDraft draft)
    {
        _draft = draft;

        title = draft.Title;
        durationSeconds = draft.DurationSeconds;

        Questions = new ObservableCollection<EditorQuestionViewModel>(
            draft.Questions.Select(question =>
                new EditorQuestionViewModel(question)));
    }

    partial void OnTitleChanged(string value)
    {
        _draft.Title = value;
    }

    partial void OnDurationSecondsChanged(int value)
    {
        _draft.DurationSeconds = value;
    }

    [RelayCommand]
    private void AddQuestion()
    {
        var question = new QuestionDraft
        {
            Content = string.Empty,
            Options =
            [
                new OptionDraft { Content = "Option 1" },
                new OptionDraft { Content = "Option 2" }
            ]
        };

        _draft.Questions.Add(question);

        var viewModel = new EditorQuestionViewModel(question);
        Questions.Add(viewModel);

        SelectedQuestion = viewModel;
    }

    [RelayCommand]
    private void RemoveQuestion(EditorQuestionViewModel? question)
    {
        if (question is null)
            return;

        Questions.Remove(question);

        var draftQuestion = _draft.Questions
            .FirstOrDefault(item => item.Id == question.Id);

        if (draftQuestion is not null)
            _draft.Questions.Remove(draftQuestion);

        if (SelectedQuestion == question)
            SelectedQuestion = Questions.FirstOrDefault();
    }

    public QuizEditorDraft ToDraft()
    {
        return new QuizEditorDraft
        {
            Id = Id,
            Title = Title,
            DurationSeconds = DurationSeconds,
            Questions = Questions
                .Select(question => question.ToDraft())
                .ToList()
        };
    }
}