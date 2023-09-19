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

    public Task ExecuteAsync(IOperationExecutionContext context)
    {
        var outputFile = File.Resolve(context);
        var content = string.Join("", Content.Select(x => x.Resolve(context)));

        using StreamWriter output = new StreamWriter(outputFile);
        output.WriteLine(content);

        return Task.CompletedTask;
    }
    
    public Task ExecuteAsync(IMigrationExecutionContext executionContext)
    {
        var outputFile = File.Resolve(executionContext);
        var content = string.Join("", Content.Select(x => x.Resolve(executionContext)));

        using StreamWriter output = new StreamWriter(outputFile);
        output.WriteLine(content);

        return Task.CompletedTask;
    }    
}

public class WriteFileTaskBuilder : IMigrationTaskBuilder
{
    private string _file;
    private List<string> _content = new List<string>();

    public WriteFileTaskBuilder WithFile(string file)
    {
        this._file = file;
        return this;
    }

    public WriteFileTaskBuilder WithContent(string content)
    {
        _content.Add(content);
        return this;
    }

    public WriteFileTaskBuilder WithContent(List<string> content)
    {
        _content.AddRange(content);
        return this;
    }
    
    
    public IMigrationTask Build()
    {
        return new WriteFileTask()
        {
            Content = _content.Select(StringResolvable.Parse).ToList(),
            File = StringResolvable.Parse(_file),
        };
    }
}