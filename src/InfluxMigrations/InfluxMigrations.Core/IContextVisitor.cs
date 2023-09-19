namespace InfluxMigrations.Core;

/// <summary>
/// Basic implementation of a visitor pattern that allows the context hierarchy to be walked in case changes
/// are needed to be made.
/// </summary>
public interface IContextVisitor
{
    void Visit(IOperationExecutionContext ctx);
    void Visit(IMigrationExecutionContext ctx);
    void Visit(IMigrationEnvironmentContext ctx);
}