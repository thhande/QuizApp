
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;

namespace QuizApp.Desktop.ViewModels.Questions;

public partial class QuestionViewModel : ObservableObject
{
    public Guid Id { get; }

    public string Content { get; }

    public QuestionType Type { get; }

    public int Points { get; }

    public int DisplayNumber { get; }

    public ObservableCollection<QuestionOptionViewModel> Options
    { get; } = [];

    [ObservableProperty]
    private bool _isCurrent;

    public bool IsMultipleChoice =>
        Type == QuestionType.MultipleChoice;

    public bool IsAnswered =>
        Options.Any(option => option.IsSelected);

    public QuestionViewModel(
     Question question,
     int displayNumber,
     IEnumerable<Guid>? selectedOptionIds = null)
    {
        ArgumentNullException.ThrowIfNull(question);

        Id = question.Id;
        DisplayNumber = displayNumber;
        Content = question.Content;
        Type = question.Type;
        Points = question.Points;

        var selectedIds = selectedOptionIds?.ToHashSet()
            ?? [];

        foreach (var option in question.Options)
        {
            Options.Add(new QuestionOptionViewModel(
                option.Id,
                option.Content,
                selectedIds.Contains(option.Id)));
        }
    }

    public void SelectOption(Guid optionId, bool isSelected)
    {
        var option = Options.FirstOrDefault(
            item => item.Id == optionId);

        if (option is null)
        {
            throw new ArgumentException(
                "Option does not belong to this question.",
                nameof(optionId));
        }

        if (Type == QuestionType.SingleChoice && isSelected)
        {
            foreach (var item in Options)
            {
                item.IsSelected = item.Id == optionId;
            }
        }
        else
        {
            option.IsSelected = isSelected;
        }

        OnPropertyChanged(nameof(IsAnswered));
    }

    public IReadOnlyList<Guid> GetSelectedOptionIds()
    {
        return Options
            .Where(option => option.IsSelected)
            .Select(option => option.Id)
            .ToList();
    }
}
