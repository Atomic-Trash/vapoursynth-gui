Generate comprehensive xUnit test file for a service or ViewModel.

## Arguments
- `<ClassName>` - Name of the class to test (e.g., "EditViewModel", "MpvPlayer") - required
- `--mock-deps` - Generate mock setup for all dependencies
- `--edge-cases` - Include edge case and error condition tests

## Instructions

### 1. Locate the Class
Search for the class in:
- `src/gui/VapourSynthPortable/Services/`
- `src/gui/VapourSynthPortable/ViewModels/`
- `src/gui/VapourSynthPortable/Models/`

### 2. Analyze the Class
Extract:
- Constructor dependencies (for mocking)
- Public methods (for test methods)
- Public properties (for state tests)
- Events (for event tests)
- Commands (RelayCommand attributes)

### 3. Determine Test File Location
- ViewModels: `VapourSynthPortable.Tests/ViewModels/<ClassName>Tests.cs`
- Services: `VapourSynthPortable.Tests/Services/<ClassName>Tests.cs`
- Models: `VapourSynthPortable.Tests/Models/<ClassName>Tests.cs`
- Node Editor: `VapourSynthPortable.Tests/ViewModels/NodeEditor/<ClassName>Tests.cs`

### 4. Generate Test File Structure

```csharp
using System.Collections.ObjectModel;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.ViewModels;

public class <ClassName>Tests
{
    #region Test Setup

    private static Mock<IDependency> CreateMockDependency()
    {
        var mock = new Mock<IDependency>();
        // Setup default behavior
        return mock;
    }

    private static <ClassName> CreateSut(/* optional overrides */)
    {
        return new <ClassName>(
            CreateMockDependency().Object,
            // ... other dependencies
        );
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut.SomeProperty);
    }

    #endregion

    #region Method Tests

    [Fact]
    public void MethodName_WhenCondition_ExpectedBehavior()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.MethodName();

        // Assert
        Assert.True(sut.SomeResult);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void MethodName_WithNullInput_HandlesGracefully()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var exception = Record.Exception(() => sut.MethodName(null));
        Assert.Null(exception); // or Assert.Throws<ArgumentNullException>
    }

    #endregion
}
```

### 5. Generate Tests for Each Public Method

For each method, create tests for:
- **Happy path**: Normal input, expected output
- **Edge cases**: Null, empty, boundary values
- **Error conditions**: Invalid input, exceptions
- **State changes**: Property updates, events raised

### 6. Generate Theory Tests for Multiple Scenarios

```csharp
[Theory]
[InlineData("input1", "expected1")]
[InlineData("input2", "expected2")]
[InlineData("", null)] // edge case
public void MethodName_WithVariousInputs_ReturnsExpected(string input, string expected)
{
    // Arrange
    var sut = CreateSut();

    // Act
    var result = sut.MethodName(input);

    // Assert
    Assert.Equal(expected, result);
}
```

### 7. Mock Common Dependencies

| Interface | Mock Setup |
|-----------|------------|
| IMediaPoolService | MediaPool = new ObservableCollection, CurrentSource = null |
| ISettingsService | Load() returns new AppSettings() |
| INavigationService | NavigateTo() verifiable |
| IProjectService | LoadAsync/SaveAsync verifiable |
| IVapourSynthService | ProcessAsync returns Result.Success() |

### 8. Verify and Report

```
Test Generation Report
════════════════════════════════════════════

Class: <ClassName>
Location: <FilePath>

Generated Tests:
  ✓ Constructor_InitializesDefaultValues
  ✓ Constructor_InjectsAllDependencies
  ✓ Method1_WhenCalled_ReturnsExpected
  ✓ Method1_WithNullInput_ThrowsException
  ✓ Method2_WhenCondition_UpdatesState
  ✓ Property_WhenSet_RaisesPropertyChanged
  ... (X total tests)

Mock Dependencies:
  • IMediaPoolService
  • ISettingsService
  • ILogger<ClassName>

Run tests with:
  dotnet test --filter "FullyQualifiedName~<ClassName>Tests"
```

## Examples

```bash
# Generate basic tests
/add-tests EditViewModel

# Generate with mock setup helpers
/add-tests MpvPlayer --mock-deps

# Generate comprehensive tests including edge cases
/add-tests NodeEditorViewModel --mock-deps --edge-cases
```
