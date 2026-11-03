using QuizApp.Application.Exceptions;
using QuizApp.Application.Models;
using QuizApp.Application.Services;
using QuizApp.Application.Tests.Fakes;
using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;
using QuizApp.Domain.Services;

namespace QuizApp.Application.Tests.Services;

public class QuizAttemptServiceTests
{
    private const string ContentHash = "test-content-hash";

    private readonly Guid _quizId = Guid.NewGuid();
    private readonly Guid _questionId = Guid.NewGuid();
    private readonly Guid _option1Id = Guid.NewGuid();
    private readonly Guid _option2Id = Guid.NewGuid();

    private readonly FixedTimeProvider _timeProvider;
    private readonly FakeQuizAttemptRepository _repository;
    private readonly QuizAttemptService _service;

    public DateTime? CompletedAtUtc { get; init; }

    public QuizAttemptServiceTests()
    {
        _timeProvider = new FixedTimeProvider(
            new DateTimeOffset(
                2026, 11, 1, 10, 0, 0, TimeSpan.Zero));

        _repository = new FakeQuizAttemptRepository();

        _service = new QuizAttemptService(
     _repository,
     new ScoringService(),
     _timeProvider);
    }

    private QuizDefinition CreateQuiz(
        QuestionType type = QuestionType.SingleChoice)
    {
        return new QuizDefinition
        {
            Id = _quizId,
            Title = "Test Quiz",
            DurationSeconds = 1800,
            Questions =
            [
                new Question
                {
                    Id = _questionId,
                    Content = "Choose the correct answer.",
                    Type = type,
                    Points = 2,
                    Options =
                    [
                        new QuestionOption
                        {
                            Id = _option1Id,
                            Content = "Option 1"
                        },
                        new QuestionOption
                        {
                            Id = _option2Id,
                            Content = "Option 2"
                        }
                    ],
                    CorrectOptionIds = [_option1Id]
                }
            ]
        };
    }

    [Fact]
    public async Task StartAttemptAsync_CreatesAttemptWithCorrectDeadline()
    {
        // Arrange
        var quiz = CreateQuiz();

        // Act
        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        // Assert
        Assert.NotEqual(Guid.Empty, attempt.Id);
        Assert.Equal(quiz.Id, attempt.QuizId);
        Assert.Equal(ContentHash, attempt.ExamContentHash);

        Assert.Equal(
            _timeProvider.GetUtcNow().UtcDateTime,
            attempt.StartedAtUtc);

        Assert.Equal(
            attempt.StartedAtUtc.AddSeconds(quiz.DurationSeconds),
            attempt.DeadlineUtc);

        Assert.Equal(
            AttemptStatus.InProgress,
            attempt.Status);

        Assert.Empty(attempt.Answers);
    }

    [Fact]
    public async Task SaveAnswerAsync_ValidAnswer_SavesAnswer()
    {
        // Arrange
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        var answer = new Answer
        {
            QuestionId = _questionId,
            SelectedOptionIds = [_option1Id],
            AnsweredAtUtc = DateTime.MinValue
        };

        // Act
        await _service.SaveAnswerAsync(
            attempt.Id,
            quiz,
            ContentHash,
            answer);

        // Assert
        var restored = await _repository.GetByIdAsync(attempt.Id);

        Assert.NotNull(restored);

        var savedAnswer = Assert.Single(restored.Answers);

        Assert.Equal(_questionId, savedAnswer.QuestionId);
        Assert.Equal([_option1Id], savedAnswer.SelectedOptionIds);

        Assert.Equal(
            _timeProvider.GetUtcNow().UtcDateTime,
            savedAnswer.AnsweredAtUtc);
    }

    [Fact]
    public async Task SaveAnswerAsync_InvalidOption_ThrowsArgumentException()
    {
        // Arrange
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        var answer = new Answer
        {
            QuestionId = _questionId,
            SelectedOptionIds = [Guid.NewGuid()]
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SaveAnswerAsync(
                attempt.Id,
                quiz,
                ContentHash,
                answer));
    }

    [Fact]
    public async Task SaveAnswerAsync_MultipleOptionsForSingleChoice_ThrowsException()
    {
        // Arrange
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        var answer = new Answer
        {
            QuestionId = _questionId,
            SelectedOptionIds = [_option1Id, _option2Id]
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SaveAnswerAsync(
                attempt.Id,
                quiz,
                ContentHash,
                answer));
    }

    [Fact]
    public async Task SaveAnswerAsync_AfterDeadline_ThrowsAttemptNotActiveException()
    {
        // Arrange
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        _timeProvider.Advance(
            TimeSpan.FromSeconds(quiz.DurationSeconds));

        var answer = new Answer
        {
            QuestionId = _questionId,
            SelectedOptionIds = [_option1Id]
        };

        // Act & Assert
        await Assert.ThrowsAsync<AttemptNotActiveException>(
            () => _service.SaveAnswerAsync(
                attempt.Id,
                quiz,
                ContentHash,
                answer));
    }

