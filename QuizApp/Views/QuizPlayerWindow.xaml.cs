using System.Windows;
using QuizApp.Desktop.ViewModels;

namespace QuizApp.Desktop.Views;

public partial class QuizPlayerWindow : Window
{
    private readonly QuizPlayerViewModel _viewModel;

    public QuizPlayerWindow(QuizPlayerViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Closed -= OnWindowClosed;
        _viewModel.Dispose();
    }
}