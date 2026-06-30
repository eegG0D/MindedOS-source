using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Scans prior recorded EEG word CSVs into a multi-session processor trend.
/// Reuses <see cref="PatternScan.LoadWords"/>. Degrades to a single session.
/// </summary>
public static class ProcessorScan
{
    public sealed record ProcessorSession(string Id, DateTime Time, double Speed, double Throughput, double Logic);

    public static IReadOnlyList<ProcessorSession> Scan(string outputDir)
    {
        var sessions = new List<ProcessorSession>();
        if (!Directory.Exists(outputDir)) return sessions;
        foreach (var f in Directory.EnumerateFiles(outputDir, "recorded_eeg_*.csv"))
        {
            try
            {
                var words = PatternScan.LoadWords(f);
                if (words.Count == 0) continue;
                var d = ProcessorProfile.Dashboard(50, 50, Array.Empty<BandReading>(), words);
                // d: [0]=Processing Speed, [1]=Efficiency, [2]=Throughput, [3]=Parallel, [4]=Logic, ...
                sessions.Add(new ProcessorSession(Path.GetFileNameWithoutExtension(f), File.GetLastWriteTime(f),
                    d[0].Value /* Speed */, d[2].Value /* Throughput */, d[4].Value /* Logic */));
            }
            catch { /* skip unreadable */ }
        }
        return sessions.OrderBy(s => s.Time).ToList();
    }

    public static string TrendsCsv(IReadOnlyList<ProcessorSession> sessions)
    {
        var ordered = sessions.OrderBy(s => s.Time).ToList();
        bool single = ordered.Count < 2;
        var first = ordered.First();
        var last = ordered.Last();
        string Trend(double e, double l) => single ? "insufficient data" : l > e + 2 ? "growth" : l < e - 2 ? "declining" : "stable";
        var sb = new System.Text.StringBuilder("metric,earliest,latest,trend\n");
        sb.AppendLine($"Speed,{first.Speed:0.0},{last.Speed:0.0},{Trend(first.Speed, last.Speed)}");
        sb.AppendLine($"Throughput,{first.Throughput:0.0},{last.Throughput:0.0},{Trend(first.Throughput, last.Throughput)}");
        sb.AppendLine($"Logic,{first.Logic:0.0},{last.Logic:0.0},{Trend(first.Logic, last.Logic)}");
        return sb.ToString();
    }
}
