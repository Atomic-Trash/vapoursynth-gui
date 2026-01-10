using System.Windows;
using System.Windows.Controls;
using VapourSynthPortable.Models.NodeModels;

namespace VapourSynthPortable.Helpers;

/// <summary>
/// Selects the appropriate DataTemplate for a NodeParameter based on its type
/// </summary>
public class ParameterTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FloatTemplate { get; set; }
    public DataTemplate? IntegerTemplate { get; set; }
    public DataTemplate? BooleanTemplate { get; set; }
    public DataTemplate? ChoiceTemplate { get; set; }
    public DataTemplate? StringTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is NodeParameter parameter)
        {
            // First try the new ParameterType enum
            if (parameter.ParameterType != ParameterType.String)
            {
                return parameter.ParameterType switch
                {
                    ParameterType.Float => FloatTemplate,
                    ParameterType.Integer => IntegerTemplate,
                    ParameterType.Boolean => BooleanTemplate,
                    ParameterType.Choice => ChoiceTemplate,
                    _ => StringTemplate
                };
            }

            // Fall back to legacy string-based type for backwards compatibility
            return parameter.Type?.ToLowerInvariant() switch
            {
                "float" or "double" => FloatTemplate,
                "int" or "integer" => IntegerTemplate,
                "bool" or "boolean" => BooleanTemplate,
                "choice" or "enum" => ChoiceTemplate,
                _ => StringTemplate
            };
        }

        return StringTemplate ?? base.SelectTemplate(item, container);
    }
}
