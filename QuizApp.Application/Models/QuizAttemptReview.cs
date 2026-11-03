
using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;

namespace QuizApp.Application.Models;

public class QuizAttemptReview
{
    public QuizAttempt Attempt { get; init; } = null!;

    public QuizResult Result { get; init; } = null!;

    public IReadOnlyList<QuestionReviewItem> Questions { get; init; }
        = [];

    public AttemptStatus Status => Attempt.Status;
}
