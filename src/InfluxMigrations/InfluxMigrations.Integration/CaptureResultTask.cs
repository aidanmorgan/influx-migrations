using InfluxMigrations.Core;

namespace InfluxMigrations.IntegrationTests;

public class CaptureResultTask : IMigrationTask, IEnvironmentTask, IOperationTask
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

    public Task<TaskResult> ExecuteAsync(IMigrationExecutionContext context)
    {
        return TaskResults.TaskSuccessAsync();
    }

    public Task<TaskResult> ExecuteAsync(IEnvironmentExecutionContext ctx)
    {
        return TaskResults.TaskSuccessAsync();
    }
}

public class CaptureResultBuilder : IMigrationTaskBuilder, IOperationTaskBuilder, IEnvironmentTaskBuilder
{
    public object? Result { get; private set; }

    public T? As<T>()
    {
        return (T?)Result;
    }

    public IMigrationTask BuildMigration()
    {
        return new CaptureResultTask(x => { });
    }

    public IOperationTask BuildOperation()
    {
        return new CaptureResultTask(x => this.Result = x);
    }

    public IEnvironmentTask BuildEnvironment()
    {
        return new CaptureResultTask(x => { });
    }
}