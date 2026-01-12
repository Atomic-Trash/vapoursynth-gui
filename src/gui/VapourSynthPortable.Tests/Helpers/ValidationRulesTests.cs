using System.Globalization;
using VapourSynthPortable.Helpers;

namespace VapourSynthPortable.Tests.Helpers;

public class PositiveIntegerRuleTests
{
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    #region Valid Input Tests

    [Fact]
    public void Validate_ValidInteger_ReturnsValid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { Minimum = 1, Maximum = 100 };

        // Act
        var result = rule.Validate("50", _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AtMinimum_ReturnsValid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { Minimum = 10, Maximum = 100 };

        // Act
        var result = rule.Validate("10", _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AtMaximum_ReturnsValid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { Minimum = 1, Maximum = 100 };

        // Act
        var result = rule.Validate("100", _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Invalid Input Tests

    [Fact]
    public void Validate_BelowMinimum_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { Minimum = 10, Maximum = 100, FieldName = "Test" };

        // Act
        var result = rule.Validate("5", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("at least 10", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_AboveMaximum_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { Minimum = 1, Maximum = 100, FieldName = "Test" };

        // Act
        var result = rule.Validate("150", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("cannot exceed 100", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_NonInteger_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { FieldName = "Test" };

        // Act
        var result = rule.Validate("abc", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("whole number", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_Decimal_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { FieldName = "Test" };

        // Act
        var result = rule.Validate("10.5", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("whole number", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_EmptyString_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { FieldName = "Test" };

        // Act
        var result = rule.Validate("", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_Null_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { FieldName = "Test" };

        // Act
        var result = rule.Validate(null!, _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_NegativeNumber_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { Minimum = 1, Maximum = 100, FieldName = "Test" };

        // Act
        var result = rule.Validate("-5", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("at least 1", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_Zero_WithMinimumOne_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveIntegerRule { Minimum = 1, Maximum = 100, FieldName = "Test" };

        // Act
        var result = rule.Validate("0", _culture);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion

    #region FieldName Tests

    [Fact]
    public void Validate_FieldNameInErrorMessage()
    {
        // Arrange
        var rule = new PositiveIntegerRule { FieldName = "Max cache size" };

        // Act
        var result = rule.Validate("", _culture);

        // Assert
        Assert.Contains("Max cache size", result.ErrorContent?.ToString());
    }

    #endregion
}

public class DirectoryPathRuleTests
{
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    #region Valid Path Tests

    [Fact]
    public void Validate_ValidAbsolutePath_ReturnsValid()
    {
        // Arrange
        var rule = new DirectoryPathRule { AllowEmpty = false };

        // Act
        var result = rule.Validate(@"C:\Users\Test\Documents", _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyWithAllowEmpty_ReturnsValid()
    {
        // Arrange
        var rule = new DirectoryPathRule { AllowEmpty = true };

        // Act
        var result = rule.Validate("", _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_UncPath_ReturnsValid()
    {
        // Arrange
        var rule = new DirectoryPathRule { AllowEmpty = false };

        // Act
        var result = rule.Validate(@"\\server\share\folder", _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Invalid Path Tests

    [Fact]
    public void Validate_EmptyWithoutAllowEmpty_ReturnsInvalid()
    {
        // Arrange
        var rule = new DirectoryPathRule { AllowEmpty = false, FieldName = "Test" };

        // Act
        var result = rule.Validate("", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_RelativePath_ReturnsInvalid()
    {
        // Arrange
        var rule = new DirectoryPathRule { FieldName = "Test" };

        // Act
        var result = rule.Validate(@"folder\subfolder", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("absolute path", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_InvalidCharacters_ReturnsInvalid()
    {
        // Arrange
        var rule = new DirectoryPathRule { FieldName = "Test" };

        // Act - Use actual invalid path characters
        var result = rule.Validate("C:\\Test\0Folder", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("invalid characters", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_Null_ReturnsInvalid()
    {
        // Arrange
        var rule = new DirectoryPathRule { AllowEmpty = false, FieldName = "Test" };

        // Act
        var result = rule.Validate(null!, _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorContent?.ToString());
    }

    #endregion

    #region RequireExists Tests

    [Fact]
    public void Validate_NonExistentPath_WhenRequireExists_ReturnsInvalid()
    {
        // Arrange
        var rule = new DirectoryPathRule { RequireExists = true, FieldName = "Test" };

        // Act
        var result = rule.Validate(@"C:\ThisPathDoesNotExist12345\Folder", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("does not exist", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_ExistingPath_WhenRequireExists_ReturnsValid()
    {
        // Arrange
        var rule = new DirectoryPathRule { RequireExists = true };
        var tempPath = Path.GetTempPath();

        // Act
        var result = rule.Validate(tempPath, _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region FieldName Tests

    [Fact]
    public void Validate_FieldNameInErrorMessage()
    {
        // Arrange
        var rule = new DirectoryPathRule { FieldName = "Cache directory", AllowEmpty = false };

        // Act
        var result = rule.Validate("", _culture);

        // Assert
        Assert.Contains("Cache directory", result.ErrorContent?.ToString());
    }

    #endregion
}

public class PositiveDecimalRuleTests
{
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    #region Valid Input Tests

    [Fact]
    public void Validate_ValidDecimal_ReturnsValid()
    {
        // Arrange
        var rule = new PositiveDecimalRule { Minimum = 0.1, Maximum = 10.0 };

        // Act
        var result = rule.Validate("5.5", _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidInteger_ReturnsValid()
    {
        // Arrange
        var rule = new PositiveDecimalRule { Minimum = 0.1, Maximum = 10.0 };

        // Act
        var result = rule.Validate("5", _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AtMinimum_ReturnsValid()
    {
        // Arrange
        var rule = new PositiveDecimalRule { Minimum = 0.1, Maximum = 10.0 };

        // Act
        var result = rule.Validate("0.1", _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AtMaximum_ReturnsValid()
    {
        // Arrange
        var rule = new PositiveDecimalRule { Minimum = 0.1, Maximum = 10.0 };

        // Act
        var result = rule.Validate("10.0", _culture);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Invalid Input Tests

    [Fact]
    public void Validate_BelowMinimum_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveDecimalRule { Minimum = 0.5, Maximum = 10.0, FieldName = "Test" };

        // Act
        var result = rule.Validate("0.1", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("at least 0.5", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_AboveMaximum_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveDecimalRule { Minimum = 0.1, Maximum = 10.0, FieldName = "Test" };

        // Act
        var result = rule.Validate("15.0", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("cannot exceed 10", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_NonNumeric_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveDecimalRule { FieldName = "Test" };

        // Act
        var result = rule.Validate("abc", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("must be a number", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_EmptyString_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveDecimalRule { FieldName = "Test" };

        // Act
        var result = rule.Validate("", _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorContent?.ToString());
    }

    [Fact]
    public void Validate_Null_ReturnsInvalid()
    {
        // Arrange
        var rule = new PositiveDecimalRule { FieldName = "Test" };

        // Act
        var result = rule.Validate(null!, _culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorContent?.ToString());
    }

    #endregion
}
