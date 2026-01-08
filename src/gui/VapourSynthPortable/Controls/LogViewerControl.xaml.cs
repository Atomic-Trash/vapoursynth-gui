using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Controls;

public partial class LogViewerControl : UserControl
{
    private ICollectionView? _filteredView;
    private LogLevel _minLevel = LogLevel.Trace;
    private string _searchText = "";

    public LogViewerControl()
    {
        InitializeComponent();

        // Bind to LoggingService entries
        _filteredView = CollectionViewSource.GetDefaultView(LoggingService.LogEntries);
        _filteredView.Filter = FilterLogEntry;
        LogList.ItemsSource = _filteredView;

        // Auto-scroll when new entries added
        LoggingService.LogEntries.CollectionChanged += OnLogEntriesChanged;

        // Update stats
        UpdateStats();

        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        LoggingService.LogEntries.CollectionChanged -= OnLogEntriesChanged;
    }

    private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && AutoScrollCheck.IsChecked == true)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (LogList.Items.Count > 0)
                {
                    LogList.ScrollIntoView(LogList.Items[^1]);
                }
            });
        }

        Dispatcher.BeginInvoke(UpdateStats);
    }

    private bool FilterLogEntry(object obj)
    {
        if (obj is not LogEntry entry) return false;

        // Filter by level
        if (entry.Level < _minLevel) return false;

        // Filter by search text
        if (!string.IsNullOrEmpty(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            if (!entry.Message.ToLowerInvariant().Contains(search) &&
                !entry.Source.ToLowerInvariant().Contains(search))
            {
                return false;
            }
        }

        return true;
    }

    private void LevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Guard against calls during XAML initialization
        if (!IsLoaded) return;

        _minLevel = LevelFilter.SelectedIndex switch
        {
            0 => LogLevel.Trace,      // All
            1 => LogLevel.Information,
            2 => LogLevel.Warning,
            3 => LogLevel.Error,
            _ => LogLevel.Trace
        };

        _filteredView?.Refresh();
        UpdateStats();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;

        _searchText = SearchBox.Text;
        _filteredView?.Refresh();
        UpdateStats();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        LoggingService.ClearLogEntries();
        UpdateStats();
    }

    private void OpenLogsButton_Click(object sender, RoutedEventArgs e)
    {
        LoggingService.OpenLogDirectory();
    }

    private void UpdateStats()
    {
        if (StatsText == null) return;

        var total = LoggingService.LogEntries.Count;
        var visible = _filteredView?.Cast<object>().Count() ?? 0;
        var errors = LoggingService.LogEntries.Count(e => e.Level >= LogLevel.Error);
        var warnings = LoggingService.LogEntries.Count(e => e.Level == LogLevel.Warning);

        StatsText.Text = visible == total
            ? $"{total} entries"
            : $"{visible} of {total} entries";

        if (errors > 0 || warnings > 0)
        {
            var parts = new List<string>();
            if (errors > 0) parts.Add($"{errors} errors");
            if (warnings > 0) parts.Add($"{warnings} warnings");
            StatsText.Text += $" ({string.Join(", ", parts)})";
        }

        // Update empty state visibility
        if (EmptyStateText != null)
        {
            EmptyStateText.Visibility = visible == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
