using QuizApp.Domain.Models;

namespace QuizApp.Application.Abstractions;

public interface IQuizPackWriter
{
    Task WriteAsync(
        QuizDefinition quiz,
        string filePath,
        CancellationToken cancellationToken = default);
}