using System.Windows;
using QuizApp.Desktop.ViewModels.Editor;

namespace QuizApp.Desktop.Views;

public partial class QuizEditorWindow : Window
{
    public QuizEditorWindow(QuizEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}