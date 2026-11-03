using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizApp.Application.Models.QuizEditor;
using QuizApp.Domain.Enums;

namespace QuizApp.Desktop.ViewModels.Editor;

public partial class EditorQuestionViewModel : ObservableObject
{
    private readonly QuestionDraft _draft;

    public Guid Id => _draft.Id;

    public ObservableCollection<EditorOptionViewModel> Options { get; }

    [ObservableProperty]
    private string content;

    [ObservableProperty]
    private QuestionType type;

    [ObservableProperty]
    private int points;

    public EditorQuestionViewModel(QuestionDraft draft)
    {
        _draft = draft;

        content = draft.Content;
        type = draft.Type;
        points = draft.Points;

        Options = new ObservableCollection<EditorOptionViewModel>(
            draft.Options.Select(option =>
                new EditorOptionViewModel(option)));
    }

    partial void OnContentChanged(string value)
    {
        _draft.Content = value;
    }

    partial void OnTypeChanged(QuestionType value)
    {
        _draft.Type = value;

        // A single-choice question cannot have multiple correct options.
        if (value == QuestionType.SingleChoice)
        {
            var firstCorrect = Options.FirstOrDefault(option => option.IsCorrect);

            foreach (var option in Options)
            {
                if (option != firstCorrect)
                    option.IsCorrect = false;
            }
        }
    }

    partial void OnPointsChanged(int value)
    {
        _draft.Points = value;
    }

    [RelayCommand]
    private void AddOption()
    {
        var option = new OptionDraft
        {
            Content = string.Empty
        };

        _draft.Options.Add(option);
        Options.Add(new EditorOptionViewModel(option));
    }

    [RelayCommand]
    private void RemoveOption(EditorOptionViewModel? option)
    {
        if (option is null)
            return;

        Options.Remove(option);

        var draftOption = _draft.Options
            .FirstOrDefault(item => item.Id == option.Id);

        if (draftOption is not null)
            _draft.Options.Remove(draftOption);
    }

    public QuestionDraft ToDraft()
    {
        return new QuestionDraft
        {
            Id = Id,
            Content = Content,
            Type = Type,
            Points = Points,
            ImagePath = _draft.ImagePath,
            Options = Options
                .Select(option => option.ToDraft())
                .ToList()
        };
    }
}