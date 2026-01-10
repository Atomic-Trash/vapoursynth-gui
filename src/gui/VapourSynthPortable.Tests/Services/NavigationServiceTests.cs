using FluentAssertions;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class NavigationServiceTests
{
    private NavigationService CreateService() => new();

    #region CurrentPage Tests

    [Fact]
    public void CurrentPage_InitialValue_ShouldBeRestore()
    {
        var service = CreateService();

        service.CurrentPage.Should().Be(PageType.Restore);
    }

    #endregion

    #region NavigateTo Tests

    [Fact]
    public void NavigateTo_DifferentPage_ShouldUpdateCurrentPage()
    {
        var service = CreateService();

        service.NavigateTo(PageType.Media);

        service.CurrentPage.Should().Be(PageType.Media);
    }

    [Fact]
    public void NavigateTo_SamePage_ShouldNotRaiseEvent()
    {
        var service = CreateService();
        var eventRaised = false;
        service.PageChanged += (_, _) => eventRaised = true;

        service.NavigateTo(PageType.Restore); // Same as initial

        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void NavigateTo_DifferentPage_ShouldRaisePageChangedEvent()
    {
        var service = CreateService();
        PageChangedEventArgs? receivedArgs = null;
        service.PageChanged += (_, args) => receivedArgs = args;

        service.NavigateTo(PageType.Edit);

        receivedArgs.Should().NotBeNull();
        receivedArgs!.PreviousPage.Should().Be(PageType.Restore);
        receivedArgs.NewPage.Should().Be(PageType.Edit);
    }

    [Theory]
    [InlineData(PageType.Media)]
    [InlineData(PageType.Edit)]
    [InlineData(PageType.Color)]
    [InlineData(PageType.Export)]
    [InlineData(PageType.Settings)]
    public void NavigateTo_AllPageTypes_ShouldWork(PageType page)
    {
        var service = CreateService();

        service.NavigateTo(page);

        service.CurrentPage.Should().Be(page);
    }

    #endregion

    #region GoBack Tests

    [Fact]
    public void CanGoBack_InitialState_ShouldBeFalse()
    {
        var service = CreateService();

        service.CanGoBack.Should().BeFalse();
    }

    [Fact]
    public void CanGoBack_AfterNavigation_ShouldBeTrue()
    {
        var service = CreateService();

        service.NavigateTo(PageType.Media);

        service.CanGoBack.Should().BeTrue();
    }

    [Fact]
    public void GoBack_WithHistory_ShouldReturnToPreviousPage()
    {
        var service = CreateService();
        service.NavigateTo(PageType.Media);
        service.NavigateTo(PageType.Edit);

        var result = service.GoBack();

        result.Should().BeTrue();
        service.CurrentPage.Should().Be(PageType.Media);
    }

    [Fact]
    public void GoBack_WithoutHistory_ShouldReturnFalse()
    {
        var service = CreateService();

        var result = service.GoBack();

        result.Should().BeFalse();
        service.CurrentPage.Should().Be(PageType.Restore);
    }

    [Fact]
    public void GoBack_ShouldRaisePageChangedEvent()
    {
        var service = CreateService();
        service.NavigateTo(PageType.Media);
        PageChangedEventArgs? receivedArgs = null;
        service.PageChanged += (_, args) => receivedArgs = args;

        service.GoBack();

        receivedArgs.Should().NotBeNull();
        receivedArgs!.PreviousPage.Should().Be(PageType.Media);
        receivedArgs.NewPage.Should().Be(PageType.Restore);
    }

    [Fact]
    public void GoBack_MultipleNavigations_ShouldTraverseHistory()
    {
        var service = CreateService();
        service.NavigateTo(PageType.Media);
        service.NavigateTo(PageType.Edit);
        service.NavigateTo(PageType.Color);

        service.GoBack().Should().BeTrue();
        service.CurrentPage.Should().Be(PageType.Edit);

        service.GoBack().Should().BeTrue();
        service.CurrentPage.Should().Be(PageType.Media);

        service.GoBack().Should().BeTrue();
        service.CurrentPage.Should().Be(PageType.Restore);

        service.GoBack().Should().BeFalse();
        service.CurrentPage.Should().Be(PageType.Restore);
    }

    #endregion

    #region GoForward Tests

    [Fact]
    public void CanGoForward_InitialState_ShouldBeFalse()
    {
        var service = CreateService();

        service.CanGoForward.Should().BeFalse();
    }

    [Fact]
    public void CanGoForward_AfterGoBack_ShouldBeTrue()
    {
        var service = CreateService();
        service.NavigateTo(PageType.Media);
        service.GoBack();

        service.CanGoForward.Should().BeTrue();
    }

    [Fact]
    public void GoForward_AfterGoBack_ShouldReturnToPage()
    {
        var service = CreateService();
        service.NavigateTo(PageType.Media);
        service.GoBack();

        var result = service.GoForward();

        result.Should().BeTrue();
        service.CurrentPage.Should().Be(PageType.Media);
    }

    [Fact]
    public void GoForward_WithoutForwardHistory_ShouldReturnFalse()
    {
        var service = CreateService();
        service.NavigateTo(PageType.Media);

        var result = service.GoForward();

        result.Should().BeFalse();
    }

    [Fact]
    public void GoForward_ShouldRaisePageChangedEvent()
    {
        var service = CreateService();
        service.NavigateTo(PageType.Media);
        service.GoBack();
        PageChangedEventArgs? receivedArgs = null;
        service.PageChanged += (_, args) => receivedArgs = args;

        service.GoForward();

        receivedArgs.Should().NotBeNull();
        receivedArgs!.PreviousPage.Should().Be(PageType.Restore);
        receivedArgs.NewPage.Should().Be(PageType.Media);
    }

    #endregion

    #region Forward History Clearing Tests

    [Fact]
    public void NavigateTo_AfterGoBack_ShouldClearForwardHistory()
    {
        var service = CreateService();
        service.NavigateTo(PageType.Media);
        service.NavigateTo(PageType.Edit);
        service.GoBack();
        service.CanGoForward.Should().BeTrue();

        service.NavigateTo(PageType.Color);

        service.CanGoForward.Should().BeFalse();
        service.CurrentPage.Should().Be(PageType.Color);
    }

    #endregion

    #region Complex Navigation Scenarios

    [Fact]
    public void ComplexNavigation_BackAndForward_ShouldMaintainCorrectState()
    {
        var service = CreateService();

        // Navigate through several pages
        service.NavigateTo(PageType.Media);
        service.NavigateTo(PageType.Edit);
        service.NavigateTo(PageType.Color);
        service.NavigateTo(PageType.Export);

        // Go back twice
        service.GoBack();
        service.GoBack();
        service.CurrentPage.Should().Be(PageType.Edit);

        // Go forward once
        service.GoForward();
        service.CurrentPage.Should().Be(PageType.Color);

        // Navigate to new page (should clear forward history)
        service.NavigateTo(PageType.Settings);
        service.CurrentPage.Should().Be(PageType.Settings);
        service.CanGoForward.Should().BeFalse();

        // Back should go to Color
        service.GoBack();
        service.CurrentPage.Should().Be(PageType.Color);
    }

    #endregion
}
