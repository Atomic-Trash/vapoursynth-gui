namespace VapourSynthPortable.Models;

/// <summary>
/// Serializable representation of a node graph for save/load functionality.
/// </summary>
public class NodeGraphData
{
    public string Version { get; set; } = "1.0";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<NodeData> Nodes { get; set; } = new();
    public List<ConnectionData> Connections { get; set; } = new();
}

public class NodeData
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = ""; // Source, Filter, Output
    public string Title { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }

    // Source node properties
    public string? FilePath { get; set; }
    public string? SourcePlugin { get; set; }

    // Filter node properties
    public string? FilterType { get; set; } // resize, bm3d, etc.
    public string? PluginNamespace { get; set; }
    public string? Function { get; set; }
    public List<ParameterData>? Parameters { get; set; }

    // Output node properties
    public int? OutputIndex { get; set; }
}

public class ParameterData
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string Type { get; set; } = "string";
}

public class ConnectionData
{
    public string SourceNodeId { get; set; } = "";
    public string SourceConnectorName { get; set; } = "";
    public string TargetNodeId { get; set; } = "";
    public string TargetConnectorName { get; set; } = "";
}
