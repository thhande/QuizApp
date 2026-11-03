
using System.Windows;

namespace QuizApp.Desktop.Services;

public class WpfAttemptPromptService : IAttemptPromptService
{
    public bool ConfirmResumeAttempt(string quizTitle)
    {
        var result = MessageBox.Show(
            $"An unfinished attempt for \"{quizTitle}\" was found.\n\n" +
            "Would you like to resume it?",
            "Resume Quiz",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    public bool ConfirmStartNewAttempt(string quizTitle)
    {
        var result = MessageBox.Show(
            $"Start a new attempt for \"{quizTitle}\"?\n\n" +
            "Your previous unfinished attempt will remain saved, " +
            "but this new attempt will have a separate result.",
            "Start New Attempt",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }

    public void ShowExpiredAttempt(string quizTitle)
    {
        MessageBox.Show(
            $"Your previous attempt for \"{quizTitle}\" has expired. " +
            "Its result has been saved. A new attempt will be started.",
            "Attempt Expired",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
