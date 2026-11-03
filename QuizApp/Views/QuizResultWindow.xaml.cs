
using System.Windows;
using QuizApp.Desktop.ViewModels.Results;

namespace QuizApp.Desktop.Views;

public partial class QuizResultWindow : Window
{
    public QuizResultWindow(QuizResultViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
