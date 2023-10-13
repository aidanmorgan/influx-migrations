using InfluxMigrations.Core;

namespace InfluxMigrations.CodeFirst;

public class CodeFirstEnvironmentConfigurator : IEnvironmentConfigurator
{
    public async Task ConfigureEnvironmentAsync(IEnvironmentExecutionContext context, IMigrationRunnerOptions opts)
    {
        var configurator = AppDomain.CurrentDomain.GetExtensionService()?.GetExtensionTypes()
            .WithAttributeAndInterface(typeof(InfluxConfigurationAttribute), typeof(IEnvironmentConfigurator));

        if (configurator is { Count: 1 })
        {
            if (Activator.CreateInstance(configurator.First().Item2) is IEnvironmentConfigurator instance)
            {
                await instance.ConfigureEnvironmentAsync(context, opts);
            }
        }
    }
}