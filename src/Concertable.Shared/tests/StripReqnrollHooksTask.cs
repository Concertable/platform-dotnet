using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class StripReqnrollHooks : Task
{
    [Required]
    public ITaskItem[] Files { get; set; } = [];

    public override bool Execute()
    {
        foreach (var f in Files)
        {
            var path = f.GetMetadata("FullPath");
            if (!File.Exists(path)) continue;

            var lines = File.ReadAllLines(path);
            var filtered = lines.Where(line =>
            {
                var t = line.TrimStart();
                return !t.StartsWith("[assembly: global::Xunit.TestFramework(")
                    && !t.StartsWith("[assembly: global::Reqnroll.xUnit.ReqnrollPlugin.AssemblyFixture(");
            }).ToArray();

            if (filtered.Length != lines.Length)
                File.WriteAllLines(path, filtered);
        }
        return true;
    }
}
