using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models.NodeModels;

public partial class FilterNode : NodeBase
{
    public FilterNode(string name, string pluginNamespace, string function)
    {
        Title = name;
        PluginNamespace = pluginNamespace;
        Function = function;

        // All filter nodes have one input and one output (clip)
        Inputs.Add(new ConnectorModel { Name = "clip", IsInput = true, ParentNode = this });
        Outputs.Add(new ConnectorModel { Name = "clip", IsInput = false, ParentNode = this });
    }

    public override string NodeType => "Filter";

    [ObservableProperty]
    private string _pluginNamespace = "";

    [ObservableProperty]
    private string _function = "";

    public ObservableCollection<NodeParameter> Parameters { get; } = new();

    public override string GenerateScript(string inputVariable)
    {
        var args = new List<string> { inputVariable };

        foreach (var param in Parameters.Where(p => !string.IsNullOrEmpty(p.Value)))
        {
            args.Add($"{param.Name}={param.Value}");
        }

        return $"core.{PluginNamespace}.{Function}({string.Join(", ", args)})";
    }
}

public partial class NodeParameter : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _value = "";

    [ObservableProperty]
    private string _type = "string"; // string, int, float, bool

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string? _defaultValue;
}
