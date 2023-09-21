namespace InfluxMigrations.Core;

public enum MigrationIssueSeverity 
{
    Fail,
    Warning
}

public enum MigrationPhase
{
    Execute,
    Commit,
    Rollback,
    Task,
    Migration
}

public enum MigrationIssueCategory
{
    Task,
    Operation
}

public class MigrationIssue
{
    public string Id { get; init; }
    public MigrationIssueSeverity Severity { get; init; }
    public MigrationPhase Phase { get; init; }
    public MigrationIssueCategory Category { get; init; }
    public Exception? Exception { get; init; }
}

public class MigrationResult 
{
    public string Version { get; init; }
    
    private readonly List<MigrationIssue> _issues = new List<MigrationIssue>();
    public List<MigrationIssue> Issues => new List<MigrationIssue>(_issues);
    
    public MigrationIssue AddIssue(MigrationIssue issue)
    {
        _issues.Add(issue);
        return issue;
    }

    public MigrationIssue AddIssue<TS,TR>(string id, MigrationIssueCategory category, MigrationPhase phase, MigrationIssueSeverity severity, OperationResult<TS, TR> result = null) where TS : Enum
    {
        var issue = new MigrationIssue()
        {
            Id = id,
            Category = category,
            Phase = phase,
            Severity = severity,
            Exception = result.Result is IExceptionResult exceptionResult ? exceptionResult.Exception : null
        };
        
        _issues.Add(issue);
        return issue;
    }

    /// <summary>
    /// Returns true if the migration was a success, false if any issues were encountered.
    /// </summary>
    public bool Success => _issues.Count == 0;

    /// <summary>
    /// Returns true if the underlying database is now in an inconsistent state, false otherwise
    /// </summary>
    public bool Inconsistent => _issues.Any(x => x.Phase == MigrationPhase.Rollback);

    public MigrationDirection Direction { get; set; }
    public MigrationOptions Options { get; set; }
}


/// <summary>
/// This pulls together all of the bits into a something that has a chance of being executed. We need to be able to assemble
/// an operation (and it's corresponding tasks) together with a run-time identifier.
/// </summary>
public class MigrationOperationInstance
{
    public string Id { get; private set; }
    public IMigrationOperationBuilder Operation { get; private set; }
    public List<IMigrationTaskBuilder> ExecuteTasks { get; private set; } = new List<IMigrationTaskBuilder>();
    public List<IMigrationTaskBuilder> CommitTasks { get; private set; } = new List<IMigrationTaskBuilder>();
    public List<IMigrationTaskBuilder> RollbackTasks { get; private set; } = new List<IMigrationTaskBuilder>();
    
   
    public MigrationOperationInstance(string id, IMigrationOperationBuilder operation)
    {
        Id = id;
        Operation = operation;
    }

    public MigrationOperationInstance AddExecuteTask(IMigrationTaskBuilder builder)
    {
        ExecuteTasks.Add(builder);
        return this;
    }

    public MigrationOperationInstance AddCommitTask(IMigrationTaskBuilder builder)
    {
        CommitTasks.Add(builder);
        return this;
    }

    public MigrationOperationInstance AddRollbackTask(IMigrationTaskBuilder builder)
    {
        RollbackTasks.Add(builder);
        return this;
    }
}

record MigrationOperationRuntimeState(string Id, MigrationOperationInstance Instance, IMigrationOperation Operation,
    OperationResult<OperationExecutionState, IExecuteResult> ExecuteResult, IOperationExecutionContext ExecutionContext);

public interface IMigration
{
    string Version { get; set; }
    MigrationOperationInstance AddUp(string id, IMigrationOperationBuilder operation);
    MigrationOperationInstance AddDown(string id, IMigrationOperationBuilder operation);
    Task<MigrationResult> ExecuteAsync(IMigrationEnvironmentContext env, MigrationDirection direction, MigrationOptions? opts = null);
    IMigration AddTask(IMigrationTaskBuilder task);
}

public class MigrationOptions
{
    public IMigrationLoggerFactory Logger { get; init; } = new NoOpMigrationLoggerFactory();
}

/// <summary>
/// A Migration is a collection of MigrationTasks that are performed together to complete a database migration.
/// </summary>
public class Migration : IMigration
{
    private readonly List<MigrationOperationInstance> _upOperations = new List<MigrationOperationInstance>();
    private readonly List<MigrationOperationInstance> _downOperations = new List<MigrationOperationInstance>();
    private readonly List<IMigrationTaskBuilder> _migrationTasks = new List<IMigrationTaskBuilder>();

    public string Version { get; set; }

    public Migration()
    {
    }
    
    public Migration(string version)
    {
        Version = version;
    }

    public MigrationOperationInstance AddUp(string id, IMigrationOperationBuilder operation)
    {
        var entry = new MigrationOperationInstance(id, operation);
        _upOperations.Add(entry);
        return entry;
    }

