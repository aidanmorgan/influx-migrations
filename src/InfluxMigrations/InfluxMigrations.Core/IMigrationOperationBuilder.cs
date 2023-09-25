namespace InfluxMigrations.Core;

public interface IMigrationOperationBuilder
{
    IMigrationOperation Build(IOperationExecutionContext context);
}