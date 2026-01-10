using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    /// <summary>
    /// Gets all ViewModels that implement IProjectPersistable from the pages
    /// </summary>
    private IEnumerable<IProjectPersistable> GetPersistableViewModels()
    {
        if (PageEdit?.DataContext is IProjectPersistable editVm)
            yield return editVm;
        if (PageColor?.DataContext is IProjectPersistable colorVm)
            yield return colorVm;
        if (PageRestore?.DataContext is IProjectPersistable restoreVm)
            yield return restoreVm;
    }

    public MainWindow()
    {
        InitializeComponent();

        // Get ViewModel from DI
        _viewModel = App.Services?.GetService(typeof(MainWindowViewModel)) as MainWindowViewModel
            ?? new MainWindowViewModel();
        DataContext = _viewModel;

        // Wire up ViewModel events
        _viewModel.SaveChangesRequested += OnSaveChangesRequested;
        _viewModel.OpenFileDialogRequested += OnOpenFileDialogRequested;
        _viewModel.SaveFileDialogRequested += OnSaveFileDialogRequested;
        _viewModel.ImportStateRequested += OnImportStateRequested;
        _viewModel.ExportStateRequested += OnExportStateRequested;

        // Restore window state from settings
        RestoreWindowState();

        // Load recent projects into menu
        RefreshRecentProjectsMenu();
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.RecentProjects))
                RefreshRecentProjectsMenu();
        };

        // Register toast notification with service
        ToastService.Instance.SetToastControl(ToastNotification);

        // Setup keyboard shortcuts
        SetupCommandBindings();
    }

    #region ViewModel Event Handlers

    private async Task<bool> OnSaveChangesRequested()
    {
        var result = MessageBox.Show(
            "Do you want to save changes to the current project?",
            "Save Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
            return false;

        if (result == MessageBoxResult.Yes)
        {
            await _viewModel.SaveProjectAsync();
        }

        return true;
    }

    private string? OnOpenFileDialogRequested()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Project",
            Filter = Project.FileFilter,
            DefaultExt = Project.FileExtension
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private string? OnSaveFileDialogRequested(string defaultName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Project As",
            Filter = Project.FileFilter,
            DefaultExt = Project.FileExtension,
            FileName = defaultName
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private void OnImportStateRequested(Project project)
    {
        foreach (var vm in GetPersistableViewModels())
        {
            vm.ImportFromProject(project);
        }
    }

    private void OnExportStateRequested(Project project)
    {
        foreach (var vm in GetPersistableViewModels())
        {
            vm.ExportToProject(project);
        }
        project.MarkDirty();
    }

    #endregion

    #region Command Bindings

    private void SetupCommandBindings()
    {
        CommandBindings.Add(new CommandBinding(ApplicationCommands.New,
            async (s, e) => await _viewModel.NewProjectCommand.ExecuteAsync(null)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Open,
            async (s, e) => await _viewModel.OpenProjectCommand.ExecuteAsync(null)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save,
            async (s, e) => await _viewModel.SaveProjectCommand.ExecuteAsync(null)));

        // Ctrl+Shift+S for Save As
        var saveAsCommand = new RoutedCommand();
        saveAsCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));
        CommandBindings.Add(new CommandBinding(saveAsCommand,
            async (s, e) => await _viewModel.SaveProjectAsCommand.ExecuteAsync(null)));
    }

    #endregion

    #region Window State Management

    private void RestoreWindowState()
    {
        var settings = _viewModel.LoadSettings();

        // Restore window size
        if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
        {
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
        }

        // Restore window position (only if valid coordinates)
        if (settings.WindowLeft >= 0 && settings.WindowTop >= 0)
        {
            var screenWidth = SystemParameters.VirtualScreenWidth;
            var screenHeight = SystemParameters.VirtualScreenHeight;

            if (settings.WindowLeft < screenWidth - 100 && settings.WindowTop < screenHeight - 100)
            {
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
            }
        }

        // Restore maximized state
        if (settings.WindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }

        // Restore log panel visibility
        _viewModel.IsLogPanelVisible = settings.ShowLogPanel;
        UpdateLogPanelVisibility();
    }

    private void SaveWindowState()
    {
        var settings = _viewModel.LoadSettings();

        // Save window state (only if not minimized)
        if (WindowState != WindowState.Minimized)
        {
            settings.WindowMaximized = WindowState == WindowState.Maximized;

            // Save position/size only when in normal state
            if (WindowState == WindowState.Normal)
            {
                settings.WindowLeft = Left;
                settings.WindowTop = Top;
                settings.WindowWidth = Width;
                settings.WindowHeight = Height;
            }
        }

        // Save log panel visibility
        settings.ShowLogPanel = _viewModel.IsLogPanelVisible;

        // Save active page
        settings.LastActivePage = _viewModel.CurrentPage;

        _viewModel.SaveSettings(settings);
    }

    #endregion

    #region Navigation

    private void NavButton_Checked(object sender, RoutedEventArgs e)
    {
        // Hide all pages
        PageMedia?.SetValue(VisibilityProperty, Visibility.Collapsed);
        PageEdit?.SetValue(VisibilityProperty, Visibility.Collapsed);
        PageRestore?.SetValue(VisibilityProperty, Visibility.Collapsed);
        PageColor?.SetValue(VisibilityProperty, Visibility.Collapsed);
        PageExport?.SetValue(VisibilityProperty, Visibility.Collapsed);
        PageSettings?.SetValue(VisibilityProperty, Visibility.Collapsed);

        // Show selected page and update ViewModel
        if (sender == NavMedia && PageMedia != null)
        {
            PageMedia.Visibility = Visibility.Visible;
            _viewModel.NavigateTo("Media");
        }
        else if (sender == NavEdit && PageEdit != null)
        {
            PageEdit.Visibility = Visibility.Visible;
            _viewModel.NavigateTo("Edit");
        }
        else if (sender == NavRestore && PageRestore != null)
        {
            PageRestore.Visibility = Visibility.Visible;
            _viewModel.NavigateTo("Restore");
        }
        else if (sender == NavColor && PageColor != null)
        {
            PageColor.Visibility = Visibility.Visible;
            _viewModel.NavigateTo("Color");
        }
        else if (sender == NavExport && PageExport != null)
        {
            PageExport.Visibility = Visibility.Visible;
            _viewModel.NavigateTo("Export");
        }
        else if (sender == NavSettings && PageSettings != null)
        {
            PageSettings.Visibility = Visibility.Visible;
            _viewModel.NavigateTo("Settings");
        }
    }

    #endregion

    #region Recent Projects Menu

    private void RefreshRecentProjectsMenu()
    {
        RecentProjectsMenu.Items.Clear();

        if (_viewModel.HasRecentProjects)
        {
            RecentProjectsMenu.IsEnabled = true;

            foreach (var recent in _viewModel.RecentProjects)
            {
                var menuItem = new MenuItem
                {
                    Header = recent.DisplayName,
                    ToolTip = recent.FilePath,
                    Tag = recent.FilePath
                };
                menuItem.Click += RecentProject_Click;
                RecentProjectsMenu.Items.Add(menuItem);
            }
        }
        else
        {
            RecentProjectsMenu.IsEnabled = false;
        }
    }

    private async void RecentProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string path)
        {
            await _viewModel.OpenRecentProjectAsync(path);
        }
    }

    #endregion

    #region Menu Event Handlers

    private async void NewProject_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.NewProjectCommand.ExecuteAsync(null);
    }

    private async void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.OpenProjectCommand.ExecuteAsync(null);
    }

    private async void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveProjectCommand.ExecuteAsync(null);
    }

    private async void SaveProjectAs_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveProjectAsCommand.ExecuteAsync(null);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Log Panel

    private void LogPanelToggle_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsLogPanelVisible = LogPanelToggle.IsChecked == true;
        UpdateLogPanelVisibility();
    }

    private void UpdateLogPanelVisibility()
    {
        if (_viewModel.IsLogPanelVisible)
        {
            LogPanelRow.Height = new GridLength(180);
            LogViewer.Visibility = Visibility.Visible;
        }
        else
        {
            LogPanelRow.Height = new GridLength(0);
            LogViewer.Visibility = Visibility.Collapsed;
        }

        // Sync toggle button state
        if (LogPanelToggle != null)
        {
            LogPanelToggle.IsChecked = _viewModel.IsLogPanelVisible;
        }
    }

    #endregion

    #region Window Closing

    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_viewModel.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "Do you want to save changes before closing?",
                "Save Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.SaveProjectAsync();
            }
        }

        // Save window state before closing
        SaveWindowState();

        base.OnClosing(e);
    }

    #endregion
}
