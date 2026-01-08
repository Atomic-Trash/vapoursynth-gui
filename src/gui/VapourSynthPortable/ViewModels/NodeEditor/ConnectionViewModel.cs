using CommunityToolkit.Mvvm.ComponentModel;
using VapourSynthPortable.Models.NodeModels;

namespace VapourSynthPortable.ViewModels.NodeEditor;

public partial class ConnectionViewModel : ObservableObject
{
    public ConnectionViewModel(ConnectorViewModel source, ConnectorViewModel target)
    {
        Source = source;
        Target = target;

        Model = new ConnectionModel
        {
            Source = source.Model,
            Target = target.Model
        };

        source.IsConnected = true;
        target.IsConnected = true;
    }

    public ConnectionModel Model { get; }

    [ObservableProperty]
    private ConnectorViewModel _source;

    [ObservableProperty]
    private ConnectorViewModel _target;
}
