using System.Windows;
using VapourSynthPortable.ViewModels.NodeEditor;

namespace VapourSynthPortable.Tests.ViewModels.NodeEditor;

/// <summary>
/// Tests for NodeEditorViewModel that manages the visual scripting interface.
/// </summary>
public class NodeEditorViewModelTests
{
    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        // Arrange & Act
        var sut = new NodeEditorViewModel();

        // Assert
        Assert.Equal(1.0, sut.ZoomLevel);
        Assert.Equal("Ready", sut.StatusMessage);
        Assert.Empty(sut.Nodes);
        Assert.Empty(sut.Connections);
    }

    [Fact]
    public void Constructor_InitializesAvailableNodes()
    {
        // Arrange & Act
        var sut = new NodeEditorViewModel();

        // Assert
        Assert.NotEmpty(sut.AvailableNodes);
    }

    [Fact]
    public void Constructor_InitiallyCannotUndo()
    {
        // Arrange & Act
        var sut = new NodeEditorViewModel();

        // Assert
        Assert.False(sut.CanUndo);
        Assert.False(sut.CanRedo);
    }

    [Fact]
    public void ZoomInCommand_IncreasesZoomLevel()
    {
        // Arrange
        var sut = new NodeEditorViewModel();
        var initialZoom = sut.ZoomLevel;

        // Act
        sut.ZoomInCommand.Execute(null);

        // Assert
        Assert.True(sut.ZoomLevel > initialZoom);
    }

    [Fact]
    public void ZoomOutCommand_DecreasesZoomLevel()
    {
        // Arrange
        var sut = new NodeEditorViewModel();
        var initialZoom = sut.ZoomLevel;

        // Act
        sut.ZoomOutCommand.Execute(null);

        // Assert
        Assert.True(sut.ZoomLevel < initialZoom);
    }

    [Fact]
    public void ZoomLevel_HasMaximumLimit()
    {
        // Arrange
        var sut = new NodeEditorViewModel();

        // Act - zoom in many times
        for (int i = 0; i < 20; i++)
        {
            sut.ZoomInCommand.Execute(null);
        }

        // Assert
        Assert.True(sut.ZoomLevel <= 2.0);
    }

    [Fact]
    public void ZoomLevel_HasMinimumLimit()
    {
        // Arrange
        var sut = new NodeEditorViewModel();

        // Act - zoom out many times
        for (int i = 0; i < 20; i++)
        {
            sut.ZoomOutCommand.Execute(null);
        }

        // Assert
        Assert.True(sut.ZoomLevel >= 0.25);
    }

    [Fact]
    public void ResetZoomCommand_SetsZoomToOne()
    {
        // Arrange
        var sut = new NodeEditorViewModel();
        sut.ZoomInCommand.Execute(null);
        sut.ZoomInCommand.Execute(null);

        // Act
        sut.ResetZoomCommand.Execute(null);

        // Assert
        Assert.Equal(1.0, sut.ZoomLevel);
    }

    [Fact]
    public void SelectedNode_InitiallyNull()
    {
        // Arrange & Act
        var sut = new NodeEditorViewModel();

        // Assert
        Assert.Null(sut.SelectedNode);
    }

    [Fact]
    public void GeneratedScript_InitiallyEmpty()
    {
        // Arrange & Act
        var sut = new NodeEditorViewModel();

        // Assert
        Assert.Equal("", sut.GeneratedScript);
    }

    [Fact]
    public void NodeSearchText_CanBeSet()
    {
        // Arrange
        var sut = new NodeEditorViewModel();

        // Act
        sut.NodeSearchText = "source";

        // Assert
        Assert.Equal("source", sut.NodeSearchText);
    }

    [Fact]
    public void ViewportOffset_DefaultsToZero()
    {
        // Arrange & Act
        var sut = new NodeEditorViewModel();

        // Assert
        Assert.Equal(default(Point), sut.ViewportOffset);
    }

    // TODO: Add more tests for:
    // - Node creation and deletion
    // - Connection management
    // - Script generation
    // - Undo/Redo operations
}