    [Fact]
    public async Task ResumeAttemptAsync_ContentHashMismatch_ThrowsException()
    {
        // Arrange
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        // Act & Assert
        await Assert.ThrowsAsync<QuizContentMismatchException>(
            () => _service.ResumeAttemptAsync(
                attempt.Id,
                quiz,
                "different-content-hash"));
    }

    [Fact]
    public async Task ResumeAttemptAsync_ValidAttempt_ReturnsAttempt()
    {
        // Arrange
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        // Act
        var resumed = await _service.ResumeAttemptAsync(
            attempt.Id,
            quiz,
            ContentHash);

        // Assert
        Assert.Equal(attempt.Id, resumed.Id);
        Assert.Equal(AttemptStatus.InProgress, resumed.Status);
    }


    [Fact]
    public async Task SubmitAttemptAsync_BeforeDeadline_CompletesAttemptAndCalculatesResult()
    {
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        // Chọn đáp án đúng.
        await _service.SaveAnswerAsync(
            attempt.Id,
            quiz,
            ContentHash,
            new Answer
            {
                QuestionId = _questionId,
                SelectedOptionIds = new List<Guid>
                {
                _option1Id
                }
            });

        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        var result = await _service.SubmitAttemptAsync(
            attempt.Id,
            quiz,
            ContentHash);

        var savedAttempt = await _repository.GetByIdAsync(
            attempt.Id);

        Assert.NotNull(savedAttempt);
        Assert.Equal(AttemptStatus.Submitted, savedAttempt.Status);
        Assert.Equal(
            attempt.StartedAtUtc.AddMinutes(5),
            savedAttempt.SubmittedAtUtc);

        Assert.Equal(attempt.Id, result.AttemptId);
        Assert.Equal(1, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.Equal(2, result.EarnedPoints);
        Assert.Equal(2, result.MaximumPoints);
        Assert.Equal(100, result.ScorePercentage);

        Assert.True(_repository.Results.ContainsKey(attempt.Id));
    }

    [Fact]
    public async Task SubmitAttemptAsync_AfterDeadline_ThrowsAttemptNotActiveException()
    {
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        _timeProvider.Advance(
            TimeSpan.FromSeconds(quiz.DurationSeconds));

        await Assert.ThrowsAsync<AttemptNotActiveException>(
            () => _service.SubmitAttemptAsync(
                attempt.Id,
                quiz,
                ContentHash));

        var savedAttempt = await _repository.GetByIdAsync(
            attempt.Id);

        Assert.NotNull(savedAttempt);
        Assert.Equal(AttemptStatus.InProgress, savedAttempt.Status);
        Assert.False(_repository.Results.ContainsKey(attempt.Id));
    }

    [Fact]
    public async Task ExpireAttemptAsync_BeforeDeadline_ThrowsInvalidOperationException()
    {
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExpireAttemptAsync(
                attempt.Id,
                quiz,
                ContentHash));

        var savedAttempt = await _repository.GetByIdAsync(
            attempt.Id);

        Assert.NotNull(savedAttempt);
        Assert.Equal(AttemptStatus.InProgress, savedAttempt.Status);
        Assert.False(_repository.Results.ContainsKey(attempt.Id));
    }

