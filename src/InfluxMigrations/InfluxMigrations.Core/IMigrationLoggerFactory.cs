namespace InfluxMigrations.Core;

public interface IMigrationLoggerFactory
{
    IMigrationLogger MigrationStart(string version, MigrationDirection dir);
}

public interface IMigrationHistoryLogger
{
    void LoadException(Exception x);
    void SaveException(Exception x);
}

public interface IMigrationLoaderLogger
{
    void Exception(Exception exception);
    void FoundMigration(string file, IMigration x);
    void ParsingFailed(string file, MigrationParsingException x);
}

public interface IMigrationRunnerLogger
{
    void ExecutionPlan(List<IMigration> toExecute, MigrationDirection down);
    void NoMigrations();
    void MigrationSaved(MigrationHistory entry);
    void MigrationSaveFailed(MigrationHistory entry);
    ITaskLogger StartTask(IEnvironmentTask task);
}

public interface IMigrationLogger
{
    IMigrationOperationLogger<OperationExecutionState, IExecuteResult> ExecuteStart(MigrationOperationInstance op);
    IMigrationOperationLogger<OperationCommitState, ICommitResult> CommitStart(MigrationOperationInstance op);
    IMigrationOperationLogger<OperationRollbackState, IRollbackResult> RollbackStart(MigrationOperationInstance op);
    ITaskLogger TaskStart(IMigrationTask task);
    void Complete();
    void Failed(Exception x);
}

public interface IMigrationOperationLogger<S, R> where S : Enum
{
    void Complete(OperationResult<S, R?> result);
    
    ITaskLogger TaskStart(IMigrationTask task);
    ITaskLogger TaskStart(IOperationTask task);
    
    void Failed(OperationResult<S, R?> result);
    void Failed(Exception result);
}

public interface ITaskLogger
{
    void Complete();
    void Failed(Exception x);

    void TaskResult(TaskResult taskResult)
    {
        if (taskResult.State == TaskState.Success)
        {
            Complete();
        }
        else
        {
            Failed(taskResult.Exception);
        }
    }
}