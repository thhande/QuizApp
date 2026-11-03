namespace QuizApp.Application.Abstractions;

public interface IQuizContentHasher
{
    Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}