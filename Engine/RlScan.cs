using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Scans prior recorded EEG word CSVs (learning trends) and a csv_files/ folder (multi-agent
/// competition). Reuses <see cref="PatternScan.LoadWords"/>. Degrades gracefully.
/// </summary>
public static class RlScan
{
    public sealed record RlSession(string Id, DateTime Time, double LearningRate, double Adaptability, double Innovation);

    public static IReadOnlyList<RlSession> Scan(string outputDir)
    {
        var sessions = new List<RlSession>();
        if (!Directory.Exists(outputDir)) return sessions;
        foreach (var f in Directory.EnumerateFiles(outputDir, "recorded_eeg_*.csv"))
        {
            try
            {
                var words = PatternScan.LoadWords(f);
                if (words.Count == 0) continue;
                var d = RlProfile.Dashboard(50, 50, Array.Empty<BandReading>(), words);
                // d: [0]=Reward, [1]=Learning Efficiency, [2]=Adaptability, ... [6]=Innovation
                sessions.Add(new RlSession(Path.GetFileNameWithoutExtension(f), File.GetLastWriteTime(f),
                    d[1].Value /* Learning Efficiency ~ learning rate */, d[2].Value /* Adaptability */, d[6].Value /* Innovation */));
            }
            catch { /* skip unreadable */ }
        }
        return sessions.OrderBy(s => s.Time).ToList();
    }

    public static string TrendsCsv(IReadOnlyList<RlSession> sessions)
    {
        var ordered = sessions.OrderBy(s => s.Time).ToList();
        bool single = ordered.Count < 2;
        var first = ordered.First();
        var last = ordered.Last();
        string Trend(double e, double l) => single ? "insufficient data" : l > e + 2 ? "growth" : l < e - 2 ? "declining" : "stable";
        var sb = new System.Text.StringBuilder("metric,earliest,latest,trend\n");
        sb.AppendLine($"Learning Rate,{first.LearningRate:0.0},{last.LearningRate:0.0},{Trend(first.LearningRate, last.LearningRate)}");
        sb.AppendLine($"Adaptability,{first.Adaptability:0.0},{last.Adaptability:0.0},{Trend(first.Adaptability, last.Adaptability)}");
        sb.AppendLine($"Innovation,{first.Innovation:0.0},{last.Innovation:0.0},{Trend(first.Innovation, last.Innovation)}");
        return sb.ToString();
    }

    public static string NetworkRankingsCsv(string outputDir)
    {
        var rows = new List<(string Id, double Learning, double Adaptability, double Curiosity, double Creativity, double Innovation)>();
        var csvFiles = Path.Combine(outputDir, "csv_files");
        if (Directory.Exists(csvFiles))
        {
            foreach (var f in Directory.EnumerateFiles(csvFiles, "*.csv"))
            {
                try
                {
                    var words = PatternScan.LoadWords(f);
                    if (words.Count == 0) continue;
                    var d = RlProfile.Dashboard(50, 50, Array.Empty<BandReading>(), words);
                    // d: [1]=Learning Efficiency, [2]=Adaptability, [3]=Curiosity, [4]=Creativity, [6]=Innovation
                    rows.Add((Path.GetFileNameWithoutExtension(f), d[1].Value, d[2].Value, d[3].Value, d[4].Value, d[6].Value));
                }
                catch { /* skip */ }
            }
        }

        var sb = new System.Text.StringBuilder("ranking,top_session,score\n");
        void Rank(string label, Func<(string Id, double Learning, double Adaptability, double Curiosity, double Creativity, double Innovation), double> sel)
        {
            if (rows.Count == 0) { sb.AppendLine($"{label},(no agents),0.0"); return; }
            var best = rows.OrderByDescending(sel).First();
            sb.AppendLine($"{label},{best.Id},{sel(best):0.0}");
        }
        Rank("Best Learner", r => r.Learning);
        Rank("Best Adapter", r => r.Adaptability);
        Rank("Most Curious", r => r.Curiosity);
        Rank("Most Creative", r => r.Creativity);
        Rank("Best Focus", r => r.Learning);
        Rank("Best Innovator", r => r.Innovation);
        return sb.ToString();
    }
}
