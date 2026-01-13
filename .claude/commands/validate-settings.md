Audit hardcoded values that should be configurable in Settings.

## Arguments
- `--fix` - Generate settings entries for found values
- `--category <name>` - Focus on specific category (ui, performance, behavior)

## Instructions

### 1. Search for Hardcoded Values

Scan `src/gui/VapourSynthPortable/` for:

**Numeric Constants:**
```csharp
// Panel/control sizes
Width="280"
Height="380"
MinWidth, MaxWidth, etc.

// Timeouts and intervals
Task.Delay(200)
TimeSpan.FromMilliseconds(50)
new Timer(100)

// Cache limits
_maxItems = 150
MaxCacheSize = 100

// Thresholds
if (value > 0.5)
```

**Color Values:**
```xml
Background="#333"
Foreground="#888"
BorderBrush="#1E1E1E"
```

**Magic Strings:**
```csharp
"default_preset"
"output.mp4"
```

### 2. Categorize Findings

| Category | Examples | Configurable? |
|----------|----------|---------------|
| **UI Layout** | Panel widths, margins | Maybe (advanced) |
| **Performance** | Cache sizes, timeouts | Yes |
| **Behavior** | Auto-save interval, history size | Yes |
| **Theme** | Colors not using resources | Should use themes |
| **Paths** | Default directories | Yes |

### 3. Cross-Reference with SettingsService

Read `src/gui/VapourSynthPortable/Services/SettingsService.cs` and `Models/AppSettings.cs`:
- List existing settings
- Identify gaps between hardcoded values and settings

### 4. Generate Report

```
Settings Audit Report
════════════════════════════════════════════

Scanned: X files
Hardcoded Values Found: Y
Already Configurable: Z
Missing Settings: W

PERFORMANCE (Should Add to Settings)
─────────────────────────────────────────────
Value           Location                    Current   Suggested Setting
───────────────────────────────────────────────────────────────────────
150             EditViewModel.cs:87         150       FrameCacheLimit
4               EditViewModel.cs:88         4         MaxConcurrentFrameExtractions
50ms            EditViewModel.cs:123        50        ScrubDebounceMs
200ms           MediaPage.xaml.cs:56        200       AutoLoadDebounceMs

UI LAYOUT (Consider for Advanced Settings)
─────────────────────────────────────────────
280px           RestorePage.xaml:98         Fixed     LeftPanelWidth
380px           ExportPage.xaml:16          Fixed     RightPanelWidth

THEME VIOLATIONS (Should Use Resources)
─────────────────────────────────────────────
#333            EditPage.xaml:55            -         Use {StaticResource PanelBackground}
#252526         EditPage.xaml:175           -         Use {StaticResource SurfaceBackground}
#1E1E1E         EditPage.xaml:269           -         Use {StaticResource WindowBackground}

ALREADY CONFIGURABLE (Good!)
─────────────────────────────────────────────
✓ DefaultExportFormat     in AppSettings
✓ CacheDirectory          in AppSettings
✓ AutoSaveInterval        in AppSettings
✓ UndoHistoryLimit        in AppSettings
```

### 5. Generate Settings Entries (if --fix)

For missing settings, generate:

**Add to AppSettings.cs:**
```csharp
/// <summary>
/// Maximum frames to keep in preview cache
/// </summary>
public int FrameCacheLimit { get; set; } = 150;

/// <summary>
/// Debounce delay for scrub preview updates (ms)
/// </summary>
public int ScrubDebounceMs { get; set; } = 50;
```

**Add to SettingsService.cs:**
```csharp
// In Load() defaults
settings.FrameCacheLimit ??= 150;
settings.ScrubDebounceMs ??= 50;
```

**Add to SettingsPage.xaml:**
```xml
<!-- Performance Section -->
<TextBlock Text="Frame Cache Limit" Style="{StaticResource SettingLabelStyle}"/>
<Slider Value="{Binding FrameCacheLimit}"
        Minimum="50" Maximum="500"
        TickFrequency="50"/>
```

### 6. Recommendations

```
Recommendations
════════════════════════════════════════════

Priority 1 - Add to Settings:
  • FrameCacheLimit - Affects memory usage
  • ScrubDebounceMs - Affects responsiveness
  • MaxConcurrentFrameExtractions - Affects CPU usage

Priority 2 - Move to Theme Resources:
  • All hardcoded color values (#333, #252526, etc.)
  • Standardize on design tokens

Priority 3 - Consider for Power Users:
  • Panel widths (maybe as "Compact Mode" toggle)
  • Animation durations
  • Tooltip delays
```

## Value Categories Reference

| Value Type | Range | Where to Configure |
|------------|-------|-------------------|
| Cache sizes | 50-1000 | Settings > Performance |
| Timeouts (ms) | 10-5000 | Settings > Performance |
| Panel widths | 200-500 | Settings > UI (Advanced) |
| Colors | Hex | Theme resources only |
| Paths | Directory | Settings > Paths |
| Limits | Varies | Settings > Behavior |
