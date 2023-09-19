using InfluxMigrations.Core;

namespace InfluxMigrations.CodeFirst;

public record CodeFirstMigrationOperation(string Id, IMigrationOperationBuilder Builder);

public interface ICodeFirstMigration
{
    Task AddUp(Migration migration);
    Task AddDown(Migration migration);
    Task AddTask(Migration migration);
}