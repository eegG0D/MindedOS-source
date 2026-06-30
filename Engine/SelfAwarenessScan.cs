using System.IO;

namespace MindedOS.Engine;

/// <summary>
/// Scans prior recorded EEG word CSVs to build the historical comparison of interests.
/// Reuses <see cref="PatternScan.LoadWords"/>. Degrades gracefully.
/// </summary>
public static class SelfAwarenessScan
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

    /// <summary>
    /// Compares the current top interests against interests pooled from prior recordings,
    /// labeling each new / stable / declining, then adds one emerging row. `interest,status,trend`.
    /// </summary>
    public static string HistoricalComparisonCsv(string outputDir, IReadOnlyList<string> currentWords)
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

        var sb = new System.Text.StringBuilder("interest,status,trend\n");
        var topCurrent = current.OrderByDescending(kv => kv.Value).Take(8).ToList();
        if (topCurrent.Count == 0) { sb.AppendLine("(no interests),none,flat"); return sb.ToString(); }
        foreach (var (word, cur) in topCurrent)
        {
            bool seen = prior.TryGetValue(word, out var pc) && pc > 0;
            string status = seen ? "stable" : "new";
            string trend = !seen ? "rising" : cur >= pc ? "rising" : "declining";
            sb.AppendLine($"{word},{status},{trend}");
        }
        // one emerging interest = the top prior word not in the current top set
        var emerging = prior.Where(kv => !topCurrent.Any(c => string.Equals(c.Key, kv.Key, StringComparison.OrdinalIgnoreCase)))
                            .OrderByDescending(kv => kv.Value).FirstOrDefault();
        if (emerging.Key is not null) sb.AppendLine($"{emerging.Key},emerging,watch");
        return sb.ToString();
    }
}
