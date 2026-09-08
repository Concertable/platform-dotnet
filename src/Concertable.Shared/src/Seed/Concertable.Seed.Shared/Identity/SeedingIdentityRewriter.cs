using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Concertable.Seed.Shared.Identity;

internal static class SeedingIdentityRewriter
{
    private static readonly Regex insertRegex = new(
        @"INSERT\s+INTO\s+(?<table>\[?[\w]+\]?(?:\.\[?[\w]+\]?)?)\s*\((?<cols>[^)]*)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex mergeRegex = new(
        @"MERGE\s+(?:INTO\s+)?(?<table>\[?[\w]+\]?(?:\.\[?[\w]+\]?)?)[^;]*?INSERT\s*\((?<cols>[^)]*)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    internal static string? Rewrite(string commandText, IReadOnlyDictionary<string, string> identityTables)
    {
        if (!commandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase))
            return null;

        var tables = insertRegex.Matches(commandText)
            .Concat(mergeRegex.Matches(commandText))
            .Where(m => identityTables.TryGetValue(Normalize(m.Groups["table"].Value), out var col)
                     && m.Groups["cols"].Value.Split(',')
                         .Any(c => c.Trim(' ', '[', ']').Equals(col, StringComparison.OrdinalIgnoreCase)))
            .Select(m => Normalize(m.Groups["table"].Value))
            .ToHashSet();

        if (tables.Count == 0)
            return null;

        if (tables.Count > 1)
            throw new InvalidOperationException(
                $"Seeding staged explicit identity values for {string.Join(" and ", tables.Order())} in a single command. "
                + "SQL Server permits IDENTITY_INSERT on one table at a time, so these entities must be saved in "
                + "separate windows and no navigation may drag one into the other's save.");

        return On(tables) + commandText + "\n" + Off(tables);
    }

    internal static Dictionary<string, string> BuildTableMap(IModel model) =>
        model.GetEntityTypes()
            .Where(e => e.BaseType is null && !string.IsNullOrEmpty(e.GetTableName()))
            .Select(e => (
                Table: e.GetSchema() is { } s ? $"[{s}].[{e.GetTableName()}]" : $"[{e.GetTableName()}]",
                Col: e.FindPrimaryKey()?.Properties.FirstOrDefault(p =>
                    p.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn)?.GetColumnName()
            ))
            .Where(x => x.Col is not null)
            .ToDictionary(x => x.Table, x => x.Col!, StringComparer.OrdinalIgnoreCase);

    private static string On(IEnumerable<string> tables) =>
        string.Concat(tables.Select(t => $"SET IDENTITY_INSERT {t} ON;\n"));

    private static string Off(IEnumerable<string> tables) =>
        string.Concat(tables.Select(t => $"SET IDENTITY_INSERT {t} OFF;\n"));

    private static string Normalize(string raw) =>
        string.Join('.', raw.Split('.').Select(p => $"[{p.Trim('[', ']')}]"));
}
