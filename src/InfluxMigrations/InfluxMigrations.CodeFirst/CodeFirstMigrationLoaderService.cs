using InfluxMigrations.Core;

namespace InfluxMigrations.CodeFirst;

public class CodeFirstMigrationLoaderService : IMigrationLoaderService
{
    private static readonly List<Tuple<string, Type>> FoundMigrations;

    static CodeFirstMigrationLoaderService()
    {
        FoundMigrations = AppDomain.CurrentDomain
            .GetAssemblies()
            .AsParallel()
            .SelectMany(x => x.GetTypes())
            .Select(x =>
            {
                var attributed = x.GetCustomAttributes(typeof(InfluxMigrationAttribute), true);
                if (attributed.Length > 0 && x.GetInterfaces().Contains(typeof(ICodeFirstMigration)))
                {
                    var attr = (InfluxMigrationAttribute)attributed.First();
                    return new Tuple<string,Type>(attr.Version, x);
                }

                return null;
            })
            .Where(x => x != null)
            .ToList()!;
        
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
                throw new MigrationLoadingException($"Could not create instance of ICodeFirstMigration {codeFirstMigration.Item2} for version {codeFirstMigration.Item1}");
            }
            
            await instance.AddUp(migration);
            await instance.AddDown(migration);
            await instance.AddTask(migration);

            migrations.Add(migration);
        }

        return migrations;
    }
}