using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class StripFeatureTraits : Task
{
    [Required]
    public ITaskItem[] Files { get; set; } = [];

    public string Category { get; set; } = null!;

    public override bool Execute()
    {
        var stripPattern = new Regex(
            @"^\s*\[global::Xunit\.TraitAttribute\(""(FeatureTitle|Description|Category)"",\s*""[^""]*""\)\]\s*$");

        var injectMarker = !string.IsNullOrEmpty(Category)
            ? $"[global::Xunit.TraitAttribute(\"Category\", \"{Category}\")]"
            : null;

        foreach (var f in Files)
        {
            var path = f.GetMetadata("FullPath");
            if (!File.Exists(path)) continue;

            var original = File.ReadAllText(path);
            var sb = new StringBuilder();
            bool changed = false;
            string? lastAddedLine = null;

            foreach (var line in original.Split('\n'))
            {
                if (stripPattern.IsMatch(line))
                {
                    changed = true;
                    continue;
                }

                if (injectMarker != null && line.TrimStart().StartsWith("[global::Xunit.SkippableFactAttribute("))
                {
                    if (lastAddedLine == null || !lastAddedLine.Contains(injectMarker))
                    {
                        var indent = line.Length - line.TrimStart().Length;
                        var injectedLine = new string(' ', indent) + injectMarker;
                        sb.Append(injectedLine);
                        sb.Append('\n');
                        lastAddedLine = injectedLine;
                        changed = true;
                    }
                }

                if (line.TrimStart().StartsWith("public partial class ") &&
                    (lastAddedLine == null || !lastAddedLine.Contains("CollectionAttribute")))
                {
                    var indent = line.Length - line.TrimStart().Length;
                    var collectionLine = new string(' ', indent) + "[global::Xunit.CollectionAttribute(\"ReqnrollCollection\")]";
                    sb.Append(collectionLine);
                    sb.Append('\n');
                    lastAddedLine = collectionLine;
                    changed = true;
                }

                sb.Append(line);
                sb.Append('\n');
                lastAddedLine = line;
            }

            if (changed)
            {
                var result = sb.ToString();
                if (result.EndsWith("\n") && !original.EndsWith("\n"))
                    result = result.Substring(0, result.Length - 1);
                if (result != original)
                    File.WriteAllText(path, result);
            }
        }

        return true;
    }
}
