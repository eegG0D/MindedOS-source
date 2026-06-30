using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Scans prior recorded EEG word CSVs into a multi-session problem-solving trend.
/// Reuses <see cref="PatternScan.LoadWords"/>. Degrades to a single session.
/// </summary>
public static class ProblemSolvingScan
{
    public sealed record SolvingSession(string Id, DateTime Time, double Reasoning, double Innovation, double Decision);

    public static IReadOnlyList<SolvingSession> Scan(string outputDir)
    {
        var sessions = new List<SolvingSession>();
        if (!Directory.Exists(outputDir)) return sessions;
        foreach (var f in Directory.EnumerateFiles(outputDir, "recorded_eeg_*.csv"))
        {
            try
            {
                var words = PatternScan.LoadWords(f);
                if (words.Count == 0) continue;
                var d = ProblemSolvingProfile.Dashboard(50, 50, Array.Empty<BandReading>(), words);
                // d: [0]=Problem Solving, [1]=Logic, [2]=Innovation, [3]=Decision, ...
                sessions.Add(new SolvingSession(Path.GetFileNameWithoutExtension(f), File.GetLastWriteTime(f),
                    d[1].Value /* Logic ~ Reasoning */, d[2].Value /* Innovation */, d[3].Value /* Decision */));
            }
            catch { /* skip unreadable */ }
        }
        return sessions.OrderBy(s => s.Time).ToList();
    }

    public static string TrendsCsv(IReadOnlyList<SolvingSession> sessions)
    {
        var ordered = sessions.OrderBy(s => s.Time).ToList();
        bool single = ordered.Count < 2;
        var first = ordered.First();
        var last = ordered.Last();
        string Trend(double e, double l) => single ? "insufficient data" : l > e + 2 ? "growth" : l < e - 2 ? "declining" : "stable";
        var sb = new System.Text.StringBuilder("metric,earliest,latest,trend\n");
        sb.AppendLine($"Reasoning,{first.Reasoning:0.0},{last.Reasoning:0.0},{Trend(first.Reasoning, last.Reasoning)}");
        sb.AppendLine($"Innovation,{first.Innovation:0.0},{last.Innovation:0.0},{Trend(first.Innovation, last.Innovation)}");
        sb.AppendLine($"Decision,{first.Decision:0.0},{last.Decision:0.0},{Trend(first.Decision, last.Decision)}");
        return sb.ToString();
    }
}
