using System.Windows;
using VapourSynthPortable.Models.NodeModels;
using VapourSynthPortable.ViewModels.NodeEditor;

namespace VapourSynthPortable.Tests.ViewModels.NodeEditor;

/// <summary>
/// Tests for NodeViewModel and its derived types (Source, Filter, Output nodes).
/// </summary>
public class NodeViewModelTests
{
    [Fact]
    public void SourceNodeViewModel_DefaultValues()
    {
        // Arrange & Act
        var node = new SourceNodeViewModel(new SourceNode());

        // Assert
        Assert.NotNull(node);
        Assert.Contains("Source", node.Title);
    }

    [Fact]
    public void SourceNodeViewModel_Location_CanBeSet()
    {
        // Arrange
        var node = new SourceNodeViewModel(new SourceNode());

        // Act
        node.Location = new Point(150, 250);

        // Assert
        Assert.Equal(150, node.Location.X);
        Assert.Equal(250, node.Location.Y);
    }

    [Fact]
    public void OutputNodeViewModel_DefaultValues()
    {
        // Arrange & Act
        var node = new OutputNodeViewModel(new OutputNode());

        // Assert
        Assert.NotNull(node);
        Assert.Contains("Output", node.Title);
    }

    [Fact]
    public void OutputNodeViewModel_OutputIndex_DefaultsToZero()
    {
        // Arrange & Act
        var node = new OutputNodeViewModel(new OutputNode());

        // Assert
        Assert.Equal(0, node.OutputIndex);
    }

    [Fact]
    public void OutputNodeViewModel_SetOutputIndex()
    {
        // Arrange
        var node = new OutputNodeViewModel(new OutputNode());

        // Act
        node.OutputIndex = 1;

        // Assert
        Assert.Equal(1, node.OutputIndex);
    }

    [Fact]
    public void NodeViewModel_LocationRaisesPropertyChanged()
    {
        // Arrange
        var node = new SourceNodeViewModel(new SourceNode());
        var propertyChanged = false;
        node.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(node.Location))
                propertyChanged = true;
        };

        // Act
        node.Location = new Point(200, 300);

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void NodeViewModel_IsSelected_RaisesPropertyChanged()
    {
        // Arrange
        var node = new SourceNodeViewModel(new SourceNode());
        var propertyChanged = false;
        node.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(node.IsSelected))
                propertyChanged = true;
        };

        // Act
        node.IsSelected = true;

        // Assert
        Assert.True(propertyChanged);
        Assert.True(node.IsSelected);
    }

    [Fact]
    public void SourceNodeViewModel_HasOutputConnector()
    {
        // Arrange & Act
        var node = new SourceNodeViewModel(new SourceNode());

        // Assert
        Assert.NotEmpty(node.Output);
    }

    [Fact]
    public void OutputNodeViewModel_HasInputConnector()
    {
        // Arrange & Act
        var node = new OutputNodeViewModel(new OutputNode());

        // Assert
        Assert.NotEmpty(node.Input);
    }

    [Fact]
    public void SourceNodeViewModel_FilePath_CanBeSet()
    {
        // Arrange
        var node = new SourceNodeViewModel(new SourceNode());

        // Act
        node.FilePath = @"C:\test\video.mp4";

        // Assert
        Assert.Equal(@"C:\test\video.mp4", node.FilePath);
    }

    [Fact]
    public void SourceNodeViewModel_SourcePlugin_DefaultsToFfms2()
    {
        // Arrange & Act
        var node = new SourceNodeViewModel(new SourceNode());

        // Assert
        Assert.Equal("ffms2", node.SourcePlugin);
    }
}
