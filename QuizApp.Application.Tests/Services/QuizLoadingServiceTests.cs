
using QuizApp.Application.Abstractions;
using QuizApp.Application.Services;
using QuizApp.Domain.Models;

namespace QuizApp.Application.Tests.Services;

public class QuizLoadingServiceTests
{
    private readonly FakeQuizFileReader _fileReader = new();
    private readonly FakeQuizContentHasher _contentHasher = new();

    private QuizLoadingService CreateService()
    {
        return new QuizLoadingService(
            _fileReader,
            _contentHasher);
    }

    [Fact]
    public async Task LoadAsync_ValidFile_ReturnsLoadedQuiz()
    {
        var filePath = "sample.quizpack";

        var quiz = new QuizDefinition
        {
            Id = Guid.NewGuid(),
            Title = "Test Quiz",
            DurationSeconds = 1800,
            Questions = new List<Question>()
        };

        const string expectedHash =
            "e3b0c44298fc1c149afbf4c8996fb924" +
            "27ae41e4649b934ca495991b7852b855";

        _fileReader.Result = quiz;
        _contentHasher.Result = expectedHash;

        var service = CreateService();

        var loadedQuiz = await service.LoadAsync(filePath);

        Assert.Same(quiz, loadedQuiz.Quiz);
        Assert.Equal(expectedHash, loadedQuiz.ContentHash);

        Assert.Equal(filePath, _fileReader.ReceivedFilePath);
        Assert.Equal(filePath, _contentHasher.ReceivedFilePath);
    }

    [Fact]
    public async Task LoadAsync_InvalidFilePath_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.LoadAsync(" "));

        Assert.Null(_fileReader.ReceivedFilePath);
        Assert.Null(_contentHasher.ReceivedFilePath);
    }

    [Fact]
    public async Task LoadAsync_ReaderFails_DoesNotCalculateHash()
    {
        _fileReader.Exception = new InvalidDataException(
            "Invalid QuizPack.");

        var service = CreateService();

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.LoadAsync("invalid.quizpack"));

        Assert.Null(_contentHasher.ReceivedFilePath);
    }

    [Fact]
    public async Task LoadAsync_PassesCancellationTokenToDependencies()
    {
        var quiz = new QuizDefinition
        {
            Id = Guid.NewGuid(),
            Title = "Test Quiz",
            DurationSeconds = 1800
        };

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        _fileReader.Result = quiz;
        _contentHasher.Result = "test-hash";

        var service = CreateService();

        await service.LoadAsync(
            "sample.quizpack",
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            _fileReader.ReceivedCancellationToken);

        Assert.Equal(
            cancellationToken,
            _contentHasher.ReceivedCancellationToken);
    }

    private sealed class FakeQuizFileReader : IQuizFileReader
    {
        public QuizDefinition Result { get; set; } = null!;

        public Exception? Exception { get; set; }

        public string? ReceivedFilePath { get; private set; }

        public CancellationToken ReceivedCancellationToken
        { get; private set; }

        public Task<QuizDefinition> ReadAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            ReceivedFilePath = filePath;
            ReceivedCancellationToken = cancellationToken;

            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(Result);
        }
    }

    private sealed class FakeQuizContentHasher : IQuizContentHasher
    {
        public string Result { get; set; } = string.Empty;

        public string? ReceivedFilePath { get; private set; }

        public CancellationToken ReceivedCancellationToken
        { get; private set; }

        public Task<string> ComputeHashAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            ReceivedFilePath = filePath;
            ReceivedCancellationToken = cancellationToken;

            return Task.FromResult(Result);
        }
    }
}
