using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Concertable.Seed.Shared.Identity;

internal static class SeedingIdentityRewriter
{
    internal const string PostgreSqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    internal const string SqlServerProviderName = "Microsoft.EntityFrameworkCore.SqlServer";

    private static readonly Regex insertRegex = new(
        @"INSERT\s+INTO\s+(?<table>\[?[\w]+\]?(?:\.\[?[\w]+\]?)?)\s*\((?<cols>[^)]*)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex mergeRegex = new(
        @"MERGE\s+(?:INTO\s+)?(?<table>\[?[\w]+\]?(?:\.\[?[\w]+\]?)?)[^;]*?INSERT\s*\((?<cols>[^)]*)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    internal static bool RequiresIdentityInsert(string? providerName) =>
        providerName switch
        {
            SqlServerProviderName => true,
            PostgreSqlProviderName => false,
            _ => throw new NotSupportedException($"The database provider '{providerName}' is not supported for seeding.")
        };

    internal static string? Rewrite(string commandText, IReadOnlyDictionary<string, string> identityTables)
    {
        var tables = insertRegex.Matches(commandText)
            .Concat(mergeRegex.Matches(commandText))
            .Where(m => identityTables.TryGetValue(Normalize(m.Groups["table"].Value), out var column)
                     && m.Groups["cols"].Value.Split(',')
                         .Any(c => c.Trim(' ', '[', ']').Equals(column, StringComparison.OrdinalIgnoreCase)))
            .Select(m => Normalize(m.Groups["table"].Value))
            .ToHashSet();

        if (tables.Count == 0)
            return null;

        if (tables.Count > 1)
        {
            throw new InvalidOperationException(
                $"Seeding staged explicit identity values for {string.Join(" and ", tables.Order())} in a single command. "
                + "SQL Server permits IDENTITY_INSERT on one table at a time, so these entities must be saved in "
                + "separate windows and no navigation may drag one into the other's save.");
        }

        var table = tables.Single();
        return $"SET IDENTITY_INSERT {table} ON;\n{commandText}\nSET IDENTITY_INSERT {table} OFF;\n";
    }

    internal static Dictionary<string, string> BuildTableMap(IModel model) =>
        model.GetEntityTypes()
            .Where(e => e.BaseType is null && !string.IsNullOrEmpty(e.GetTableName()))
            .Select(e => (
                Table: e.GetSchema() is { } schema ? $"[{schema}].[{e.GetTableName()}]" : $"[{e.GetTableName()}]",
                Column: e.FindPrimaryKey()?.Properties.FirstOrDefault(p =>
                    p.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn)?.GetColumnName()
            ))
            .Where(x => x.Column is not null)
            .ToDictionary(x => x.Table, x => x.Column!, StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string raw) =>
        string.Join('.', raw.Split('.').Select(part => $"[{part.Trim('[', ']')}]"));
}
