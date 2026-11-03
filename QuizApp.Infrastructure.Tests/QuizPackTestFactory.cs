using System.IO.Compression;
using System.Text;
using System.Text.Json;
using QuizApp.Infrastructure.FileFormats.QuizPack;

namespace QuizApp.Infrastructure.Tests.Helpers;

public static class QuizPackTestFactory
{
    public static readonly Guid ExamId =
        Guid.Parse("a2b1c3d4-1111-2222-3333-123456789abc");

    public static readonly Guid QuestionId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid Option1Id =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static readonly Guid Option2Id =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static void CreateValidPackage(string filePath)
    {
        CreatePackage(filePath);
    }

    public static void CreatePackage(
        string filePath,
        int schemaVersion = 1,
        Guid? documentId = null,
        string? imagePath = null,
        bool includeImage = true)
    {
        var manifest = new QuizPackManifest
        {
            Format = "quizpack",
            SchemaVersion = schemaVersion,
            ExamId = ExamId,
            Title = "Test Quiz",
            ExamFile = "exam.json"
        };

        var document = new QuizPackDocument
        {
            Id = documentId ?? ExamId,
            Title = "Test Quiz",
            DurationSeconds = 1800,
            Questions =
            [
                new QuizPackQuestion
                {
                    Id = QuestionId,
                    Content = "Which component executes instructions?",
                    Type = "SingleChoice",
                    Points = 2,
                    Image = imagePath,
                    Options =
                    [
                        new QuizPackOption
                        {
                            Id = Option1Id,
                            Content = "CPU"
                        },
                        new QuizPackOption
                        {
                            Id = Option2Id,
                            Content = "GPU"
                        }
                    ],
                    CorrectOptionIds = [Option1Id]
                }
            ]
        };

        using var fileStream = File.Create(filePath);

        using var archive = new ZipArchive(
            fileStream,
            ZipArchiveMode.Create);

        AddJsonEntry(archive, "manifest.json", manifest);
        AddJsonEntry(archive, "exam.json", document);

        if (includeImage && imagePath == "assets/images/question.png")
        {
            var imageEntry = archive.CreateEntry(imagePath);

            using var imageStream = imageEntry.Open();

            imageStream.Write([1, 2, 3, 4]);
        }
    }

    private static void AddJsonEntry<T>(
        ZipArchive archive,
        string entryName,
        T value)
    {
        var entry = archive.CreateEntry(entryName);

        using var stream = entry.Open();

        var json = JsonSerializer.Serialize(
            value,
            QuizPackJsonOptions.Default);

        var bytes = Encoding.UTF8.GetBytes(json);

        stream.Write(bytes);
    }
}