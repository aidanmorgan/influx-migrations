using InfluxMigrations.Core;
using Pastel;

namespace InfluxMigrations.Impl;

public class TextWriterMigrationLoaderLogger : IMigrationLoaderLogger
{
    private readonly TextWriter _stream;

    public TextWriterMigrationLoaderLogger(TextWriter writer)
    {
        _stream = writer;
    }

    public void Exception(Exception exception)
    {
        _stream.WriteLine($"Exception thrown loading Migrations.".Pastel(ConsoleColor.Red));
        _stream.WriteLine($"{exception.Message}".Pastel(ConsoleColor.Red));
        _stream.WriteLine($"{exception.StackTrace}".Pastel(ConsoleColor.Red));
    }

    public void FoundMigration(string file, IMigration x)
    {
        _stream.WriteLine($"Found migration: {x.Version} in {file}");
    }

    public void ParsingFailed(string file, MigrationParsingException x)
    {
        _stream.WriteLine($"Could not parse Migration from {file}".Pastel(ConsoleColor.Red));
        _stream.WriteLine($"{x.Message}".Pastel(ConsoleColor.Red));
        _stream.WriteLine($"{x.StackTrace}".Pastel(ConsoleColor.Red));
    }
}