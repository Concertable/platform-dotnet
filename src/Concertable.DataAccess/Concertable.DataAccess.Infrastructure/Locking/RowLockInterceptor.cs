using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Concertable.DataAccess.Infrastructure.Locking;

internal sealed partial class RowLockInterceptor : DbCommandInterceptor
{
    public static RowLockInterceptor Instance { get; } = new();

    private RowLockInterceptor() { }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Lock(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Lock(command);
        return ValueTask.FromResult(result);
    }

    private static void Lock(DbCommand command)
    {
        var tag = RowLockTag().Match(command.CommandText);
        if (!tag.Success)
            return;
        if (command is not NpgsqlCommand)
            throw new NotSupportedException("A row lock is a PostgreSQL locking clause.");

        var root = RootTable().Match(command.CommandText);
        if (!root.Success || Join().IsMatch(command.CommandText))
            throw new InvalidOperationException("A row lock covers one table; lock with LockRowsAsync, then load.");

        command.CommandText += $"\n{Clause(Enum.Parse<RowLock>(tag.Groups["mode"].Value))} OF {root.Groups["alias"].Value}";
    }

    private static string Clause(RowLock mode) => mode switch
    {
        RowLock.KeyShare => "FOR KEY SHARE",
        RowLock.Share => "FOR SHARE",
        RowLock.NoKeyUpdate => "FOR NO KEY UPDATE",
        RowLock.Update => "FOR UPDATE",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    [GeneratedRegex($@"^-- {RowLockExtensions.TagPrefix}(?<mode>\w+)\r?$", RegexOptions.Multiline)]
    private static partial Regex RowLockTag();

    [GeneratedRegex(@"^FROM \S+ AS (?<alias>\S+)\r?$", RegexOptions.Multiline)]
    private static partial Regex RootTable();

    [GeneratedRegex(@"^\s*(?:LEFT |INNER |CROSS |RIGHT |FULL )?(?:OUTER )?JOIN ", RegexOptions.Multiline)]
    private static partial Regex Join();
}
