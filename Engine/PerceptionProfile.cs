using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic perception scores derived from EEG averages, band shares and word
/// diversity. Mirrors <see cref="NlpProfile"/> math. All scores are 0–100.
/// </summary>
public static class PerceptionProfile
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

    public static IReadOnlyList<(string Metric, double Value)> EnvironmentalAwareness(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Situational Awareness", C(avgAtt * 0.6 + alpha * 40)),
            ("Environmental Attention", C(avgAtt * 0.7 + beta * 30)),
            ("Context Recognition", C(div * 60 + alpha * 40)),
            ("Observation Quality", C(avgAtt * 0.5 + div * 50)),
            ("Awareness Depth", C(avgMed * 0.4 + theta * 40 + div * 30)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> AttentionAnalysis(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Attention Stability", C(avgAtt * 0.7 + (1 - div) * 30)),
            ("Focus Duration", C(avgAtt * 0.8 + beta * 20)),
            ("Attention Switching", C(div * 80 + gamma * 20)),
            ("Distraction Susceptibility", C((100 - avgAtt) * 0.6 + div * 40)),
            ("Selective Attention", C(avgAtt * 0.6 + beta * 40)),
        };
    }

    public static IReadOnlyList<(string Style, double Value)> PerceptionStyles(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Analytical", C(beta * 90 + avgAtt * 0.3)),
            ("Creative", C(alpha * 70 + gamma * 50 + div * 30)),
            ("Technical", C(beta * 80 + avgAtt * 0.2)),
            ("Emotional", C((100 - avgMed) * 0.5 + theta * 30)),
            ("Scientific", C(beta * 60 + div * 40 + avgAtt * 0.2)),
            ("Strategic", C(avgAtt * 0.5 + alpha * 40 + div * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> CuriosityMetrics(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Curiosity", C(div * 60 + theta * 40)),
            ("Exploration", C(div * 80 + theta * 20)),
            ("Discovery Potential", C(div * 50 + gamma * 50)),
            ("Learning Drive", C(avgAtt * 0.5 + div * 50)),
            ("Innovation Potential", C(gamma * 80 + div * 30)),
        };
    }

    /// <summary>The six dashboard scores.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Awareness", C(avgAtt * 0.6 + alpha * 40)),
            ("Attention", C(avgAtt)),
            ("Curiosity", C(div * 60 + theta * 40)),
            ("Innovation", C(gamma * 80 + div * 30)),
            ("Observation", C(avgAtt * 0.5 + div * 50)),
            ("Cognitive Perception", C(beta * 70 + alpha * 30 + avgAtt * 0.2)),
        };
    }

    private static string Csv(string headerA, string headerB, IReadOnlyList<(string, double)> rows)
    {
        var sb = new System.Text.StringBuilder($"{headerA},{headerB}\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string EnvironmentalAwarenessCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", EnvironmentalAwareness(a, m, b, w));
    public static string AttentionAnalysisCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", AttentionAnalysis(a, b, w));
    public static string PerceptionStylesCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("style", "score", PerceptionStyles(a, m, b, w));
    public static string CuriosityMetricsCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", CuriosityMetrics(a, b, w));

    public static string HistoryHeader() =>
        "date,awareness,attention,curiosity,innovation,observation,cognitive_perception,top_category";

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
