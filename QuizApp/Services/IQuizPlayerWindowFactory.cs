using QuizApp.Application.Models;
using QuizApp.Domain.Models;

namespace QuizApp.Desktop.Services;

public interface IQuizPlayerWindowFactory
{
    void Show(
        LoadedQuiz loadedQuiz,
        QuizAttempt attempt);
}