namespace InfluxMigrations.Core;

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

    public static TaskResult TaskFailure(string message)
    {
        return TaskFailure(new MigrationTaskExecutionException(message));
    }
    
    public static Task<TaskResult> TaskFailureAsymc(string message)
    {
        return TaskFailureAsync(new MigrationTaskExecutionException(message));
    }
    

    public static TaskResult TaskSuccess()
    {
        return new TaskResult()
        {
            State = TaskState.Success
        };
    }
}