    [Fact]
    public async Task ExpireAttemptAsync_AfterDeadline_CompletesAttemptAndCalculatesResult()
    {
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        // Lưu đáp án đúng trước khi hết giờ.
        await _service.SaveAnswerAsync(
            attempt.Id,
            quiz,
            ContentHash,
            new Answer
            {
                QuestionId = _questionId,
                SelectedOptionIds = new List<Guid>
                {
                _option1Id
                }
            });

        _timeProvider.Advance(
            TimeSpan.FromSeconds(quiz.DurationSeconds));

        var result = await _service.ExpireAttemptAsync(
            attempt.Id,
            quiz,
            ContentHash);

        var savedAttempt = await _repository.GetByIdAsync(
            attempt.Id);

        Assert.NotNull(savedAttempt);
        Assert.Equal(AttemptStatus.Expired, savedAttempt.Status);
        Assert.Equal(attempt.DeadlineUtc, savedAttempt.SubmittedAtUtc);

        Assert.Equal(attempt.Id, result.AttemptId);
        Assert.Equal(1, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.Equal(2, result.EarnedPoints);
        Assert.Equal(100, result.ScorePercentage);

        Assert.True(_repository.Results.ContainsKey(attempt.Id));
    }

    [Fact]
    public async Task SubmitAttemptAsync_AlreadyCompleted_ThrowsAttemptNotActiveException()
    {
        var quiz = CreateQuiz();

        var attempt = await _service.StartAttemptAsync(
            quiz,
            ContentHash);

        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // Submit lần đầu.
        await _service.SubmitAttemptAsync(
            attempt.Id,
            quiz,
            ContentHash);

        // Submit lần hai phải bị từ chối.
        await Assert.ThrowsAsync<AttemptNotActiveException>(
            () => _service.SubmitAttemptAsync(
                attempt.Id,
                quiz,
                ContentHash));

        var savedAttempt = await _repository.GetByIdAsync(
            attempt.Id);

        Assert.NotNull(savedAttempt);
        Assert.Equal(AttemptStatus.Submitted, savedAttempt.Status);
    }


    [Fact]
    public async Task FindOrResumeAttemptAsync_NoMatchingAttempt_ReturnsNoExistingAttempt()
    {
        var result = await _service.FindOrResumeAttemptAsync(CreateQuiz(), ContentHash);
        Assert.Equal(AttemptResumeStatus.NoExistingAttempt, result.Status);
        Assert.Null(result.Attempt);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task FindOrResumeAttemptAsync_MatchingAttemptBeforeDeadline_ReturnsResumedWithRestoredAnswers()
    {
        var quiz = CreateQuiz();
        var answer = new Answer { QuestionId = _questionId, SelectedOptionIds = [_option1Id], AnsweredAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1) };
        var attempt = CreateInProgressAttempt(quiz, ContentHash, _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5), [answer]);
        await _repository.AddAsync(attempt);

        var result = await _service.FindOrResumeAttemptAsync(quiz, ContentHash);

        Assert.Equal(AttemptResumeStatus.Resumed, result.Status);
        Assert.NotNull(result.Attempt);
        var restoredAnswer = Assert.Single(result.Attempt.Answers);
        Assert.Equal(answer.QuestionId, restoredAnswer.QuestionId);
        Assert.Equal(answer.SelectedOptionIds, restoredAnswer.SelectedOptionIds);
        Assert.Equal(answer.AnsweredAtUtc, restoredAnswer.AnsweredAtUtc);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task FindOrResumeAttemptAsync_ExactlyAtDeadline_MarksExpiredAndPersistsResult()
    {
        var quiz = CreateQuiz();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var attempt = CreateInProgressAttempt(quiz, ContentHash, now, [new Answer { QuestionId = _questionId, SelectedOptionIds = [_option1Id], AnsweredAtUtc = now.AddMinutes(-1) }]);
        await _repository.AddAsync(attempt);

        var result = await _service.FindOrResumeAttemptAsync(quiz, ContentHash);

        Assert.Equal(AttemptResumeStatus.Expired, result.Status);
        Assert.NotNull(result.Result);
        Assert.Equal(1, result.Result.CorrectAnswers);
        Assert.Same(result.Result, _repository.Results[attempt.Id]);
        var saved = await _repository.GetByIdAsync(attempt.Id);
        Assert.NotNull(saved);
        Assert.Equal(AttemptStatus.Expired, saved.Status);
        Assert.Equal(now, saved.SubmittedAtUtc);
    }

    [Fact]
    public async Task FindOrResumeAttemptAsync_AfterDeadline_MarksAttemptExpired()
    {
        var quiz = CreateQuiz();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var attempt = CreateInProgressAttempt(quiz, ContentHash, now.AddSeconds(-1));
        await _repository.AddAsync(attempt);

        var result = await _service.FindOrResumeAttemptAsync(quiz, ContentHash);

        Assert.Equal(AttemptResumeStatus.Expired, result.Status);
        var saved = await _repository.GetByIdAsync(attempt.Id);
        Assert.NotNull(saved);
        Assert.Equal(AttemptStatus.Expired, saved.Status);
        Assert.Equal(now, saved.SubmittedAtUtc);
    }

    [Fact]
    public async Task FindOrResumeAttemptAsync_DifferentContentHash_ReturnsNoExistingAttempt()
    {
        var quiz = CreateQuiz();
        await _repository.AddAsync(CreateInProgressAttempt(quiz, ContentHash, _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5)));

        var result = await _service.FindOrResumeAttemptAsync(quiz, "different-content-hash");

        Assert.Equal(AttemptResumeStatus.NoExistingAttempt, result.Status);
        Assert.Null(result.Attempt);
        Assert.Null(result.Result);
        Assert.Empty(_repository.Results);
    }

    private QuizAttempt CreateInProgressAttempt(QuizDefinition quiz, string contentHash, DateTime deadlineUtc, IReadOnlyList<Answer>? answers = null)
    {
        return new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            ExamContentHash = contentHash,
            StartedAtUtc = deadlineUtc.AddSeconds(-quiz.DurationSeconds),
            DeadlineUtc = deadlineUtc,
            Status = AttemptStatus.InProgress,
            Answers = answers ?? []
        };
    }
}
