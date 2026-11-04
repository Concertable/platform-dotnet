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
        if (!RowLockTag().IsMatch(command.CommandText))
            return;
        if (command is not NpgsqlCommand)
            throw new NotSupportedException("WithRowLock emits a PostgreSQL locking clause.");

        var root = RootAlias().Match(command.CommandText);
        if (!root.Success)
            throw new InvalidOperationException("WithRowLock could not find the root table of the query.");

        command.CommandText += $"\nFOR NO KEY UPDATE OF {root.Groups["alias"].Value}";
    }

    [GeneratedRegex($@"^-- {RowLockExtensions.Tag}\r?$", RegexOptions.Multiline)]
    private static partial Regex RowLockTag();

    [GeneratedRegex(@"^(?:FROM \S+|\)) AS (?<alias>\S+)\r?$", RegexOptions.Multiline)]
    private static partial Regex RootAlias();
}
