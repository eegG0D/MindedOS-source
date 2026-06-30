using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Unsupervised Learning math: headline scores, signal features, cognitive archetypes
/// and the history log. Self-contained. All scores 0–100.
/// </summary>
public static class UnsupProfile
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

    private static int Distinct(IReadOnlyList<string> words) =>
        words.Where(x => x.Trim().Length > 0 && x.Trim() != "—").Distinct(StringComparer.OrdinalIgnoreCase).Count();

    /// <summary>Shannon entropy of the word distribution, normalized to 0–1.</summary>
    public static double Entropy(IReadOnlyList<string> words)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var x = w.Trim().ToLowerInvariant();
            if (x.Length == 0 || x == "—") continue;
            freq[x] = freq.TryGetValue(x, out var c) ? c + 1 : 1;
        }
        int n = freq.Values.Sum();
        if (n == 0 || freq.Count <= 1) return 0;
        double h = 0;
        foreach (var c in freq.Values)
        {
            double p = (double)c / n;
            h -= p * Math.Log2(p);
        }
        return h / Math.Log2(freq.Count); // 0..1
    }

    /// <summary>The six headline scores (0–100). Order: Cognitive Diversity, Cluster Separation, Topic Diversity, Anomaly Rate, Emergence, Similarity Density.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words, int activeTopics)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        double ent = Entropy(words);
        return new (string, double)[]
        {
            ("Cognitive Diversity", C(div * 100)),
            ("Cluster Separation", C(ent * 60 + div * 30 + 10)),
            ("Topic Diversity", C(activeTopics * 9 + div * 20)),
            ("Anomaly Rate", C((1 - div) * 40 + gamma * 30)),
            ("Emergence", C(gamma * 40 + div * 40 + theta * 20)),
            ("Similarity Density", C((1 - div) * 60 + avgMed * 0.2)),
        };
    }

    public static string SignalFeaturesCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double mean = b.Count > 0 ? b.Average(x => x.Value) : 0;
        double variance = b.Count > 0 ? b.Average(x => (x.Value - mean) * (x.Value - mean)) : 0;
        double delta = 0;
        foreach (var r in b) if (r.Key == "delta") delta = r.Value;
        double ent = Entropy(w);
        double complexity = C(ent * 60 + Diversity(w) * 40);
        var sb = new System.Text.StringBuilder("feature,value\n");
        sb.AppendLine($"Signal Variance,{variance:0.0}");
        sb.AppendLine($"Delta,{delta:0.0}");
        sb.AppendLine($"Theta,{theta * 100:0.0}");
        sb.AppendLine($"Alpha,{alpha * 100:0.0}");
        sb.AppendLine($"Beta,{beta * 100:0.0}");
        sb.AppendLine($"Gamma,{gamma * 100:0.0}");
        sb.AppendLine($"Entropy,{ent:0.000}");
        sb.AppendLine($"Complexity,{complexity:0.0}");
        return sb.ToString();
    }

    public static string CognitiveArchetypesCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("archetype,score\n");
        sb.AppendLine($"Inventive Thinking,{C(gamma * 45 + alpha * 35 + div * 20):0.0}");
        sb.AppendLine($"Systems Thinking,{C(alpha * 40 + beta * 40 + div * 20):0.0}");
        sb.AppendLine($"Strategic Reasoning,{C(a * 0.4 + alpha * 30 + beta * 20):0.0}");
        sb.AppendLine($"Deep Curiosity,{C(div * 50 + theta * 30 + gamma * 20):0.0}");
        sb.AppendLine($"Interdisciplinary Thinking,{C(div * 60 + alpha * 20 + gamma * 20):0.0}");
        return sb.ToString();
    }

    // ---- long-term learning database ----

    public static string HistoryHeader() =>
        "date,distinct_concepts,cognitive_diversity,cluster_separation,topic_diversity,anomaly_rate,emergence,similarity_density,dominant_topic";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int activeTopics, string dominantTopic)
    {
        var d = Dashboard(a, m, b, w, activeTopics);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{Distinct(w)},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{dominantTopic}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int activeTopics, string dominantTopic)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, activeTopics, dominantTopic));
    }
}
