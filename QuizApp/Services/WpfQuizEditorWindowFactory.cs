using Microsoft.Extensions.DependencyInjection;
using QuizApp.Application.Models.QuizEditor;
using QuizApp.Desktop.ViewModels.Editor;
using QuizApp.Desktop.Views;

namespace QuizApp.Desktop.Services;

public sealed class WpfQuizEditorWindowFactory : IQuizEditorWindowFactory
{
    private readonly IServiceProvider _serviceProvider;

    public WpfQuizEditorWindowFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Show(QuizEditorDraft draft)
    {
        var viewModel = ActivatorUtilities.CreateInstance<QuizEditorViewModel>(
            _serviceProvider,
            draft);

        var window = new QuizEditorWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.Show();
    }
}