using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class FilterNdjsonTags : Task
{
    [Required]
    public ITaskItem[] Files { get; set; } = [];

    public string? Category { get; set; }

    public override bool Execute()
    {
        var tagPattern = new Regex(@"""tags"":\[[^\]]*\]");
        var injectTag = string.IsNullOrEmpty(Category)
            ? null
            : @"""tags"":[{""name"":""@" + Category + @""",""id"":""00000000-0000-0000-0000-000000000001""}]";

        foreach (var f in Files)
        {
            var path = f.GetMetadata("FullPath");
            if (!File.Exists(path)) continue;

            var original = File.ReadAllText(path);
            var sb = new StringBuilder();
            bool changed = false;

            foreach (var line in original.Split('\n'))
            {
                bool isPickle = line.TrimStart().StartsWith("{\"pickle\"");
                string replacement = (isPickle && injectTag != null) ? injectTag : @"""tags"":[]";
                var newLine = tagPattern.Replace(line, replacement);
                if (newLine != line) changed = true;
                sb.Append(newLine);
                sb.Append('\n');
            }

            if (changed)
            {
                var result = sb.ToString();
                if (result.EndsWith("\n") && !original.EndsWith("\n"))
                    result = result.Substring(0, result.Length - 1);
                File.WriteAllText(path, result);
            }
        }

        return true;
    }
}
