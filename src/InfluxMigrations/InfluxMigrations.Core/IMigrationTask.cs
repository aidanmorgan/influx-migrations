using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

public enum TaskState
{
    Success,
    Failure
}

public class TaskResult
{
    public TaskState State { get; init; }
    public Exception? Exception { get; init; }
}

public static class TaskResults
{
    public static Task<TaskResult> TaskSuccessAsync()
    {
        return Task.FromResult(new TaskResult()
        {
            State = TaskState.Success
        });
    }

    public static Task<TaskResult> TaskFailureAsync(Exception? x)
    {
        return Task.FromResult(new TaskResult()
        {
            State = TaskState.Failure,
            Exception = x
        });
    }

    public static TaskResult TaskFailure(Exception? x)
    {
        return new TaskResult()
        {
            State = TaskState.Failure,
            Exception = x
        };
    }

    public static TaskResult TaskSuccess()
    {
        return new TaskResult()
        {
            State = TaskState.Success
        };
    }
}

public enum OutputPhase
{
    [EnumMember(Value = "up")] Up = 0,
    [EnumMember(Value = "down")] Down = 1,
    [EnumMember(Value = "finalize")] Finalize = 2
}

/// <summary>
/// A IMigrationTask is a set of tasks that are performed after a MigrationOperation or after the Migration as a whole
/// have completed.
///
/// MigrationOutput's are not allowed to throw exceptions, they can be invoked at different phases.
/// </summary>
public interface IMigrationTask
{
    // runs the task in the context of an 
    Task<TaskResult> ExecuteAsync(IOperationExecutionContext ctx);

    Task<TaskResult> ExecuteAsync(IMigrationExecutionContext ctx);
}

public interface IMigrationTaskBuilder
{
    IMigrationTask Build();
}