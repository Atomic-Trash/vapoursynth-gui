using VapourSynthPortable.Models.NodeModels;
using VapourSynthPortable.ViewModels.NodeEditor;

namespace VapourSynthPortable.Tests.ViewModels.NodeEditor;

/// <summary>
/// Tests for ConnectionViewModel that represents connections between nodes.
/// </summary>
public class ConnectionViewModelTests
{
    [Fact]
    public void IsValid_WhenBothEndpointsSet_ReturnsTrue()
    {
        // Arrange
        var sourceNode = new SourceNode();
        var outputNode = new OutputNode();

        var sourceVm = new SourceNodeViewModel(sourceNode);
        var outputVm = new OutputNodeViewModel(outputNode);

        var sourceConnector = sourceVm.Output.First();
        var targetConnector = outputVm.Input.First();

        // Act
        var connection = new ConnectionViewModel(sourceConnector, targetConnector);

        // Assert
        Assert.NotNull(connection.Source);
        Assert.NotNull(connection.Target);
    }

    [Fact]
    public void Source_IsSet_FromConstructor()
    {
        // Arrange
        var sourceNode = new SourceNode();
        var outputNode = new OutputNode();

        var sourceVm = new SourceNodeViewModel(sourceNode);
        var outputVm = new OutputNodeViewModel(outputNode);

        var sourceConnector = sourceVm.Output.First();
        var targetConnector = outputVm.Input.First();

        // Act
        var connection = new ConnectionViewModel(sourceConnector, targetConnector);

        // Assert
        Assert.Equal(sourceConnector, connection.Source);
    }

    [Fact]
    public void Target_IsSet_FromConstructor()
    {
        // Arrange
        var sourceNode = new SourceNode();
        var outputNode = new OutputNode();

        var sourceVm = new SourceNodeViewModel(sourceNode);
        var outputVm = new OutputNodeViewModel(outputNode);

        var sourceConnector = sourceVm.Output.First();
        var targetConnector = outputVm.Input.First();

        // Act
        var connection = new ConnectionViewModel(sourceConnector, targetConnector);

        // Assert
        Assert.Equal(targetConnector, connection.Target);
    }

    [Fact]
    public void Connection_SetsConnectorIsConnectedFlags()
    {
        // Arrange
        var sourceNode = new SourceNode();
        var outputNode = new OutputNode();

        var sourceVm = new SourceNodeViewModel(sourceNode);
        var outputVm = new OutputNodeViewModel(outputNode);

        var sourceConnector = sourceVm.Output.First();
        var targetConnector = outputVm.Input.First();

        // Act
        var connection = new ConnectionViewModel(sourceConnector, targetConnector);

        // Assert
        Assert.True(sourceConnector.IsConnected);
        Assert.True(targetConnector.IsConnected);
    }

    [Fact]
    public void Connection_HasModel()
    {
        // Arrange
        var sourceNode = new SourceNode();
        var outputNode = new OutputNode();

        var sourceVm = new SourceNodeViewModel(sourceNode);
        var outputVm = new OutputNodeViewModel(outputNode);

        var sourceConnector = sourceVm.Output.First();
        var targetConnector = outputVm.Input.First();

        // Act
        var connection = new ConnectionViewModel(sourceConnector, targetConnector);

        // Assert
        Assert.NotNull(connection.Model);
    }
}

/// <summary>
/// Tests for ConnectorViewModel that represents connection points on nodes.
/// </summary>
public class ConnectorViewModelTests
{
    [Fact]
    public void SourceNode_HasOutputConnector()
    {
        // Arrange & Act
        var node = new SourceNodeViewModel(new SourceNode());

        // Assert
        Assert.NotEmpty(node.Output);
    }

    [Fact]
    public void OutputNode_HasInputConnector()
    {
        // Arrange & Act
        var node = new OutputNodeViewModel(new OutputNode());

        // Assert
        Assert.NotEmpty(node.Input);
    }
}
