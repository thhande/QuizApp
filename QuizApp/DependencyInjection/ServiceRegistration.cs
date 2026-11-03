using Microsoft.Extensions.DependencyInjection;
using QuizApp.Application.Abstractions;
using QuizApp.Application.Services;
using QuizApp.Application.Services.QuizEditor;
using QuizApp.Desktop.Services;
using QuizApp.Desktop.ViewModels;
using QuizApp.Desktop.Views;
using QuizApp.Domain.Services;
using QuizApp.Infrastructure.FileFormats.QuizPack;
using QuizApp.Infrastructure.Hashing;
using QuizApp.Infrastructure.Persistance.Sqlite;
using QuizApp.Infrastructure.Persistence.Repositories;

namespace QuizApp.Desktop.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddQuizAppServices(
        this IServiceCollection services)
    {
        // Domain
        services.AddSingleton<ScoringService>();

        // Application abstractions
        services.AddSingleton<IQuizFileReader, QuizPackReader>();
        services.AddSingleton<IQuizContentHasher, Sha256QuizContentHasher>();
        services.AddSingleton<IQuizAttemptRepository, QuizAttemptRepository>();

        // Application services
        services.AddTransient<QuizLoadingService>();
        services.AddTransient<QuizAttemptService>();

        // Infrastructure
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<DatabaseInitializer>();

        // System services
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        // Presentation
        services.AddSingleton<IQuizFilePicker, WpfQuizFilePicker>();
        services.AddTransient<MainViewModel>();
        services.AddSingleton<MainWindow>();

        // Player presentation
        services.AddSingleton<IQuizPlayerWindowFactory,
            WpfQuizPlayerWindowFactory>();

        services.AddTransient<QuizPlayerViewModel>();
        services.AddTransient<QuizPlayerWindow>();
        services.AddSingleton<IAttemptPromptService, WpfAttemptPromptService>();

        services.AddSingleton<IQuizResultWindowFactory, WpfQuizResultWindowFactory>();
        services.AddSingleton<IQuizPackWriter, QuizPackWriter>();

        services.AddSingleton<IQuizEditorWindowFactory, WpfQuizEditorWindowFactory>();

        services.AddSingleton<QuizEditorDraftValidator>();
        services.AddSingleton<QuizEditorDraftMapper>();

        return services;
    }
}
