# Contributing to VapourSynth Studio

Thank you for your interest in contributing! This document provides guidelines for contributing to VapourSynth Studio.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Set up the development environment** (see [DEVELOPMENT.md](DEVELOPMENT.md))
4. **Create a feature branch** from `master`

## Development Workflow

### Branch Naming
Use descriptive branch names with prefixes:
- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation changes
- `refactor/` - Code refactoring

Examples:
```bash
git checkout -b feature/audio-waveform
git checkout -b fix/export-crash
git checkout -b docs/architecture-update
```

### Commit Messages
We use conventional commits for clear history:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:**
- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation
- `style` - Formatting (no code change)
- `refactor` - Code restructuring
- `test` - Adding tests
- `chore` - Maintenance tasks

**Examples:**
```
feat(edit): Add audio waveform display to timeline

fix(export): Resolve crash when output path contains spaces

docs(readme): Update installation instructions for Windows 11
```

### Code Style

#### C# Guidelines
- Use C# 12 features where appropriate
- File-scoped namespaces
- Primary constructors for simple classes
- Use `var` for obvious types
- Async methods end with `Async` suffix
- Use `_` prefix for private fields

```csharp
// Good
namespace VapourSynthPortable.Services;

public class ExampleService(ILogger logger)
{
    private readonly ILogger _logger = logger;

    public async Task<Result> ProcessAsync(CancellationToken ct = default)
    {
        var items = await GetItemsAsync(ct);
        return Result.Success(items);
    }
}
```

#### XAML Guidelines
- Use StaticResource for reusable resources
- Prefer styles over inline properties
- Use Grid for layout, StackPanel for simple lists
- Always set `x:Name` for controls accessed in code-behind

```xml
<!-- Good -->
<Button Style="{StaticResource PrimaryButton}"
        Content="{Binding ButtonText}"
        Command="{Binding ClickCommand}"/>
```

### Testing

#### Unit Tests
- Add tests for new service methods
- Use xUnit with FluentAssertions
- Mock dependencies with NSubstitute

```csharp
[Fact]
public async Task ProcessAsync_WhenSuccessful_ReturnsResult()
{
    // Arrange
    var service = new ExampleService();

    // Act
    var result = await service.ProcessAsync();

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
}
```

#### Manual Testing
- Test all affected features before submitting PR
- Update `docs/MANUAL_TEST_CHECKLIST.md` for new features
- Include steps to reproduce in bug fix PRs

### Pull Request Process

1. **Update your branch** with latest `master`
   ```bash
   git fetch origin
   git rebase origin/master
   ```

2. **Run tests and build**
   ```bash
   dotnet test
   dotnet build --configuration Release
   ```

3. **Create Pull Request**
   - Use descriptive title
   - Reference any related issues
   - Include screenshots for UI changes
   - Fill out PR template

4. **Address review feedback**
   - Respond to all comments
   - Push fixes as new commits
   - Request re-review when ready

### PR Template

```markdown
## Summary
Brief description of changes

## Changes
- Change 1
- Change 2

## Test Plan
- [ ] Step to test feature 1
- [ ] Step to test feature 2

## Screenshots (if UI changes)
Before | After

## Related Issues
Closes #123
```

## Types of Contributions

### Bug Reports
- Use the GitHub issue template
- Include reproduction steps
- Attach logs from `%LocalAppData%\VapourSynthStudio\logs`
- Include system info (Windows version, GPU, .NET version)

### Feature Requests
- Check existing issues first
- Describe the use case
- Propose implementation approach if possible

### Code Contributions
- Small PRs are easier to review
- Focus on one feature/fix per PR
- Include tests for new code
- Update documentation if needed

### Documentation
- Fix typos and improve clarity
- Add examples and screenshots
- Keep docs in sync with code changes

## Architecture Guidelines

### Adding a New Page
1. Create Page XAML in `Pages/`
2. Create ViewModel in `ViewModels/`
3. Register ViewModel in `App.xaml.cs`
4. Add navigation in `MainWindow`
5. See `/new-page` command for scaffolding

### Adding a New Service
1. Create Service class in `Services/`
2. Register in DI container (`App.xaml.cs`)
3. Use constructor injection in ViewModels
4. See `/new-service` command for scaffolding

### Adding a VapourSynth Effect
1. Add effect definition to `EffectService.cs`
2. Create VapourSynth script template
3. Add parameter definitions
4. Update UI to expose the effect

## Code of Conduct

- Be respectful and inclusive
- Focus on constructive feedback
- Help newcomers learn the codebase
- Report unacceptable behavior to maintainers

## Questions?

- Open a GitHub Discussion for questions
- Tag issues with `question` label
- Check existing issues and discussions first

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
