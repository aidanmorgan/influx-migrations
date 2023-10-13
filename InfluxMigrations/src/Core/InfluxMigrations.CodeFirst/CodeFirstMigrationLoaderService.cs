using InfluxMigrations.Core;

namespace InfluxMigrations.CodeFirst;

public class CodeFirstMigrationLoaderService : IMigrationLoaderService
{
    private static readonly List<Tuple<string, Type>> FoundMigrations;

    static CodeFirstMigrationLoaderService()
    {
        AppDomain.CurrentDomain.GetExtensionService()?.AddSharedTypes(typeof(ICodeFirstMigration).Assembly);

        var coreTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .ToList()
            .WithAttributeAndInterface(typeof(InfluxMigrationAttribute), typeof(ICodeFirstMigration));

        var extensionTypes = AppDomain.CurrentDomain
            .GetExtensionService()?
            .GetExtensionTypes()
            .WithAttributeAndInterface(typeof(InfluxMigrationAttribute), typeof(ICodeFirstMigration));


        var join = new List<Tuple<Attribute, Type>>().Concat(coreTypes).Concat(extensionTypes).ToList();

        FoundMigrations =
            join.Select(x => new Tuple<string, Type>(((InfluxMigrationAttribute)x.Item1).Version, x.Item2)).ToList();
    }

    public async Task<List<IMigration>> LoadMigrationsAsync()
    {
        var migrations = new List<IMigration>();
        foreach (var codeFirstMigration in FoundMigrations)
        {
            var migration = new Migration(codeFirstMigration.Item1);
            var instance = (ICodeFirstMigration?)Activator.CreateInstance(codeFirstMigration.Item2);

            if (instance == null)
            {
                throw new MigrationLoadingException(
                    $"Could not create instance of {typeof(ICodeFirstMigration).FullName}:{codeFirstMigration.Item2.FullName} for version {codeFirstMigration.Item1}");
            }

            await instance.AddUp(migration);
            await instance.AddDown(migration);
            await instance.AddTask(migration);

            migrations.Add(migration);
        }

        return migrations;
    }

    public async Task ConfigureEnvironmentAsync(IEnvironmentExecutionContext env, IMigrationRunnerOptions options)
    {
        var configurer = AppDomain.CurrentDomain.GetExtensionService()?.GetExtensionTypes()
            .WithAttributeAndInterface(typeof(InfluxMigrationAttribute), typeof(IEnvironmentConfigurator)).FirstOrDefault();

        if (configurer != null)
        {
            var instance = (IEnvironmentConfigurator)Activator.CreateInstance(configurer.Item2);
            await instance?.ConfigureEnvironmentAsync(env, options);
        }
    }
}