using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Virtual Reality math: the six dashboard scores and the history log. Self-contained.
/// All scores 0–100.
/// </summary>
public static class VrProfile
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

    /// <summary>The six dashboard scores (0–100). Order: World Complexity, Creativity, Exploration, Innovation, Educational Value, Immersion.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("World Complexity", C(div * 40 + beta * 30 + avgAtt * 0.3)),
            ("Creativity", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Exploration", C(div * 50 + theta * 30 + gamma * 20)),
            ("Innovation", C(alpha * 40 + gamma * 40 + div * 20)),
            ("Educational Value", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Immersion", C(alpha * 40 + theta * 30 + avgAtt * 0.3)),
        };
    }

    // ---- VR memory database ----

    public static string HistoryHeader() =>
        "date,distinct_concepts,world_complexity,creativity,exploration,innovation,educational_value,immersion,theme";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string theme)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{Distinct(w)},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{theme}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string theme)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, theme));
    }
}
