using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VapourSynthPortable.ViewModels.NodeEditor;

public partial class PendingConnectionViewModel : ObservableObject
{
    private readonly NodeEditorViewModel _editor;

    public PendingConnectionViewModel(NodeEditorViewModel editor)
    {
        _editor = editor;
    }

    [ObservableProperty]
    private ConnectorViewModel? _source;

    [ObservableProperty]
    private ConnectorViewModel? _target;

    [ObservableProperty]
    private bool _isVisible;

    [RelayCommand]
    private void Start(ConnectorViewModel connector)
    {
        Source = connector;
        IsVisible = true;
    }

    [RelayCommand]
    private void Finish(ConnectorViewModel? connector)
    {
        if (Source != null && connector != null && Source != connector)
        {
            // Determine which is source (output) and which is target (input)
            if (!Source.IsInput && connector.IsInput)
            {
                _editor.Connect(Source, connector);
            }
            else if (Source.IsInput && !connector.IsInput)
            {
                _editor.Connect(connector, Source);
            }
        }

        Source = null;
        Target = null;
        IsVisible = false;
    }
}
