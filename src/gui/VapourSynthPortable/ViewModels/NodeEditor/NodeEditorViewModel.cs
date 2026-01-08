using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using VapourSynthPortable.Models;
using VapourSynthPortable.Models.NodeModels;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels.NodeEditor;

public partial class NodeEditorViewModel : ObservableObject
{
    private readonly ScriptGeneratorService _scriptGenerator;

    public NodeEditorViewModel()
    {
        _scriptGenerator = new ScriptGeneratorService();
        _pendingConnection = new PendingConnectionViewModel(this);

        // Available node types for the palette
        AvailableNodes = new ObservableCollection<NodePaletteItem>
        {
            new("Video Source", "Source", "Load a video file"),
            new("Output", "Output", "Set script output"),
        };
    }

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();
    public ObservableCollection<NodePaletteItem> AvailableNodes { get; }

    [ObservableProperty]
    private NodeViewModel? _selectedNode;

    [ObservableProperty]
    private string _generatedScript = "";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Pending connection for Nodify
    [ObservableProperty]
    private PendingConnectionViewModel _pendingConnection;

    [RelayCommand]
    private void AddSourceNode()
    {
        var node = new SourceNode { X = 100, Y = 100 };
        var vm = new SourceNodeViewModel(node) { Location = new Point(100, 100) };
        Nodes.Add(vm);
        StatusMessage = "Added Source node";
    }

    [RelayCommand]
    private void AddOutputNode()
    {
        var node = new OutputNode { X = 400, Y = 100 };
        var vm = new OutputNodeViewModel(node) { Location = new Point(400, 100) };
        Nodes.Add(vm);
        StatusMessage = "Added Output node";
    }

    [RelayCommand]
    private void AddFilterNode(string filterType)
    {
        FilterNode node = filterType switch
        {
            // Resize
            "resize" => CreateResizeNode(),
            "descale" => CreateDescaleNode(),
            "fmtconv" => CreateFmtConvNode(),
            // Denoise
            "bm3d" => CreateBM3DNode(),
            "dfttest" => CreateDFTTestNode(),
            "knlm" => CreateKNLMNode(),
            "bilateral" => CreateBilateralNode(),
            // Deinterlace
            "eedi3" => CreateEEDI3Node(),
            "nnedi3" => CreateNNEDI3Node(),
            // Sharpen
            "cas" => CreateCASNode(),
            "tcanny" => CreateTCannyNode(),
            // Utility
            "crop" => CreateCropNode(),
            "trim" => CreateTrimNode(),
            _ => throw new ArgumentException($"Unknown filter type: {filterType}")
        };

        var vm = new FilterNodeViewModel(node) { Location = new Point(250, 100) };
        Nodes.Add(vm);
        StatusMessage = $"Added {node.Title} node";
    }

    #region Resize Filters

    private FilterNode CreateResizeNode()
    {
        var node = new FilterNode("Resize", "resize", "Lanczos");
        node.Parameters.Add(new NodeParameter { Name = "width", Type = "int", DefaultValue = "1920" });
        node.Parameters.Add(new NodeParameter { Name = "height", Type = "int", DefaultValue = "1080" });
        return node;
    }

    private FilterNode CreateDescaleNode()
    {
        var node = new FilterNode("Descale", "descale", "Debicubic");
        node.Parameters.Add(new NodeParameter { Name = "width", Type = "int", DefaultValue = "1280" });
        node.Parameters.Add(new NodeParameter { Name = "height", Type = "int", DefaultValue = "720" });
        node.Parameters.Add(new NodeParameter { Name = "b", Type = "float", DefaultValue = "0.33" });
        node.Parameters.Add(new NodeParameter { Name = "c", Type = "float", DefaultValue = "0.33" });
        return node;
    }

    private FilterNode CreateFmtConvNode()
    {
        var node = new FilterNode("FmtConv", "fmtc", "resample");
        node.Parameters.Add(new NodeParameter { Name = "w", Type = "int", DefaultValue = "1920" });
        node.Parameters.Add(new NodeParameter { Name = "h", Type = "int", DefaultValue = "1080" });
        node.Parameters.Add(new NodeParameter { Name = "kernel", Type = "string", DefaultValue = "spline36" });
        return node;
    }

    #endregion

    #region Denoise Filters

    private FilterNode CreateBM3DNode()
    {
        var node = new FilterNode("BM3D Denoise", "bm3d", "VAggregate");
        node.Parameters.Add(new NodeParameter { Name = "sigma", Type = "float", DefaultValue = "3.0" });
        return node;
    }

