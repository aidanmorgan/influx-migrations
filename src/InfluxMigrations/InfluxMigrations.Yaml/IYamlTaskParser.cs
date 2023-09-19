using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml;

public interface IYamlTaskParser
{
    IMigrationTaskBuilder Parse(YamlMappingNode node);
}