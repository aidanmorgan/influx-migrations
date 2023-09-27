using System.Drawing;
using InfluxMigrations.Core;
using Pastel;

namespace InfluxMigrations.Impl;

public class TextWriterMigrationLoggerFactory : IMigrationLoggerFactory
{
    private readonly TextWriter _stream;

    public TextWriterMigrationLoggerFactory(TextWriter stream)
    {
        _stream = stream;
    }

    public IMigrationLogger MigrationStart(string version, MigrationDirection dir)
    {
        _stream.WriteLine($"Starting Migration {version} - {dir}".Pastel(ConsoleColor.Magenta));
        return new TextWriterMigrationLogger(_stream, version, dir);
    }

    public IMigrationRunnerLogger MigrationRunnerStart()
    {
        return new TextWriterRunnerLogger(_stream);
    }
}

public class TextWriterRunnerLogger : IMigrationRunnerLogger
{
    private readonly TextWriter _writer;

    public TextWriterRunnerLogger(TextWriter writer)
    {
        _writer = writer;
    }

    public void ExecutionPlan(List<IMigration> toExecute, MigrationDirection down)
    {
        _writer.WriteLine($"Executing migrations:");

        foreach (var migration in toExecute)
        {
            _writer.WriteLine($"{migration.Version} - {down}");
        }
    }

    public void FoundHistory(List<MigrationHistory> history)
    {
        _writer.WriteLine($"Loaded {history.Count} previous migrations.");
    }

    public void FoundMigrations(List<IMigration> loaded)
    {
        _writer.WriteLine($"Loaded {loaded.Count} potential migrations.");
    }

    public void NoMigrations()
    {
        _writer.WriteLine($"No suitable migrations found.");
    }

    public void MigrationSaved(MigrationHistory entry)
    {
    }

    public void MigrationSaveFailed(MigrationHistory entry)
    {
        _writer.WriteLine($"Migration {entry.Version} was not saved.".Pastel(ConsoleColor.Red));
    }

    public ITaskLogger StartTask(IEnvironmentTask task)
    {
        return new TextWriterTaskLogger(_writer, null, task);
    }
}

public class TextWriterMigrationLogger : IMigrationLogger
{
    private readonly TextWriter _stream;
    private readonly string _version;
    private readonly MigrationDirection _direction;

    public TextWriterMigrationLogger(TextWriter stream, string version, MigrationDirection dir)
    {
        _stream = stream;
        _version = version;
        _direction = dir;
    }

    public IMigrationOperationLogger<OperationExecutionState, IExecuteResult> ExecuteStart(
        MigrationOperationInstance op)
    {
        return new TextWriterMigrationOperationLogger<OperationExecutionState, IExecuteResult>(_stream, op, "execute");
    }

    public IMigrationOperationLogger<OperationCommitState, ICommitResult> CommitStart(MigrationOperationInstance op)
    {
        return new TextWriterMigrationOperationLogger<OperationCommitState, ICommitResult>(_stream, op,
            "commit");
    }

    public IMigrationOperationLogger<OperationRollbackState, IRollbackResult> RollbackStart(
        MigrationOperationInstance op)
    {
        return new TextWriterMigrationOperationLogger<OperationRollbackState, IRollbackResult>(_stream, op, "rollback");
    }

    public ITaskLogger TaskStart(IMigrationTask task)
    {
        return new TextWriterTaskLogger(_stream, null, task);
    }
    
    

    public void Complete()
    {
        _stream.Write($"Finished Migration".Pastel(ConsoleColor.Magenta));
        _stream.WriteLine($"   COMPLETE".Pastel(ConsoleColor.Green));
    }

    public void Failed(Exception ex)
    {
        _stream.Write($"Finished Migration".Pastel(ConsoleColor.Magenta));

        _stream.WriteLine($"   ERROR".Pastel(ConsoleColor.Red));
        _stream.WriteLine($"{ex.Message}".Pastel(ConsoleColor.Red));
        _stream.WriteLine($"{ex.StackTrace}".Pastel(ConsoleColor.Red));
    }
}

