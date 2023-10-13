using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Tasks;

public class WriteFileTask : IMigrationTask, IOperationTask, IEnvironmentTask
{
    public IResolvable<string> File { get; set; }
    public List<IResolvable<string>> Content { get; set; }

    public WriteFileTask()
    {
    }

    public async Task<TaskResult> ExecuteAsync(IOperationExecutionContext ctx)
    {
        return await Execute(ctx);
    }

    public async Task<TaskResult> ExecuteAsync(IMigrationExecutionContext executionContext)
    {
        return await Execute(executionContext);
    }

    public async Task<TaskResult> ExecuteAsync(IEnvironmentExecutionContext ctx)
    {
        return await Execute(ctx);
    }

    private Task<TaskResult> Execute(IContext executionContext)
    {
        var outputFile = File.Resolve(executionContext);
        var content = string.Join("", Content.Select(x => x.Resolve(executionContext)));

        try
        {
            using StreamWriter output = new StreamWriter(outputFile);
            output.WriteLine(content);

            return TaskResults.TaskSuccessAsync();
        }
        catch (IOException x)
        {
            return TaskResults.TaskFailureAsync(x);
        }
    }
}

public class WriteFileTaskBuilder : IMigrationTaskBuilder, IOperationTaskBuilder, IEnvironmentTaskBuilder
{
    public string File { get; private set; }
    public List<string> Content { get; private set; } = new List<string>();

    public WriteFileTaskBuilder WithFile(string file)
    {
        this.File = file;
        return this;
    }

    public WriteFileTaskBuilder WithContent(string content)
    {
        Content.Add(content);
        return this;
    }

    public WriteFileTaskBuilder WithContent(List<string> content)
    {
        Content.AddRange(content);
        return this;
    }


    public IMigrationTask BuildMigration()
    {
        return new WriteFileTask()
        {
            Content = Content.Select(StringResolvable.Parse).ToList(),
            File = StringResolvable.Parse(File),
        };
    }

    public IOperationTask BuildOperation()
    {
        return new WriteFileTask()
        {
            Content = Content.Select(StringResolvable.Parse).ToList(),
            File = StringResolvable.Parse(File),
        };
    }

    public IEnvironmentTask BuildEnvironment()
    {
        return new WriteFileTask()
        {
            Content = Content.Select(StringResolvable.Parse).ToList(),
            File = StringResolvable.Parse(File),
        };
    }
}