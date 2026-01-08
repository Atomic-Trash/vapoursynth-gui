using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using VapourSynthPortable.Models.NodeModels;

namespace VapourSynthPortable.ViewModels.NodeEditor;

public partial class NodeViewModel : ObservableObject
{
    public NodeViewModel(NodeBase model)
    {
        Model = model;

        // Create connector view models
        foreach (var input in model.Inputs)
        {
            Input.Add(new ConnectorViewModel(input, this));
        }

        foreach (var output in model.Outputs)
        {
            Output.Add(new ConnectorViewModel(output, this));
        }
    }

    public NodeBase Model { get; }

    public string Title
    {
        get => Model.Title;
        set => Model.Title = value;
    }

    public string NodeType => Model.NodeType;

    [ObservableProperty]
    private Point _location;

    [ObservableProperty]
    private bool _isSelected;

    public ObservableCollection<ConnectorViewModel> Input { get; } = new();
    public ObservableCollection<ConnectorViewModel> Output { get; } = new();

    partial void OnLocationChanged(Point value)
    {
        Model.X = value.X;
        Model.Y = value.Y;
    }
}

public partial class SourceNodeViewModel : NodeViewModel
{
    public SourceNodeViewModel(SourceNode model) : base(model)
    {
        _sourceModel = model;
    }

    private readonly SourceNode _sourceModel;

    public string FilePath
    {
        get => _sourceModel.FilePath;
        set => SetProperty(_sourceModel.FilePath, value, _sourceModel, (m, v) => m.FilePath = v);
    }

    public string SourcePlugin
    {
        get => _sourceModel.SourcePlugin;
        set => SetProperty(_sourceModel.SourcePlugin, value, _sourceModel, (m, v) => m.SourcePlugin = v);
    }
}

public partial class OutputNodeViewModel : NodeViewModel
{
    public OutputNodeViewModel(OutputNode model) : base(model)
    {
        _outputModel = model;
    }

    private readonly OutputNode _outputModel;

    public int OutputIndex
    {
        get => _outputModel.OutputIndex;
        set => SetProperty(_outputModel.OutputIndex, value, _outputModel, (m, v) => m.OutputIndex = v);
    }
}

public partial class FilterNodeViewModel : NodeViewModel
{
    public FilterNodeViewModel(FilterNode model) : base(model)
    {
        _filterModel = model;
    }

    private readonly FilterNode _filterModel;

    public string PluginNamespace => _filterModel.PluginNamespace;
    public string Function => _filterModel.Function;
    public ObservableCollection<NodeParameter> Parameters => _filterModel.Parameters;
}
