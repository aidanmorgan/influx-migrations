using InfluxMigrations.Core;
using Pastel;

namespace InfluxMigrations.Impl;

public class TextWriterMigrationHistoryLogger : IMigrationHistoryLogger
{
    private readonly TextWriter _stream;

    public TextWriterMigrationHistoryLogger(TextWriter writer)
    {
        _stream = writer;
    }

    public void LoadException(Exception? x)
    {
        _stream.WriteLine(
            $"Exception thrown {x?.GetType().FullName} loading Migration History.".Pastel(ConsoleColor.Red));
        _stream.WriteLine($"{x.Message}.".Pastel(ConsoleColor.Red));
        _stream.WriteLine($"{x.StackTrace}.".Pastel(ConsoleColor.Red));
    }

    public void SaveException(Exception? x)
    {
        _stream.WriteLine(
            $"Exception thrown {x?.GetType().FullName} saving Migration History.".Pastel(ConsoleColor.Red));
        _stream.WriteLine($"{x.Message}.".Pastel(ConsoleColor.Red));
        _stream.WriteLine($"{x.StackTrace}.".Pastel(ConsoleColor.Red));
    }
}