using FluentAssertions;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Tests.Models;

public class PageTypeTests
{
    #region ToDisplayName Tests

    [Theory]
    [InlineData(PageType.Media, "Media")]
    [InlineData(PageType.Restore, "Restore")]
    [InlineData(PageType.Edit, "Edit")]
    [InlineData(PageType.Color, "Color")]
    [InlineData(PageType.Export, "Export")]
    [InlineData(PageType.Settings, "Settings")]
    public void ToDisplayName_AllPageTypes_ShouldReturnCorrectName(PageType pageType, string expected)
    {
        pageType.ToDisplayName().Should().Be(expected);
    }

    #endregion

    #region ParsePageType Tests

    [Theory]
    [InlineData("Media", PageType.Media)]
    [InlineData("Restore", PageType.Restore)]
    [InlineData("Edit", PageType.Edit)]
    [InlineData("Color", PageType.Color)]
    [InlineData("Export", PageType.Export)]
    [InlineData("Settings", PageType.Settings)]
    public void ParsePageType_ValidNames_ShouldReturnCorrectType(string name, PageType expected)
    {
        PageTypeExtensions.ParsePageType(name).Should().Be(expected);
    }

    [Theory]
    [InlineData("media")]
    [InlineData("MEDIA")]
    [InlineData("MeDiA")]
    public void ParsePageType_CaseInsensitive_ShouldWork(string name)
    {
        PageTypeExtensions.ParsePageType(name).Should().Be(PageType.Media);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParsePageType_NullOrWhitespace_ShouldReturnNull(string? name)
    {
        PageTypeExtensions.ParsePageType(name).Should().BeNull();
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("Home")]
    [InlineData("Dashboard")]
    public void ParsePageType_InvalidNames_ShouldReturnNull(string name)
    {
        PageTypeExtensions.ParsePageType(name).Should().BeNull();
    }

    #endregion

    #region Enum Coverage Tests

    [Fact]
    public void PageType_ShouldHaveExpectedValues()
    {
        var values = Enum.GetValues<PageType>();

        values.Should().HaveCount(6);
        values.Should().Contain(PageType.Media);
        values.Should().Contain(PageType.Restore);
        values.Should().Contain(PageType.Edit);
        values.Should().Contain(PageType.Color);
        values.Should().Contain(PageType.Export);
        values.Should().Contain(PageType.Settings);
    }

    #endregion
}
