using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

/// <summary>
/// ViewModel for the main application window
/// </summary>
public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private bool _disposed;
    private readonly IProjectService _projectService;
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;

    public MainWindowViewModel(IProjectService projectService, ISettingsService settingsService, INavigationService navigationService)
    {
        _projectService = projectService;
        _settingsService = settingsService;
        _navigationService = navigationService;

        // Subscribe to navigation changes
        _navigationService.PageChanged += OnPageChanged;

        // Initialize with a new project
        _currentProject = _projectService.CreateNew();
        UpdateWindowTitle();
        LoadRecentProjects();
    }

    // Parameterless constructor for XAML designer
    public MainWindowViewModel() : this(
        App.Services?.GetService(typeof(IProjectService)) as IProjectService ?? new ProjectService(),
        App.Services?.GetService(typeof(ISettingsService)) as ISettingsService ?? new SettingsService(),
        App.Services?.GetService(typeof(INavigationService)) as INavigationService ?? new NavigationService())
    {
    }

    private void OnPageChanged(object? sender, PageChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    #region Project State

    private Project _currentProject;

    /// <summary>
    /// The current project being edited
    /// </summary>
    public Project CurrentProject
    {
        get => _currentProject;
        private set
        {
            if (SetProperty(ref _currentProject, value))
            {
                UpdateWindowTitle();
                OnPropertyChanged(nameof(HasUnsavedChanges));
            }
        }
    }

    /// <summary>
    /// Whether the current project has unsaved changes
    /// </summary>
    public bool HasUnsavedChanges => _currentProject?.IsDirty ?? false;

    /// <summary>
    /// Marks the project as dirty (has unsaved changes)
    /// </summary>
    public void MarkProjectDirty()
    {
        _currentProject?.MarkDirty();
        OnPropertyChanged(nameof(HasUnsavedChanges));
        UpdateWindowTitle();
    }

    #endregion

    #region Window Title

    [ObservableProperty]
    private string _windowTitle = "VapourSynth Studio";

    private void UpdateWindowTitle()
    {
        WindowTitle = $"VapourSynth Studio - {_currentProject?.DisplayName ?? "Untitled"}";
    }

    #endregion

    #region Version Information

    /// <summary>
    /// The VapourSynth version string
    /// </summary>
    [ObservableProperty]
    private string _vapourSynthVersion = "VapourSynth R70";

    /// <summary>
    /// The Python version string
    /// </summary>
    [ObservableProperty]
    private string _pythonVersion = "Python 3.12";

    /// <summary>
    /// The FFmpeg status string
    /// </summary>
    [ObservableProperty]
    private string _ffmpegStatus = "FFmpeg Ready";

    /// <summary>
    /// Whether FFmpeg is available
    /// </summary>
    [ObservableProperty]
    private bool _isFFmpegReady = true;

    /// <summary>
    /// The application status message
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    #endregion

    #region Recent Projects

    [ObservableProperty]
    private ObservableCollection<RecentProjectItem> _recentProjects = [];

    [ObservableProperty]
    private bool _hasRecentProjects;

    private void LoadRecentProjects()
    {
        var recent = _projectService.GetRecentProjects();
        RecentProjects.Clear();

        foreach (var path in recent)
        {
            if (File.Exists(path))
            {
                RecentProjects.Add(new RecentProjectItem
                {
                    FilePath = path,
                    DisplayName = Path.GetFileNameWithoutExtension(path)
                });
            }
        }

        HasRecentProjects = RecentProjects.Count > 0;
    }

    #endregion

    #region Navigation

    /// <summary>
    /// The current page as a string for XAML binding compatibility
    /// </summary>
    public string CurrentPage => _navigationService.CurrentPage.ToDisplayName();

    /// <summary>
    /// The current page type
    /// </summary>
    public PageType CurrentPageType => _navigationService.CurrentPage;

    /// <summary>
    /// Whether back navigation is available
    /// </summary>
    public bool CanGoBack => _navigationService.CanGoBack;

    /// <summary>
    /// Whether forward navigation is available
    /// </summary>
    public bool CanGoForward => _navigationService.CanGoForward;

    /// <summary>
    /// Navigate to a specific page by name (for backwards compatibility)
    /// </summary>
    public void NavigateTo(string pageName)
    {
        var pageType = PageTypeExtensions.ParsePageType(pageName);
        if (pageType.HasValue)
        {
            _navigationService.NavigateTo(pageType.Value);
        }
    }

    /// <summary>
    /// Navigate to a specific page type
    /// </summary>
    public void NavigateTo(PageType page)
    {
        _navigationService.NavigateTo(page);
    }

    /// <summary>
    /// Navigate back to the previous page
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Navigate forward to the next page
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void GoForward()
    {
        _navigationService.GoForward();
    }

    #endregion

    #region Log Panel

    [ObservableProperty]
    private bool _isLogPanelVisible = true;

    [RelayCommand]
    private void ToggleLogPanel()
    {
        IsLogPanelVisible = !IsLogPanelVisible;
    }

    #endregion

    #region Project Commands

    /// <summary>
    /// Event raised when a new project is requested
    /// </summary>
    public event Func<Task<bool>>? SaveChangesRequested;

    /// <summary>
    /// Event raised when a file dialog is needed for opening
    /// </summary>
    public event Func<string?>? OpenFileDialogRequested;

    /// <summary>
    /// Event raised when a file dialog is needed for saving
    /// </summary>
    public event Func<string, string?>? SaveFileDialogRequested;

    /// <summary>
    /// Event raised to import state from project to all pages
    /// </summary>
    public event Action<Project>? ImportStateRequested;

    /// <summary>
    /// Event raised to export state from all pages to project
    /// </summary>
    public event Action<Project>? ExportStateRequested;

    /// <summary>
    /// Creates a new project, prompting to save if needed
    /// </summary>
    [RelayCommand]
    public async Task NewProjectAsync()
    {
        if (HasUnsavedChanges)
        {
            var shouldContinue = await (SaveChangesRequested?.Invoke() ?? Task.FromResult(true));
            if (!shouldContinue)
                return;
        }

        CurrentProject = _projectService.CreateNew();
        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    /// <summary>
    /// Opens an existing project
    /// </summary>
    [RelayCommand]
    public async Task OpenProjectAsync()
    {
        if (HasUnsavedChanges)
        {
            var shouldContinue = await (SaveChangesRequested?.Invoke() ?? Task.FromResult(true));
            if (!shouldContinue)
                return;
        }

        var path = OpenFileDialogRequested?.Invoke();
        if (!string.IsNullOrEmpty(path))
        {
            await LoadProjectAsync(path);
        }
    }

    /// <summary>
    /// Opens a recent project by path
    /// </summary>
    public async Task OpenRecentProjectAsync(string path)
    {
        if (HasUnsavedChanges)
        {
            var shouldContinue = await (SaveChangesRequested?.Invoke() ?? Task.FromResult(true));
            if (!shouldContinue)
                return;
        }

        await LoadProjectAsync(path);
    }

    /// <summary>
    /// Loads a project from the specified path
    /// </summary>
    public async Task<bool> LoadProjectAsync(string path)
    {
        try
        {
            var project = await _projectService.LoadAsync(path);
            if (project != null)
            {
                CurrentProject = project;
                _projectService.AddToRecentProjects(path);
                LoadRecentProjects();

                // Request pages to import state
                ImportStateRequested?.Invoke(project);

                ToastService.Instance.ShowSuccess($"Project loaded: {Path.GetFileName(path)}");
                OnPropertyChanged(nameof(HasUnsavedChanges));
                return true;
            }
            else
            {
                ToastService.Instance.ShowError("Failed to load project file");
                return false;
            }
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("Error loading project", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Saves the current project
    /// </summary>
    [RelayCommand]
    public async Task SaveProjectAsync()
    {
        if (string.IsNullOrEmpty(_currentProject.FilePath))
        {
            await SaveProjectAsAsync();
        }
        else
        {
            await SaveToPathAsync(_currentProject.FilePath);
        }
    }

    /// <summary>
    /// Saves the project with a new name
    /// </summary>
    [RelayCommand]
    public async Task SaveProjectAsAsync()
    {
        var path = SaveFileDialogRequested?.Invoke(_currentProject.Name);
        if (!string.IsNullOrEmpty(path))
        {
            _currentProject.Name = Path.GetFileNameWithoutExtension(path);
            await SaveToPathAsync(path);
            _projectService.AddToRecentProjects(path);
            LoadRecentProjects();
        }
    }

    private async Task SaveToPathAsync(string path)
    {
        try
        {
            // Request pages to export state
            ExportStateRequested?.Invoke(_currentProject);

            await _projectService.SaveAsync(_currentProject, path);
            UpdateWindowTitle();
            OnPropertyChanged(nameof(HasUnsavedChanges));
            ToastService.Instance.ShowSuccess("Project saved");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("Error saving project", ex.Message);
        }
    }

    #endregion

    #region Settings

    /// <summary>
    /// Loads settings from the settings service
    /// </summary>
    public AppSettings LoadSettings()
    {
        return _settingsService.Load();
    }

    /// <summary>
    /// Saves settings to the settings service
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        _settingsService.Save(settings);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases resources used by the ViewModel
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Unsubscribe from navigation events
        _navigationService.PageChanged -= OnPageChanged;

        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Represents a recent project item for display
/// </summary>
public class RecentProjectItem
{
    public string FilePath { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
