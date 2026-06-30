using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Scans prior recorded EEG word CSVs and builds the multi-session perception trend.
/// Reuses <see cref="PatternScan.LoadWords"/>. Degrades to a single session.
/// </summary>
public static class PerceptionScan
{
    public sealed record PerceptionSession(string Id, DateTime Time, double Awareness, double Curiosity, double Innovation, string TopCategory);

    public static IReadOnlyList<PerceptionSession> Scan(string outputDir, string dataDir)
    {
        var sessions = new List<PerceptionSession>();
        if (!Directory.Exists(outputDir)) return sessions;
        foreach (var f in Directory.EnumerateFiles(outputDir, "recorded_eeg_*.csv"))
        {
            try
            {
                var words = PatternScan.LoadWords(f);
                if (words.Count == 0) continue;
                var d = PerceptionProfile.Dashboard(50, 50, Array.Empty<BandReading>(), words);
                string top = "General";
                if (!string.IsNullOrEmpty(dataDir))
                {
                    var t = PerceptionTopics.DetectFromFile(dataDir, words, "perception_topics.csv");
                    if (t.Count > 0) top = t[0].Topic;
                }
                sessions.Add(new PerceptionSession(Path.GetFileNameWithoutExtension(f), File.GetLastWriteTime(f),
                    d[0].Value, d[2].Value, d[3].Value, top));
            }
            catch { /* skip unreadable */ }
        }
        return sessions.OrderBy(s => s.Time).ToList();
    }

    public static string TrendsCsv(IReadOnlyList<PerceptionSession> sessions)
    {
        var ordered = sessions.OrderBy(s => s.Time).ToList();
        bool single = ordered.Count < 2;
        var first = ordered.First();
        var last = ordered.Last();
        string Trend(double e, double l) => single ? "insufficient data" : l > e + 2 ? "growth" : l < e - 2 ? "declining" : "stable";
        var sb = new System.Text.StringBuilder("metric,earliest,latest,trend\n");
        sb.AppendLine($"Awareness,{first.Awareness:0.0},{last.Awareness:0.0},{Trend(first.Awareness, last.Awareness)}");
        sb.AppendLine($"Curiosity,{first.Curiosity:0.0},{last.Curiosity:0.0},{Trend(first.Curiosity, last.Curiosity)}");
        sb.AppendLine($"Innovation,{first.Innovation:0.0},{last.Innovation:0.0},{Trend(first.Innovation, last.Innovation)}");
        return sb.ToString();
    }
}
