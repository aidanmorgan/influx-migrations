using InfluxMigrations.Core;
using InfluxMigrations.Yaml;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Extensions.Vault;

public class VaultReaderTask : AbstractVaultTask, IMigrationTask
{
    public IResolvable<string?> ContextName { get; init; }
    protected override async Task<TaskResult> ExecuteTask(IContext taskParameters, VaultTaskParameters vault)
    {
        var contextName = ContextName.Resolve(taskParameters);
        
        var result = await vault.Client.Secrets.KvV2ReadAsync(vault.Path);

        if (string.IsNullOrEmpty(contextName))
        {
            
        }
        return TaskResults.TaskSuccess();
    }

}

public class VaultReaderTaskBuilder : IMigrationTask
{
    public Task<TaskResult> ExecuteAsync(IOperationExecutionContext ctx)
    {
        throw new NotImplementedException();
    }

    public Task<TaskResult> ExecuteAsync(IMigrationExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}

[YamlTaskParser("vault:read")]
public class VaultReaderTaskParser : IYamlTaskParser
{
    public IOperationTaskBuilder ParseOperationTask(YamlMappingNode node)
    {
        throw new NotImplementedException();
    }

    public IMigrationTaskBuilder ParseMigrationTask(YamlMappingNode node)
    {
        throw new NotImplementedException();
    }

    public IEnvironmentTaskBuilder ParseEnvironmentTask(YamlMappingNode node)
    {
        throw new NotImplementedException();
    }
}