    public MigrationOperationInstance AddDown(string id, IMigrationOperationBuilder operation)
    {
        var entry = new MigrationOperationInstance(id, operation);
        _downOperations.Add(entry);
        return entry;
    }

    public async Task<MigrationResult> ExecuteAsync(IMigrationEnvironmentContext env, MigrationDirection direction, MigrationOptions? o = null)
    {
        // TODO : refactor this method, it's long and doing too many things
        var options = o ?? new MigrationOptions();
        var migrationResult = new MigrationResult()
        {
            Version = Version,
            Direction = direction,
            Options = options
        };
        
        var logger = env.LoggerFactory.MigrationStart(Version, direction);
        var context = env.CreateMigrationContext(Version);

        var instanceOperations = new List<MigrationOperationRuntimeState>();

        try
        {
            foreach (var op in (direction == MigrationDirection.Up ? _upOperations : _downOperations))
            {
                var executeLog = logger.ExecuteStart(op);

                var executionContext = context.CreateExecutionContext(op.Id);
                var operation = op.Operation.Build(executionContext);
                var operationResult = await operation.ExecuteAsync();

                if (operationResult.State is OperationExecutionState.Success or OperationExecutionState.Skipped)
                {
                    executionContext.ExecuteResult = operationResult.Result;
                    instanceOperations.Add(new MigrationOperationRuntimeState(op.Id, op, operation, operationResult, executionContext));
                    executeLog.Complete(operationResult);

                    foreach (var output in op.ExecuteTasks.Select(x => x.Build()))
                    {
                        var outputLog = executeLog.TaskStart(output);
                        var taskResult = await output.ExecuteAsync(executionContext);
                        outputLog.TaskResult(taskResult);
                    }
                }
                else
                {
                    migrationResult.AddIssue(op.Id, MigrationIssueCategory.Operation, MigrationPhase.Execute, MigrationIssueSeverity.Fail, operationResult);
                    executeLog.Failed(operationResult);
                    
                    throw new MigrationExecutionException($"Execution of step {op.Id} failed.");
                }
            }

            // execute has finished successfully, time to perform commits
            foreach (var state in instanceOperations)
            {
                var commitLog = logger.CommitStart(state.Instance);
                var commitResult = await state.Operation.CommitAsync(state.ExecuteResult.Result);
                state.ExecutionContext.CommitResult = commitResult?.Result;

                if (commitResult?.State == OperationCommitState.Failed)
                {
                    migrationResult.AddIssue(state.Id, MigrationIssueCategory.Operation, MigrationPhase.Commit, MigrationIssueSeverity.Fail, commitResult);
                    commitLog.Failed(commitResult!);
                }
                else
                {
                    commitLog.Complete(commitResult);
                }

                foreach (var output in state.Instance.CommitTasks.Select(x => x.Build()))
                {
                    var outputLog = commitLog.TaskStart(output);

                    var taskResult = await output.ExecuteAsync(state.ExecutionContext);
                    outputLog.TaskResult(taskResult);
                }
            }

            logger.Complete();
        }
        catch (MigrationException ex)
        {
            // if there's an exception then we should try to rollback any changes that were made
            foreach (var state in instanceOperations)
            {
                // we only have to rollback tasks that were completed successfully
                if (state.ExecuteResult.State == OperationExecutionState.Success)
                {
                    var rollbackLog = logger.RollbackStart(state.Instance);
                    
                    var rollbackResult = await state.Operation.RollbackAsync(state.ExecuteResult.Result);
                    state.ExecutionContext.RollbackResult = rollbackResult?.Result;

                    if (rollbackResult?.State == OperationRollbackState.Failed)
                    {
                        migrationResult.AddIssue(state.Id, MigrationIssueCategory.Operation, MigrationPhase.Rollback, MigrationIssueSeverity.Fail, rollbackResult);                            
                        rollbackLog.Failed(rollbackResult);
                    }
                    else
                    {
                        rollbackLog.Complete(rollbackResult);
                    }

                    foreach (var task in state.Instance.RollbackTasks.Select(x => x.Build()))
                    {
                        var outputLog = rollbackLog.TaskStart(task);

                        var result = await task.ExecuteAsync(state.ExecutionContext);
                        outputLog.TaskResult(result);
                    }
                }
            }
            
            logger.Failed(ex);
        }
        finally
        {
            foreach (var task in _migrationTasks.Select(x => x.Build()))
            {
                var outputLogger = logger.TaskStart(task);

                var result = await task.ExecuteAsync(context);
                outputLogger.TaskResult(result);
            }
        }

        return migrationResult;
    }

    public IMigration AddTask(IMigrationTaskBuilder task)
    {
        _migrationTasks.Add(task);
        return this;
    }
}