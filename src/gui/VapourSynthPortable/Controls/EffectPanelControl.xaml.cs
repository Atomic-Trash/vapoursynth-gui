using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
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
        if (value is Visibility v)
            return v != Visibility.Visible;
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts EffectParameterType to Visibility based on parameter
/// </summary>
public class ParameterTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is EffectParameterType paramType && parameter is string targetType2)
        {
            var matches = paramType.ToString().Equals(targetType2, StringComparison.OrdinalIgnoreCase);
            return matches ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
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

    private void ResetParameter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is EffectParameter param)
        {
            param.Reset();
        }
    }

    private void ColorPicker_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is EffectParameter param)
        {
            // Prompt for hex color input
            var currentHex = "#FFFFFF";
            if (param.Value is System.Windows.Media.Color currentColor)
            {
                currentHex = $"#{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2}";
            }

            // Use simple input dialog
            var inputDialog = new Window
            {
                Title = $"Enter Color for {param.DisplayName}",
                Width = 300,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(10) };
            var textBox = new TextBox { Text = currentHex, Margin = new Thickness(0, 0, 0, 10) };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 60, Margin = new Thickness(0, 0, 5, 0) };
            var cancelButton = new Button { Content = "Cancel", Width = 60 };

            okButton.Click += (s, args) => { inputDialog.DialogResult = true; inputDialog.Close(); };
            cancelButton.Click += (s, args) => { inputDialog.DialogResult = false; inputDialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(new TextBlock { Text = "Enter hex color (e.g., #FF0000):" });
            stack.Children.Add(textBox);
            stack.Children.Add(buttonPanel);
            inputDialog.Content = stack;

            if (inputDialog.ShowDialog() == true)
            {
                try
                {
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(textBox.Text);
                    param.Value = color;
                }
                catch (FormatException)
                {
                    ToastService.Instance.ShowError("Invalid color format", "Please use hex format like #FF0000");
                }
            }
        }
    }

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is EffectParameter param)
        {
            var dialog = new OpenFileDialog
            {
                Title = $"Select {param.DisplayName}",
                Filter = "All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                param.Value = dialog.FileName;
            }
        }
    }
}
