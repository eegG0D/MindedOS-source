using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic quantum scores from EEG averages, band shares and word diversity.
/// Mirrors <see cref="PerceptionProfile"/> math. All scores are 0–100.
/// </summary>
public static class QuantumProfile
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

    /// <summary>The six quantum scores.</summary>
    public static IReadOnlyList<(string Score, double Value)> Scores(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Research Potential", C(div * 60 + theta * 40)),
            ("Technical Interest", C(beta * 70 + avgAtt * 0.3)),
            ("Mathematical Interest", C(beta * 60 + alpha * 20 + avgAtt * 0.2)),
            ("Innovation Score", C(gamma * 80 + div * 30)),
            ("Learning Readiness", C(avgAtt * 0.5 + avgMed * 0.2 + div * 30)),
            ("Exploration Score", C(div * 80 + theta * 20)),
        };
    }

    public static string ScoresCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var sb = new System.Text.StringBuilder("score,value\n");
        foreach (var (name, value) in Scores(a, m, b, w)) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string HistoryHeader() =>
        "date,research_potential,technical_interest,mathematical_interest,innovation,learning_readiness,exploration,top_concept";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topConcept)
    {
        var s = Scores(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{s[0].Value:0},{s[1].Value:0},{s[2].Value:0},{s[3].Value:0},{s[4].Value:0},{s[5].Value:0},{topConcept}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topConcept)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topConcept));
    }
}
