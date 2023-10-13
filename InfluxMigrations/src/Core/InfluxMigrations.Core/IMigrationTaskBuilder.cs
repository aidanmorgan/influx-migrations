namespace InfluxMigrations.Core;

public interface ITaskBuilder
{
    // flagging interface only
}

public interface IMigrationTaskBuilder  : ITaskBuilder
{
    IMigrationTask BuildMigration();
}

public interface IOperationTaskBuilder : ITaskBuilder
{
    IOperationTask BuildOperation();
}

public interface IEnvironmentTaskBuilder : ITaskBuilder
{
    IEnvironmentTask BuildEnvironment();
}