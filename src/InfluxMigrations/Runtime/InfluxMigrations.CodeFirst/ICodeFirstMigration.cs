using InfluxMigrations.Core;

namespace InfluxMigrations.CodeFirst;

public interface ICodeFirstMigration
{
    /// <summary>
    /// Add all of the up operations to the provided migration  
    /// </summary>
    Task AddUp(IMigration migration);

    /// <summary>
    /// Add all of the down operations to the provided migration  
    /// </summary>
    Task AddDown(IMigration migration);
    
    /// <summary>
    /// Add all of the task operations to the provided migration  
    /// </summary>
    Task AddTask(IMigration migration);
}