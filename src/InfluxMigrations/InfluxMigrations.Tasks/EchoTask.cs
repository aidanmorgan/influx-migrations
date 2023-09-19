using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Outputs;

public class EchoTask : IMigrationTask
{
    public List<IResolvable<string>> Values { get; set; } = new List<IResolvable<string>>();
    
    public EchoTask()
    {
        
    }

    public Task ExecuteAsync(IMigrationExecutionContext ctx)
    {
        var line = string.Join("\n", Values.Select(x => x.Resolve(ctx)));

        if (!string.IsNullOrEmpty(line))
        {
            Console.WriteLine(line);
        }
        return Task.CompletedTask;
    }

   
    public async Task ExecuteAsync(IOperationExecutionContext ctx)
    {
        var line  = string.Join("\n", Values.Select(x => x.Resolve(ctx)));

        if (!string.IsNullOrEmpty(line))
        {
            Console.WriteLine(line);
            await Console.Out.FlushAsync();            
        }
    }
    
}

public class EchoTaskBuilder : IMigrationTaskBuilder
{
    private readonly List<string> _resolvables = new List<string>();

    public EchoTaskBuilder WithString(string val)
    {
        _resolvables.Add(val);
        return this;
    }
    
    public IMigrationTask Build()
    {
        return new EchoTask()
        {
            Values = _resolvables.Select(StringResolvable.Parse).ToList(),
        };
    }
}