using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic semi-supervised-learning score-sets from EEG averages, band shares and word
/// diversity. Mirrors <see cref="RlProfile"/> math. All scores 0–100; counts are raw ints.
/// </summary>
public static class SemiSupProfile
{
    private static double C(double v) => Math.Clamp(v, 0, 100);

    private static (double theta, double alpha, double beta, double gamma) Shares(IReadOnlyList<BandReading> bands)
    {
        double total = 0;
        foreach (var b in bands) total += b.Value;
        if (total <= 0) total = 1;
        double Share(string key)
        {
            foreach (var b in bands) if (b.Key == key) return b.Value / total;
            return 0;
        }
        return (Share("theta"), Share("lowAlpha") + Share("highAlpha"),
                Share("lowBeta") + Share("highBeta"), Share("lowGamma") + Share("midGamma"));
    }

    private static double Diversity(IReadOnlyList<string> words)
    {
        int n = words.Count;
        if (n == 0) return 0;
        return (double)words.Distinct(StringComparer.OrdinalIgnoreCase).Count() / n;
    }

    /// <summary>Distinct decoded words (treated as "known/labeled" patterns).</summary>
    public static int KnownCount(IReadOnlyList<string> words) =>
        words.Where(w => w.Trim().Length > 0 && w.Trim() != "—").Distinct(StringComparer.OrdinalIgnoreCase).Count();

    /// <summary>Frequency-1 decoded words (treated as "unlabeled/novel" patterns).</summary>
    public static int UnknownCount(IReadOnlyList<string> words)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }
        return freq.Count(kv => kv.Value == 1);
    }

    public static double DiscoveryRate(IReadOnlyList<string> words)
    {
        int known = KnownCount(words);
        if (known == 0) return 0;
        return C(100.0 * UnknownCount(words) / known);
    }

    /// <summary>The six dashboard scores (0–100). Order: Knowledge Coverage, Discovery Rate, Classification Accuracy, Knowledge Growth, Learning Confidence, Pattern Diversity.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Knowledge Coverage", C(avgAtt * 0.4 + beta * 30 + (1 - div) * 20)),
            ("Discovery Rate", DiscoveryRate(words)),
            ("Classification Accuracy", C(beta * 60 + avgAtt * 0.3)),
            ("Knowledge Growth", C(div * 60 + gamma * 30 + avgAtt * 0.1)),
            ("Learning Confidence", C(avgAtt * 0.4 + avgMed * 0.2 + beta * 30)),
            ("Pattern Diversity", C(div * 100)),
        };
    }

    public static IReadOnlyList<(string Cluster, double Value)> BrainClusters(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Analytical Thinking", C(beta * 70 + avgAtt * 0.3)),
            ("Creative Thinking", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Problem Solving", C(beta * 60 + avgAtt * 0.3 + div * 10)),
            ("Innovation", C(gamma * 70 + div * 30)),
            ("Learning", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Research", C(div * 50 + theta * 30 + avgAtt * 0.2)),
            ("Exploration", C(div * 60 + gamma * 40)),
        };
    }

    private static string ScoreCsv(string headerA, string headerB, IReadOnlyList<(string, double)> rows)
    {
        var sb = new System.Text.StringBuilder($"{headerA},{headerB}\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string BrainClustersCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("cluster", "score", BrainClusters(a, m, b, w));

    public static string LearningProgressCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var d = Dashboard(a, m, b, w);
        var sb = new System.Text.StringBuilder("metric,value\n");
        sb.AppendLine($"Vocabulary,{KnownCount(w)}");
        sb.AppendLine($"Known Patterns,{KnownCount(w)}");
        sb.AppendLine($"Unknown Patterns,{UnknownCount(w)}");
        sb.AppendLine($"Discovery Rate,{DiscoveryRate(w):0.0}");
        sb.AppendLine($"Knowledge Growth,{d[3].Value:0.0}");
        sb.AppendLine($"Classification Accuracy,{d[2].Value:0.0}");
        return sb.ToString();
    }

    public static string ConfidenceScoresCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var d = Dashboard(a, m, b, w);
        double known = C(d[0].Value + 10), predicted = C(d[2].Value), discovered = C(d[1].Value);
        var sb = new System.Text.StringBuilder("mapping_type,confidence,reliability,certainty\n");
        sb.AppendLine($"Known,{known:0},{C(known - 5):0},{C(known - 10):0}");
        sb.AppendLine($"Predicted,{predicted:0},{C(predicted - 10):0},{C(predicted - 15):0}");
        sb.AppendLine($"New,{discovered:0},{C(discovered - 15):0},{C(discovered - 20):0}");
        return sb.ToString();
    }

    // ---- history ----

    public static string HistoryHeader() =>
        "date,knowledge_coverage,discovery_rate,classification_accuracy,knowledge_growth,learning_confidence,pattern_diversity,top_category";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topCategory)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{topCategory}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topCategory)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topCategory));
    }
}