    private FilterNode CreateDFTTestNode()
    {
        var node = new FilterNode("DFTTest", "dfttest", "DFTTest");
        node.Parameters.Add(new NodeParameter { Name = "sigma", Type = "float", DefaultValue = "4.0" });
        node.Parameters.Add(new NodeParameter { Name = "tbsize", Type = "int", DefaultValue = "1" });
        return node;
    }

    private FilterNode CreateKNLMNode()
    {
        var node = new FilterNode("KNLMeansCL", "knlm", "KNLMeansCL");
        node.Parameters.Add(new NodeParameter { Name = "d", Type = "int", DefaultValue = "1" });
        node.Parameters.Add(new NodeParameter { Name = "a", Type = "int", DefaultValue = "2" });
        node.Parameters.Add(new NodeParameter { Name = "s", Type = "int", DefaultValue = "4" });
        node.Parameters.Add(new NodeParameter { Name = "h", Type = "float", DefaultValue = "1.2" });
        return node;
    }

    private FilterNode CreateBilateralNode()
    {
        var node = new FilterNode("Bilateral", "bilateral", "Bilateral");
        node.Parameters.Add(new NodeParameter { Name = "sigmaS", Type = "float", DefaultValue = "3.0" });
        node.Parameters.Add(new NodeParameter { Name = "sigmaR", Type = "float", DefaultValue = "0.02" });
        return node;
    }

    #endregion

    #region Deinterlace Filters

    private FilterNode CreateEEDI3Node()
    {
        var node = new FilterNode("EEDI3", "eedi3", "eedi3");
        node.Parameters.Add(new NodeParameter { Name = "field", Type = "int", DefaultValue = "1" });
        node.Parameters.Add(new NodeParameter { Name = "dh", Type = "bool", DefaultValue = "False" });
        return node;
    }

    private FilterNode CreateNNEDI3Node()
    {
        var node = new FilterNode("NNEDI3", "nnedi3", "nnedi3");
        node.Parameters.Add(new NodeParameter { Name = "field", Type = "int", DefaultValue = "1" });
        node.Parameters.Add(new NodeParameter { Name = "nsize", Type = "int", DefaultValue = "0" });
        node.Parameters.Add(new NodeParameter { Name = "nns", Type = "int", DefaultValue = "3" });
        return node;
    }

    #endregion

    #region Sharpen Filters

    private FilterNode CreateCASNode()
    {
        var node = new FilterNode("CAS Sharpen", "cas", "CAS");
        node.Parameters.Add(new NodeParameter { Name = "sharpness", Type = "float", DefaultValue = "0.5" });
        return node;
    }

    private FilterNode CreateTCannyNode()
    {
        var node = new FilterNode("TCanny", "tcanny", "TCanny");
        node.Parameters.Add(new NodeParameter { Name = "sigma", Type = "float", DefaultValue = "1.5" });
        node.Parameters.Add(new NodeParameter { Name = "mode", Type = "int", DefaultValue = "0" });
        return node;
    }

    #endregion

    #region Utility Filters

    private FilterNode CreateCropNode()
    {
        var node = new FilterNode("Crop", "std", "Crop");
        node.Parameters.Add(new NodeParameter { Name = "left", Type = "int", DefaultValue = "0" });
        node.Parameters.Add(new NodeParameter { Name = "right", Type = "int", DefaultValue = "0" });
        node.Parameters.Add(new NodeParameter { Name = "top", Type = "int", DefaultValue = "0" });
        node.Parameters.Add(new NodeParameter { Name = "bottom", Type = "int", DefaultValue = "0" });
        return node;
    }

    private FilterNode CreateTrimNode()
    {
        var node = new FilterNode("Trim", "std", "Trim");
        node.Parameters.Add(new NodeParameter { Name = "first", Type = "int", DefaultValue = "0" });
        node.Parameters.Add(new NodeParameter { Name = "last", Type = "int", DefaultValue = "0" });
        return node;
    }

    #endregion

    [RelayCommand]
    private void DeleteNode(NodeViewModel? node)
    {
        if (node == null) return;

        // Remove connections involving this node
        var connectionsToRemove = Connections
            .Where(c => c.Source.Parent == node || c.Target.Parent == node)
            .ToList();

        foreach (var connection in connectionsToRemove)
        {
            Connections.Remove(connection);
        }

        Nodes.Remove(node);
        StatusMessage = $"Deleted {node.Title}";
    }

