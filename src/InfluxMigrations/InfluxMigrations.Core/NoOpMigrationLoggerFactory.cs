namespace InfluxMigrations.Core;


public class NoOpMigrationHistoryLogger : IMigrationHistoryLogger
{
    public void LoadException(Exception x)
    {
    }

    public void SaveException(Exception x)
    {
    }
}

public class NoOpMigrationRunnerLogger : IMigrationRunnerLogger
{
    public void ExecutionPlan(List<IMigration> toExecute, MigrationDirection down)
    {
    }

    public void NoMigrations()
    {
    }

    public void MigrationSaved(MigrationHistory entry)
    {
    }

    public void MigrationSaveFailed(MigrationHistory entry)
    {
    }
}

public class NoOpMigrationLoaderLogger : IMigrationLoaderLogger
{
    public void Exception(Exception exception)
    {
    }

    public void FoundMigration(string file, Migration x)
    {
    }

    public void ParsingFailed(string file, MigrationParsingException x)
    {
    }
}

public class NoOpMigrationLoggerFactory : IMigrationLoggerFactory
{
    public IMigrationLogger MigrationStart(string version, MigrationDirection dir)
    {
        return new NoOpMigrationLogger();
    }
    
}

public class NoOpMigrationLogger : IMigrationLogger
{
    public IMigrationOperationLogger<OperationExecutionState, IExecuteResult> ExecuteStart(MigrationOperationInstance op)
    {
        return new NoOpMigrationOperationLogger<OperationExecutionState, IExecuteResult>();
    }

    public IMigrationOperationLogger<OperationCommitState, ICommitResult> CommitStart(MigrationOperationInstance op)
    {
        return new NoOpMigrationOperationLogger<OperationCommitState, ICommitResult>();
    }

    public IMigrationOperationLogger<OperationRollbackState, IRollbackResult> RollbackStart(MigrationOperationInstance op)
    {
        return new NoOpMigrationOperationLogger<OperationRollbackState, IRollbackResult>();
    }

    public IMigrationTaskLogger TaskStart(IMigrationTask task)
    {
        return new NoOpTaskLogger();
    }
    
    public void Complete()
    {
            
    }

    public void Failed(Exception ex)
    {
        
    }    
}

public class NoOpMigrationOperationLogger<A,B> : IMigrationOperationLogger<A, B> where A : Enum
{
    public NoOpMigrationOperationLogger()
    {
    }

    public void Complete(OperationResult<A, B?> result)
    {
        
    }

    public IMigrationTaskLogger TaskStart(IMigrationTask migrationTask)
    {
        return new NoOpTaskLogger();
    }

    public void Failed(OperationResult<A, B?> result)
    {
    }

    public void Failed(Exception result)
    {
    }
}

public class NoOpTaskLogger : IMigrationTaskLogger
{
    public NoOpTaskLogger()
    {
    }

    public void Complete()
    {
    }

    public void Failed(Exception x)
    {
    }
}