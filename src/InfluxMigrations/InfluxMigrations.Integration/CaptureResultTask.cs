using InfluxMigrations.Core;

namespace InfluxMigrations.IntegrationTests;

public class CaptureResultTask : IMigrationTask
{
    private readonly Action<object?> _callback;

    public CaptureResultTask(Action<object?> callback)
    {
        _callback = callback;
    }

    public Task ExecuteAsync(IOperationExecutionContext context)
    {
        _callback(context.ExecuteResult);
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(IMigrationExecutionContext ctx)
    {
        throw new NotImplementedException($"Cannot resolve a result of a Migration.");
    }
}

public class CaptureResultBuilder : IMigrationTaskBuilder
{
    public object? Result { get; private set; }

    public T? As<T>()
    {
        return (T?)Result;
    }

    public IMigrationTask Build()
    {
        return new CaptureResultTask(x => this.Result = x);
    }
}