
using QuizApp.Application.Models;
using QuizApp.Domain.Models;

namespace QuizApp.Desktop.Services;

public interface IQuizResultWindowFactory
{
    void Show(LoadedQuiz loadedQuiz, QuizAttemptReview review);
}
