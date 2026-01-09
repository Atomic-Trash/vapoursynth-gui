using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class ColorViewModel : ObservableObject, IDisposable
{
    private readonly IMediaPoolService _mediaPool;
    private readonly ColorGradingService _colorGradingService;
    private readonly FrameCacheService _frameCache;
    private CancellationTokenSource? _previewUpdateCts;
    private bool _disposed;

    [ObservableProperty]
    private ColorGrade _currentGrade = new();

    [ObservableProperty]
    private ObservableCollection<ColorGradePreset> _presets = [];

    [ObservableProperty]
    private ColorGradePreset? _selectedPreset;

    [ObservableProperty]
    private ObservableCollection<string> _presetCategories = [];

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private ObservableCollection<LutFile> _luts = [];

    [ObservableProperty]
    private LutFile? _selectedLut;

    [ObservableProperty]
    private string _lutSearchQuery = "";

    [ObservableProperty]
    private bool _showFavoritesOnly;

    [ObservableProperty]
    private ObservableCollection<string> _lutCategories = ["All"];

    [ObservableProperty]
    private string _selectedLutCategory = "All";

    // Source is now managed by MediaPoolService
    public string SourcePath => _mediaPool.CurrentSource?.FilePath ?? "";
    public bool HasSource => _mediaPool.HasSource;

    [ObservableProperty]
    private bool _showScopes = true;

    [ObservableProperty]
    private bool _showCurves;

    [ObservableProperty]
    private string _scopeMode = "Waveform";

    [ObservableProperty]
    private bool _isCompareMode;

    [ObservableProperty]
    private CompareDisplayMode _compareMode = CompareDisplayMode.SideBySide;

    [ObservableProperty]
    private double _comparePosition = 0.5;

    [ObservableProperty]
    private ColorGrade? _originalGrade;

    [ObservableProperty]
    private bool _hasOriginalGrade;

    [ObservableProperty]
    private string _statusText = "No clip loaded";

    [ObservableProperty]
    private BitmapSource? _originalFrame;

    [ObservableProperty]
    private BitmapSource? _gradedFrame;

    [ObservableProperty]
    private bool _isProcessingPreview;

    [ObservableProperty]
    private double _previewPosition;

    // Undo/Redo stack
    private readonly Stack<ColorGrade> _undoStack = new();
    private readonly Stack<ColorGrade> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public ColorViewModel(IMediaPoolService mediaPool)
    {
        _mediaPool = mediaPool;
        _mediaPool.CurrentSourceChanged += OnCurrentSourceChanged;

        _colorGradingService = new ColorGradingService();
        _frameCache = new FrameCacheService(maxCacheSize: 50);

        LoadPresets();
        LoadLuts();

        // Subscribe to grade changes for real-time preview
        CurrentGrade.PropertyChanged += OnGradePropertyChanged;
    }

    // Parameterless constructor for XAML design-time support
    public ColorViewModel() : this(App.Services?.GetService(typeof(IMediaPoolService)) as IMediaPoolService
        ?? new MediaPoolService())
    {
    }

    private void OnCurrentSourceChanged(object? sender, MediaItem? item)
    {
        OnPropertyChanged(nameof(SourcePath));
        OnPropertyChanged(nameof(HasSource));
        StatusText = item != null ? $"Source: {item.Name}" : "No clip loaded";

        // Extract a preview frame when source changes
        if (item != null)
        {
            _ = ExtractPreviewFrameAsync();
        }
        else
        {
            OriginalFrame = null;
            GradedFrame = null;
        }
    }

    private void OnGradePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Debounce preview updates during rapid changes
        _ = UpdateGradedPreviewAsync();
    }

    /// <summary>
    /// Extract a frame from the source video for preview
    /// </summary>
    private async Task ExtractPreviewFrameAsync()
    {
        if (!HasSource || string.IsNullOrEmpty(SourcePath))
            return;

        IsProcessingPreview = true;
        try
        {
            _previewUpdateCts?.Cancel();
            _previewUpdateCts = new CancellationTokenSource();

            // Extract frame at current preview position
            var frameRate = _mediaPool.CurrentSource?.FrameRate ?? 24.0;
            var frameNumber = (long)(PreviewPosition * frameRate);

            var frame = await _frameCache.GetFrameAsync(
                SourcePath,
                frameNumber,
                frameRate,
                width: 640,
                height: 360,
                ct: _previewUpdateCts.Token);

            if (frame != null)
            {
                OriginalFrame = frame;
                await UpdateGradedPreviewAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        finally
        {
            IsProcessingPreview = false;
        }
    }

    /// <summary>
    /// Apply the current grade to the preview frame
    /// </summary>
    private async Task UpdateGradedPreviewAsync()
    {
        if (OriginalFrame == null)
            return;

        IsProcessingPreview = true;
        try
        {
            _previewUpdateCts?.Cancel();
            _previewUpdateCts = new CancellationTokenSource();

            // Run grading on background thread
            var graded = await Task.Run(() =>
                _colorGradingService.ApplyGrade(OriginalFrame, CurrentGrade),
                _previewUpdateCts.Token);

            GradedFrame = graded;
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        finally
        {
            IsProcessingPreview = false;
        }
    }

    partial void OnPreviewPositionChanged(double value)
    {
        _ = ExtractPreviewFrameAsync();
    }

    private void LoadPresets()
    {
        var presets = ColorGradePreset.GetPresets();
        var categories = new HashSet<string> { "All" };

        foreach (var preset in presets)
        {
            Presets.Add(preset);
            categories.Add(preset.Category);
        }

        foreach (var cat in categories.OrderBy(c => c == "All" ? "" : c))
        {
            PresetCategories.Add(cat);
        }
    }

    private void LoadLuts()
    {
        // Look for LUTs in common locations
        var lutPaths = new List<string>
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "luts"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LUTs"),
        };

        foreach (var path in lutPaths)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path, "*.cube", SearchOption.AllDirectories))
                {
                    Luts.Add(new LutFile { Name = Path.GetFileNameWithoutExtension(file), Path = file });
                }
                foreach (var file in Directory.GetFiles(path, "*.3dl", SearchOption.AllDirectories))
                {
                    Luts.Add(new LutFile { Name = Path.GetFileNameWithoutExtension(file), Path = file });
                }
            }
        }
    }

    partial void OnSelectedPresetChanged(ColorGradePreset? value)
    {
        if (value != null)
        {
            SaveUndoState();
            ApplyPreset(value.Grade);
            StatusText = $"Applied preset: {value.Name}";
        }
    }

    partial void OnSelectedLutChanged(LutFile? value)
    {
        if (value != null)
        {
            SaveUndoState();
            CurrentGrade.LutPath = value.Path;
            CurrentGrade.LutIntensity = 1.0;
            StatusText = $"Applied LUT: {value.Name}";
        }
    }

    private void ApplyPreset(ColorGrade preset)
    {
        CurrentGrade.LiftX = preset.LiftX;
        CurrentGrade.LiftY = preset.LiftY;
        CurrentGrade.LiftMaster = preset.LiftMaster;
        CurrentGrade.GammaX = preset.GammaX;
        CurrentGrade.GammaY = preset.GammaY;
        CurrentGrade.GammaMaster = preset.GammaMaster;
        CurrentGrade.GainX = preset.GainX;
        CurrentGrade.GainY = preset.GainY;
        CurrentGrade.GainMaster = preset.GainMaster;
        CurrentGrade.Exposure = preset.Exposure;
        CurrentGrade.Contrast = preset.Contrast;
        CurrentGrade.Saturation = preset.Saturation;
        CurrentGrade.Temperature = preset.Temperature;
        CurrentGrade.Tint = preset.Tint;
        CurrentGrade.Highlights = preset.Highlights;
        CurrentGrade.Shadows = preset.Shadows;
        CurrentGrade.Whites = preset.Whites;
        CurrentGrade.Blacks = preset.Blacks;
        CurrentGrade.Vibrance = preset.Vibrance;
        CurrentGrade.Clarity = preset.Clarity;
    }

    [RelayCommand]
    private async Task LoadSourceAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Video for Color Grading",
            Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.webm|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            var item = await _mediaPool.ImportMediaAsync(dialog.FileName);
            if (item != null)
            {
                _mediaPool.SetCurrentSource(item);
                StatusText = $"Loaded: {item.Name}";
            }
        }
    }

    [RelayCommand]
    private void LoadLut()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load LUT File",
            Filter = "LUT Files|*.cube;*.3dl|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            SaveUndoState();
            var lut = new LutFile
            {
                Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                Path = dialog.FileName
            };

            if (!Luts.Any(l => l.Path == lut.Path))
            {
                Luts.Add(lut);
            }

            SelectedLut = lut;
            CurrentGrade.LutPath = dialog.FileName;
            StatusText = $"Loaded LUT: {lut.Name}";
        }
    }

    [RelayCommand]
    private void ClearLut()
    {
        SaveUndoState();
        CurrentGrade.LutPath = "";
        CurrentGrade.LutIntensity = 1.0;
        SelectedLut = null;
        StatusText = "LUT cleared";
    }

    [RelayCommand]
    private void ResetAll()
    {
        SaveUndoState();
        CurrentGrade.Reset();
        SelectedPreset = null;
        SelectedLut = null;
        StatusText = "All settings reset";
    }

    [RelayCommand]
    private void ResetWheels()
    {
        SaveUndoState();
        CurrentGrade.LiftX = CurrentGrade.LiftY = CurrentGrade.LiftMaster = 0;
        CurrentGrade.GammaX = CurrentGrade.GammaY = CurrentGrade.GammaMaster = 0;
        CurrentGrade.GainX = CurrentGrade.GainY = CurrentGrade.GainMaster = 0;
        StatusText = "Color wheels reset";
    }

    [RelayCommand]
    private void ResetAdjustments()
    {
        SaveUndoState();
        CurrentGrade.Exposure = 0;
        CurrentGrade.Contrast = 0;
        CurrentGrade.Saturation = 0;
        CurrentGrade.Temperature = 0;
        CurrentGrade.Tint = 0;
        CurrentGrade.Highlights = 0;
        CurrentGrade.Shadows = 0;
        CurrentGrade.Whites = 0;
        CurrentGrade.Blacks = 0;
        CurrentGrade.Vibrance = 0;
        CurrentGrade.Clarity = 0;
        StatusText = "Adjustments reset";
    }

    [RelayCommand]
    private void Undo()
    {
        if (_undoStack.Count > 0)
        {
            _redoStack.Push(CurrentGrade.Clone());
            var previous = _undoStack.Pop();
            ApplyPreset(previous);
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            StatusText = "Undo";
        }
    }

    [RelayCommand]
    private void Redo()
    {
        if (_redoStack.Count > 0)
        {
            _undoStack.Push(CurrentGrade.Clone());
            var next = _redoStack.Pop();
            ApplyPreset(next);
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            StatusText = "Redo";
        }
    }

    private void SaveUndoState()
    {
        _undoStack.Push(CurrentGrade.Clone());
        _redoStack.Clear();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    [RelayCommand]
    private void ToggleCompare()
    {
        if (!IsCompareMode && !HasOriginalGrade)
        {
            // Capture original when entering compare mode for first time
            CaptureOriginal();
        }
        IsCompareMode = !IsCompareMode;
        StatusText = IsCompareMode ? "Compare mode: ON (press 'C' to toggle)" : "Compare mode: OFF";
    }

    [RelayCommand]
    private void CaptureOriginal()
    {
        OriginalGrade = CurrentGrade.Clone();
        HasOriginalGrade = true;
        StatusText = "Original grade captured for comparison";
    }

    [RelayCommand]
    private void ClearOriginal()
    {
        OriginalGrade = null;
        HasOriginalGrade = false;
        IsCompareMode = false;
        StatusText = "Original grade cleared";
    }

    [RelayCommand]
    private void SetCompareMode(string mode)
    {
        if (Enum.TryParse<CompareDisplayMode>(mode, out var displayMode))
        {
            CompareMode = displayMode;
            StatusText = $"Compare mode: {mode}";
        }
    }

    [RelayCommand]
    private void SwapGrades()
    {
        if (OriginalGrade == null) return;

        SaveUndoState();
        var temp = CurrentGrade.Clone();
        ApplyPreset(OriginalGrade);
        OriginalGrade = temp;
        StatusText = "Swapped current and original grades";
    }

    [RelayCommand]
    private void RevertToOriginal()
    {
        if (OriginalGrade == null) return;

        SaveUndoState();
        ApplyPreset(OriginalGrade);
        StatusText = "Reverted to original grade";
    }

    [RelayCommand]
    private void SetScopeMode(string mode)
    {
        ScopeMode = mode;
    }

    [RelayCommand]
    private void ExportGrade()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Color Grade",
            Filter = "VapourSynth Script|*.vpy|JSON|*.json",
            FileName = "color_grade"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                if (dialog.FileName.EndsWith(".vpy"))
                {
                    var script = CurrentGrade.ToVapourSynthScript();
                    File.WriteAllText(dialog.FileName, script);
                }
                else
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(CurrentGrade, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(dialog.FileName, json);
                }
                StatusText = $"Exported: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                StatusText = $"Export failed: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void ImportGrade()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Color Grade",
            Filter = "JSON|*.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(dialog.FileName);
                var grade = System.Text.Json.JsonSerializer.Deserialize<ColorGrade>(json);
                if (grade != null)
                {
                    SaveUndoState();
                    ApplyPreset(grade);
                    StatusText = $"Imported: {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Import failed: {ex.Message}";
            }
        }
    }

    public IEnumerable<ColorGradePreset> FilteredPresets
    {
        get
        {
            if (SelectedCategory == "All")
                return Presets;
            return Presets.Where(p => p.Category == SelectedCategory);
        }
    }

    public IEnumerable<LutFile> FilteredLuts
    {
        get
        {
            var filtered = Luts.AsEnumerable();

            // Filter by favorites
            if (ShowFavoritesOnly)
            {
                filtered = filtered.Where(l => l.IsFavorite);
            }

            // Filter by category
            if (SelectedLutCategory != "All")
            {
                filtered = filtered.Where(l => l.Category == SelectedLutCategory);
            }

            // Filter by search query
            if (!string.IsNullOrWhiteSpace(LutSearchQuery))
            {
                var query = LutSearchQuery.ToLowerInvariant();
                filtered = filtered.Where(l =>
                    l.Name.ToLowerInvariant().Contains(query) ||
                    l.Category.ToLowerInvariant().Contains(query));
            }

            return filtered.OrderByDescending(l => l.IsFavorite).ThenBy(l => l.Name);
        }
    }

    partial void OnLutSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredLuts));
    }

    partial void OnShowFavoritesOnlyChanged(bool value)
    {
        OnPropertyChanged(nameof(FilteredLuts));
    }

    partial void OnSelectedLutCategoryChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredLuts));
    }

    [RelayCommand]
    private void ToggleLutFavorite(LutFile? lut)
    {
        if (lut == null) return;
        lut.IsFavorite = !lut.IsFavorite;
        OnPropertyChanged(nameof(FilteredLuts));
        StatusText = lut.IsFavorite ? $"Added {lut.Name} to favorites" : $"Removed {lut.Name} from favorites";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _mediaPool.CurrentSourceChanged -= OnCurrentSourceChanged;
        CurrentGrade.PropertyChanged -= OnGradePropertyChanged;

        _previewUpdateCts?.Cancel();
        _previewUpdateCts?.Dispose();
        _frameCache.Dispose();
    }
}

public partial class LutFile : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _path = "";

    [ObservableProperty]
    private string _category = "Uncategorized";

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private DateTime _dateAdded = DateTime.Now;
}

public enum CompareDisplayMode
{
    SideBySide,
    VerticalSplit,
    HorizontalSplit,
    Wipe
}
