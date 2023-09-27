namespace InfluxMigrations.Core;

public interface IEnvironmentConfigurator
{
    Task ConfigureEnvironmentAsync(IEnvironmentExecutionContext context, IMigrationRunnerOptions opts);
}

public class NoOpEnvironmentConfigurator : IEnvironmentConfigurator
{
    public Task ConfigureEnvironmentAsync(IEnvironmentExecutionContext context, IMigrationRunnerOptions opts)
    {
        return Task.CompletedTask;
    }
}