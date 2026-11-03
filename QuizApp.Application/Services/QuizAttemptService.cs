using QuizApp.Application.Abstractions;
using QuizApp.Application.Exceptions;
using QuizApp.Application.Models;
using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;
using QuizApp.Domain.Services;

namespace QuizApp.Application.Services;

public class QuizAttemptService
{
    private readonly IQuizAttemptRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ScoringService _scoringService;

    public QuizAttemptService(
        IQuizAttemptRepository repository,
        ScoringService scoringService,
        TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _scoringService = scoringService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<QuizAttempt> StartAttemptAsync(
        QuizDefinition quiz,
        string examContentHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quiz);

        if (quiz.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "Quiz ID cannot be empty.",
                nameof(quiz));
        }

        if (quiz.DurationSeconds <= 0)
        {
            throw new ArgumentException(
                "Quiz duration must be greater than zero.",
                nameof(quiz));
        }

        if (string.IsNullOrWhiteSpace(examContentHash))
        {
            throw new ArgumentException(
                "Exam content hash cannot be empty.",
                nameof(examContentHash));
        }

        var startedAt = _timeProvider.GetUtcNow().UtcDateTime;

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            ExamContentHash = examContentHash,
            StartedAtUtc = startedAt,
            DeadlineUtc = startedAt.AddSeconds(quiz.DurationSeconds),
            SubmittedAtUtc = null,
            Status = AttemptStatus.InProgress,
            Answers = []
        };

        await _repository.AddAsync(attempt, cancellationToken);

        return attempt;
    }

    public async Task SaveAnswerAsync(
        Guid attemptId,
        QuizDefinition quiz,
        string examContentHash,
        Answer answer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quiz);
        ArgumentNullException.ThrowIfNull(answer);

        var attempt = await GetRequiredAttemptAsync(
            attemptId,
            cancellationToken);

        ValidateAttempt(attempt, quiz, examContentHash);
        ValidateAnswer(quiz, answer);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (now >= attempt.DeadlineUtc)
        {
            throw new AttemptNotActiveException(
                "The quiz deadline has passed.");
        }

        var normalizedAnswer = new Answer
        {
            QuestionId = answer.QuestionId,
            SelectedOptionIds = answer.SelectedOptionIds
                .Distinct()
                .ToList(),
            AnsweredAtUtc = now
        };

        await _repository.SaveAnswerAsync(
            attemptId,
            normalizedAnswer,
            cancellationToken);
    }

    public async Task<QuizAttempt> ResumeAttemptAsync(
        Guid attemptId,
        QuizDefinition quiz,
        string examContentHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quiz);

        var attempt = await GetRequiredAttemptAsync(
            attemptId,
            cancellationToken);

        ValidateAttempt(attempt, quiz, examContentHash);

        if (attempt.Status != AttemptStatus.InProgress)
        {
            throw new AttemptNotActiveException(
                $"Cannot resume an attempt with status '{attempt.Status}'.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (now >= attempt.DeadlineUtc)
        {
            throw new AttemptNotActiveException(
                "The quiz deadline has passed.");
        }

        return attempt;
    }

    private async Task<QuizAttempt> GetRequiredAttemptAsync(
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await _repository.GetByIdAsync(
            attemptId,
            cancellationToken);

        return attempt ?? throw new AttemptNotFoundException(attemptId);
    }

    private static void ValidateAttempt(
        QuizAttempt attempt,
        QuizDefinition quiz,
        string examContentHash)
    {
        if (attempt.QuizId != quiz.Id ||
            !string.Equals(
                attempt.ExamContentHash,
                examContentHash,
                StringComparison.Ordinal))
        {
            throw new QuizContentMismatchException();
        }

        if (attempt.Status != AttemptStatus.InProgress)
        {
            throw new AttemptNotActiveException(
                $"Attempt status is '{attempt.Status}'.");
        }
    }

    private static void ValidateAnswer(
        QuizDefinition quiz,
        Answer answer)
    {
        var question = quiz.Questions.FirstOrDefault(
            q => q.Id == answer.QuestionId);

        if (question is null)
        {
            throw new ArgumentException(
                "The question does not belong to this quiz.",
                nameof(answer));
        }

        var selectedIds = answer.SelectedOptionIds.ToHashSet();

        if (selectedIds.Count != answer.SelectedOptionIds.Count)
        {
            throw new ArgumentException(
                "Selected option IDs must be unique.",
                nameof(answer));
        }

        var validOptionIds = question.Options
            .Select(option => option.Id)
            .ToHashSet();

        if (!selectedIds.IsSubsetOf(validOptionIds))
        {
            throw new ArgumentException(
                "One or more selected options do not belong to this question.",
                nameof(answer));
        }

        if (question.Type == QuestionType.SingleChoice &&
            selectedIds.Count > 1)
        {
            throw new ArgumentException(
                "A single-choice question cannot have multiple selected options.",
                nameof(answer));
        }
    }

    public async Task<QuizResult> SubmitAttemptAsync(
    Guid attemptId,
    QuizDefinition quiz,
    string examContentHash,
    CancellationToken cancellationToken = default)
    {
        var attempt = await GetRequiredAttemptAsync(
            attemptId,
            cancellationToken);

        ValidateAttempt(attempt, quiz, examContentHash);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (now >= attempt.DeadlineUtc)
        {
            throw new AttemptNotActiveException(
                "The quiz deadline has passed.");
        }

        var result = _scoringService.Calculate(quiz, attempt);

        await _repository.CompleteAttemptAsync(
            attemptId,
            AttemptStatus.Submitted,
            now,
            result,
            cancellationToken);

        return result;
    }

    public async Task<QuizResult> ExpireAttemptAsync(
        Guid attemptId,
        QuizDefinition quiz,
        string examContentHash,
        CancellationToken cancellationToken = default)
    {
        var attempt = await GetRequiredAttemptAsync(
            attemptId,
            cancellationToken);

        ValidateAttempt(attempt, quiz, examContentHash);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (now < attempt.DeadlineUtc)
        {
            throw new InvalidOperationException(
                "Cannot expire an attempt before its deadline.");
        }

        var result = _scoringService.Calculate(quiz, attempt);

        await _repository.CompleteAttemptAsync(
            attemptId,
            AttemptStatus.Expired,
            now,
            result,
            cancellationToken);

        return result;
    }


    public async Task<AttemptResumeResult> FindOrResumeAttemptAsync(
        QuizDefinition quiz,
        string examContentHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quiz);

        if (string.IsNullOrWhiteSpace(examContentHash))
        {
            throw new ArgumentException(
                "Exam content hash cannot be empty.",
                nameof(examContentHash));
        }

        var attempt = await _repository.GetInProgressAttemptAsync(
            quiz.Id,
            examContentHash,
            cancellationToken);

        if (attempt is null)
        {
            return new AttemptResumeResult
            {
                Status = AttemptResumeStatus.NoExistingAttempt
            };
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (now < attempt.DeadlineUtc)
        {
            return new AttemptResumeResult
            {
                Status = AttemptResumeStatus.Resumed,
                Attempt = attempt
            };
        }

        var result = _scoringService.Calculate(quiz, attempt);

        await _repository.CompleteAttemptAsync(
            attempt.Id,
            AttemptStatus.Expired,
            now,
            result,
            cancellationToken);

        return new AttemptResumeResult
        {
            Status = AttemptResumeStatus.Expired,
            Attempt = attempt,
            Result = result
        };
    }



    public async Task<QuizAttemptReview> GetAttemptReviewAsync(
        Guid attemptId,
        QuizDefinition quiz,
        string examContentHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quiz);

        if (string.IsNullOrWhiteSpace(examContentHash))
        {
            throw new ArgumentException(
                "Exam content hash cannot be empty.",
                nameof(examContentHash));
        }

        var attempt = await _repository.GetByIdAsync(
            attemptId,
            cancellationToken);

        if (attempt is null)
        {
            throw new AttemptNotFoundException(attemptId);
        }

        if (attempt.QuizId != quiz.Id ||
            attempt.ExamContentHash != examContentHash)
        {
            throw new QuizContentMismatchException();
        }

        if (attempt.Status == AttemptStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Cannot review an attempt that is still in progress.");
        }

        var result = await _repository.GetResultByAttemptIdAsync(
            attemptId,
            cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "Completed attempt does not have a saved result.");
        }

        var answersByQuestionId = attempt.Answers
            .ToDictionary(answer => answer.QuestionId);

        var reviewQuestions = new List<QuestionReviewItem>();

        foreach (var question in quiz.Questions)
        {
            answersByQuestionId.TryGetValue(
                question.Id,
                out var answer);

            var selectedIds = answer?.SelectedOptionIds ?? [];
            var correctIds = question.CorrectOptionIds;

            var isCorrect = selectedIds
                .ToHashSet()
                .SetEquals(correctIds);

            reviewQuestions.Add(new QuestionReviewItem
            {
                Question = question,
                SelectedOptionIds = selectedIds,
                CorrectOptionIds = correctIds,
                IsCorrect = isCorrect,
                EarnedPoints = isCorrect ? question.Points : 0
            });
        }

        return new QuizAttemptReview
        {
            Attempt = attempt,
            Result = result,
            Questions = reviewQuestions
        };
    }

}