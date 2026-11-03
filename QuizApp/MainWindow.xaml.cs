using System.Windows;
using QuizApp.Desktop.ViewModels;

namespace QuizApp.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }
}