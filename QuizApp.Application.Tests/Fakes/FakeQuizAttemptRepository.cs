using QuizApp.Application.Abstractions;
using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;

namespace QuizApp.Application.Tests.Fakes;

public class FakeQuizAttemptRepository : IQuizAttemptRepository
{
    private readonly Dictionary<Guid, QuizAttempt> _attempts = [];
    public Dictionary<Guid, QuizResult> Results { get; } = [];

    public Task AddAsync(
        QuizAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        _attempts.Add(attempt.Id, attempt);

        return Task.CompletedTask;
    }

    public Task<QuizAttempt?> GetByIdAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        _attempts.TryGetValue(attemptId, out var attempt);

        return Task.FromResult(attempt);
    }

    public Task SaveAnswerAsync(
        Guid attemptId,
        Answer answer,
        CancellationToken cancellationToken = default)
    {
        if (!_attempts.TryGetValue(attemptId, out var attempt))
        {
            throw new InvalidOperationException(
                "Attempt does not exist.");
        }

        var answers = attempt.Answers
            .Where(a => a.QuestionId != answer.QuestionId)
            .Append(answer)
            .ToList();

        _attempts[attemptId] = new QuizAttempt
        {
            Id = attempt.Id,
            QuizId = attempt.QuizId,
            ExamContentHash = attempt.ExamContentHash,
            StartedAtUtc = attempt.StartedAtUtc,
            DeadlineUtc = attempt.DeadlineUtc,
            SubmittedAtUtc = attempt.SubmittedAtUtc,
            Status = attempt.Status,
            Answers = answers
        };

        return Task.CompletedTask;
    }

    public Task CompleteAttemptAsync(
    Guid attemptId,
    AttemptStatus status,
    DateTime submittedAtUtc,
    QuizResult result,
    CancellationToken cancellationToken = default)
    {
        if (!_attempts.TryGetValue(attemptId, out var attempt))
        {
            throw new InvalidOperationException(
                "Attempt does not exist.");
        }

        _attempts[attemptId] = new QuizAttempt
        {
            Id = attempt.Id,
            QuizId = attempt.QuizId,
            ExamContentHash = attempt.ExamContentHash,
            StartedAtUtc = attempt.StartedAtUtc,
            DeadlineUtc = attempt.DeadlineUtc,
            SubmittedAtUtc = submittedAtUtc,
            Status = status,
            Answers = attempt.Answers
        };

        Results[attemptId] = result;

        return Task.CompletedTask;
    }



    public Task<QuizAttempt?> GetInProgressAttemptAsync(
        Guid quizId,
        string examContentHash,
        CancellationToken cancellationToken = default)
    {
        var attempt = _attempts.Values
            .Where(item =>
                item.QuizId == quizId &&
                item.ExamContentHash == examContentHash &&
                item.Status == AttemptStatus.InProgress)
            .OrderByDescending(item => item.StartedAtUtc)
            .FirstOrDefault();

        return Task.FromResult(attempt);
    }

    public Task<QuizResult?> GetResultByAttemptIdAsync(
    Guid attemptId,
    CancellationToken cancellationToken = default)
    {
        Results.TryGetValue(attemptId, out var result);

        return Task.FromResult(result);
    }

}