    public void Connect(ConnectorViewModel source, ConnectorViewModel target)
    {
        // Validate connection: output to input
        if (source.IsInput || !target.IsInput)
        {
            StatusMessage = "Invalid connection: must connect output to input";
            return;
        }

        // Check if target already has a connection
        var existingConnection = Connections.FirstOrDefault(c => c.Target == target);
        if (existingConnection != null)
        {
            Connections.Remove(existingConnection);
            existingConnection.Target.IsConnected = false;
        }

        var connection = new ConnectionViewModel(source, target);
        Connections.Add(connection);
        StatusMessage = "Connected nodes";
    }

    [RelayCommand]
    private void ConnectionCompleted(object? parameter)
    {
        // Nodify passes a tuple (Source, Target) when connection is completed
        if (parameter is ValueTuple<object?, object?> tuple)
        {
            var (sourceObj, targetObj) = tuple;

            if (sourceObj is ConnectorViewModel source && targetObj is ConnectorViewModel target)
            {
                // Determine direction - ensure we connect output to input
                if (!source.IsInput && target.IsInput)
                {
                    Connect(source, target);
                }
                else if (source.IsInput && !target.IsInput)
                {
                    Connect(target, source);
                }
                else
                {
                    StatusMessage = "Invalid connection: connect output to input";
                }
            }
        }
    }

