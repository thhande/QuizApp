
namespace QuizApp.Desktop.Services;

public interface IAttemptPromptService
{
    bool ConfirmResumeAttempt(string quizTitle);

    bool ConfirmStartNewAttempt(string quizTitle);

    void ShowExpiredAttempt(string quizTitle);
}
