using System.IO.Compression;
using System.Text.Json;
using QuizApp.Application.Abstractions;
using QuizApp.Domain.Enums;
using QuizApp.Domain.Models;

namespace QuizApp.Infrastructure.FileFormats.QuizPack;

public class QuizPackReader : IQuizFileReader
{
    private const int SupportedSchemaVersion = 1;

    private const long MaxPackageSize = 100 * 1024 * 1024;
    private const long MaxEntrySize = 20 * 1024 * 1024;
    private const int MaxEntryCount = 500;

    public async Task<QuizDefinition> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "File path cannot be empty.",
                nameof(filePath));
        }

        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException(
                "QuizPack file was not found.",
                filePath);
        }

        if (fileInfo.Length > MaxPackageSize)
        {
            throw new QuizPackReadException(
                "QuizPack file exceeds the maximum allowed size.");
        }

        try
        {
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            using var archive = new ZipArchive(
                fileStream,
                ZipArchiveMode.Read);

            ValidateArchive(archive);

            var manifest = await ReadJsonEntryAsync<QuizPackManifest>(
                archive,
                "manifest.json",
                cancellationToken);

            ValidateManifest(manifest);

            var examEntry = GetRequiredEntry(
                archive,
                manifest.ExamFile);

            var document = await ReadJsonEntryAsync<QuizPackDocument>(
                examEntry,
                cancellationToken);

            ValidateDocument(manifest, document, archive);

            return MapToDomain(document);
        }
        catch (QuizPackReadException)
        {
            throw;
        }
        catch (InvalidDataException ex)
        {
            throw new QuizPackReadException(
                "The QuizPack archive is invalid.",
                ex);
        }
        catch (JsonException ex)
        {
            throw new QuizPackReadException(
                "The QuizPack contains invalid JSON.",
                ex);
        }
    }

    private static void ValidateArchive(ZipArchive archive)
    {
        if (archive.Entries.Count > MaxEntryCount)
        {
            throw new QuizPackReadException(
                "QuizPack contains too many entries.");
        }

        foreach (var entry in archive.Entries)
        {
            if (entry.Length > MaxEntrySize)
            {
                throw new QuizPackReadException(
                    $"Entry '{entry.FullName}' exceeds the allowed size.");
            }
        }
    }

    private static void ValidateManifest(QuizPackManifest manifest)
    {
        if (manifest.Format != "quizpack")
        {
            throw new QuizPackReadException(
                "Unsupported package format.");
        }

        if (manifest.SchemaVersion != SupportedSchemaVersion)
        {
            throw new QuizPackReadException(
                $"Unsupported QuizPack schema version: {manifest.SchemaVersion}.");
        }

        if (manifest.ExamId == Guid.Empty)
        {
            throw new QuizPackReadException(
                "Manifest exam ID cannot be empty.");
        }

        ValidateArchivePath(manifest.ExamFile);
    }

    private static void ValidateDocument(
        QuizPackManifest manifest,
        QuizPackDocument document,
        ZipArchive archive)
    {
        if (document.Id != manifest.ExamId)
        {
            throw new QuizPackReadException(
                "Exam ID in manifest does not match exam document.");
        }

        if (string.IsNullOrWhiteSpace(document.Title))
        {
            throw new QuizPackReadException(
                "Exam title cannot be empty.");
        }

        if (document.DurationSeconds <= 0)
        {
            throw new QuizPackReadException(
                "Exam duration must be greater than zero.");
        }

        if (document.Questions.Count == 0)
        {
            throw new QuizPackReadException(
                "Exam must contain at least one question.");
        }

        var questionIds = new HashSet<Guid>();

        foreach (var question in document.Questions)
        {
            ValidateQuestion(question, questionIds, archive);
        }
    }

    private static void ValidateQuestion(
        QuizPackQuestion question,
        HashSet<Guid> questionIds,
        ZipArchive archive)
    {
        if (question.Id == Guid.Empty ||
            !questionIds.Add(question.Id))
        {
            throw new QuizPackReadException(
                "Question IDs must be non-empty and unique.");
        }

        if (string.IsNullOrWhiteSpace(question.Content))
        {
            throw new QuizPackReadException(
                $"Question '{question.Id}' has empty content.");
        }

        if (question.Points < 0)
        {
            throw new QuizPackReadException(
                $"Question '{question.Id}' has negative points.");
        }

        if (!Enum.TryParse<QuestionType>(
                question.Type,
                ignoreCase: false,
                out var questionType) ||
            !Enum.IsDefined(questionType))
        {
            throw new QuizPackReadException(
                $"Question '{question.Id}' has an unsupported type.");
        }

        if (question.Options.Count < 2)
        {
            throw new QuizPackReadException(
                $"Question '{question.Id}' must have at least two options.");
        }

        var optionIds = new HashSet<Guid>();

        foreach (var option in question.Options)
        {
            if (option.Id == Guid.Empty ||
                !optionIds.Add(option.Id))
            {
                throw new QuizPackReadException(
                    $"Question '{question.Id}' has invalid or duplicate option IDs.");
            }

            if (string.IsNullOrWhiteSpace(option.Content))
            {
                throw new QuizPackReadException(
                    $"Question '{question.Id}' has an empty option.");
            }
        }

        var correctIds = question.CorrectOptionIds.ToHashSet();

        if (correctIds.Count != question.CorrectOptionIds.Count ||
            correctIds.Count == 0 ||
            !correctIds.IsSubsetOf(optionIds))
        {
            throw new QuizPackReadException(
                $"Question '{question.Id}' has invalid correct option IDs.");
        }

        if (questionType == QuestionType.SingleChoice &&
            correctIds.Count != 1)
        {
            throw new QuizPackReadException(
                $"Single-choice question '{question.Id}' must have exactly one correct option.");
        }

        if (question.Image is not null)
        {
            ValidateArchivePath(question.Image);
            GetRequiredEntry(archive, question.Image);
        }
    }

    private static void ValidateArchivePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            path.Contains('\\') ||
            path.StartsWith('/') ||
            Path.IsPathRooted(path) ||
            path.Split('/').Any(segment =>
                segment is "" or "." or ".."))
        {
            throw new QuizPackReadException(
                $"Invalid archive path: '{path}'.");
        }
    }

    private static ZipArchiveEntry GetRequiredEntry(
        ZipArchive archive,
        string path)
    {
        var entry = archive.GetEntry(path);

        return entry ?? throw new QuizPackReadException(
            $"Required entry '{path}' was not found in the package.");
    }

    private static async Task<T> ReadJsonEntryAsync<T>(
        ZipArchive archive,
        string path,
        CancellationToken cancellationToken)
    {
        var entry = GetRequiredEntry(archive, path);

        return await ReadJsonEntryAsync<T>(
            entry,
            cancellationToken);
    }

    private static async Task<T> ReadJsonEntryAsync<T>(
        ZipArchiveEntry entry,
        CancellationToken cancellationToken)
    {
        await using var stream = entry.Open();

        var result = await JsonSerializer.DeserializeAsync<T>(
            stream,
            QuizPackJsonOptions.Default,
            cancellationToken);

        return result ?? throw new QuizPackReadException(
            $"Entry '{entry.FullName}' contains empty JSON.");
    }

    private static QuizDefinition MapToDomain(
        QuizPackDocument document)
    {
        var questions = document.Questions
            .Select(question => new Question
            {
                Id = question.Id,
                Content = question.Content,
                Type = Enum.Parse<QuestionType>(question.Type),
                Points = question.Points,
                ImagePath = question.Image,
                Options = question.Options
                    .Select(option => new QuestionOption
                    {
                        Id = option.Id,
                        Content = option.Content
                    })
                    .ToList(),
                CorrectOptionIds = question.CorrectOptionIds
            })
            .ToList();

        return new QuizDefinition
        {
            Id = document.Id,
            Title = document.Title,
            DurationSeconds = document.DurationSeconds,
            Questions = questions
        };
    }
}