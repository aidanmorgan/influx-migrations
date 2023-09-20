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
}

public interface IMigrationLogger
{
    IMigrationOperationLogger<OperationExecutionState, IExecuteResult> ExecuteStart(MigrationOperationInstance op);
    IMigrationOperationLogger<OperationCommitState, ICommitResult> CommitStart(MigrationOperationInstance op);
    IMigrationOperationLogger<OperationRollbackState, IRollbackResult> RollbackStart(MigrationOperationInstance op);
    IMigrationTaskLogger TaskStart(IMigrationTask task);
    void Complete();
    void Failed(Exception x);
}

public interface IMigrationOperationLogger<S,R> where S : Enum
{
    void Complete(OperationResult<S, R?> result);
    IMigrationTaskLogger TaskStart(IMigrationTask task);
    void Failed(OperationResult<S, R?> result);
    void Failed(Exception result);
}

public interface IMigrationTaskLogger
{
    void Complete();
    void Failed(Exception x);
}