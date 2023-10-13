using InfluxMigrations.Core;
using Vault;
using Vault.Client;

namespace InfluxMigrations.Extensions.Vault;

public abstract class AbstractVaultTask : IMigrationTask, IEnvironmentTask, IOperationTask
{
    public IResolvable<string?> VaultHost { get; init; }
    
    public IResolvable<string?> VaultToken { get; init; }
    
    public IResolvable<string?> VaultPath { get; init; }
    
    public IResolvable<string?> VaultNamespace { get; init; }
   
    public IResolvable<string?> KeyName { get; init; }

    public Task<TaskResult> ExecuteAsync(IOperationExecutionContext ctx)
    {
        return _Execute(ctx);
    }

    public Task<TaskResult> ExecuteAsync(IMigrationExecutionContext ctx)
    {
        return _Execute(ctx);
    }
    
    public Task<TaskResult> ExecuteAsync(IEnvironmentExecutionContext ctx)
    {
        return _Execute(ctx);
    }


    private async Task<TaskResult> _Execute(IContext ctx)
    {
        var key = KeyName?.Resolve(ctx);
        if (string.IsNullOrEmpty(key))
        {
            return TaskResults.TaskFailure("Cannot write to Vault, key cannot be resolved.");
        }
        
        var host = VaultHost?.Resolve(ctx);
        if (string.IsNullOrEmpty(host))
        {
            return TaskResults.TaskFailure("Cannot weite to Vault, host cannot be resolved.");
        }

        var token = VaultToken?.Resolve(ctx);
        if (string.IsNullOrEmpty(token))
        {
            return TaskResults.TaskFailure("Cannot write to Vault, token cannot be resolved.");
        }

        var path = VaultPath?.Resolve(ctx);
        if (string.IsNullOrEmpty(path))
        {
            return TaskResults.TaskFailure("Cannot write to Vault, path cannot be resolved.");
        }

        var ns = VaultNamespace?.Resolve(ctx);

        try
        {
            var vaultClient = new VaultClient(new VaultConfiguration(host));
            vaultClient.SetToken(token);
            if (!string.IsNullOrEmpty(ns))
            {
                vaultClient.SetNamespace(ns);
            }

            return await ExecuteTask(ctx, new VaultTaskParameters(host, token, ns, path, key, vaultClient));
        }
        catch (VaultApiException x)
        {
            return TaskResults.TaskFailure(x);
        }
    }

    protected abstract Task<TaskResult> ExecuteTask(IContext taskParameters, VaultTaskParameters vaultTaskParameters);
}

public record VaultTaskParameters(string Host, string Token, string Namespace, string Path, string Key, VaultClient Client);