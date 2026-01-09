namespace VapourSynthPortable.Tests.Services;

public class ScriptGeneratorServiceTests
{
    private readonly ScriptGeneratorService _service;

    public ScriptGeneratorServiceTests()
    {
        _service = new ScriptGeneratorService();
    }

    #region Validation Tests

    [Fact]
    public void Generate_ThrowsException_WhenNoSourceNode()
    {
        // Arrange - only output node, no source
        var nodes = new List<NodeBase>
        {
            new OutputNode()
        };
        var connections = new List<ConnectionModel>();

        // Act & Assert
        var action = () => _service.Generate(nodes, connections);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*source node*");
    }

    [Fact]
    public void Generate_ThrowsException_WhenNoOutputNode()
    {
        // Arrange - only source node, no output
        var nodes = new List<NodeBase>
        {
            new SourceNode { FilePath = "test.mp4" }
        };
        var connections = new List<ConnectionModel>();

        // Act & Assert
        var action = () => _service.Generate(nodes, connections);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*output node*");
    }

    [Fact]
    public void Generate_ThrowsException_WhenCycleDetected()
    {
        // Arrange - create a cycle by having a filter connected to itself
        var filter1 = new FilterNode("Filter1", "std", "Crop");
        var filter2 = new FilterNode("Filter2", "std", "Crop");
        var source = new SourceNode { FilePath = "test.mp4" };
        var output = new OutputNode();

        var nodes = new List<NodeBase> { source, filter1, filter2, output };

        // Create cycle: filter1 -> filter2 -> filter1
        var connection1 = new ConnectionModel
        {
            Source = filter1.Outputs[0],
            Target = filter2.Inputs[0]
        };
        var connection2 = new ConnectionModel
        {
            Source = filter2.Outputs[0],
            Target = filter1.Inputs[0]
        };
        var connectionOut = new ConnectionModel
        {
            Source = filter1.Outputs[0],
            Target = output.Inputs[0]
        };

        var connections = new List<ConnectionModel> { connection1, connection2, connectionOut };

        // Act & Assert
        var action = () => _service.Generate(nodes, connections);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cycle*");
    }

    #endregion

    #region Script Generation Tests

    [Fact]
    public void Generate_ReturnsValidScript_WithSourceAndOutput()
    {
        // Arrange
        var source = new SourceNode { FilePath = "C:\\videos\\test.mp4" };
        var output = new OutputNode { OutputIndex = 0 };

        var nodes = new List<NodeBase> { source, output };

        var connection = new ConnectionModel
        {
            Source = source.Outputs[0],
            Target = output.Inputs[0]
        };
        var connections = new List<ConnectionModel> { connection };

        // Act
        var script = _service.Generate(nodes, connections);

        // Assert
        script.Should().NotBeNullOrEmpty();
        script.Should().Contain("import vapoursynth as vs");
        script.Should().Contain("from vapoursynth import core");
        script.Should().Contain("core.ffms2.Source");
        script.Should().Contain(".set_output(0)");
    }

    [Fact]
    public void Generate_UsesLSmash_WhenSourcePluginIsLSmash()
    {
        // Arrange
        var source = new SourceNode
        {
            FilePath = "C:\\videos\\test.mp4",
            SourcePlugin = "lsmashsource"
        };
        var output = new OutputNode { OutputIndex = 0 };

        var nodes = new List<NodeBase> { source, output };

        var connection = new ConnectionModel
        {
            Source = source.Outputs[0],
            Target = output.Inputs[0]
        };
        var connections = new List<ConnectionModel> { connection };

        // Act
        var script = _service.Generate(nodes, connections);

        // Assert
        script.Should().Contain("core.lsmas.LWLibavSource");
        script.Should().NotContain("core.ffms2");
    }

