using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models.NodeModels;

public abstract partial class NodeBase : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _title = "Node";

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    public ObservableCollection<ConnectorModel> Inputs { get; } = new();
    public ObservableCollection<ConnectorModel> Outputs { get; } = new();

    public abstract string NodeType { get; }

    public abstract string GenerateScript(string inputVariable);
}

public partial class ConnectorModel : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private bool _isInput;

    [ObservableProperty]
    private NodeBase? _parentNode;

    [ObservableProperty]
    private bool _isConnected;
}

public partial class ConnectionModel : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private ConnectorModel? _source;

    [ObservableProperty]
    private ConnectorModel? _target;
}
