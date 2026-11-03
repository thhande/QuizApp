
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizApp.Application.Models;
using QuizApp.Application.Services;
using QuizApp.Desktop.Services;
using QuizApp.Desktop.ViewModels.Questions;
using QuizApp.Domain.Models;

namespace QuizApp.Desktop.ViewModels;

public partial class QuizPlayerViewModel : ObservableObject
{
    private readonly QuizAttemptService _attemptService;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly DispatcherTimer _timer;
    private readonly TimeProvider _timeProvider;
    private readonly IQuizResultWindowFactory _resultWindowFactory;

    public LoadedQuiz LoadedQuiz { get; }
    public QuizAttempt Attempt { get; }

    public ObservableCollection<QuestionViewModel> Questions { get; }
        = [];

    [ObservableProperty]
    private int _currentQuestionIndex;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isExpiring;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private bool _isExpired;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _remainingTimeDisplay = "--:--";

    public QuestionViewModel? CurrentQuestion =>
        CurrentQuestionIndex >= 0 &&
        CurrentQuestionIndex < Questions.Count
            ? Questions[CurrentQuestionIndex]
            : null;

    public int QuestionCount => Questions.Count;

    public int DisplayQuestionNumber => CurrentQuestionIndex + 1;

    public bool CanGoPrevious => CurrentQuestionIndex > 0;

    public bool CanGoNext =>
        CurrentQuestionIndex < Questions.Count - 1;

    public bool CanInteract =>
        !IsCompleted && !IsExpiring;

    public string QuizTitle => LoadedQuiz.Quiz.Title;

    public string DeadlineDisplay =>
        Attempt.DeadlineUtc.ToLocalTime().ToString("HH:mm:ss");

    public QuizPlayerViewModel(
    LoadedQuiz loadedQuiz,
    QuizAttempt attempt,
    QuizAttemptService attemptService,
    IQuizResultWindowFactory resultWindowFactory,
    TimeProvider? timeProvider = null)
    {
        LoadedQuiz = loadedQuiz
            ?? throw new ArgumentNullException(nameof(loadedQuiz));

        Attempt = attempt
            ?? throw new ArgumentNullException(nameof(attempt));

        _attemptService = attemptService
            ?? throw new ArgumentNullException(nameof(attemptService));

        _timeProvider = timeProvider ?? TimeProvider.System;
        _resultWindowFactory = resultWindowFactory
    ?? throw new ArgumentNullException(nameof(resultWindowFactory));

        InitializeQuestions();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _timer.Tick += OnTimerTick;

        UpdateRemainingTime();

        if (!IsCompleted)
        {
            _timer.Start();
        }
    }

    private void InitializeQuestions()
    {
        for (var i = 0; i < LoadedQuiz.Quiz.Questions.Count; i++)
        {
            var question = LoadedQuiz.Quiz.Questions[i];

            var existingAnswer = Attempt.Answers.FirstOrDefault(
                answer => answer.QuestionId == question.Id);

            Questions.Add(new QuestionViewModel(
                question,
                i + 1,
                existingAnswer?.SelectedOptionIds));
        }

        if (Questions.Count > 0)
        {
            Questions[0].IsCurrent = true;
        }

        NotifyNavigationProperties();
    }

    partial void OnCurrentQuestionIndexChanged(int value)
    {
        for (var i = 0; i < Questions.Count; i++)
        {
            Questions[i].IsCurrent = i == value;
        }

        NotifyNavigationProperties();
    }

