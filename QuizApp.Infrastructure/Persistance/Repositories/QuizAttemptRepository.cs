using System.Globalization;
using Microsoft.Data.Sqlite;
using QuizApp.Application.Abstractions;
using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;
using QuizApp.Infrastructure.Persistance.Sqlite;


namespace QuizApp.Infrastructure.Persistence.Repositories;

public class QuizAttemptRepository : IQuizAttemptRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public QuizAttemptRepository(
        SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(
        QuizAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO QuizAttempts
            (
                Id,
                QuizId,
                ExamContentHash,
                StartedAtUtc,
                DeadlineUtc,
                SubmittedAtUtc,
                Status
            )
            VALUES
            (
                $id,
                $quizId,
                $hash,
                $startedAt,
                $deadline,
                $submittedAt,
                $status
            );
            """;

        command.Parameters.AddWithValue("$id", attempt.Id.ToString());
        command.Parameters.AddWithValue("$quizId", attempt.QuizId.ToString());
        command.Parameters.AddWithValue("$hash", attempt.ExamContentHash);
        command.Parameters.AddWithValue(
            "$startedAt", attempt.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue(
            "$deadline", attempt.DeadlineUtc.ToString("O"));

        command.Parameters.AddWithValue(
            "$submittedAt",
            attempt.SubmittedAtUtc.HasValue
                ? attempt.SubmittedAtUtc.Value.ToString("O")
                : DBNull.Value);

        command.Parameters.AddWithValue(
            "$status", (int)attempt.Status);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<QuizAttempt?> GetByIdAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        QuizAttempt? attempt = null;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT
                    Id,
                    QuizId,
                    ExamContentHash,
                    StartedAtUtc,
                    DeadlineUtc,
                    SubmittedAtUtc,
                    Status
                FROM QuizAttempts
                WHERE Id = $id;
                """;

            command.Parameters.AddWithValue("$id", attemptId.ToString());

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                attempt = new QuizAttempt
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    QuizId = Guid.Parse(reader.GetString(1)),
                    ExamContentHash = reader.GetString(2),
                    StartedAtUtc = ParseUtc(reader.GetString(3)),
                    DeadlineUtc = ParseUtc(reader.GetString(4)),
                    SubmittedAtUtc = reader.IsDBNull(5)
                        ? null
                        : ParseUtc(reader.GetString(5)),
                    Status = (AttemptStatus)reader.GetInt32(6)
                };
            }
        }

        if (attempt is null)
        {
            return null;
        }

        var answers = await LoadAnswersAsync(
            connection,
            attemptId,
            cancellationToken);

        return new QuizAttempt
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
    }

    public async Task SaveAnswerAsync(
        Guid attemptId,
        Answer answer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(answer);

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        using var transaction = connection.BeginTransaction();

        // Insert or update the answer timestamp.
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;

            command.CommandText = """
                INSERT INTO Answers
                (
                    AttemptId,
                    QuestionId,
                    AnsweredAtUtc
                )
                VALUES
                (
                    $attemptId,
                    $questionId,
                    $answeredAt
                )
                ON CONFLICT (AttemptId, QuestionId)
                DO UPDATE SET
                    AnsweredAtUtc = excluded.AnsweredAtUtc;
                """;

            command.Parameters.AddWithValue(
                "$attemptId", attemptId.ToString());

            command.Parameters.AddWithValue(
                "$questionId", answer.QuestionId.ToString());

            command.Parameters.AddWithValue(
                "$answeredAt", answer.AnsweredAtUtc.ToString("O"));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // Replace the selected options for this answer.
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;

            command.CommandText = """
                DELETE FROM SelectedOptions
                WHERE AttemptId = $attemptId
                  AND QuestionId = $questionId;
                """;

            command.Parameters.AddWithValue(
                "$attemptId", attemptId.ToString());

            command.Parameters.AddWithValue(
                "$questionId", answer.QuestionId.ToString());

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var optionId in answer.SelectedOptionIds.Distinct())
        {
            await using var command = connection.CreateCommand();

            command.Transaction = transaction;

            command.CommandText = """
                INSERT INTO SelectedOptions
                (
                    AttemptId,
                    QuestionId,
                    OptionId
                )
                VALUES
                (
                    $attemptId,
                    $questionId,
                    $optionId
                );
                """;

            command.Parameters.AddWithValue(
                "$attemptId", attemptId.ToString());

            command.Parameters.AddWithValue(
                "$questionId", answer.QuestionId.ToString());

            command.Parameters.AddWithValue(
                "$optionId", optionId.ToString());

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<List<Answer>> LoadAnswersAsync(
        SqliteConnection connection,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var answers = new Dictionary<Guid, Answer>();

        // Load answer records.
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT QuestionId, AnsweredAtUtc
                FROM Answers
                WHERE AttemptId = $attemptId;
                """;

            command.Parameters.AddWithValue(
                "$attemptId", attemptId.ToString());

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var questionId = Guid.Parse(reader.GetString(0));

                answers.Add(
                    questionId,
                    new Answer
                    {
                        QuestionId = questionId,
                        AnsweredAtUtc = ParseUtc(reader.GetString(1)),
                        SelectedOptionIds = new List<Guid>()
                    });
            }
        }

        // Load selected options and group them by question.
        var selectedOptions = new Dictionary<Guid, List<Guid>>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT QuestionId, OptionId
                FROM SelectedOptions
                WHERE AttemptId = $attemptId;
                """;

            command.Parameters.AddWithValue(
                "$attemptId", attemptId.ToString());

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var questionId = Guid.Parse(reader.GetString(0));
                var optionId = Guid.Parse(reader.GetString(1));

                if (!selectedOptions.TryGetValue(
                    questionId, out var options))
                {
                    options = [];
                    selectedOptions.Add(questionId, options);
                }

                options.Add(optionId);
            }
        }

        return answers.Values
            .Select(answer => new Answer
            {
                QuestionId = answer.QuestionId,
                AnsweredAtUtc = answer.AnsweredAtUtc,
                SelectedOptionIds = selectedOptions.TryGetValue(
                    answer.QuestionId, out var options)
                        ? options
                        : []
            })
            .ToList();
    }

    private static DateTime ParseUtc(string value)
    {
        return DateTime.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }

    public async Task CompleteAttemptAsync(
    Guid attemptId,
    AttemptStatus status,
    DateTime submittedAtUtc,
    QuizResult result,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.AttemptId != attemptId)
        {
            throw new ArgumentException(
                "Result does not belong to this attempt.",
                nameof(result));
        }

        if (status is not AttemptStatus.Submitted
            and not AttemptStatus.Expired)
        {
            throw new ArgumentException(
                "Completion status must be Submitted or Expired.",
                nameof(status));
        }

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;

            command.CommandText = """
            UPDATE QuizAttempts
            SET Status = $status,
                SubmittedAtUtc = $submittedAt
            WHERE Id = $id
              AND Status = $inProgress;
            """;

            command.Parameters.AddWithValue("$status", (int)status);
            command.Parameters.AddWithValue(
                "$submittedAt", submittedAtUtc.ToString("O"));
            command.Parameters.AddWithValue("$id", attemptId.ToString());
            command.Parameters.AddWithValue(
                "$inProgress", (int)AttemptStatus.InProgress);

            var affectedRows = await command.ExecuteNonQueryAsync(
                cancellationToken);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    "Attempt does not exist or is no longer active.");
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;

            command.CommandText = """
            INSERT INTO QuizResults
            (
                AttemptId,
                TotalQuestions,
                CorrectAnswers,
                EarnedPoints,
                MaximumPoints,
                ScorePercentage
            )
            VALUES
            (
                $attemptId,
                $totalQuestions,
                $correctAnswers,
                $earnedPoints,
                $maximumPoints,
                $scorePercentage
            );
            """;

            command.Parameters.AddWithValue(
                "$attemptId", attemptId.ToString());
            command.Parameters.AddWithValue(
                "$totalQuestions", result.TotalQuestions);
            command.Parameters.AddWithValue(
                "$correctAnswers", result.CorrectAnswers);
            command.Parameters.AddWithValue(
                "$earnedPoints", result.EarnedPoints);
            command.Parameters.AddWithValue(
                "$maximumPoints", result.MaximumPoints);
            command.Parameters.AddWithValue(
                "$scorePercentage", result.ScorePercentage);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }


    public async Task<QuizAttempt?> GetInProgressAttemptAsync(
        Guid quizId,
        string examContentHash,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT Id
        FROM QuizAttempts
        WHERE QuizId = $quizId
          AND ExamContentHash = $examContentHash
          AND Status = $status
        ORDER BY StartedAtUtc DESC
        LIMIT 1;
        """;

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        command.Parameters.AddWithValue("$quizId", quizId.ToString());
        command.Parameters.AddWithValue("$examContentHash", examContentHash);
        command.Parameters.AddWithValue(
            "$status",
            (int)AttemptStatus.InProgress);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null || result == DBNull.Value)
            return null;

        var attemptId = Guid.Parse((string)result);

        return await GetByIdAsync(attemptId, cancellationToken);
    }


    public async Task<QuizResult?> GetResultByAttemptIdAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT
            AttemptId,
            TotalQuestions,
            CorrectAnswers,
            EarnedPoints,
            MaximumPoints,
            ScorePercentage
        FROM QuizResults
        WHERE AttemptId = $attemptId;
        """;

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = sql;
        command.Parameters.AddWithValue(
            "$attemptId",
            attemptId.ToString());

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new QuizResult
        {
            AttemptId = Guid.Parse(reader.GetString(0)),
            TotalQuestions = reader.GetInt32(1),
            CorrectAnswers = reader.GetInt32(2),
            EarnedPoints = reader.GetDouble(3),
            MaximumPoints = reader.GetDouble(4),
            ScorePercentage = reader.GetDouble(5)
        };
    }


}