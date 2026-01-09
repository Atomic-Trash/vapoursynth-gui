using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Controls;

/// <summary>
/// Converts bool to inverted Visibility
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class EffectPanelControl : UserControl
{
    private bool _showAddEffectDropdown;

    public EffectPanelControl()
    {
        InitializeComponent();
    }

    private EditViewModel? ViewModel => DataContext as EditViewModel;

    private void AddEffect_Click(object sender, RoutedEventArgs e)
    {
        _showAddEffectDropdown = !_showAddEffectDropdown;
        AddEffectDropdown.Visibility = _showAddEffectDropdown ? Visibility.Visible : Visibility.Collapsed;

        if (_showAddEffectDropdown)
        {
            // Populate effect list
            var effects = EffectService.Instance.AvailableEffects;
            EffectList.ItemsSource = effects;

            // Focus search box
            EffectSearchBox.Focus();
        }
    }

    private void EffectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EffectList.SelectedItem is EffectDefinition definition && ViewModel != null)
        {
            // Add the selected effect
            ViewModel.AddEffectToClipCommand.Execute(definition);

            // Hide dropdown
            _showAddEffectDropdown = false;
            AddEffectDropdown.Visibility = Visibility.Collapsed;
            EffectList.SelectedItem = null;
        }
    }

    private void EffectHeader_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TimelineEffect effect)
        {
            // Toggle expanded state
            effect.IsExpanded = !effect.IsExpanded;
        }
    }

    private void MoveEffectUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TimelineEffect effect && ViewModel != null)
        {
            ViewModel.MoveEffectUpCommand.Execute(effect);
        }
    }

    private void MoveEffectDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TimelineEffect effect && ViewModel != null)
        {
            ViewModel.MoveEffectDownCommand.Execute(effect);
        }
    }

    private void DeleteEffect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TimelineEffect effect && ViewModel != null)
        {
            ViewModel.RemoveEffectFromClipCommand.Execute(effect);
        }
    }
}
