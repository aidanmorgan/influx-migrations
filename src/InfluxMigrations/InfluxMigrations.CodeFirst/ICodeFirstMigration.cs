using InfluxMigrations.Core;

namespace InfluxMigrations.CodeFirst;

public interface ICodeFirstMigration
{
    Task AddUp(Migration migration);
    Task AddDown(Migration migration);
    Task AddTask(Migration migration);
}