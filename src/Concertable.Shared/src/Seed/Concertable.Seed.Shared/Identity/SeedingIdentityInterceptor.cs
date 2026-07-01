using System.Collections.Concurrent;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Concertable.Seed.Shared.Identity;

public sealed class SeedingIdentityInterceptor : DbCommandInterceptor
{
    private static readonly Regex insertRegex = new(
        @"INSERT\s+INTO\s+(?<table>\[?[\w]+\]?(?:\.\[?[\w]+\]?)?)\s*\((?<cols>[^)]*)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly ConcurrentDictionary<Type, Dictionary<string, string>> tableCache = new();

    private readonly SeedingScope scope;

    public SeedingIdentityInterceptor(SeedingScope scope)
    {
        this.scope = scope;
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand cmd, CommandEventData e, InterceptionResult<int> r) => Intercept(cmd, e, r);
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand cmd, CommandEventData e, InterceptionResult<int> r, CancellationToken ct = default) => ValueTask.FromResult(Intercept(cmd, e, r));
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand cmd, CommandEventData e, InterceptionResult<DbDataReader> r) => Intercept(cmd, e, r);
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand cmd, CommandEventData e, InterceptionResult<DbDataReader> r, CancellationToken ct = default) => ValueTask.FromResult(Intercept(cmd, e, r));

    private T Intercept<T>(DbCommand cmd, CommandEventData e, T r)
    {
        Rewrite(cmd, e);
        return r;
    }

    private void Rewrite(DbCommand command, CommandEventData e)
    {
        if (!scope.IsActive || e.Context is null) return;
        if (!command.CommandText.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase)) return;

        var identityTables = tableCache.GetOrAdd(e.Context.GetType(), _ => BuildTableMap(e.Context.Model));

        var tables = insertRegex.Matches(command.CommandText)
            .Where(m => identityTables.TryGetValue(Normalize(m.Groups["table"].Value), out var col)
                     && m.Groups["cols"].Value.Split(',').Any(c => c.Trim(' ', '[', ']').Equals(col, StringComparison.OrdinalIgnoreCase)))
            .Select(m => Normalize(m.Groups["table"].Value))
            .ToHashSet();

        if (tables.Count == 0) return;

        command.CommandText = On(tables) + command.CommandText + "\n" + Off(tables);
    }

    private static string On(IEnumerable<string> tables)  => string.Concat(tables.Select(t => $"SET IDENTITY_INSERT {t} ON;\n"));
    private static string Off(IEnumerable<string> tables) => string.Concat(tables.Select(t => $"SET IDENTITY_INSERT {t} OFF;\n"));

    private static string Normalize(string raw) =>
        string.Join('.', raw.Split('.').Select(p => $"[{p.Trim('[', ']')}]"));

    private static Dictionary<string, string> BuildTableMap(IModel model) =>
        model.GetEntityTypes()
            .Where(e => e.BaseType is null && !string.IsNullOrEmpty(e.GetTableName()))
            .Select(e => (
                Table: e.GetSchema() is { } s ? $"[{s}].[{e.GetTableName()}]" : $"[{e.GetTableName()}]",
                Col: e.FindPrimaryKey()?.Properties.FirstOrDefault(p =>
                    p.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn)?.GetColumnName()
            ))
            .Where(x => x.Col is not null)
            .ToDictionary(x => x.Table, x => x.Col!, StringComparer.OrdinalIgnoreCase);
}
