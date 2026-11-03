using Microsoft.Win32;

namespace QuizApp.Desktop.Services;

public class WpfQuizFilePicker : IQuizFilePicker
{
    public string? PickQuizPackFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open QuizPack",
            Filter = "QuizPack files (*.quizpack)|*.quizpack",
            DefaultExt = ".quizpack",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }
}