public class TextWriterMigrationOperationLogger<TStateEnum, TResult> : IMigrationOperationLogger<TStateEnum, TResult>
    where TStateEnum : Enum
{
    private readonly TextWriter _textWriter;
    private readonly MigrationOperationInstance _operationInstance;
    private readonly string _prefix;

    public TextWriterMigrationOperationLogger(TextWriter textWriter, MigrationOperationInstance operationInstance,
        string prefix)
    {
        _textWriter = textWriter;
        _operationInstance = operationInstance;
        _prefix = prefix;
    }

    public void Complete(OperationResult<TStateEnum, TResult?> result)
    {
        _textWriter.WriteLine(
            $"{_operationInstance.Id.Pastel(ConsoleColor.White)}#{$"{_operationInstance.Operation.GetType().FullName}".Pastel(Color.Beige)}  {$"{_prefix}".Pastel(ConsoleColor.Cyan)} {"[COMPLETE]".Pastel(ConsoleColor.Green)}");
    }

    public ITaskLogger TaskStart(IMigrationTask task)
    {
        return new TextWriterTaskLogger(_textWriter, _operationInstance, task);
    }

    public ITaskLogger TaskStart(IOperationTask task)
    {
        return new TextWriterTaskLogger(_textWriter, _operationInstance, task);
    }

    public void Failed(OperationResult<TStateEnum, TResult?> result)
    {
        _textWriter.WriteLine(
            $"{_operationInstance.Id.Pastel(ConsoleColor.White)}#{$"{_operationInstance.Operation.GetType().FullName}".Pastel(Color.Beige)} {$"{_prefix}".Pastel(ConsoleColor.Cyan)} {"[FAILED]".Pastel(ConsoleColor.Red)}");
    }

    public void Failed(Exception result)
    {
        _textWriter.WriteLine(
            $"{_operationInstance.Id.Pastel(ConsoleColor.White)}#({$"{_operationInstance.Operation.GetType().FullName}]".Pastel(Color.Beige)}  {$"{_prefix}".Pastel(ConsoleColor.Cyan)} {"[EXCEPTION]".Pastel(ConsoleColor.Red)}");
        _textWriter.WriteLine($"{result.Message}".Pastel(ConsoleColor.Red));
        _textWriter.WriteLine($"{result.StackTrace}".Pastel(ConsoleColor.Red));
    }
}

public class TextWriterTaskLogger : ITaskLogger
{
    private readonly TextWriter _textWriter;
    private readonly MigrationOperationInstance? _operationInstance;
    private readonly ITask _task;
    private readonly string _prefix;

    public TextWriterTaskLogger(TextWriter textWriter, MigrationOperationInstance? operationInstance,
        ITask task)
    {
        _textWriter = textWriter;
        _operationInstance = operationInstance;
        _task = task;

        _prefix = task switch
        {
            IOperationTask => "operation",
            IMigrationTask => "migration",
            IEnvironmentTask => "environment",
            _ => _prefix
        } ?? string.Empty;
    }

    public void Complete()
    {
        if (_operationInstance == null)
        {
            _textWriter.WriteLine(
                $"Output#{_task.GetType().FullName.Pastel(Color.Beige)} {$"{_prefix}]".Pastel(ConsoleColor.Cyan)} {"COMPLETE".Pastel(ConsoleColor.Green)}");
        }
        else
        {
            _textWriter.WriteLine(
                $" Output#{_task.GetType().FullName.Pastel(Color.Beige)} {_operationInstance.Id.Pastel(ConsoleColor.White)} {$"{_prefix}".Pastel(ConsoleColor.Cyan)} {"COMPLETE".Pastel(ConsoleColor.Green)}");
        }
    }

    public void Failed(Exception x)
    {
        if (_operationInstance == null)
        {
            _textWriter.WriteLine(
                $"Output#{_task.GetType().FullName.Pastel(Color.Beige)} Migration {$"{_prefix}".Pastel(ConsoleColor.Cyan)} {"FAILED".Pastel(ConsoleColor.Red)}");
        }
        else
        {
            _textWriter.WriteLine(
                $"Output#{_task.GetType().FullName.Pastel(Color.Beige)} {_operationInstance.Id.Pastel(ConsoleColor.White)} {$"{_prefix}".Pastel(ConsoleColor.Cyan)} {"FAILED".Pastel(ConsoleColor.Red)}");
        }

        _textWriter.WriteLine($"{x.Message}".Pastel(ConsoleColor.Red));
        _textWriter.WriteLine($"{x.StackTrace}".Pastel(ConsoleColor.Red));
    }
}