    partial void OnIsCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInteract));
        SelectOptionCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsExpiringChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInteract));
        SelectOptionCommand.NotifyCanExecuteChanged();
    }

    private void NotifyNavigationProperties()
    {
        OnPropertyChanged(nameof(CurrentQuestion));
        OnPropertyChanged(nameof(DisplayQuestionNumber));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));

        PreviousQuestionCommand.NotifyCanExecuteChanged();
        NextQuestionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void PreviousQuestion()
    {
        CurrentQuestionIndex--;
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextQuestion()
    {
        CurrentQuestionIndex++;
    }

    [RelayCommand]
    private void GoToQuestion(QuestionViewModel question)
    {
        var index = Questions.IndexOf(question);

        if (index >= 0)
        {
            CurrentQuestionIndex = index;
        }
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task SelectOptionAsync(
        QuestionOptionViewModel option)
    {
        if (CurrentQuestion is null || !CanInteract)
        {
            return;
        }

        ErrorMessage = null;

        try
        {
            CurrentQuestion.SelectOption(
                option.Id,
                option.IsSelected);

            await SaveCurrentAnswerAsync();

            StatusMessage = "Answer saved";
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = "Failed to save answer";
        }
    }

    private async Task SaveCurrentAnswerAsync()
    {
        var question = CurrentQuestion;

        if (question is null || !CanInteract)
        {
            return;
        }

        await _saveLock.WaitAsync();

        try
        {
            if (!CanInteract)
            {
                return;
            }

            IsSaving = true;

            var answer = new Answer
            {
                QuestionId = question.Id,
                SelectedOptionIds = question.GetSelectedOptionIds()
            };

            await _attemptService.SaveAnswerAsync(
                Attempt.Id,
                LoadedQuiz.Quiz,
                LoadedQuiz.ContentHash,
                answer);
        }
        finally
        {
            IsSaving = false;
            _saveLock.Release();
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        UpdateRemainingTime();
    }

    private void UpdateRemainingTime()
    {
        var remaining = Attempt.DeadlineUtc -
    _timeProvider.GetUtcNow().UtcDateTime;

        if (remaining <= TimeSpan.Zero)
        {
            RemainingTimeDisplay = "00:00";
            _timer.Stop();

            if (!IsCompleted && !IsExpiring)
            {
                _ = ExpireAsync();
            }

            return;
        }

        RemainingTimeDisplay = remaining.TotalHours >= 1
            ? remaining.ToString(@"hh\:mm\:ss")
            : remaining.ToString(@"mm\:ss");
    }


    private async Task ExpireAsync()
    {
        if (IsCompleted || IsExpiring)
        {
            return;
        }

        IsExpiring = true;
        _timer.Stop();

        try
        {
            await _saveLock.WaitAsync();

            try
            {
                await _attemptService.ExpireAttemptAsync(
                    Attempt.Id,
                    LoadedQuiz.Quiz,
                    LoadedQuiz.ContentHash);

                IsCompleted = true;
                IsExpired = true;
                StatusMessage = "Time expired. Quiz submitted automatically.";
                ErrorMessage = null;
            }
            finally
            {
                _saveLock.Release();
            }

            // Open the result screen after the attempt is finalized.
            await ShowResultAsync();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = "Failed to finalize expired attempt";
        }
        finally
        {
            IsExpiring = false;
        }
    }


    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task SubmitAsync()
    {
        if (IsCompleted || IsExpiring)
        {
            return;
        }

        IsExpiring = true;
        _timer.Stop();

        try
        {
            await _saveLock.WaitAsync();

            try
            {
                await _attemptService.SubmitAttemptAsync(
                    Attempt.Id,
                    LoadedQuiz.Quiz,
                    LoadedQuiz.ContentHash);

                IsCompleted = true;
                StatusMessage = "Quiz submitted successfully.";
                ErrorMessage = null;
            }
            finally
            {
                _saveLock.Release();
            }

            await ShowResultAsync();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = "Failed to submit quiz";

            // Submission may fail for reasons other than expiration.
            // Refresh the timer so the user isn't left with a frozen UI.
            if (!IsCompleted)
            {
                _timer.Start();
            }
        }
        finally
        {
            IsExpiring = false;
        }
    }




    private async Task ShowResultAsync()
    {
        var review = await _attemptService.GetAttemptReviewAsync(
            Attempt.Id,
            LoadedQuiz.Quiz,
            LoadedQuiz.ContentHash);

        _resultWindowFactory.Show(LoadedQuiz, review);
    }






    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _saveLock.Dispose();
    }
}
