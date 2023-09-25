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