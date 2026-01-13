Add loading overlay and state management to a page.

## Arguments
- `<PageName>` - Name of the page (e.g., "Edit", "Restore", "Export") - required

## Instructions

### 1. Locate Files
- Page XAML: `src/gui/VapourSynthPortable/Pages/<PageName>Page.xaml`
- Page code-behind: `src/gui/VapourSynthPortable/Pages/<PageName>Page.xaml.cs`
- ViewModel: `src/gui/VapourSynthPortable/ViewModels/<PageName>ViewModel.cs`

### 2. Add Loading Properties to ViewModel

If not already present, add these properties:

```csharp
[ObservableProperty]
private bool _isLoading;

[ObservableProperty]
private string _loadingMessage = "Loading...";

// Optional: for determinate progress
[ObservableProperty]
private double _loadingProgress;

[ObservableProperty]
private bool _isIndeterminate = true;
```

### 3. Add Loading Overlay to XAML

Add this overlay as the LAST child of the root Grid (so it appears on top):

```xml
<!-- Loading Overlay -->
<Grid Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}"
      Background="#CC1A1A1A"
      Panel.ZIndex="1000">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <!-- Spinner -->
        <ProgressBar IsIndeterminate="{Binding IsIndeterminate}"
                     Value="{Binding LoadingProgress}"
                     Width="200"
                     Height="4"
                     Foreground="{StaticResource AccentBrush}"
                     Background="#333"/>

        <!-- Message -->
        <TextBlock Text="{Binding LoadingMessage}"
                   Foreground="#AAA"
                   FontSize="14"
                   Margin="0,12,0,0"
                   HorizontalAlignment="Center"/>

        <!-- Cancel Button (optional) -->
        <Button Content="Cancel"
                Command="{Binding CancelLoadingCommand}"
                Margin="0,16,0,0"
                Visibility="{Binding CanCancelLoading, Converter={StaticResource BoolToVisibilityConverter}}"
                Style="{StaticResource SecondaryButtonStyle}"/>
    </StackPanel>
</Grid>
```

### 4. Wrap Async Operations

Update async methods to show/hide loading:

```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    if (IsLoading) return;

    try
    {
        IsLoading = true;
        LoadingMessage = "Loading data...";

        // Actual work
        await DoWorkAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load data");
        ToastService.Instance.ShowError("Load Failed", ex.Message);
    }
    finally
    {
        IsLoading = false;
    }
}
```

### 5. Add Cancellation Support (Optional)

```csharp
private CancellationTokenSource? _loadingCts;

[ObservableProperty]
private bool _canCancelLoading;

[RelayCommand]
private void CancelLoading()
{
    _loadingCts?.Cancel();
}

private async Task LoadWithCancellationAsync()
{
    _loadingCts?.Cancel();
    _loadingCts = new CancellationTokenSource();
    CanCancelLoading = true;

    try
    {
        IsLoading = true;
        await DoWorkAsync(_loadingCts.Token);
    }
    catch (OperationCanceledException)
    {
        LoadingMessage = "Cancelled";
    }
    finally
    {
        IsLoading = false;
        CanCancelLoading = false;
    }
}
```

### 6. Add Timeout Handling

```csharp
private async Task LoadWithTimeoutAsync(int timeoutMs = 30000)
{
    using var cts = new CancellationTokenSource(timeoutMs);

    try
    {
        IsLoading = true;
        await DoWorkAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        ToastService.Instance.ShowWarning("Timeout", "Operation took too long");
    }
    finally
    {
        IsLoading = false;
    }
}
```

### 7. Verify BoolToVisibilityConverter

Ensure this converter exists in App.xaml resources:
```xml
<BooleanToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
```

Or use the custom one if available:
```xml
<converters:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
```

### 8. Summary Report

```
Loading State Added
════════════════════════════════════════════

Page: <PageName>Page
ViewModel: <PageName>ViewModel

Properties Added:
  ✓ IsLoading (bool)
  ✓ LoadingMessage (string)
  ✓ LoadingProgress (double) [if determinate]
  ✓ IsIndeterminate (bool)

XAML Changes:
  ✓ Loading overlay added to root Grid

Methods Updated:
  • LoadDataAsync - wrapped with loading state
  • ProcessAsync - wrapped with loading state

Optional Features:
  [x] Cancellation support
  [x] Timeout handling
  [ ] Progress reporting

Test by:
  1. Trigger a long operation
  2. Verify overlay appears
  3. Verify message updates
  4. Verify overlay hides when complete
```

## Loading State Patterns

### Simple Loading
```csharp
IsLoading = true;
try { await Work(); }
finally { IsLoading = false; }
```

### With Progress
```csharp
IsIndeterminate = false;
for (int i = 0; i < total; i++)
{
    LoadingProgress = (i + 1) * 100.0 / total;
    LoadingMessage = $"Processing {i + 1}/{total}...";
    await ProcessItem(i);
}
```

### Cascading Operations
```csharp
LoadingMessage = "Step 1: Loading media...";
await LoadMedia();

LoadingMessage = "Step 2: Generating previews...";
await GeneratePreviews();

LoadingMessage = "Step 3: Initializing timeline...";
await InitTimeline();
```
