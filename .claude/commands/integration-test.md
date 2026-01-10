Run integration tests for a feature.

## Usage
```
/integration-test [feature-name]
```

## Instructions

### 1. Determine Test Scope
Based on the feature name provided, identify relevant test files:

| Feature | Test Files |
|---------|-----------|
| restore | `**/RestoreViewModelTests.cs`, `**/RestorePageTests.cs` |
| color | `**/ColorViewModelTests.cs`, `**/ColorPageTests.cs` |
| edit | `**/EditViewModelTests.cs`, `**/EditPageTests.cs` |
| media | `**/MediaViewModelTests.cs`, `**/MediaPageTests.cs` |
| export | `**/ExportViewModelTests.cs`, `**/ExportPageTests.cs` |
| converters | `**/ConverterTests.cs` |
| services | `**/Services/*Tests.cs` |
| all | All test files |

If no feature specified, run all tests.

### 2. Build Test Project
```bash
dotnet build src/gui/VapourSynthPortable.Tests/VapourSynthPortable.Tests.csproj --verbosity quiet
```

### 3. Run Tests with Filter
```bash
dotnet test src/gui/VapourSynthPortable.Tests/VapourSynthPortable.Tests.csproj --no-build --verbosity normal --filter "FullyQualifiedName~{feature}"
```

If running all tests:
```bash
dotnet test src/gui/VapourSynthPortable.Tests/VapourSynthPortable.Tests.csproj --no-build --verbosity normal
```

### 4. Collect Results
Parse test output:
- Total tests run
- Passed count
- Failed count
- Skipped count
- Duration

### 5. For Failed Tests
For each failure, provide:
- Test name
- Expected vs actual result
- Stack trace (condensed)
- Relevant source code location
- Suggested fix

### 6. Code Coverage (Optional)
If user requests coverage:
```bash
dotnet test src/gui/VapourSynthPortable.Tests/VapourSynthPortable.Tests.csproj --collect:"XPlat Code Coverage"
```
- Report coverage percentage for tested namespaces
- Highlight files with low coverage (<70%)

### 7. Summary Report
```
Integration Test Report - {Feature}
═══════════════════════════════════════════════

Test Results
─────────────────────────────────────────────────
Total:    X tests
Passed:   X (XX%)
Failed:   X (XX%)
Skipped:  X (XX%)
Duration: X.XXs

Failed Tests
─────────────────────────────────────────────────
1. TestClassName.TestMethodName
   Expected: <value>
   Actual:   <value>
   Location: File.cs:123

Coverage (if requested)
─────────────────────────────────────────────────
Namespace               Coverage
RestoreViewModel        85%
Converters              72%
Services                90%
```

### 8. Recommendations
- For failing tests, suggest potential fixes
- For low coverage areas, suggest additional test cases
- Note any test categories that should be added
