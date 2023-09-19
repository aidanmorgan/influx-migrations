using InfluxMigrations.Core;
using InfluxMigrations.Outputs;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Output;

[YamlTaskParser("set")]
public class SetVariableTaskParser : IYamlTaskParser
{
    public IMigrationTaskBuilder Parse(YamlMappingNode node)
    {
        var children = node.Children;
        var scope = VariableScope.Local;

        if (children.ContainsKey("scope"))
        {
            var t = ((YamlScalarNode)children["scope"]).Value;
            if (!Enum.TryParse(t, out scope))
            {
                throw new YamlMigrationParsingException($"Could not parse scope value {t}.");
            } 
        }

        if (children.ContainsKey("key") && children.ContainsKey("value"))
        {
            return new SetVariableTaskBuilder()
                .WithKey(((YamlScalarNode)node["key"]).Value)
                .WithValue(((YamlScalarNode)node["value"]).Value)
                .WithScope(scope);
        }
        else if (children.ContainsKey("expr"))
        {
            var value = ((YamlScalarNode)node["expr"]).Value;

            if (string.IsNullOrEmpty(value) || value.IndexOf("=", StringComparison.Ordinal) == -1)
            {
                throw new YamlMigrationParsingException($"Cannot parse expression {value}, no '=' found.");
            }

            var split = value.Split("=").Select(x => x.Trim()).ToList();
            if (split.Count() != 2)
            {
                throw new YamlMigrationParsingException(
                    $"Cannot parse expression {value}, no name / expression pair found.");
            }

            return new SetVariableTaskBuilder()
                .WithKey(split[0])
                .WithValue(split[1])
                .WithScope(scope);
        }
        else
        {
            throw new YamlMigrationParsingException("Cannot parse set variable output, unknown attributes.");
        }
    }
}