
using QuizApp.Domain.Models;

namespace QuizApp.Application.Models;

public enum AttemptResumeStatus
{
    NoExistingAttempt,
    Resumed,
    Expired
}

public class AttemptResumeResult
{
    public AttemptResumeStatus Status { get; init; }

    public QuizAttempt? Attempt { get; init; }

    public QuizResult? Result { get; init; }
}
