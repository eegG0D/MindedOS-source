using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Scans prior recorded EEG word CSVs (trends) and a csv_files/ folder (multi-user
/// network). Reuses <see cref="PatternScan.LoadWords"/>. Degrades gracefully.
/// </summary>
public static class ReasoningScan
{
    public sealed record ReasoningSession(string Id, DateTime Time, double Logic, double Innovation, double Decision);

    public static IReadOnlyList<ReasoningSession> Scan(string outputDir)
    {
        var sessions = new List<ReasoningSession>();
        if (!Directory.Exists(outputDir)) return sessions;
        foreach (var f in Directory.EnumerateFiles(outputDir, "recorded_eeg_*.csv"))
        {
            try
            {
                var words = PatternScan.LoadWords(f);
                if (words.Count == 0) continue;
                var d = ReasoningProfile.Dashboard(50, 50, Array.Empty<BandReading>(), words);
                // d: [0]=Logic, [1]=Critical Thinking, [2]=Decision Quality, [3]=Innovation, ...
                sessions.Add(new ReasoningSession(Path.GetFileNameWithoutExtension(f), File.GetLastWriteTime(f),
                    d[0].Value /* Logic */, d[3].Value /* Innovation */, d[2].Value /* Decision Quality */));
            }
            catch { /* skip unreadable */ }
        }
        return sessions.OrderBy(s => s.Time).ToList();
    }

    public static string TrendsCsv(IReadOnlyList<ReasoningSession> sessions)
    {
        var ordered = sessions.OrderBy(s => s.Time).ToList();
        bool single = ordered.Count < 2;
        var first = ordered.First();
        var last = ordered.Last();
        string Trend(double e, double l) => single ? "insufficient data" : l > e + 2 ? "growth" : l < e - 2 ? "declining" : "stable";
        var sb = new System.Text.StringBuilder("metric,earliest,latest,trend\n");
        sb.AppendLine($"Logic,{first.Logic:0.0},{last.Logic:0.0},{Trend(first.Logic, last.Logic)}");
        sb.AppendLine($"Innovation,{first.Innovation:0.0},{last.Innovation:0.0},{Trend(first.Innovation, last.Innovation)}");
        sb.AppendLine($"Decision,{first.Decision:0.0},{last.Decision:0.0},{Trend(first.Decision, last.Decision)}");
        return sb.ToString();
    }

    public static string NetworkRankingsCsv(string outputDir)
    {
        var rows = new List<(string Id, double Logic, double Strategy, double Innovation, double Scientific)>();
        var csvFiles = Path.Combine(outputDir, "csv_files");
        if (Directory.Exists(csvFiles))
        {
            foreach (var f in Directory.EnumerateFiles(csvFiles, "*.csv"))
            {
                try
                {
                    var words = PatternScan.LoadWords(f);
                    if (words.Count == 0) continue;
                    var d = ReasoningProfile.Dashboard(50, 50, Array.Empty<BandReading>(), words);
                    var sci = ReasoningProfile.ScientificReasoning(50, Array.Empty<BandReading>(), words);
                    rows.Add((Path.GetFileNameWithoutExtension(f),
                        d[0].Value /* Logic */, d[2].Value /* Decision~Strategy */, d[3].Value /* Innovation */,
                        sci.Count > 4 ? sci[4].Value : d[4].Value /* Research Potential */));
                }
                catch { /* skip */ }
            }
        }

        var sb = new System.Text.StringBuilder("ranking,top_session,score\n");
        void Rank(string label, Func<(string Id, double Logic, double Strategy, double Innovation, double Scientific), double> sel)
        {
            if (rows.Count == 0) { sb.AppendLine($"{label},(no profiles),0.0"); return; }
            var best = rows.OrderByDescending(sel).First();
            sb.AppendLine($"{label},{best.Id},{sel(best):0.0}");
        }
        Rank("Best Analyst", r => r.Logic);
        Rank("Best Strategist", r => r.Strategy);
        Rank("Best Problem Solver", r => r.Innovation);
        Rank("Best Researcher", r => r.Scientific);
        Rank("Best Innovator", r => r.Innovation);
        return sb.ToString();
    }
}
