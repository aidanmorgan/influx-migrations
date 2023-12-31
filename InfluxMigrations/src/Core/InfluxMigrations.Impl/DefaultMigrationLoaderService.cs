using System.Linq.Expressions;
using InfluxMigrations.Core;

namespace InfluxMigrations.Impl;

public class DefaultMigrationLoaderOptions : IMigrationLoaderServiceOptions
{
    public const string GlobalConfigurationFilename = "global.yml";
    
    public bool IncludeSubdirectories { get; init; } = true;

    public List<string> FileExtensions { get; init; } = new List<string>()
    {
        ".yml",
        ".yaml"
    };

    // allows us to determine the version of a migration from the filename rather than 
    // a value that is inside the file.
    public Func<string, string> VersionFromFilenameCallback { get; init; } = Path.GetFileNameWithoutExtension;
    
    public Func<string, Task<IMigration>> ParseMigration { get; init; }
    
    public IMigrationLoaderLogger Logger { get; init; } = new NoOpMigrationLoaderLogger();

    public IComparer<string> VersionComparator { get; init; } = new DefaultVersionComparer();
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
        var migrations = new List<IMigration>();
        var option = Options.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var logger = Options.Logger;

        try
        {
            foreach (var file in Directory.GetFiles(_baseDirectory, "*.*", option))
            {
                if (Options.FileExtensions.Any(x => file.EndsWith(x, StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (file.ToLowerInvariant().EndsWith(DefaultMigrationLoaderOptions.GlobalConfigurationFilename))
                    {
                        continue;
                    }
                    
                    try
                    {
                        var migration = await Options.ParseMigration(file);

                        // if the version is not in the migration file, then we try to derive the version from the filename itself
                        if (string.IsNullOrEmpty(migration.Version))
                        {
                            migration.Version = Options.VersionFromFilenameCallback(file);
                        }

                        if (string.IsNullOrEmpty(migration.Version))
                        {
                            throw new MigrationLoadingException($"Found migration file {file}, but cannot determine version.");
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

        var result = migrations.OfType<IMigration>().ToList();
        return result;
    }
}

public class DelegateIEqualityComparer : IEqualityComparer<string>
{
    private readonly IComparer<string> _comparer;

    public DelegateIEqualityComparer(IComparer<string> comparer)
    {
        _comparer = comparer;
    }

    public bool Equals(string? x, string? y)
    {
        return _comparer.Compare(x, y) == 0;
    }

    public int GetHashCode(string obj)
    {
        return obj.GetHashCode();
    }
}