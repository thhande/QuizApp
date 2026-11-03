using System.IO.Compression;
using System.Text.Json;
using QuizApp.Application.Abstractions;
using QuizApp.Domain.Models;

namespace QuizApp.Infrastructure.FileFormats.QuizPack;

public sealed class QuizPackWriter : IQuizPackWriter
{
    private const int CurrentSchemaVersion = 1;
    private const string ManifestFileName = "manifest.json";
    private const string ExamFileName = "exam.json";

    public async Task WriteAsync(
        QuizDefinition quiz,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quiz);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(
                "File path cannot be empty.",
                nameof(filePath));

        // The first editor version supports text-only questions.
        // Never silently discard image references.
        if (quiz.Questions.Any(question =>
                !string.IsNullOrWhiteSpace(question.ImagePath)))
        {
            throw new NotSupportedException(
                "Saving quizzes containing images is not supported yet.");
        }

        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath)!;

        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(
            directory,
            $"{Guid.NewGuid():N}.tmp");

        try
        {
            await WriteArchiveAsync(
                quiz,
                temporaryPath,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static async Task WriteArchiveAsync(
        QuizDefinition quiz,
        string filePath,
        CancellationToken cancellationToken)
    {
        var manifest = new QuizPackManifest
        {
            SchemaVersion = CurrentSchemaVersion,
            ExamId = quiz.Id,
            Title = quiz.Title,
            ExamFile = ExamFileName
        };

        var document = MapToDocument(quiz);

        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(
            manifest,
            QuizPackJsonOptions.Default);

        var examBytes = JsonSerializer.SerializeToUtf8Bytes(
            document,
            QuizPackJsonOptions.Default);

        await using var fileStream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        using var archive = new ZipArchive(
            fileStream,
            ZipArchiveMode.Create,
            leaveOpen: true);

        await WriteEntryAsync(
            archive,
            ManifestFileName,
            manifestBytes,
            cancellationToken);

        await WriteEntryAsync(
            archive,
            ExamFileName,
            examBytes,
            cancellationToken);
    }

    private static async Task WriteEntryAsync(
        ZipArchive archive,
        string entryName,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(
            entryName,
            CompressionLevel.Optimal);

        await using var entryStream = entry.Open();

        await entryStream.WriteAsync(
            content.AsMemory(),
            cancellationToken);
    }

    private static QuizPackDocument MapToDocument(QuizDefinition quiz)
    {
        return new QuizPackDocument
        {
            Id = quiz.Id,
            Title = quiz.Title,
            DurationSeconds = quiz.DurationSeconds,

            Questions = quiz.Questions
                .Select(question => new QuizPackQuestion
                {
                    Id = question.Id,
                    Content = question.Content,
                    Type = question.Type.ToString(),
                    Points = question.Points,
                    Image = question.ImagePath,

                    Options = question.Options
                        .Select(option => new QuizPackOption
                        {
                            Id = option.Id,
                            Content = option.Content
                        })
                        .ToList(),

                    CorrectOptionIds = question.CorrectOptionIds.ToList()
                })
                .ToList()
        };
    }
}