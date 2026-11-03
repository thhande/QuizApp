using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;

namespace QuizApp.Infrastructure.Persistance.Sqlite;

public class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string? databasePath = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            var appDataPath = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            databasePath = Path.Combine(
                appDataPath,
                "QuizApp",
                "quizapp.db");
        }

        var databaseDirectory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrEmpty(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Pooling = true,
            DefaultTimeout = 5
        };

        _connectionString = builder.ToString();
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    public async Task<SqliteConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection();

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}