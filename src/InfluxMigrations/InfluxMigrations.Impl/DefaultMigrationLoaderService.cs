using System.Linq.Expressions;
using InfluxMigrations.Core;
using InfluxMigrations.Yaml;

namespace InfluxMigrations.Impl;

public class DefaultMigrationLoaderOptions : IMigrationLoaderServiceOptions
{
    public bool IncludeSubdirectories { get; init; } = true;

    public List<string> FileExtensions { get; init; } = new List<string>()
    {
        ".yml",
        ".yaml"
    };

    // allows us to determine the version of a migration from the filename rather than 
    // a value that is inside the file.
    public Func<string, string> VersionFromFilenameCallback { get; init; } = null;

    private static YamlMigrationParser _parser = new YamlMigrationParser();

    public Func<string, Task<Migration>> Parse { get; init; } = (x) => _parser.ParseFile(x);

    public IMigrationLoaderLogger Logger { get; init; } = new NoOpMigrationLoaderLogger();
}

public class DefaultMigrationLoaderService : IMigrationLoaderService
{
    private readonly string _baseDirectory;
    public DefaultMigrationLoaderOptions Options { get; init; }

    public DefaultMigrationLoaderService(string basePath, DefaultMigrationLoaderOptions? options = null)
    {
        if (string.IsNullOrEmpty(basePath))
        {
            throw new MigrationLoadingException($"Cannot create a DefaultMigrationLoader with no base path.");
        }

        this._baseDirectory = basePath;
        this.Options = options ?? new DefaultMigrationLoaderOptions();
    }

    public async Task<List<IMigration>> LoadMigrationsAsync()
    {
        var migrations = new List<Migration>();
        var option = Options.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var logger = Options.Logger;

        try
        {
            foreach (var file in Directory.GetFiles(_baseDirectory, "*.*", option))
            {
                if (Options.FileExtensions.Any(x => file.EndsWith(x, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        var migration = await Options.Parse(file);

                        if (Options.VersionFromFilenameCallback != null)
                        {
                            migration.Version = Options.VersionFromFilenameCallback(file);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(migration.Version))
                            {
                                throw new MigrationLoadingException($"Found migration file {file}, but cannot determine version.");
                            }
                        }

                        logger.FoundMigration(file, migration);
                        migrations.Add(migration);
                    }
                    catch (MigrationParsingException x)
                    {
                        logger.ParsingFailed(file, x);
                    }
                }
            }
        }
        catch (Exception x)
        {
            logger.Exception(x);
            throw new MigrationLoadingException($"Exception thrown parsing migrations.", x);
        }

        return migrations.OfType<IMigration>().ToList();
    }
}
