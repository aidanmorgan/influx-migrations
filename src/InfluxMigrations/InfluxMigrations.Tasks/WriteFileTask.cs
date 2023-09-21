using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;

namespace InfluxMigrations.Outputs;

public class WriteFileTask : IMigrationTask
{
    public IResolvable<string> File { get; set; }
    public List<IResolvable<string>> Content { get; set; }

    public WriteFileTask()
    {
    }

    public Task<TaskResult> ExecuteAsync(IOperationExecutionContext context)
    {
        var outputFile = File.Resolve(context);
        var content = string.Join("", Content.Select(x => x.Resolve(context)));

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

    public Task<TaskResult> ExecuteAsync(IMigrationExecutionContext executionContext)
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

public class WriteFileTaskBuilder : IMigrationTaskBuilder
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


    public IMigrationTask Build()
    {
        return new WriteFileTask()
        {
            Content = Content.Select(StringResolvable.Parse).ToList(),
            File = StringResolvable.Parse(File),
        };
    }
}