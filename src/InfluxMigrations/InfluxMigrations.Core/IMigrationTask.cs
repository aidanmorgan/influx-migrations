using System.Runtime.Serialization;

namespace InfluxMigrations.Core;

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
    Task ExecuteAsync(IOperationExecutionContext ctx);

    Task ExecuteAsync(IMigrationExecutionContext ctx);
}


public interface IMigrationTaskBuilder
{
    IMigrationTask Build();
}

 

