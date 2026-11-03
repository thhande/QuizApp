
using System.Windows;
using QuizApp.Application.Models;
using QuizApp.Desktop.ViewModels.Results;
using QuizApp.Desktop.Views;

namespace QuizApp.Desktop.Services;

public class WpfQuizResultWindowFactory : IQuizResultWindowFactory
{
    public void Show(LoadedQuiz loadedQuiz, QuizAttemptReview review)
    {
        var viewModel = new QuizResultViewModel(
            review,
            loadedQuiz.Quiz.Title);

        var window = new QuizResultWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.Show();
    }
}
