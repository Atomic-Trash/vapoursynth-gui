using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models.NodeModels;

public partial class SourceNode : NodeBase
{
    public SourceNode()
    {
        Title = "Video Source";
        Outputs.Add(new ConnectorModel { Name = "clip", IsInput = false, ParentNode = this });
    }

    public override string NodeType => "Source";

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _sourcePlugin = "ffms2"; // ffms2 or lsmashsource

    public override string GenerateScript(string inputVariable)
    {
        var escapedPath = FilePath.Replace("\\", "\\\\");
        return SourcePlugin switch
        {
            "lsmashsource" => $"core.lsmas.LWLibavSource(r\"{FilePath}\")",
            _ => $"core.ffms2.Source(r\"{FilePath}\")"
        };
    }
}
