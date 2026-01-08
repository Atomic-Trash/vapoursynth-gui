using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable;

public partial class MainWindow : Window
{
    private readonly ProjectService _projectService = new();
    private Project _currentProject;

    public MainWindow()
    {
        InitializeComponent();

        // Create a new project on startup
        _currentProject = _projectService.CreateNew();
        UpdateTitle();
        LoadRecentProjects();

        // Setup keyboard shortcuts
        CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (s, e) => NewProject_Click(s, e)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, (s, e) => OpenProject_Click(s, e)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (s, e) => SaveProject_Click(s, e)));

        // Ctrl+Shift+S for Save As
        var saveAsCommand = new RoutedCommand();
        saveAsCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));
        CommandBindings.Add(new CommandBinding(saveAsCommand, (s, e) => SaveProjectAs_Click(s, e)));
    }

    private void NavButton_Checked(object sender, RoutedEventArgs e)
    {
        // Hide all pages (null checks for designer support)
        PageMedia?.SetValue(VisibilityProperty, Visibility.Collapsed);
        PageEdit?.SetValue(VisibilityProperty, Visibility.Collapsed);
        PageRestore?.SetValue(VisibilityProperty, Visibility.Collapsed);
        PageColor?.SetValue(VisibilityProperty, Visibility.Collapsed);
        PageExport?.SetValue(VisibilityProperty, Visibility.Collapsed);

        // Show selected page
        if (sender == NavMedia && PageMedia != null)
            PageMedia.Visibility = Visibility.Visible;
        else if (sender == NavEdit && PageEdit != null)
            PageEdit.Visibility = Visibility.Visible;
        else if (sender == NavRestore && PageRestore != null)
            PageRestore.Visibility = Visibility.Visible;
        else if (sender == NavColor && PageColor != null)
            PageColor.Visibility = Visibility.Visible;
        else if (sender == NavExport && PageExport != null)
            PageExport.Visibility = Visibility.Visible;
    }

    private void UpdateTitle()
    {
        Title = $"VapourSynth Studio - {_currentProject.DisplayName}";
    }

    private void LoadRecentProjects()
    {
        var recentProjects = _projectService.GetRecentProjects();

        if (recentProjects.Count > 0)
        {
            RecentProjectsMenu.IsEnabled = true;
            RecentProjectsMenu.Items.Clear();

            foreach (var path in recentProjects)
            {
                if (File.Exists(path))
                {
                    var menuItem = new MenuItem
                    {
                        Header = Path.GetFileNameWithoutExtension(path),
                        ToolTip = path,
                        Tag = path
                    };
                    menuItem.Click += RecentProject_Click;
                    RecentProjectsMenu.Items.Add(menuItem);
                }
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
            await LoadProjectAsync(path);
        }
    }

    private async void NewProject_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProject.IsDirty)
        {
            var result = MessageBox.Show(
                "Do you want to save changes to the current project?",
                "Save Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
            {
                await SaveProjectAsync();
            }
        }

        _currentProject = _projectService.CreateNew();
        UpdateTitle();
    }

    private async void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Project",
            Filter = Project.FileFilter,
            DefaultExt = Project.FileExtension
        };

        if (dialog.ShowDialog() == true)
        {
            if (_currentProject.IsDirty)
            {
                var result = MessageBox.Show(
                    "Do you want to save changes to the current project?",
                    "Save Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                    return;

                if (result == MessageBoxResult.Yes)
                {
                    await SaveProjectAsync();
                }
            }

            await LoadProjectAsync(dialog.FileName);
        }
    }

    private async Task LoadProjectAsync(string path)
    {
        try
        {
            var project = await _projectService.LoadAsync(path);
            if (project != null)
            {
                _currentProject = project;
                _projectService.AddToRecentProjects(path);
                LoadRecentProjects();
                UpdateTitle();

                // TODO: Apply loaded project data to pages
                // This would involve updating EditPage timeline, ColorPage grades, etc.

                MessageBox.Show($"Project loaded: {Path.GetFileName(path)}", "Project Loaded",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to load project file.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading project: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        await SaveProjectAsync();
    }

    private async Task SaveProjectAsync()
    {
        if (string.IsNullOrEmpty(_currentProject.FilePath))
        {
            await SaveProjectAsAsync();
        }
        else
        {
            try
            {
                // TODO: Export current state from pages
                // _currentProject.TimelineData = _projectService.ExportTimeline(PageEdit.Timeline);
                // etc.

                await _projectService.SaveAsync(_currentProject, _currentProject.FilePath);
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void SaveProjectAs_Click(object sender, RoutedEventArgs e)
    {
        await SaveProjectAsAsync();
    }

    private async Task SaveProjectAsAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Project As",
            Filter = Project.FileFilter,
            DefaultExt = Project.FileExtension,
            FileName = _currentProject.Name
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _currentProject.Name = Path.GetFileNameWithoutExtension(dialog.FileName);

                // TODO: Export current state from pages

                await _projectService.SaveAsync(_currentProject, dialog.FileName);
                _projectService.AddToRecentProjects(dialog.FileName);
                LoadRecentProjects();
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_currentProject.IsDirty)
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
                await SaveProjectAsync();
            }
        }

        base.OnClosing(e);
    }
}
