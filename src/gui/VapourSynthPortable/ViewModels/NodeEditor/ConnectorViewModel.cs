using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using VapourSynthPortable.Models.NodeModels;

namespace VapourSynthPortable.ViewModels.NodeEditor;

public partial class ConnectorViewModel : ObservableObject
{
    public ConnectorViewModel(ConnectorModel model, NodeViewModel parent)
    {
        Model = model;
        Parent = parent;
    }

    public ConnectorModel Model { get; }
    public NodeViewModel Parent { get; }

    public string Name => Model.Name;
    public bool IsInput => Model.IsInput;

    [ObservableProperty]
    private Point _anchor;

    [ObservableProperty]
    private bool _isConnected;
}
