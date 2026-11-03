
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

using QuizApp.Desktop.DependencyInjection;
using QuizApp.Infrastructure.Persistance.Sqlite;


namespace QuizApp.Desktop;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AddQuizAppServices();

        _serviceProvider = services.BuildServiceProvider();

        try
        {
            var databaseInitializer =
                _serviceProvider.GetRequiredService<DatabaseInitializer>();

            await databaseInitializer.InitializeAsync();

            var mainWindow =
                _serviceProvider.GetRequiredService<MainWindow>();

            mainWindow.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Application startup failed:\n{exception.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();

        base.OnExit(e);
    }
}
