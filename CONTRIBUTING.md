# Contributing to VapourSynth Studio

Thank you for your interest in contributing to VapourSynth Studio!

## Development Setup

### Prerequisites

- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- Git

### Getting Started

1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR-USERNAME/vapoursynth-gui.git
   cd vapoursynth-gui
   ```

3. Build the solution:
   ```bash
   dotnet build src/gui/VapourSynthPortable.sln
   ```

4. Run the tests:
   ```bash
   dotnet test src/gui/VapourSynthPortable.Tests
   ```

5. Run the application:
   ```bash
   dotnet run --project src/gui/VapourSynthPortable
   ```

## Project Structure

```
src/gui/
├── VapourSynthPortable/           # Main WPF application
│   ├── Controls/                  # Custom WPF controls
│   ├── Helpers/                   # Converters and utilities
│   ├── Models/                    # Data models
│   ├── Pages/                     # Page views (XAML + code-behind)
│   ├── Services/                  # Business logic services
│   ├── Styles/                    # XAML resource dictionaries
│   └── ViewModels/                # MVVM ViewModels
└── VapourSynthPortable.Tests/     # Unit tests
```

## Coding Standards

### General Guidelines

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful, descriptive names
- Keep methods small and focused
- Prefer composition over inheritance

### MVVM Pattern

- Views (Pages) should contain minimal code-behind
- ViewModels handle all business logic
- Use `CommunityToolkit.Mvvm` for property notification and commands
- Services are injected via constructor

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
type(scope): description

[optional body]

[optional footer]
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code formatting (no logic changes)
- `refactor`: Code restructuring
- `test`: Adding/updating tests
- `chore`: Maintenance tasks

Examples:
```
feat(color): add RGB parade scope display
fix(export): handle missing output directory
docs: update keyboard shortcuts table
test(services): add LutService unit tests
```

### Pull Request Process

1. Create a feature branch from `master`
2. Make your changes
3. Write/update tests for your changes
4. Ensure all tests pass locally
5. Update documentation if needed
6. Submit a pull request

### Testing

- All new code should have unit tests
- Aim for >70% code coverage
- Use xUnit for tests
- Use Moq for mocking

Run tests with coverage:
```bash
dotnet test src/gui/VapourSynthPortable.Tests --collect:"XPlat Code Coverage"
```

## Architecture

### Services

Services are singleton classes that provide specific functionality:

- `IMediaPoolService` - Manages imported media
- `ISettingsService` - Handles application settings
- `IEffectService` - Manages VapourSynth effects
- `IVapourSynthService` - Interfaces with VapourSynth

### Commands (Undo/Redo)

All undoable operations implement `IUndoAction`:

```csharp
public class MyCommand : IUndoAction
{
    public string Description => "My Action";
    public void Execute() { /* do action */ }
    public void Undo() { /* undo action */ }
}
```

### Validation

Use WPF validation rules for input validation:

```csharp
public class MyRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        // validation logic
    }
}
```

## Reporting Issues

When reporting issues, please include:

1. Steps to reproduce
2. Expected behavior
3. Actual behavior
4. System information (Windows version, .NET version)
5. Error messages or crash logs

## Feature Requests

Feature requests are welcome! Please:

1. Check existing issues first
2. Describe the use case
3. Provide mockups if applicable

## License

By contributing, you agree that your contributions will be licensed under the project's license.
