using InfluxMigrations.Core;
using InfluxMigrations.Tasks;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml.Parsers.Tasks;

[YamlTaskParser("echo")]
public class EchoTaskParser : IYamlTaskParser
{
    public IOperationTaskBuilder ParseOperationTask(YamlMappingNode node)
    {
        return Parse(node);
    }

    public IMigrationTaskBuilder ParseMigrationTask(YamlMappingNode node)
    {
        return Parse(node);
    }

    public IEnvironmentTaskBuilder ParseEnvironmentTask(YamlMappingNode node)
    {
        return Parse(node);
    }

    private EchoTaskBuilder Parse(YamlMappingNode node)
    {
        EchoTaskBuilder builder = new EchoTaskBuilder();

        node.ForEach("expr", (x) =>
        {
            var strVal = x.GetStringValue();
            if (!string.IsNullOrEmpty(strVal))
            {
                builder.WithString(strVal);
            }
        });

        return builder;
        
    }
}