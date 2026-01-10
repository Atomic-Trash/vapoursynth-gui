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
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly ISettingsService _settingsService;

    public MainWindowViewModel(IProjectService projectService, ISettingsService settingsService)
    {
        _projectService = projectService;
        _settingsService = settingsService;

        // Initialize with a new project
        _currentProject = _projectService.CreateNew();
        UpdateWindowTitle();
        LoadRecentProjects();
    }

    // Parameterless constructor for XAML designer
    public MainWindowViewModel() : this(
        App.Services?.GetService(typeof(IProjectService)) as IProjectService ?? new ProjectService(),
        App.Services?.GetService(typeof(ISettingsService)) as ISettingsService ?? new SettingsService())
    {
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

    [ObservableProperty]
    private string _currentPage = "Restore";

    /// <summary>
    /// Navigate to a specific page
    /// </summary>
    public void NavigateTo(string pageName)
    {
        CurrentPage = pageName;
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
}

/// <summary>
/// Represents a recent project item for display
/// </summary>
public class RecentProjectItem
{
    public string FilePath { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
