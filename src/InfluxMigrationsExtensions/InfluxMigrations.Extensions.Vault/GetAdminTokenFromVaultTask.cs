using InfluxMigrations.Commands.Setup;
using InfluxMigrations.Core;
using InfluxMigrations.Yaml;

namespace InfluxMigrations.Extensions.Vault;

/// <summary>
/// A IMigrationTask that is used to load the Influx admin token from Hashicorp Vault and then use the token
/// for the remainder of a migration.
///
/// Recommended use is to assign it as a "pre-execute" task for the Migration itself until a mechanism for having "global'
/// tasks being able to be defined is created.
/// </summary>
public class GetAdminTokenFromVaultTask : AbstractVaultTask, IMigrationTask
{
    protected override async Task<TaskResult> ExecuteTask(IContext taskParameters, VaultTaskParameters vault)
    {
        var result = await vault.Client.Secrets.KvV2ReadAsync(vault.Path);

        string token = null;
        
        taskParameters.Accept(new ChangeAdminTokenVisitor(token));

        return TaskResults.TaskSuccess();
    }
}

[YamlTaskParser("vault-get-admin-token")]
public class GetAdminTokenFromVaultTaskBuilder : IMigrationTaskBuilder
{
    public IMigrationTask Build()
    {
        return new GetAdminTokenFromVaultTask();
    }
}