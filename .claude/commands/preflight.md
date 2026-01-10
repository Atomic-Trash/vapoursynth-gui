Run all validation checks before committing code.

## Instructions

Execute the following pre-commit validation steps in order:

### 1. Build with Warnings as Errors
```bash
dotnet build src/gui/VapourSynthPortable.sln -warnaserror --verbosity quiet
```
- Report any build errors or warnings
- If build fails, stop and report issues

### 2. Run All Unit Tests
```bash
dotnet test src/gui/VapourSynthPortable.sln --no-build --verbosity normal
```
- Report test count: passed, failed, skipped
- For failures, show test name and error message
- If tests fail, continue but mark preflight as failed

### 3. Check for Unregistered Converters
Search for converter usages in XAML that may not be registered:
- Read `src/gui/VapourSynthPortable/App.xaml` to get registered converters
- Search `src/gui/VapourSynthPortable/**/*.xaml` for `{StaticResource *Converter}` patterns
- Report any converter referenced in XAML but not registered in App.xaml

### 4. Check for Code-Behind Violations
Search for event handlers in XAML that may contain business logic:
- Look for `Click=`, `MouseDown=`, `SelectionChanged=` etc in XAML files
- Check corresponding .xaml.cs files for these handlers
- Flag handlers that contain more than just simple navigation or ViewModel command invocation

### 5. Validate XAML Syntax
```bash
dotnet build src/gui/VapourSynthPortable/VapourSynthPortable.csproj -t:ValidateXaml --verbosity quiet 2>&1
```
- Report any XAML parsing errors
- Note: This is best-effort as ValidateXaml may not be available

### 6. Summary Report
Present a clear pass/fail summary:
```
Preflight Check Results
═══════════════════════════════════════════════

Check                    Status
─────────────────────────────────────────────────
Build (no warnings)      [PASS] / [FAIL]
Unit Tests (148 tests)   [PASS] / [FAIL]
Converters Registered    [PASS] / [WARN]
MVVM Compliance          [PASS] / [WARN]
XAML Syntax              [PASS] / [SKIP]

Overall: READY TO COMMIT / NEEDS ATTENTION
```

### 7. Recommendations
- For any failed or warning items, provide specific fix suggestions
- If overall status is "NEEDS ATTENTION", list what must be fixed before committing
