
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizApp.Application.Models;
using QuizApp.Application.Models.QuizEditor;
using QuizApp.Application.Services;
using QuizApp.Application.Services.QuizEditor;
using QuizApp.Desktop.Services;
using QuizApp.Domain.Models;


namespace QuizApp.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly QuizLoadingService _quizLoadingService;
    private readonly IQuizFilePicker _filePicker;
    private readonly QuizAttemptService _quizAttemptService;
    private readonly IQuizPlayerWindowFactory _playerWindowFactory;
    private readonly IAttemptPromptService _attemptPromptService;
    private readonly IQuizEditorWindowFactory _quizEditorWindowFactory;
    private readonly QuizEditorDraftMapper _quizEditorDraftMapper;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private LoadedQuiz? _loadedQuiz;

    public string QuizTitle =>
        LoadedQuiz?.Quiz.Title ?? "No quiz loaded";

    public int QuestionCount =>
        LoadedQuiz?.Quiz.Questions.Count ?? 0;

    public string DurationDisplay =>
        LoadedQuiz is null
            ? "--:--"
            : TimeSpan.FromSeconds(
                LoadedQuiz.Quiz.DurationSeconds).ToString(@"hh\:mm\:ss");

    public MainViewModel(
     QuizLoadingService quizLoadingService,
     IQuizFilePicker filePicker,
     QuizAttemptService quizAttemptService,
     IQuizEditorWindowFactory quizEditorWindowFactory,
QuizEditorDraftMapper quizEditorDraftMapper,
     IQuizPlayerWindowFactory playerWindowFactory, IAttemptPromptService attemptPromptService
     )
    {
        _quizLoadingService = quizLoadingService;
        _filePicker = filePicker;
        _quizAttemptService = quizAttemptService;
        _playerWindowFactory = playerWindowFactory;
        _attemptPromptService = attemptPromptService
    ?? throw new ArgumentNullException(nameof(attemptPromptService));
        _quizEditorWindowFactory = quizEditorWindowFactory;
        _quizEditorDraftMapper = quizEditorDraftMapper;
    }

    [RelayCommand]
    private async Task OpenQuizAsync()
    {
        var filePath = _filePicker.PickQuizPackFile();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        await LoadQuizAsync(filePath);
    }

    private async Task LoadQuizAsync(string filePath)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            StatusMessage = "Loading quiz...";

            var loadedQuiz = await _quizLoadingService.LoadAsync(
                filePath);

            LoadedQuiz = loadedQuiz;

            OnPropertyChanged(nameof(QuizTitle));
            OnPropertyChanged(nameof(QuestionCount));
            OnPropertyChanged(nameof(DurationDisplay));

            StatusMessage = "Quiz loaded successfully";
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = "Failed to load quiz";
        }
        finally
        {
            IsBusy = false;
        }
    }



    [RelayCommand]
    private async Task StartQuizAsync()
    {
        if (LoadedQuiz is null || IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var resumeResult =
                await _quizAttemptService.FindOrResumeAttemptAsync(
                    LoadedQuiz.Quiz,
                    LoadedQuiz.ContentHash);

            QuizAttempt attempt;

            switch (resumeResult.Status)
            {
                case AttemptResumeStatus.Resumed:
                    {
                        var existingAttempt = resumeResult.Attempt
                            ?? throw new InvalidOperationException(
                                "Resume result did not contain an attempt.");

                        if (_attemptPromptService.ConfirmResumeAttempt(
                            LoadedQuiz.Quiz.Title))
                        {
                            attempt = existingAttempt;
                        }
                        else
                        {
                            if (!_attemptPromptService.ConfirmStartNewAttempt(
                                LoadedQuiz.Quiz.Title))
                            {
                                return;
                            }

                            attempt = await _quizAttemptService.StartAttemptAsync(
                                LoadedQuiz.Quiz,
                                LoadedQuiz.ContentHash);
                        }

                        break;
                    }

                case AttemptResumeStatus.NoExistingAttempt:
                    {
                        attempt = await _quizAttemptService.StartAttemptAsync(
                            LoadedQuiz.Quiz,
                            LoadedQuiz.ContentHash);

                        break;
                    }

                case AttemptResumeStatus.Expired:
                    {
                        _attemptPromptService.ShowExpiredAttempt(LoadedQuiz.Quiz.Title);

                        attempt = await _quizAttemptService.StartAttemptAsync(
                            LoadedQuiz.Quiz,
                            LoadedQuiz.ContentHash);

                        break;
                    }

                default:
                    throw new InvalidOperationException(
                        "Unknown attempt resume status.");
            }

            _playerWindowFactory.Show(LoadedQuiz, attempt);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CreateQuiz()
    {
        var draft = new QuizEditorDraft();

        _quizEditorWindowFactory.Show(draft);
    }

    private bool CanEditQuiz()
    {
        return LoadedQuiz is not null;
    }

    [RelayCommand(CanExecute = nameof(CanEditQuiz))]
    private void EditQuiz()
    {
        if (LoadedQuiz is null)
            return;

        var draft = _quizEditorDraftMapper.ToDraft(LoadedQuiz.Quiz);

        _quizEditorWindowFactory.Show(draft);
    }
}
