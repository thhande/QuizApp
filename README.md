# QuizApp

QuizApp is a Windows desktop application for creating, editing, and taking quizzes. It uses WPF for the interface, separates quiz rules from storage and UI code, and saves attempt data with SQLite.

## Requirements

- Windows
- .NET 9 SDK

## Build and run

From the repository root, restore and build the solution:

```powershell
dotnet build .\QuizApp\QuizApp.sln
```

Run the desktop app:

```powershell
dotnet run --project .\QuizApp\QuizApp.Desktop.csproj
```

Run the automated tests:

```powershell
dotnet test .\QuizApp\QuizApp.sln
```

## Projects

| Project | Responsibility |
| --- | --- |
| `QuizApp` | WPF desktop UI, views, view models, and dependency registration. The solution file is also here. |
| `QuizApp.Domain` | Quiz and attempt models, enums, and scoring rules. |
| `QuizApp.Application` | Quiz loading, attempt workflows, editor operations, and application contracts. |
| `QuizApp.Infrastructure` | Quiz-pack file handling, content hashing, and SQLite persistence. |
| `QuizApp.Domain.Tests` | Tests for domain behavior. |
| `QuizApp.Application.Tests` | Tests for application workflows and editor logic. |
| `QuizApp.Infrastructure.Tests` | Tests for file formats, hashing, and persistence. |

For a folder-by-folder view, see [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md).

## Layout

The solution file lives in `QuizApp/`. The other project folders sit beside it at the repository root, and the solution references them as sibling directories.
