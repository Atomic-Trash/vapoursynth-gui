Find and fix bare catch blocks with proper logging.

## Arguments
- `--file <path>` - Optional: limit to specific file
- `--dry-run` - Show issues without fixing

## Instructions

### 1. Search for Bare Catch Blocks
Search for these patterns in `src/gui/VapourSynthPortable/`:
```
catch\s*\{
catch\s*\n\s*\{
```

Also search for:
- `catch { }` (empty catch)
- `catch { // ` (catch with only comment)
- `catch (Exception)` without variable name

### 2. For Each Issue Found
Report in this format:
```
File: path/to/file.cs
Line: 123
Current:
    catch
    {
        return false;
    }

Suggested Fix:
    catch (Exception ex)
    {
        Log.Debug(ex, "Description of what failed");
        return false;
    }
```

### 3. Determine Exception Type
Choose the most specific exception type:
- `JsonException` for JSON parsing
- `FormatException` for string/color parsing
- `IOException` for file operations
- `InvalidOperationException` for state errors
- `Exception` as fallback for unknown error types

### 4. Determine Logging Level
- `LogError` - User-facing failures, requires action
- `LogWarning` - Recoverable errors, fallback used
- `LogDebug` - Expected failures during normal operation
- `Log.Debug` (Serilog static) - For static methods

### 5. Apply Fixes (unless --dry-run)
For each bare catch:
1. Add exception variable: `catch (ExceptionType ex)`
2. Add logging statement with context
3. Keep existing fallback behavior
4. Ensure logger is available (inject or use static Log)

### 6. Verify Changes
```bash
dotnet build src/gui/VapourSynthPortable/VapourSynthPortable.csproj --verbosity quiet
```

### 7. Summary Report
```
Exception Handling Audit
════════════════════════════════════════════

Files Scanned: X
Issues Found: Y
Issues Fixed: Z

Fixed:
  ✓ path/to/file.cs:123 - Added logging for JSON parse error
  ✓ path/to/file.cs:456 - Added logging for GPU detection

Remaining Issues:
  ⚠ path/to/other.cs:789 - Needs manual review (complex catch)

Build Status: [PASS] / [FAIL]
```

## Exception Handling Patterns

### Pattern: Logging with Context
```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to {Action} for {Entity}", action, entityName);
    // fallback behavior
}
```

### Pattern: Static Serilog Logger
```csharp
catch (JsonException ex)
{
    Log.Warning(ex, "Serialization failed for type {Type}", value?.GetType().Name);
    return fallbackValue;
}
```

### Pattern: User-Facing Error
```csharp
catch (FormatException)
{
    ToastService.Instance.ShowError("Invalid format", "Please use correct format");
}
```
