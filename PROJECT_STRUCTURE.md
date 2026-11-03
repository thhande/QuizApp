# QuizApp project structure

The workspace is a .NET solution split into a WPF desktop app, application services, domain logic, persistence/file-format infrastructure, and tests.

```text
E:\Net\
├── .gitignore
├── PROJECT_STRUCTURE.md
├── QuizApp/                         # WPF desktop app and QuizApp.sln
│   ├── DependencyInjection/
│   ├── Services/                    # WPF adapters and window/file-picker factories
│   ├── ViewModels/
│   │   ├── Editor/
│   │   ├── Questions/
│   │   └── Results/
│   └── Views/
├── QuizApp.Domain/                  # Core quiz and attempt models, enums, scoring
├── QuizApp.Application/             # Use cases, contracts, models, editor validation
├── QuizApp.Infrastructure/          # Quiz-pack format, hashing, SQLite persistence
├── QuizApp.Domain.Tests/
├── QuizApp.Application.Tests/
└── QuizApp.Infrastructure.Tests/
```

The solution file currently lives in `QuizApp/` and references the other projects as sibling directories. Generated `bin/`, `obj/`, Visual Studio state, local SQLite databases, test results, and per-user settings are excluded by `.gitignore`.
