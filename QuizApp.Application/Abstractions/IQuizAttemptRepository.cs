
using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;

namespace QuizApp.Application.Abstractions;

public interface IQuizAttemptRepository
{
    Task AddAsync(
        QuizAttempt attempt,
        CancellationToken cancellationToken = default);

    Task<QuizAttempt?> GetByIdAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default);

    Task<QuizAttempt?> GetInProgressAttemptAsync(
        Guid quizId,
        string examContentHash,
        CancellationToken cancellationToken = default);

    Task SaveAnswerAsync(
        Guid attemptId,
        Answer answer,
        CancellationToken cancellationToken = default);

    Task CompleteAttemptAsync(
        Guid attemptId,
        AttemptStatus status,
        DateTime submittedAtUtc,
        QuizResult result,
        CancellationToken cancellationToken = default);
    Task<QuizResult?> GetResultByAttemptIdAsync(
    Guid attemptId,
    CancellationToken cancellationToken = default);
}
