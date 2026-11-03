
using QuizApp.Application.Abstractions;
using QuizApp.Application.Models;

namespace QuizApp.Application.Services;

public class QuizLoadingService
{
    private readonly IQuizFileReader _fileReader;
    private readonly IQuizContentHasher _contentHasher;

    public QuizLoadingService(
        IQuizFileReader fileReader,
        IQuizContentHasher contentHasher)
    {
        _fileReader = fileReader
            ?? throw new ArgumentNullException(nameof(fileReader));

        _contentHasher = contentHasher
            ?? throw new ArgumentNullException(nameof(contentHasher));
    }

    public async Task<LoadedQuiz> LoadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var quiz = await _fileReader.ReadAsync(
            filePath,
            cancellationToken);

        var contentHash = await _contentHasher.ComputeHashAsync(
            filePath,
            cancellationToken);

        return new LoadedQuiz(quiz, contentHash);
    }
}
