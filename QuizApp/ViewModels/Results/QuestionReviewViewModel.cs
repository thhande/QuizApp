
using QuizApp.Application.Models;

namespace QuizApp.Desktop.ViewModels.Results;

public class QuestionReviewViewModel
{
    public int DisplayNumber { get; }

    public string Content { get; }

    public int Points { get; }

    public double EarnedPoints { get; }

    public bool IsCorrect { get; }

    public string StatusText => IsCorrect ? "Correct" : "Incorrect";

    public IReadOnlyList<ReviewOptionViewModel> Options { get; }

    public QuestionReviewViewModel(QuestionReviewItem item, int displayNumber)
    {
        ArgumentNullException.ThrowIfNull(item);

        DisplayNumber = displayNumber;
        Content = item.Question.Content;
        Points = item.Question.Points;
        EarnedPoints = item.EarnedPoints;
        IsCorrect = item.IsCorrect;

        var selectedIds = item.SelectedOptionIds.ToHashSet();
        var correctIds = item.CorrectOptionIds.ToHashSet();

        Options = item.Question.Options
            .Select(option => new ReviewOptionViewModel
            {
                Content = option.Content,
                IsSelected = selectedIds.Contains(option.Id),
                IsCorrect = correctIds.Contains(option.Id)
            })
            .ToList();
    }
}

public class ReviewOptionViewModel
{
    public string Content { get; init; } = string.Empty;

    public bool IsSelected { get; init; }

    public bool IsCorrect { get; init; }
}
