using System.IO;

namespace MindedOS.Engine;

/// <summary>
/// Scans prior recorded EEG word CSVs (session evolution) and a csv_files/ folder (multi-user
/// collective learning). Reuses <see cref="PatternScan.LoadWords"/>. Degrades gracefully.
/// </summary>
public static class SemiSupScan
{
    private static Dictionary<string, int> Freq(IEnumerable<string> words)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }
        return freq;
    }

    /// <summary>Compares current concepts against pooled prior recordings: stable / new / growing / declining.</summary>
    public static string SessionEvolutionCsv(string outputDir, IReadOnlyList<string> currentWords)
    {
        var current = Freq(currentWords);
        var prior = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(outputDir))
        {
            foreach (var f in Directory.EnumerateFiles(outputDir, "recorded_eeg_*.csv"))
            {
                try
                {
                    foreach (var (w, c) in Freq(PatternScan.LoadWords(f)))
                        prior[w] = prior.TryGetValue(w, out var p) ? p + c : c;
                }
                catch { /* skip */ }
            }
        }

        var sb = new System.Text.StringBuilder("concept,status,trend\n");
        var top = current.OrderByDescending(kv => kv.Value).Take(8).ToList();
        if (top.Count == 0) { sb.AppendLine("(no concepts),none,flat"); return sb.ToString(); }
        foreach (var (word, cur) in top)
        {
            bool seen = prior.TryGetValue(word, out var pc) && pc > 0;
            string status = seen ? "stable" : "new";
            string trend = !seen ? "rising" : cur > pc ? "growing" : cur < pc ? "declining" : "stable";
            sb.AppendLine($"{word},{status},{trend}");
        }
        return sb.ToString();
    }

    /// <summary>Pools concepts across csv_files/ to summarize collective learning.</summary>
    public static string NetworkLearningCsv(string outputDir)
    {
        var pooled = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int files = 0;
        var csvFiles = Path.Combine(outputDir, "csv_files");
        if (Directory.Exists(csvFiles))
        {
            foreach (var f in Directory.EnumerateFiles(csvFiles, "*.csv"))
            {
                try
                {
                    var words = PatternScan.LoadWords(f);
                    if (words.Count == 0) continue;
                    files++;
                    foreach (var (w, c) in Freq(words)) pooled[w] = pooled.TryGetValue(w, out var p) ? p + c : c;
                }
                catch { /* skip */ }
            }
        }

        var sb = new System.Text.StringBuilder("metric,value\n");
        if (files == 0) { sb.AppendLine("(no network),0"); return sb.ToString(); }
        int shared = pooled.Count(kv => kv.Value >= 2);
        sb.AppendLine($"Network Recordings,{files}");
        sb.AppendLine($"Shared Discoveries,{pooled.Count}");
        sb.AppendLine($"Common Concepts,{shared}");
        sb.AppendLine($"Group Knowledge Patterns,{System.Math.Min(pooled.Count, 100)}");
        sb.AppendLine($"Network Intelligence,{System.Math.Clamp(shared * 5, 0, 100)}");
        return sb.ToString();
    }
}
