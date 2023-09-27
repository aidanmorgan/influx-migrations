using System.Transactions;
using InfluxMigrations.Core;
using Pastel;

namespace InfluxMigrations.Impl;

public class TextWriterMigrationRunnerLogger : IMigrationRunnerLogger
{
    private readonly TextWriter _writer;

    public TextWriterMigrationRunnerLogger(TextWriter textWriter)
    {
        this._writer = textWriter;
    }

    public void ExecutionPlan(List<IMigration> toExecute, MigrationDirection down)
    {
        _writer.WriteLine($"Planned Migraions:");
        foreach (var migration in toExecute)
        {
            _writer.WriteLine($"{down} : {migration.Version}");
        }
    }

    public void FoundHistory(List<MigrationHistory> history)
    {
        var successful = history.Where(x => x.Success.HasValue && x.Success.Value).ToList();
        var failed = history.Where(x => !x.Success.HasValue || !x.Success.Value).ToList();

        _writer.WriteLine(
            $"Found {successful.Count} successful Migrations :: [{string.Join(",", successful.Select(x => x.Version))}]");
    }

    public void FoundMigrations(List<IMigration> loaded)
    {
        _writer.WriteLine(
            $"Found {loaded.Count} potential Migrations :: [{string.Join(",", loaded.Select(x => x.Version))}]");
    }

    public void NoMigrations()
    {
        _writer.WriteLine($"No migrations found to execute.".Pastel(ConsoleColor.Red));
    }

    public void MigrationSaved(MigrationHistory entry)
    {
        _writer.WriteLine($"Migration {entry.Version} saved.".Pastel(ConsoleColor.Green));
    }

    public void MigrationSaveFailed(MigrationHistory entry)
    {
        _writer.WriteLine($"Migration {entry.Version} failed.".Pastel(ConsoleColor.Red));
    }

    public ITaskLogger StartTask(IEnvironmentTask task)
    {
        return new TextWriterTaskLogger(_writer, null, task);
    }
}