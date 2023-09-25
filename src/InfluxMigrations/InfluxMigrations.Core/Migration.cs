namespace InfluxMigrations.Core;

/// <summary>
/// This pulls together all of the bits into a something that has a chance of being executed. We need to be able to assemble
/// an operation (and it's corresponding tasks) together with a run-time identifier.
/// </summary>
public class MigrationOperationInstance
{
    public string Id { get; private set; }
    public IMigrationOperationBuilder Operation { get; private set; }

    public List<IMigrationTaskBuilder> BeforeExecuteTasks { get; private set; } = new List<IMigrationTaskBuilder>();
    public List<IMigrationTaskBuilder> BeforeCommitTasks { get; private set; } = new List<IMigrationTaskBuilder>();
    public List<IMigrationTaskBuilder> BeforeRollbackTasks { get; private set; } = new List<IMigrationTaskBuilder>();

    
    public List<IMigrationTaskBuilder> AfterExecuteTasks { get; private set; } = new List<IMigrationTaskBuilder>();
    public List<IMigrationTaskBuilder> AfterCommitTasks { get; private set; } = new List<IMigrationTaskBuilder>();
    public List<IMigrationTaskBuilder> AfterRollbackTasks { get; private set; } = new List<IMigrationTaskBuilder>();


    public MigrationOperationInstance(string id, IMigrationOperationBuilder operation)
    {
        Id = id;
        Operation = operation;
    }

    private static void AddTask(TaskPrecedence order, IMigrationTaskBuilder builder, List<IMigrationTaskBuilder> befores,  List<IMigrationTaskBuilder> afters)
    {
        switch (order)
        {
            case TaskPrecedence.After:
            {
                afters.Add(builder);
                break;
            }

            case TaskPrecedence.Before:
            {
                befores.Add(builder);
                break;
            }

            default:
            {
                throw new MigrationConfigurationException($"Unrecognised TaskPrecedence value : {order}");
            }
        }
    }
    
    public MigrationOperationInstance AddExecuteTask(TaskPrecedence order, IMigrationTaskBuilder builder)
    {
        AddTask(order, builder, BeforeExecuteTasks, AfterExecuteTasks);
        return this;
    }

    public MigrationOperationInstance AddCommitTask(TaskPrecedence order, IMigrationTaskBuilder builder)
    {
        AddTask(order, builder, BeforeCommitTasks, AfterCommitTasks);
        return this;
    }

    public MigrationOperationInstance AddRollbackTask(TaskPrecedence order, IMigrationTaskBuilder builder)
    {
        AddTask(order, builder, BeforeRollbackTasks, AfterRollbackTasks);
        return this;
    }
}

record MigrationOperationRuntimeState(string Id, MigrationOperationInstance Instance, IMigrationOperation Operation,
    OperationResult<OperationExecutionState, IExecuteResult> ExecuteResult,
    IOperationExecutionContext ExecutionContext);

public interface IMigration
{
    string Version { get; set; }
    MigrationOperationInstance AddUp(string id, IMigrationOperationBuilder operation);
    MigrationOperationInstance AddDown(string id, IMigrationOperationBuilder operation);

    Task<MigrationResult> ExecuteAsync(IMigrationEnvironmentContext env, MigrationDirection direction,
        MigrationOptions? opts = null);

    IMigration AddAfterTask(IMigrationTaskBuilder task);

    IMigration AddBeforeTask(IMigrationTaskBuilder task);
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
    private readonly List<IMigrationTaskBuilder> _beforeMigrationTasks = new List<IMigrationTaskBuilder>();
    private readonly List<IMigrationTaskBuilder> _afterMigrationTaks = new List<IMigrationTaskBuilder>();

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

    public async Task<MigrationResult> ExecuteAsync(IMigrationEnvironmentContext env, MigrationDirection direction,
        MigrationOptions? o = null)
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
            foreach (var task in _beforeMigrationTasks.Select(x => x.Build()))
            {
                var outputLogger = logger.TaskStart(task);
                var result = await task.ExecuteAsync(context);
                outputLogger.TaskResult(result);
            }
            
