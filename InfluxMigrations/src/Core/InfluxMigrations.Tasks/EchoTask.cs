using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Tasks;

public class EchoTask : IMigrationTask, IOperationTask, IEnvironmentTask
{
    public List<IResolvable<string>> Values { get; set; } = new List<IResolvable<string>>();

    public TextWriter Writer { get; init; }

    public EchoTask()
    {
    }

    public Task<TaskResult> ExecuteAsync(IMigrationExecutionContext ctx)
    {
        var line = string.Join("\n", Values.Select(x => x.Resolve(ctx)));

        if (!string.IsNullOrEmpty(line))
        {
            Writer.WriteLine(line);
        }

        return TaskResults.TaskSuccessAsync();
    }


    public Task<TaskResult> ExecuteAsync(IOperationExecutionContext ctx)
    {
        var line = string.Join("\n", Values.Select(x => x.Resolve(ctx)));

        if (!string.IsNullOrEmpty(line))
        {
            Writer.WriteLine(line);
        }

        return TaskResults.TaskSuccessAsync();
    }

    public Task<TaskResult> ExecuteAsync(IEnvironmentExecutionContext ctx)
    {
        var line = string.Join("\n", Values.Select(x => x.Resolve(ctx)));

        if (!string.IsNullOrEmpty(line))
        {
            Writer.WriteLine(line);
        }

        return TaskResults.TaskSuccessAsync();
    }
}

public class EchoTaskBuilder : IMigrationTaskBuilder, IOperationTaskBuilder, IEnvironmentTaskBuilder
{
    public TextWriter Writer { get; private set; }
    public List<string> Lines { get; private set; } = new List<string>();

    public EchoTaskBuilder WithString(string val)
    {
        Lines.Add(val);
        return this;
    }

    public EchoTaskBuilder WithWriter(TextWriter writer)
    {
        Writer = writer;
        return this;
    }

    public IMigrationTask BuildMigration()
    {
        return new EchoTask()
        {
            Values = Lines.Select(StringResolvable.Parse).ToList(),
            Writer = this.Writer ?? Console.Out
        };
    }

    public IOperationTask BuildOperation()
    {
        return new EchoTask()
        {
            Values = Lines.Select(StringResolvable.Parse).ToList(),
            Writer = this.Writer ?? Console.Out
        };
    }

    public IEnvironmentTask BuildEnvironment()
    {
        return new EchoTask()
        {
            Values = Lines.Select(StringResolvable.Parse).ToList(),
            Writer = this.Writer ?? Console.Out
        };
    }
}