using System.Text;
using VapourSynthPortable.Models.NodeModels;

namespace VapourSynthPortable.Services;

public class ScriptGeneratorService
{
    public string Generate(List<NodeBase> nodes, List<ConnectionModel> connections)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("import vapoursynth as vs");
        sb.AppendLine("from vapoursynth import core");
        sb.AppendLine();

        // Find source nodes (nodes with no inputs connected)
        var sourceNodes = nodes.OfType<SourceNode>().ToList();
        if (!sourceNodes.Any())
        {
            throw new InvalidOperationException("No source node found");
        }

        // Find output nodes
        var outputNodes = nodes.OfType<OutputNode>().ToList();
        if (!outputNodes.Any())
        {
            throw new InvalidOperationException("No output node found");
        }

        // Build node execution order using topological sort
        var executionOrder = TopologicalSort(nodes, connections);

        // Generate script for each node in order
        var variableMap = new Dictionary<string, string>();
        int varCounter = 0;

        foreach (var node in executionOrder)
        {
            string varName = $"clip{varCounter++}";

            if (node is SourceNode sourceNode)
            {
                sb.AppendLine($"# Source: {sourceNode.Title}");
                sb.AppendLine($"{varName} = {sourceNode.GenerateScript("")}");
                sb.AppendLine();

                // Map output connector to variable
                var outputConnector = sourceNode.Outputs.FirstOrDefault();
                if (outputConnector != null)
                {
                    variableMap[outputConnector.Id] = varName;
                }
            }
            else if (node is FilterNode filterNode)
            {
                // Find input variable
                var inputConnector = filterNode.Inputs.FirstOrDefault();
                var inputConnection = connections.FirstOrDefault(c => c.Target?.Id == inputConnector?.Id);
                var inputVar = inputConnection?.Source != null && variableMap.ContainsKey(inputConnection.Source.Id)
                    ? variableMap[inputConnection.Source.Id]
                    : "clip";

                sb.AppendLine($"# Filter: {filterNode.Title}");
                sb.AppendLine($"{varName} = {filterNode.GenerateScript(inputVar)}");
                sb.AppendLine();

                // Map output connector to variable
                var outputConnector = filterNode.Outputs.FirstOrDefault();
                if (outputConnector != null)
                {
                    variableMap[outputConnector.Id] = varName;
                }
            }
            else if (node is OutputNode outputNode)
            {
                // Find input variable
                var inputConnector = outputNode.Inputs.FirstOrDefault();
                var inputConnection = connections.FirstOrDefault(c => c.Target?.Id == inputConnector?.Id);
                var inputVar = inputConnection?.Source != null && variableMap.ContainsKey(inputConnection.Source.Id)
                    ? variableMap[inputConnection.Source.Id]
                    : "clip";

                sb.AppendLine("# Output");
                sb.AppendLine($"{inputVar}.set_output({outputNode.OutputIndex})");
            }
        }

        return sb.ToString();
    }

    private List<NodeBase> TopologicalSort(List<NodeBase> nodes, List<ConnectionModel> connections)
    {
        var result = new List<NodeBase>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        void Visit(NodeBase node)
        {
            if (visited.Contains(node.Id)) return;
            if (visiting.Contains(node.Id))
                throw new InvalidOperationException("Cycle detected in node graph");

            visiting.Add(node.Id);

            // Find nodes that feed into this node
            foreach (var input in node.Inputs)
            {
                var connection = connections.FirstOrDefault(c => c.Target?.Id == input.Id);
                if (connection?.Source?.ParentNode != null)
                {
                    Visit(connection.Source.ParentNode);
                }
            }

            visiting.Remove(node.Id);
            visited.Add(node.Id);
            result.Add(node);
        }

        // Start with output nodes and work backwards
        foreach (var node in nodes.OfType<OutputNode>())
        {
            Visit(node);
        }

        // Add any unvisited nodes (disconnected)
        foreach (var node in nodes.Where(n => !visited.Contains(n.Id)))
        {
            Visit(node);
        }

        return result;
    }
}
