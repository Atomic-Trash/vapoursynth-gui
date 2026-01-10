Build and validate app runtime behavior.

## Instructions

### 1. Build the Application
```bash
dotnet build src/gui/VapourSynthPortable/VapourSynthPortable.csproj --verbosity quiet
```
- Report any build errors
- If build fails, stop and report issues

### 2. Check for Recent Crash Reports
- Look for crash reports in `%LocalAppData%/VapourSynthStudio/crashes/`
- List any crash reports from the last 24 hours
- Read and summarize the most recent crash report if exists

### 3. Launch the Application
```bash
dotnet run --project src/gui/VapourSynthPortable/VapourSynthPortable.csproj
```
- The app will launch in a separate window
- Let user interact with the app to test functionality
- User should close app when done testing

### 4. Check Application Logs
After app closes:
- Read recent log entries from `%LocalAppData%/VapourSynthStudio/logs/`
- Look for ERROR or CRITICAL log entries
- Look for binding errors (pattern: "BindingExpression", "Cannot find", "data item null")
- Report any issues found

### 5. Summary Report
```
Runtime Validation Report
═══════════════════════════════════════════════

Build Status:        [OK] / [FAILED]
Recent Crashes:      [0 in last 24h] / [X crashes found]
Startup:             [OK] / [ERRORS DETECTED]
Binding Errors:      [0 found] / [X found]
Runtime Errors:      [0 found] / [X found]

Overall: APP RUNNING CORRECTLY / ISSUES DETECTED
```

### 6. Issue Details
For each issue found:
- Show the relevant log entry or crash report excerpt
- Suggest potential causes
- Recommend next steps to investigate

## Notes
- This skill is useful after making UI changes
- Check crash reports after unexpected behavior
- Binding errors often indicate missing DataContext or typos in property names
