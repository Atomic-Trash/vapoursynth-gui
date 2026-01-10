Run static code analysis to catch potential issues.

## Instructions

Perform the following static analysis checks:

### 1. Potential Null Reference Issues
Search for patterns that may cause null reference exceptions:
- Properties accessed without null checks after `as` casts
- `!` (null-forgiving) operators that may mask real issues
- Missing null checks on nullable parameters
- Pattern: `(\w+)\s+as\s+\w+.*\.\w+` without subsequent null check

Focus on:
- `src/gui/VapourSynthPortable/ViewModels/*.cs`
- `src/gui/VapourSynthPortable/Pages/*.xaml.cs`

### 2. MVVM Pattern Violations
Check code-behind files for business logic:
- Event handlers should only call ViewModel commands or navigate
- Flag any direct data manipulation in code-behind
- Look for database/file operations in .xaml.cs files

Patterns to flag:
- Direct property assignments beyond simple UI state
- LINQ queries in code-behind
- Service calls from code-behind

### 3. Missing INotifyPropertyChanged
For classes that should be observable:
- Check ViewModels inherit from `ObservableObject` or implement `INotifyPropertyChanged`
- Check that properties use `[ObservableProperty]` attribute or call `OnPropertyChanged()`
- Flag any public settable properties in ViewModels without notification

### 4. Converter Registration Check
- List all classes implementing `IValueConverter` or `IMultiValueConverter`
- Cross-reference with App.xaml registrations
- Report any converters not registered

### 5. Unused Code Detection
Look for:
- Private methods with no callers
- Properties with no references
- Using directives not needed
- Commands defined but never bound

### 6. Memory Leak Patterns
Check for common WPF memory leak patterns:
- Event subscriptions without corresponding unsubscriptions
- Static event handlers
- Missing `Unloaded` handler when `Loaded` is used
- Long-lived references to short-lived objects

### 7. Summary Report
```
Static Code Analysis Report
═══════════════════════════════════════════════

Category                    Issues Found
─────────────────────────────────────────────────
Potential Null Refs         [X issues]
MVVM Violations             [X issues]
Missing Property Change     [X issues]
Unregistered Converters     [X issues]
Unused Code                 [X issues]
Memory Leak Patterns        [X issues]

Total Issues: X (Y critical, Z warnings)
```

### 8. Issue Details
For each issue:
- File path and line number
- Issue description
- Severity (Critical/Warning/Info)
- Suggested fix

### 9. Auto-Fix Suggestions
For mechanical issues that can be auto-fixed:
- Offer to add missing null checks
- Offer to register missing converters
- Offer to add missing unsubscriptions
