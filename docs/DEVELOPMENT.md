# VapourSynth Studio - Development Guide

This guide covers setting up and working with the VapourSynth Studio development environment.

## Prerequisites

### Required
- **Windows 10/11** (x64)
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** or **VS Code** with C# extension
- **Git** - [Download](https://git-scm.com/)

### Optional (for full functionality)
- **Visual C++ Redistributable** - [Download](https://aka.ms/vs/17/release/vc_redist.x64.exe)
- **PowerShell 7+** - [Download](https://github.com/PowerShell/PowerShell/releases)

## Initial Setup

### 1. Clone the Repository

```powershell
git clone https://github.com/Atomic-Trash/vapoursynth-gui.git
cd vapoursynth-gui
```

### 2. Build the Application

```powershell
# Restore NuGet packages and build
dotnet build src/gui/VapourSynthPortable.sln

# Or build Release configuration
dotnet build src/gui/VapourSynthPortable.sln --configuration Release
```

### 3. Run the Application

```powershell
# Run from command line
dotnet run --project src/gui/VapourSynthPortable

# Or use the IDE's run button
```

### 4. Set Up VapourSynth Distribution (Optional)

For full VapourSynth functionality:

```powershell
# Build portable VapourSynth with plugins
.\scripts\build\Build-Portable.ps1 -PluginSet standard
```

### 5. Install libmpv for Video Playback (Optional)

```powershell
.\scripts\util\install-mpv.ps1
```

## Development Environment

### Visual Studio 2022

1. Open `src/gui/VapourSynthPortable.sln`
2. Set `VapourSynthPortable` as startup project
3. Press F5 to debug

**Recommended Extensions:**
- GitHub Copilot
- ReSharper (optional)

### VS Code

1. Open the project folder
2. Install C# extension
3. Press F5 to debug (uses `.vscode/launch.json`)

**Recommended Extensions:**
- C# Dev Kit
- XAML Styler
- GitLens

### Claude Code

The project includes custom Claude Code commands:

```bash
# Build the application
/build

# Run tests
/test

# Run the application
/run

# Check system environment
/diagnose

# See all available commands
# Look in .claude/commands/
```

## Project Structure

```
vapoursynth-gui/
├── src/gui/
│   ├── VapourSynthPortable/        # Main application
│   │   ├── App.xaml                # Entry point, DI setup
│   │   ├── MainWindow.xaml         # Application shell
│   │   ├── Pages/                  # UI pages
│   │   ├── ViewModels/             # Page logic
│   │   ├── Models/                 # Data structures
│   │   ├── Services/               # Business logic
│   │   ├── Controls/               # Custom controls
│   │   └── Helpers/                # Utilities
│   └── VapourSynthPortable.Tests/  # Unit tests
├── scripts/                        # Build & utility scripts
├── docs/                           # Documentation
├── templates/                      # VapourSynth script templates
├── dist/                           # Build output (generated)
└── .claude/                        # Claude Code configuration
```

## Common Development Tasks

### Adding a New Feature

1. **Plan the feature** - What pages/services are affected?
2. **Create branch** - `git checkout -b feature/my-feature`
3. **Implement** - Follow MVVM pattern
4. **Test** - Manual testing + unit tests
5. **Document** - Update relevant docs
6. **PR** - Create pull request

### Adding a New Page

Use the Claude Code command:
```bash
/new-page Settings
```

Or manually:
1. Create `Pages/SettingsPage.xaml` and `.xaml.cs`
2. Create `ViewModels/SettingsViewModel.cs`
3. Register ViewModel in `App.xaml.cs`
4. Add navigation in `MainWindow.xaml` and `.xaml.cs`

### Adding a New Service

Use the Claude Code command:
```bash
/new-service Cache
```

Or manually:
1. Create `Services/CacheService.cs`
2. Register in `App.xaml.cs` (singleton or transient)
3. Inject via constructor in ViewModels

### Running Tests

```powershell
# Run all tests
dotnet test src/gui/VapourSynthPortable.Tests

# Run specific tests
dotnet test --filter "ClassName~MediaTest"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Debugging

**Application Debugging:**
- Set breakpoints in Visual Studio/VS Code
- Use Debug configuration
- Check Output window for logs

**VapourSynth Script Debugging:**
- Check generated scripts in temp folder
- Run vspipe manually to isolate issues
- Use verbose logging in VapourSynthService

**Logs Location:**
```
%LocalAppData%\VapourSynthStudio\logs\
```

## Code Patterns

### ViewModel Pattern

```csharp
public partial class ExampleViewModel : ObservableObject
{
    private readonly ExampleService _service;
    private readonly ILogger _logger;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isProcessing;

    public ExampleViewModel(ExampleService service)
    {
        _service = service;
        _logger = Log.ForContext<ExampleViewModel>();
    }

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private async Task ProcessAsync()
    {
        IsProcessing = true;
        Status = "Processing...";

        try
        {
            await _service.ProcessAsync();
            Status = "Complete";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Processing failed");
            Status = "Error: " + ex.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private bool CanProcess() => !IsProcessing;
}
```

### Async Service Pattern

```csharp
public class ExampleService
{
    private readonly ILogger _logger = Log.ForContext<ExampleService>();

    public async Task<Result<Data>> ProcessAsync(
        Input input,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        _logger.Information("Starting process for {Input}", input.Name);

        try
        {
            ct.ThrowIfCancellationRequested();

            for (int i = 0; i < 100; i++)
            {
                ct.ThrowIfCancellationRequested();
                await DoWorkAsync(i, ct);
                progress?.Report(i);
            }

            return Result.Success(new Data());
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Process cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Process failed");
            return Result.Failure<Data>(ex.Message);
        }
    }
}
```

### XAML Data Binding

```xml
<Page DataContext="{Binding Source={StaticResource Locator}, Path=ExampleVM}">
    <Grid>
        <!-- Property binding -->
        <TextBlock Text="{Binding Status}"/>

        <!-- Command binding -->
        <Button Content="Process"
                Command="{Binding ProcessCommand}"
                IsEnabled="{Binding IsNotProcessing}"/>

        <!-- Collection binding -->
        <ListView ItemsSource="{Binding Items}"
                  SelectedItem="{Binding SelectedItem}"/>
    </Grid>
</Page>
```

## Troubleshooting

### Build Errors

**Missing SDK:**
```
error MSB4019: The imported project "...\Microsoft.NET.Sdk.csproj" was not found.
```
Solution: Install .NET 8 SDK

**Package Restore Failed:**
```powershell
dotnet restore src/gui/VapourSynthPortable.sln
```

### Runtime Errors

**VapourSynth not found:**
- Run `.\scripts\build\Build-Portable.ps1`
- Check `dist/vapoursynth/` exists

**libmpv not found:**
- Run `.\scripts\util\install-mpv.ps1`
- Check `dist/mpv/` exists

**GPU acceleration not working:**
- Install latest GPU drivers
- Check NVENC/AMF support with `ffmpeg -encoders`

### Check Environment

Use the diagnostic command:
```bash
/diagnose
```

## Performance Profiling

### CPU Profiling
1. Use Visual Studio's Performance Profiler
2. Select CPU Usage
3. Profile hot paths in encoding/processing

### Memory Profiling
1. Use dotMemory or VS Memory Profiler
2. Check for memory leaks in long operations
3. Monitor frame cache size

### GPU Profiling
- Check GPU utilization during encoding
- Monitor VRAM usage for large previews

## Useful Links

- [.NET 8 Documentation](https://docs.microsoft.com/dotnet/)
- [WPF Documentation](https://docs.microsoft.com/dotnet/desktop/wpf/)
- [CommunityToolkit.Mvvm](https://docs.microsoft.com/windows/communitytoolkit/mvvm/)
- [VapourSynth Documentation](http://www.vapoursynth.com/doc/)
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)