    [Fact]
    public void Generate_IncludesFilterNode_InCorrectOrder()
    {
        // Arrange
        var source = new SourceNode { FilePath = "C:\\videos\\test.mp4" };
        var filter = new FilterNode("Resize", "resize", "Bicubic");
        filter.Parameters.Add(new NodeParameter { Name = "width", Value = "1920" });
        filter.Parameters.Add(new NodeParameter { Name = "height", Value = "1080" });
        var output = new OutputNode { OutputIndex = 0 };

        var nodes = new List<NodeBase> { source, filter, output };

        var conn1 = new ConnectionModel
        {
            Source = source.Outputs[0],
            Target = filter.Inputs[0]
        };
        var conn2 = new ConnectionModel
        {
            Source = filter.Outputs[0],
            Target = output.Inputs[0]
        };
        var connections = new List<ConnectionModel> { conn1, conn2 };

        // Act
        var script = _service.Generate(nodes, connections);

        // Assert
        script.Should().Contain("core.ffms2.Source");
        script.Should().Contain("core.resize.Bicubic");
        script.Should().Contain("width=1920");
        script.Should().Contain("height=1080");
        script.Should().Contain(".set_output(0)");

        // Verify order: Source should come before Filter in script
        var sourceIndex = script.IndexOf("ffms2.Source");
        var filterIndex = script.IndexOf("resize.Bicubic");
        sourceIndex.Should().BeLessThan(filterIndex);
    }

    [Fact]
    public void Generate_HandlesMultipleFilters_InChain()
    {
        // Arrange - Source -> Crop -> Resize -> Output
        var source = new SourceNode { FilePath = "video.mp4" };
        var cropFilter = new FilterNode("Crop", "std", "Crop");
        cropFilter.Parameters.Add(new NodeParameter { Name = "left", Value = "10" });
        cropFilter.Parameters.Add(new NodeParameter { Name = "right", Value = "10" });

        var resizeFilter = new FilterNode("Resize", "resize", "Lanczos");
        resizeFilter.Parameters.Add(new NodeParameter { Name = "width", Value = "1280" });
        resizeFilter.Parameters.Add(new NodeParameter { Name = "height", Value = "720" });

        var output = new OutputNode { OutputIndex = 0 };

        var nodes = new List<NodeBase> { source, cropFilter, resizeFilter, output };

        var conn1 = new ConnectionModel
        {
            Source = source.Outputs[0],
            Target = cropFilter.Inputs[0]
        };
        var conn2 = new ConnectionModel
        {
            Source = cropFilter.Outputs[0],
            Target = resizeFilter.Inputs[0]
        };
        var conn3 = new ConnectionModel
        {
            Source = resizeFilter.Outputs[0],
            Target = output.Inputs[0]
        };
        var connections = new List<ConnectionModel> { conn1, conn2, conn3 };

        // Act
        var script = _service.Generate(nodes, connections);

        // Assert
        script.Should().Contain("core.std.Crop");
        script.Should().Contain("core.resize.Lanczos");

        // Verify order: Source -> Crop -> Resize -> Output
        var sourceIndex = script.IndexOf("ffms2.Source");
        var cropIndex = script.IndexOf("std.Crop");
        var resizeIndex = script.IndexOf("resize.Lanczos");
        var outputIndex = script.IndexOf("set_output");

        sourceIndex.Should().BeLessThan(cropIndex);
        cropIndex.Should().BeLessThan(resizeIndex);
        resizeIndex.Should().BeLessThan(outputIndex);
    }

    [Fact]
    public void Generate_IncludesHeader_Always()
    {
        // Arrange
        var source = new SourceNode { FilePath = "test.mp4" };
        var output = new OutputNode();

        var nodes = new List<NodeBase> { source, output };
        var connection = new ConnectionModel
        {
            Source = source.Outputs[0],
            Target = output.Inputs[0]
        };
        var connections = new List<ConnectionModel> { connection };

        // Act
        var script = _service.Generate(nodes, connections);

        // Assert - Script should start with standard VapourSynth imports
        script.Should().StartWith("import vapoursynth as vs");
        script.Should().Contain("from vapoursynth import core");
    }

    [Fact]
    public void Generate_HandlesFilterWithNoParameters()
    {
        // Arrange
        var source = new SourceNode { FilePath = "test.mp4" };
        var filter = new FilterNode("Transpose", "std", "Transpose");
        // No parameters added
        var output = new OutputNode();

        var nodes = new List<NodeBase> { source, filter, output };

        var conn1 = new ConnectionModel
        {
            Source = source.Outputs[0],
            Target = filter.Inputs[0]
        };
        var conn2 = new ConnectionModel
        {
            Source = filter.Outputs[0],
            Target = output.Inputs[0]
        };
        var connections = new List<ConnectionModel> { conn1, conn2 };

        // Act
        var script = _service.Generate(nodes, connections);

        // Assert
        script.Should().Contain("core.std.Transpose(clip");
    }

    #endregion
}
