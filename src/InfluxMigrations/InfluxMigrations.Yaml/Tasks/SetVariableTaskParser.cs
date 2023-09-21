using InfluxMigrations.Core;
using InfluxMigrations.Outputs;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Tasks;

[YamlTaskParser("set")]
public class SetVariableTaskParser : IYamlTaskParser
{
    public IMigrationTaskBuilder Parse(YamlMappingNode node)
    {
        var builder = new SetVariableTaskBuilder();

        node.Value("scope", (x) => { builder.WithScope(x); });

        node.Value("expr", (x) =>
        {
            if (string.IsNullOrEmpty(x) || !x.Contains("="))
            {
                throw new YamlMigrationParsingException($"Cannot parse expression {x}, no '=' found.");
            }

            var split = x.Split("=").Select(x => x.Trim()).ToList();
            if (split.Count() != 2)
            {
                throw new YamlMigrationParsingException(
                    $"Cannot parse expression {x}, no name / expression pair found.");
            }

            builder.WithKey(split[0]).WithValue(split[1]);
        });

        var key = node.GetStringValue("key");
        var value = node.GetStringValue("value");

        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
        {
            builder.WithKey(key).WithValue(value);
        }

        return builder;
    }
}