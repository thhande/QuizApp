
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QuizApp.Application.Models;
using QuizApp.Desktop.ViewModels;
using QuizApp.Desktop.Views;
using QuizApp.Domain.Models;

namespace QuizApp.Desktop.Services;

public class WpfQuizPlayerWindowFactory : IQuizPlayerWindowFactory
{
    private readonly IServiceProvider _serviceProvider;

    public WpfQuizPlayerWindowFactory(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Show(
        LoadedQuiz loadedQuiz,
        QuizAttempt attempt)
    {
        var viewModel =
            ActivatorUtilities.CreateInstance<QuizPlayerViewModel>(
                _serviceProvider,
                loadedQuiz,
                attempt);

        var window = new QuizPlayerWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.Show();
    }
}
