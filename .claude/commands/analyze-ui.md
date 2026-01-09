Perform a WCAG 2.1 AA accessibility audit on UI controls.

Arguments: `$ARGUMENTS` (optional: specific control or page to analyze)

Instructions:
1. If specific control/page provided in `$ARGUMENTS`:
   - Focus analysis on that component only
   - Example: `/analyze-ui ColorPage` or `/analyze-ui ScopesControl`

2. If no argument, scan all UI files:
   - `src/gui/VapourSynthPortable/Pages/*.xaml`
   - `src/gui/VapourSynthPortable/Controls/*.xaml`
   - `src/gui/VapourSynthPortable/MainWindow.xaml`

3. For each XAML file, check:

**Color Contrast (WCAG 2.1 AA requires 4.5:1 for normal text, 3:1 for large text):**
- TextBlock Foreground values against Background
- Common dark theme issues:
  - `#666` on `#1A1A1A` = 3.4:1 (FAIL for normal text)
  - `#888` on `#1A1A1A` = 5.3:1 (PASS)
  - `#AAA` on `#1A1A1A` = 7.5:1 (PASS)
- Flag any Foreground below `#888` on dark backgrounds

**Interactive Element Visibility:**
- Buttons must have visible boundaries or sufficient contrast
- Sliders need visible track and thumb
- CheckBox/RadioButton indicators must be distinguishable

**Font Sizes:**
- Minimum 10px for UI text (12px preferred)
- Flag any FontSize below 10

**Tooltips:**
- Interactive controls should have ToolTip property
- List controls missing tooltips

**Focus Indicators:**
- Check for FocusVisualStyle definitions
- Report missing focus indicators

4. Generate report format:
```
## WCAG 2.1 AA Audit Report

### Critical Issues (Must Fix)
- [File:Line] Description of contrast failure

### Warnings (Should Fix)
- [File:Line] Missing tooltip on interactive element

### Passed Checks
- X controls have adequate contrast
- Y controls have tooltips

### Recommendations
- Suggested fixes with specific hex values
```

5. Provide fix suggestions:
- For contrast: suggest replacement hex values
- For tooltips: suggest appropriate tooltip text
- For font size: suggest minimum 10px

6. Common patterns to apply:
- Labels: `#888` -> `#AAA` for better contrast
- Secondary text: `#666` -> `#999`
- Disabled: `#444` -> `#666`
