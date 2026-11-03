
using QuizApp.Domain.Models;

namespace QuizApp.Application.Models;

public sealed record LoadedQuiz(
    QuizDefinition Quiz,
    string ContentHash);