            foreach (var op in (direction == MigrationDirection.Up ? _upOperations : _downOperations))
            {
                var executeLog = logger.ExecuteStart(op);

                var executionContext = context.CreateExecutionContext(op.Id);

                foreach (var task in op.BeforeExecuteTasks.Select(x => x.Build()))
                {
                    await task.ExecuteAsync(executionContext);
                }
                
                var operation = op.Operation.Build(executionContext);
                var operationResult = await operation.ExecuteAsync();

                if (operationResult.State is OperationExecutionState.Success or OperationExecutionState.Skipped)
                {
                    instanceOperations.Add(new MigrationOperationRuntimeState(op.Id, op, operation, operationResult,
                        executionContext));
                    executionContext.ExecuteResult = operationResult.Result;
                    executeLog.Complete(operationResult);
                }
                else
                {
                    migrationResult.AddIssue(op.Id, MigrationIssueCategory.Operation, MigrationPhase.Execute,
                        MigrationIssueSeverity.Fail, operationResult);
                    executeLog.Failed(operationResult);

                    throw new MigrationExecutionException($"Execution of Operation {op.Id} failed.");
                }
                
                foreach (var output in op.AfterExecuteTasks.Select(x => x.Build()))
                {
                    var outputLog = executeLog.TaskStart(output);
                    var taskResult = await output.ExecuteAsync(executionContext);
                    outputLog.TaskResult(taskResult);
                }
            }

            // execute has finished successfully, time to perform commits
            foreach (var state in instanceOperations)
            {
                var commitLog = logger.CommitStart(state.Instance);

                foreach (var task in state.Instance.BeforeCommitTasks.Select(x => x.Build()))
                {
                    var outputLog = commitLog.TaskStart(task);
                    var taskResult = await task.ExecuteAsync(state.ExecutionContext);
                    outputLog.TaskResult(taskResult);
                }
                
                var commitResult = await state.Operation.CommitAsync(state.ExecuteResult.Result);
                state.ExecutionContext.CommitResult = commitResult?.Result;

                if (commitResult?.State == OperationCommitState.Failed)
                {
                    migrationResult.AddIssue(state.Id, MigrationIssueCategory.Operation, MigrationPhase.Commit,
                        MigrationIssueSeverity.Fail, commitResult);
                    commitLog.Failed(commitResult!);
                }
                else
                {
                    commitLog.Complete(commitResult);
                }

                foreach (var output in state.Instance.AfterCommitTasks.Select(x => x.Build()))
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

                    foreach (var task in state.Instance.BeforeRollbackTasks.Select(x => x.Build()))
                    {
                        var outputLog = rollbackLog.TaskStart(task);
                        var result = await task.ExecuteAsync(state.ExecutionContext);
                        outputLog.TaskResult(result);
                    }

                    
                    var rollbackResult = await state.Operation.RollbackAsync(state.ExecuteResult.Result);
                    state.ExecutionContext.RollbackResult = rollbackResult?.Result;

                    if (rollbackResult?.State == OperationRollbackState.Failed)
                    {
                        migrationResult.AddIssue(state.Id, MigrationIssueCategory.Operation, MigrationPhase.Rollback,
                            MigrationIssueSeverity.Fail, rollbackResult);
                        rollbackLog.Failed(rollbackResult);
                    }
                    else
                    {
                        rollbackLog.Complete(rollbackResult);
                    }

                    foreach (var task in state.Instance.AfterRollbackTasks.Select(x => x.Build()))
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
            foreach (var task in _afterMigrationTaks.Select(x => x.Build()))
            {
                var outputLogger = logger.TaskStart(task);

                var result = await task.ExecuteAsync(context);
                outputLogger.TaskResult(result);
            }
        }

        return migrationResult;
    }

    public IMigration AddAfterTask(IMigrationTaskBuilder task)
    {
        _afterMigrationTaks.Add(task);
        return this;
    }

    public IMigration AddBeforeTask(IMigrationTaskBuilder task)
    {
        _beforeMigrationTasks.Add(task);
        return this;
    }
}