    [RelayCommand]
    private void GenerateScript()
    {
        try
        {
            var models = Nodes.Select(n => n.Model).ToList();
            var connections = Connections.Select(c => c.Model).ToList();

            GeneratedScript = _scriptGenerator.Generate(models, connections);
            StatusMessage = "Script generated";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveScript()
    {
        if (string.IsNullOrEmpty(GeneratedScript))
        {
            GenerateScript();
        }

        var dialog = new SaveFileDialog
        {
            Filter = "VapourSynth Script|*.vpy|All Files|*.*",
            DefaultExt = ".vpy"
        };

        if (dialog.ShowDialog() == true)
        {
            System.IO.File.WriteAllText(dialog.FileName, GeneratedScript);
            StatusMessage = $"Saved to {dialog.FileName}";
        }
    }

    [RelayCommand]
    private void BrowseSourceFile(SourceNodeViewModel? node)
    {
        if (node == null) return;

        var dialog = new OpenFileDialog
        {
            Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            node.FilePath = dialog.FileName;
            StatusMessage = $"Selected: {dialog.FileName}";
        }
    }

    [RelayCommand]
    private void ClearCanvas()
    {
        Nodes.Clear();
        Connections.Clear();
        GeneratedScript = "";
        StatusMessage = "Canvas cleared";
    }

    #region Save/Load Graph

    [RelayCommand]
    private void SaveGraph()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Node Graph|*.vsgraph|All Files|*.*",
            DefaultExt = ".vsgraph",
            Title = "Save Node Graph"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var graphData = SerializeGraph();
                graphData.Name = Path.GetFileNameWithoutExtension(dialog.FileName);

                var json = JsonConvert.SerializeObject(graphData, Formatting.Indented);
                File.WriteAllText(dialog.FileName, json);

                StatusMessage = $"Graph saved to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void LoadGraph()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Node Graph|*.vsgraph|All Files|*.*",
            Title = "Load Node Graph"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(dialog.FileName);
                var graphData = JsonConvert.DeserializeObject<NodeGraphData>(json);

                if (graphData == null)
                {
                    StatusMessage = "Invalid graph file";
                    return;
                }

                DeserializeGraph(graphData);
                StatusMessage = $"Loaded {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Load failed: {ex.Message}";
            }
        }
    }

    private NodeGraphData SerializeGraph()
    {
        var graphData = new NodeGraphData();

        // Serialize nodes
        foreach (var nodeVm in Nodes)
        {
            var nodeData = new NodeData
            {
                Id = nodeVm.Model.Id,
                Type = nodeVm.Model.NodeType,
                Title = nodeVm.Model.Title,
                X = nodeVm.Location.X,
                Y = nodeVm.Location.Y
            };

            switch (nodeVm.Model)
            {
                case SourceNode source:
                    nodeData.FilePath = source.FilePath;
                    nodeData.SourcePlugin = source.SourcePlugin;
                    break;

                case FilterNode filter:
                    nodeData.FilterType = GetFilterType(filter);
                    nodeData.PluginNamespace = filter.PluginNamespace;
                    nodeData.Function = filter.Function;
                    nodeData.Parameters = filter.Parameters.Select(p => new ParameterData
                    {
                        Name = p.Name,
                        Value = p.Value,
                        Type = p.Type
                    }).ToList();
                    break;

                case OutputNode output:
                    nodeData.OutputIndex = output.OutputIndex;
                    break;
            }

            graphData.Nodes.Add(nodeData);
        }

        // Serialize connections
        foreach (var connVm in Connections)
        {
            var connData = new ConnectionData
            {
                SourceNodeId = connVm.Source.Parent?.Model.Id ?? "",
                SourceConnectorName = connVm.Source.Name,
                TargetNodeId = connVm.Target.Parent?.Model.Id ?? "",
                TargetConnectorName = connVm.Target.Name
            };
            graphData.Connections.Add(connData);
        }

        return graphData;
    }

    private void DeserializeGraph(NodeGraphData graphData)
    {
        // Clear existing graph
        Nodes.Clear();
        Connections.Clear();
        GeneratedScript = "";

        // Map old IDs to new node view models
        var nodeMap = new Dictionary<string, NodeViewModel>();

        // Recreate nodes
        foreach (var nodeData in graphData.Nodes)
        {
            NodeViewModel? nodeVm = null;

            switch (nodeData.Type)
            {
                case "Source":
                    var sourceNode = new SourceNode
                    {
                        FilePath = nodeData.FilePath ?? "",
                        SourcePlugin = nodeData.SourcePlugin ?? "ffms2"
                    };
                    nodeVm = new SourceNodeViewModel(sourceNode)
                    {
                        Location = new Point(nodeData.X, nodeData.Y)
                    };
                    break;

                case "Filter":
                    var filterType = nodeData.FilterType ?? "resize";
                    var filterNode = CreateFilterNodeFromData(nodeData);
                    if (filterNode != null)
                    {
                        nodeVm = new FilterNodeViewModel(filterNode)
                        {
                            Location = new Point(nodeData.X, nodeData.Y)
                        };
                    }
                    break;

                case "Output":
                    var outputNode = new OutputNode
                    {
                        OutputIndex = nodeData.OutputIndex ?? 0
                    };
                    nodeVm = new OutputNodeViewModel(outputNode)
                    {
                        Location = new Point(nodeData.X, nodeData.Y)
                    };
                    break;
            }

            if (nodeVm != null)
            {
                // Update the node ID to match saved ID for connection restoration
                nodeVm.Model.Id = nodeData.Id;
                nodeMap[nodeData.Id] = nodeVm;
                Nodes.Add(nodeVm);
            }
        }

        // Recreate connections
        foreach (var connData in graphData.Connections)
        {
            if (nodeMap.TryGetValue(connData.SourceNodeId, out var sourceNode) &&
                nodeMap.TryGetValue(connData.TargetNodeId, out var targetNode))
            {
                var sourceConnector = sourceNode.Output.FirstOrDefault(c => c.Name == connData.SourceConnectorName);
                var targetConnector = targetNode.Input.FirstOrDefault(c => c.Name == connData.TargetConnectorName);

                if (sourceConnector != null && targetConnector != null)
                {
                    Connect(sourceConnector, targetConnector);
                }
            }
        }
    }

    private FilterNode? CreateFilterNodeFromData(NodeData nodeData)
    {
        if (string.IsNullOrEmpty(nodeData.PluginNamespace) || string.IsNullOrEmpty(nodeData.Function))
            return null;

        var node = new FilterNode(nodeData.Title, nodeData.PluginNamespace, nodeData.Function);

        // Restore parameters
        if (nodeData.Parameters != null)
        {
            node.Parameters.Clear();
            foreach (var paramData in nodeData.Parameters)
            {
                node.Parameters.Add(new NodeParameter
                {
                    Name = paramData.Name,
                    Value = paramData.Value,
                    Type = paramData.Type
                });
            }
        }

        return node;
    }

    private string GetFilterType(FilterNode filter)
    {
        // Map plugin namespace + function back to filter type
        return (filter.PluginNamespace, filter.Function) switch
        {
            ("resize", "Lanczos") => "resize",
            ("descale", "Debicubic") => "descale",
            ("fmtc", "resample") => "fmtconv",
            ("bm3d", "VAggregate") => "bm3d",
            ("dfttest", "DFTTest") => "dfttest",
            ("knlm", "KNLMeansCL") => "knlm",
            ("bilateral", "Bilateral") => "bilateral",
            ("eedi3", "eedi3") => "eedi3",
            ("nnedi3", "nnedi3") => "nnedi3",
            ("cas", "CAS") => "cas",
            ("tcanny", "TCanny") => "tcanny",
            ("std", "Crop") => "crop",
            ("std", "Trim") => "trim",
            _ => filter.PluginNamespace
        };
    }

    #endregion
}

public class NodePaletteItem
{
    public NodePaletteItem(string name, string type, string description)
    {
        Name = name;
        Type = type;
        Description = description;
    }

    public string Name { get; }
    public string Type { get; }
    public string Description { get; }
}
