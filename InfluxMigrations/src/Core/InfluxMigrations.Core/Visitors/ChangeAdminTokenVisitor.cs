namespace InfluxMigrations.Core.Visitors;

/// <summary>
/// A IContextVisitor implementation that will change the admin token used by influx.
/// </summary>
public class ChangeAdminTokenVisitor : IContextVisitor
{
    private readonly string _token;

    public ChangeAdminTokenVisitor(string token)
    {
        _token = token;
    }

    public void Visit(IOperationExecutionContext ctx)
    {
        ctx.MigrationExecutionContext.Accept(this);
    }

    public void Visit(IMigrationExecutionContext ctx)
    {
        ctx.EnvironmentExecutionContext.Accept(this);
    }

    public void Visit(IEnvironmentExecutionContext ctx)
    {
        ctx.InfluxFactory.WithToken(_token);
    }
}