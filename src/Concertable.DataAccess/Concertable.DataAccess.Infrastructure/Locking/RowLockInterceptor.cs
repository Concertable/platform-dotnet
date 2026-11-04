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
            throw new NotSupportedException("WithRowLock emits a PostgreSQL locking clause.");

        var root = RootAlias().Match(command.CommandText);
        if (!root.Success)
            throw new InvalidOperationException("WithRowLock could not find the root table of the query.");

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

    [GeneratedRegex(@"^(?:FROM \S+|\)) AS (?<alias>\S+)\r?$", RegexOptions.Multiline)]
    private static partial Regex RootAlias();
}
