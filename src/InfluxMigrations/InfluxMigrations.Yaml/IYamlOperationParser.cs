using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml;

public interface IYamlOperationParser
{
    IMigrationOperationBuilder Parse(YamlMappingNode node);
}