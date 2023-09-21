using InfluxMigrations.Core;

namespace InfluxMigrations.IntegrationTests;

public class CaptureResultTask : IMigrationTask
{
    private readonly Action<object?> _callback;

    public CaptureResultTask(Action<object?> callback)
    {
        _callback = callback;
    }

    public Task<TaskResult> ExecuteAsync(IOperationExecutionContext context)
    {
        _callback(context.ExecuteResult);
        return TaskResults.TaskSuccessAsync();
    }

    public Task<TaskResult> ExecuteAsync(IMigrationExecutionContext ctx)
    {
        return TaskResults.TaskSuccessAsync();
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