
using System.Collections.ObjectModel;
using QuizApp.Application.Models;
using QuizApp.Domain.Enums;

namespace QuizApp.Desktop.ViewModels.Results;

public class QuizResultViewModel
{
    public QuizAttemptReview Review { get; }

    public ObservableCollection<QuestionReviewViewModel> Questions { get; }
        = [];

    public string QuizTitle { get; }

    public double ScorePercentage => Review.Result.ScorePercentage;

    public string ScoreDisplay => $"{ScorePercentage:F1}%";

    public string CorrectAnswersDisplay =>
        $"{Review.Result.CorrectAnswers} / {Review.Result.TotalQuestions}";

    public string PointsDisplay =>
        $"{Review.Result.EarnedPoints:0.##} / {Review.Result.MaximumPoints:0.##}";

    public string CompletionStatus =>
        Review.Status == AttemptStatus.Expired
            ? "Time expired"
            : "Submitted";

    public string StartedAtDisplay =>
        Review.Attempt.StartedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");

    public string CompletedAtDisplay =>
        Review.Attempt.SubmittedAtUtc?.ToLocalTime()
            .ToString("dd/MM/yyyy HH:mm:ss") ?? "Unknown";

    public QuizResultViewModel(
        QuizAttemptReview review,
        string quizTitle)
    {
        Review = review ?? throw new ArgumentNullException(nameof(review));
        QuizTitle = quizTitle;

        for (int i = 0; i < review.Questions.Count; i++)
        {
            Questions.Add(new QuestionReviewViewModel(
                review.Questions[i],
                i + 1));
        }
    }
}
