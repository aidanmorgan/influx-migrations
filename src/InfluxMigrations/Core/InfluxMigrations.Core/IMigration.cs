namespace InfluxMigrations.Core;

public interface IMigration
{
    string Version { get; set; }
    
    MigrationOperationInstance AddUp(string id, IMigrationOperationBuilder operation);
    MigrationOperationInstance AddDown(string id, IMigrationOperationBuilder operation);

    Task<MigrationResult> ExecuteAsync(IEnvironmentExecutionContext env, MigrationDirection direction, MigrationOptions? opts = null);

    IMigration AddBeforeTask(IMigrationTaskBuilder task);
    IMigration AddAfterTask(IMigrationTaskBuilder task);
}