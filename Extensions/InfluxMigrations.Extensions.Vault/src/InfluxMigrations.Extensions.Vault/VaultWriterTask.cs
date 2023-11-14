using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;
using InfluxMigrations.Yaml;
using Vault.Model;
using YamlDotNet.RepresentationModel;

namespace InfluxMigrations.Extensions.Vault;

public class VaultWriterTask : AbstractVaultTask
{
    public IResolvable<string?> KeyValue { get; init; }
    
    protected override async Task<TaskResult> ExecuteTask(IContext ctx, VaultTaskParameters vault)
    {
        var value = KeyValue.Resolve(ctx);
        if (string.IsNullOrEmpty(value))
        {
            return TaskResults.TaskFailure("Cannot write to Vault, value cannot be resolved.");
        }
        
        var requestData = new KvV2WriteRequest(new Dictionary<string, string>()
        {
            { vault.Key, value }
        });
        
        await vault.Client.Secrets.KvV2WriteAsync(vault.Path, requestData);
        return TaskResults.TaskSuccess();
    }
}

public class VaultWriterBuilder : IMigrationTaskBuilder, IOperationTaskBuilder, IEnvironmentTaskBuilder
{
    public string KeyName { get; private set; }
    public string KeyValue { get; private set; }
    
    public string VaultHost { get; private set; }
    
    public string VaultToken { get; private set; }
    
    public string VaultPath { get; private set; }

    public VaultWriterBuilder WithKeyName(string name)
    {
        this.KeyName = name;
        return this;
    }

    public VaultWriterBuilder WithKeyValue(string name)
    {
        this.KeyValue = name;
        return this;
    }

    public VaultWriterBuilder WithVaultHost(string host)
    {
        this.VaultHost = host;
        return this;
    }

    public VaultWriterBuilder WithVaultToken(string token)
    {
        this.VaultToken = token;
        return this;
    }

    public VaultWriterBuilder WithVaultPath(string path)
    {
        this.VaultPath = path;
        return this;
    }
    
    private VaultWriterTask Build()
    {
        return new VaultWriterTask()
        {
            KeyName = StringResolvable.Parse(KeyName),
            KeyValue = StringResolvable.Parse(KeyValue),
            VaultHost = StringResolvable.Parse(VaultHost),
            VaultToken = StringResolvable.Parse(VaultToken),
            VaultPath = StringResolvable.Parse(VaultPath)
        };
    }

    public IMigrationTask BuildMigration()
    {
        return Build();
    }

    public IOperationTask BuildOperation()
    {
        return Build();
    }

    public IEnvironmentTask BuildEnvironment()
    {
        return Build();
    }
}

[YamlTaskParser("vault:write")]
public class VaultWriterParser : IYamlTaskParser
{
    private VaultWriterBuilder Parse(YamlMappingNode node)
    {
        var builder = new VaultWriterBuilder();

        node.Value("key", x =>
        {
            builder.WithKeyName(x);
        });

        node.Value("value", x =>
        {
            builder.WithKeyValue(x);
        });

        node.Value("host", x =>
        {
            builder.WithVaultHost(x);
        });

        node.Value("token", x =>
        {
            builder.WithVaultToken(x);
        });

        node.Value("path", x =>
        {
            builder.WithVaultPath(x);
        });

        return builder;
    }

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
}