using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Multi-robot coordination: scans a robot_network/ folder of prior robot recordings, ranks them
/// by an overall robot dashboard score and assigns coordination roles. Reuses
/// <see cref="PatternScan.LoadWords"/>. Degrades gracefully.
/// </summary>
public static class RobotScan
{
    private static readonly string[] Roles =
        { "Leader Robot", "Worker Robot", "Scout Robot", "Research Robot", "Maintenance Robot" };

    public static string NetworkAnalysisCsv(string outputDir)
    {
        var rows = new List<(string Id, double Score)>();
        var netDir = Path.Combine(outputDir, "robot_network");
        if (Directory.Exists(netDir))
        {
            foreach (var f in Directory.EnumerateFiles(netDir, "*.csv"))
            {
                try
                {
                    var words = PatternScan.LoadWords(f);
                    if (words.Count == 0) continue;
                    var d = RobotProfile.Dashboard(50, 50, Array.Empty<BandReading>(), words);
                    double overall = d.Average(x => x.Value);
                    rows.Add((Path.GetFileNameWithoutExtension(f), overall));
                }
                catch { /* skip unreadable */ }
            }
        }

        var sb = new System.Text.StringBuilder("robot,role,score\n");
        if (rows.Count == 0) { sb.AppendLine("(no robots),—,0.0"); return sb.ToString(); }
        var ranked = rows.OrderByDescending(r => r.Score).ToList();
        for (int i = 0; i < ranked.Count; i++)
        {
            string role = i < Roles.Length ? Roles[i] : "Worker Robot";
            sb.AppendLine($"{ranked[i].Id},{role},{ranked[i].Score:0.0}");
        }
        return sb.ToString();
    }
}
