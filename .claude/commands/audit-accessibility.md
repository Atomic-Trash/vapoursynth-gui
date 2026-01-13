Perform comprehensive WCAG 2.1 AA accessibility audit with optional auto-fix.

## Arguments
- `<target>` - Optional: specific page, control, or "all" (default: all)
- `--fix` - Auto-fix issues where possible
- `--report` - Generate detailed HTML report

## Instructions

### 1. Scope Determination

If target specified:
- Single page: `src/gui/VapourSynthPortable/Pages/<Target>Page.xaml`
- Single control: `src/gui/VapourSynthPortable/Controls/<Target>.xaml`
- "all": Scan all Pages/, Controls/, Views/, MainWindow.xaml

### 2. Automation Property Audit

Check every interactive element for:

```xml
<!-- Required for screen readers -->
AutomationProperties.AutomationId="UniqueId"
AutomationProperties.Name="Human readable name"
AutomationProperties.HelpText="Description of purpose"
AutomationProperties.LabeledBy="{Binding ElementName=...}"
```

**Elements requiring automation properties:**
- Button, ToggleButton, RepeatButton
- TextBox, ComboBox, Slider
- CheckBox, RadioButton
- ListBox, ListView, DataGrid items
- Custom controls

### 3. Keyboard Navigation Audit

Check for:
- `KeyboardNavigation.TabNavigation="Cycle"` on containers
- `IsTabStop="True"` on interactive elements
- `FocusVisualStyle` definitions
- Keyboard shortcuts (InputBindings)
- Focus scope management

Test tab order makes sense:
- Left-to-right, top-to-bottom
- No trapped focus
- Skip decorative elements

### 4. Color Contrast Audit

WCAG 2.1 AA Requirements:
- **Normal text**: 4.5:1 contrast ratio
- **Large text (14pt bold/18pt)**: 3:1 contrast ratio
- **UI components**: 3:1 against adjacent colors

Check color pairs:
```
Background    Foreground    Ratio    Status
#1A1A1A       #666666       3.4:1    FAIL (normal text)
#1A1A1A       #888888       5.3:1    PASS
#1A1A1A       #AAAAAA       7.5:1    PASS
#252526       #666666       2.9:1    FAIL
```

### 5. Focus Indicator Audit

Every focusable element must have visible focus:
```xml
<Style TargetType="Button">
    <Setter Property="FocusVisualStyle">
        <Setter.Value>
            <Style>
                <Setter Property="Control.Template">
                    <Setter.Value>
                        <ControlTemplate>
                            <Border BorderBrush="{StaticResource AccentBrush}"
                                    BorderThickness="2"
                                    CornerRadius="2"/>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>
        </Setter.Value>
    </Setter>
</Style>
```

### 6. Screen Reader Announcements

Check for live regions and announcements:
```xml
<!-- For dynamic content updates -->
AutomationProperties.LiveSetting="Polite"
AutomationProperties.LiveSetting="Assertive"
```

Verify announcements for:
- Progress updates
- Error messages
- Selection changes
- State changes (playing/paused)

### 7. Generate Detailed Report

```
WCAG 2.1 AA Accessibility Audit
════════════════════════════════════════════

Scan Date: YYYY-MM-DD
Files Scanned: XX
Overall Score: XX/100

CRITICAL ISSUES (Must Fix)
─────────────────────────────────────────────
[FAIL] EditPage.xaml:245
  Issue: Button missing AutomationProperties.Name
  Element: <Button Content="{Binding Icon}"/>
  Fix: Add AutomationProperties.Name="Add clip to timeline"

[FAIL] VideoPlayerControl.xaml:56
  Issue: Contrast ratio 3.4:1 below 4.5:1 requirement
  Element: TimecodeText with Foreground="#666"
  Fix: Change to Foreground="#999" (5.9:1)

WARNINGS (Should Fix)
─────────────────────────────────────────────
[WARN] RestorePage.xaml:128
  Issue: No keyboard shortcut documented
  Element: Search TextBox
  Recommendation: Add tooltip showing Ctrl+F shortcut

[WARN] ColorPage.xaml:89
  Issue: Complex control without HelpText
  Element: ColorWheel
  Recommendation: Add AutomationProperties.HelpText

PASSED CHECKS
─────────────────────────────────────────────
✓ 142 elements have AutomationId
✓ 89 interactive elements have Name
✓ 12 pages have TabNavigation="Cycle"
✓ All buttons have focus indicators

SUMMARY BY CATEGORY
─────────────────────────────────────────────
Category              Pass    Fail    Warn
──────────────────────────────────────────
Automation Props      89      12      5
Keyboard Nav          45      2       3
Color Contrast        67      8       0
Focus Indicators      52      0       2
Screen Reader         23      4       6
──────────────────────────────────────────
Total                 276     26      16
```

### 8. Auto-Fix (if --fix)

Apply automatic fixes for:
- Missing AutomationId (generate from element name/content)
- Missing Name (derive from Content or adjacent Label)
- Low contrast (suggest higher contrast value)
- Missing TabNavigation on containers

```csharp
// Generated AutomationId pattern
AutomationProperties.AutomationId="EditPage_AddClipButton"
AutomationProperties.AutomationId="RestorePage_PresetComboBox"
```

### 9. Verification Commands

After fixes:
```bash
# Verify XAML still compiles
dotnet build src/gui/VapourSynthPortable/VapourSynthPortable.csproj

# Run UI automation tests
dotnet test --filter "Category=Accessibility"
```

## Accessibility Patterns Reference

### Pattern: Labeled Control
```xml
<TextBlock x:Name="ResolutionLabel" Text="Resolution:"/>
<ComboBox AutomationProperties.LabeledBy="{Binding ElementName=ResolutionLabel}"
          AutomationProperties.HelpText="Select output video resolution"/>
```

### Pattern: Icon Button
```xml
<Button AutomationProperties.Name="Play"
        AutomationProperties.HelpText="Start video playback"
        ToolTip="Play (Space)">
    <Path Data="{StaticResource PlayIcon}"/>
</Button>
```

### Pattern: Dynamic Status
```xml
<TextBlock AutomationProperties.LiveSetting="Polite"
           Text="{Binding StatusMessage}"/>
```

### Pattern: Custom Control
```xml
<local:TimelineControl
    AutomationProperties.Name="Video Timeline"
    AutomationProperties.HelpText="Drag clips to arrange, use arrow keys to nudge"
    KeyboardNavigation.IsTabStop="True"/>
```
