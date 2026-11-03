
using QuizApp.Domain.Models;

namespace QuizApp.Application.Models;

public class QuestionReviewItem
{
    public Question Question { get; init; } = null!;

    public IReadOnlyList<Guid> SelectedOptionIds { get; init; }
        = [];

    public IReadOnlyList<Guid> CorrectOptionIds { get; init; }
        = [];

    public bool IsCorrect { get; init; }

    public double EarnedPoints { get; init; }
}
