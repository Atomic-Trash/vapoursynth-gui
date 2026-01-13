Run tests with code coverage and generate report.

## Arguments
- `--threshold <percent>` - Minimum coverage percentage (default: 70)
- `--open` - Open HTML report in browser after generation
- `--filter <pattern>` - Filter tests by name pattern

## Instructions

### 1. Run Tests with Coverage Collection

```bash
dotnet test src/gui/VapourSynthPortable.Tests/VapourSynthPortable.Tests.csproj \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults \
    --verbosity normal
```

If filter is provided:
```bash
dotnet test ... --filter "FullyQualifiedName~<pattern>"
```

### 2. Locate Coverage Results

Find the latest coverage file:
```bash
# Coverage XML is in TestResults/<guid>/coverage.cobertura.xml
```

### 3. Parse Coverage Summary

Extract from cobertura XML:
- Line coverage percentage
- Branch coverage percentage
- Covered/Total lines per assembly
- Uncovered files list

### 4. Generate Detailed Report

If `reportgenerator` is available:
```bash
reportgenerator \
    -reports:"TestResults/**/coverage.cobertura.xml" \
    -targetdir:"TestResults/CoverageReport" \
    -reporttypes:Html
```

If not available, provide installation instructions:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### 5. Identify Coverage Gaps

List files with coverage below threshold:
- Sort by coverage percentage ascending
- Highlight critical files (ViewModels, Services)
- Note files with 0% coverage

### 6. Summary Report

```
Code Coverage Report
════════════════════════════════════════════

Overall Coverage: XX.X%
Threshold: 70%
Status: [PASS] / [FAIL]

Coverage by Assembly:
─────────────────────────────────────────────
VapourSynthPortable          XX.X%  ████████░░
VapourSynthPortable.Tests   100.0%  ██████████

Top Uncovered Files:
─────────────────────────────────────────────
1. ViewModels/EditViewModel.cs           0.0%
2. ViewModels/NodeEditor/NodeViewModel.cs 0.0%
3. Services/MpvPlayer.cs                 12.3%

Files Meeting Threshold:
─────────────────────────────────────────────
✓ Services/MediaPoolService.cs          85.2%
✓ ViewModels/ExportViewModel.cs         78.4%
✓ Services/ProjectService.cs            72.1%

Test Summary:
─────────────────────────────────────────────
Total Tests: XXX
Passed: XXX
Failed: 0
Skipped: 0

HTML Report: TestResults/CoverageReport/index.html
```

### 7. Open Report (if --open)

```bash
# Windows
start TestResults/CoverageReport/index.html

# macOS
open TestResults/CoverageReport/index.html

# Linux
xdg-open TestResults/CoverageReport/index.html
```

### 8. Recommendations

Based on coverage gaps, suggest:
- Which test files to create
- Which methods need tests
- Priority order based on file importance

## Coverage Configuration

Create `coverlet.runsettings` if not exists:
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Exclude>[*]*.Migrations.*,[*]*.Designer.cs</Exclude>
          <ExcludeByAttribute>GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

## Examples

```bash
# Run with default 70% threshold
/coverage

# Require 80% coverage
/coverage --threshold 80

# Run and open report
/coverage --open

# Run specific tests with coverage
/coverage --filter "EditViewModel"
```
