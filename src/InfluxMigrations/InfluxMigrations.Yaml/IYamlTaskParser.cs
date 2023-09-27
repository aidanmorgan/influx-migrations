using InfluxMigrations.Core;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Yaml;

public interface IYamlTaskParser
{
    IOperationTaskBuilder ParseOperationTask(YamlMappingNode node);
    
    IMigrationTaskBuilder ParseMigrationTask(YamlMappingNode node);
    
    IEnvironmentTaskBuilder ParseEnvironmentTask(YamlMappingNode node);
}