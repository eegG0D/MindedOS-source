using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Scans prior recorded EEG word CSVs (trends) and a csv_files/ folder (multi-user
/// team network). Reuses <see cref="PatternScan.LoadWords"/>. Degrades gracefully.
/// </summary>
public static class MasScan
{
    public sealed record MasSession(string Id, DateTime Time, double Cohesion, double Consensus, double Throughput);

    public static IReadOnlyList<MasSession> Scan(string outputDir)
    {
        var sessions = new List<MasSession>();
        if (!Directory.Exists(outputDir)) return sessions;
        foreach (var f in Directory.EnumerateFiles(outputDir, "recorded_eeg_*.csv"))
        {
            try
            {
                var words = PatternScan.LoadWords(f);
                if (words.Count == 0) continue;
                var m = MasTeam.CoordinationMetrics(50, 50, Array.Empty<BandReading>(), words);
                // m: [0]=Cohesion, [1]=Coverage, [2]=Consensus, [3]=Throughput, ...
                sessions.Add(new MasSession(Path.GetFileNameWithoutExtension(f), File.GetLastWriteTime(f),
                    m[0].Value, m[2].Value, m[3].Value));
            }
            catch { /* skip unreadable */ }
        }
        return sessions.OrderBy(s => s.Time).ToList();
    }

    public static string TrendsCsv(IReadOnlyList<MasSession> sessions)
    {
        var ordered = sessions.OrderBy(s => s.Time).ToList();
        bool single = ordered.Count < 2;
        var first = ordered.First();
        var last = ordered.Last();
        string Trend(double e, double l) => single ? "insufficient data" : l > e + 2 ? "growth" : l < e - 2 ? "declining" : "stable";
        var sb = new System.Text.StringBuilder("metric,earliest,latest,trend\n");
        sb.AppendLine($"Cohesion,{first.Cohesion:0.0},{last.Cohesion:0.0},{Trend(first.Cohesion, last.Cohesion)}");
        sb.AppendLine($"Consensus,{first.Consensus:0.0},{last.Consensus:0.0},{Trend(first.Consensus, last.Consensus)}");
        sb.AppendLine($"Throughput,{first.Throughput:0.0},{last.Throughput:0.0},{Trend(first.Throughput, last.Throughput)}");
        return sb.ToString();
    }

    public static string NetworkRankingsCsv(string outputDir)
    {
        var rows = new List<(string Id, double Cohesion, double Coverage, double Consensus, double Throughput, double Overall)>();
        var csvFiles = Path.Combine(outputDir, "csv_files");
        if (Directory.Exists(csvFiles))
        {
            foreach (var f in Directory.EnumerateFiles(csvFiles, "*.csv"))
            {
                try
                {
                    var words = PatternScan.LoadWords(f);
                    if (words.Count == 0) continue;
                    var m = MasTeam.CoordinationMetrics(50, 50, Array.Empty<BandReading>(), words);
                    double overall = (m[0].Value + m[1].Value + m[2].Value + m[3].Value + m[4].Value + m[5].Value) / 6;
                    rows.Add((Path.GetFileNameWithoutExtension(f), m[0].Value, m[1].Value, m[2].Value, m[3].Value, overall));
                }
                catch { /* skip */ }
            }
        }

        var sb = new System.Text.StringBuilder("ranking,top_session,score\n");
        void Rank(string label, Func<(string Id, double Cohesion, double Coverage, double Consensus, double Throughput, double Overall), double> sel)
        {
            if (rows.Count == 0) { sb.AppendLine($"{label},(no teams),0.0"); return; }
            var best = rows.OrderByDescending(sel).First();
            sb.AppendLine($"{label},{best.Id},{sel(best):0.0}");
        }
        Rank("Best Coordinator", r => r.Cohesion);
        Rank("Best Strategist", r => r.Throughput);
        Rank("Best Analyst", r => r.Consensus);
        Rank("Best Researcher", r => r.Coverage);
        Rank("Best Team", r => r.Overall);
        return sb.ToString();
    }
}
