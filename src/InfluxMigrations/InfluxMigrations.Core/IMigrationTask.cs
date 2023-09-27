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

public interface ITask
{
    // flagging interface only    
}


/// <summary>
/// A IMigrationTask is a set of tasks that are performed either before/after a Migration
/// </summary>
public interface IMigrationTask : ITask
{
    Task<TaskResult> ExecuteAsync(IMigrationExecutionContext ctx);
}

/// <summary>
/// A IOperationTask is a task that is performed before/after an operation - can be scoped to an additional phase
/// </summary>
public interface IOperationTask: ITask
{
    Task<TaskResult> ExecuteAsync(IOperationExecutionContext ctx);
}

/// <summary>
/// A IEnvironmentTask is a task that is performed before/after all Migrations have been performed.
/// </summary>
public interface IEnvironmentTask: ITask
{
    Task<TaskResult> ExecuteAsync(IEnvironmentExecutionContext ctx);

}