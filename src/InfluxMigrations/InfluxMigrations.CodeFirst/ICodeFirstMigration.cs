using InfluxMigrations.Core;

namespace InfluxMigrations.CodeFirst;

public interface ICodeFirstMigration
{
    Task AddUp(IMigration migration);
    Task AddDown(IMigration migration);
    Task AddTask(IMigration migration);
}