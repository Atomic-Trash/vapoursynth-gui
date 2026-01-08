using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models.NodeModels;

public partial class OutputNode : NodeBase
{
    public OutputNode()
    {
        Title = "Output";
        Inputs.Add(new ConnectorModel { Name = "clip", IsInput = true, ParentNode = this });
    }

    public override string NodeType => "Output";

    [ObservableProperty]
    private int _outputIndex;

    public override string GenerateScript(string inputVariable)
    {
        return $"{inputVariable}.set_output({OutputIndex})";
    }
}
