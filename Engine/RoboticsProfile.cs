using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic robotics score-sets from EEG averages, band shares and word diversity.
/// Mirrors <see cref="RobotProfile"/> math. All scores 0–100.
/// </summary>
public static class RoboticsProfile
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

    /// <summary>The six dashboard scores (0–100). Order: Robot Complexity, Autonomy, Intelligence, Engineering, Mobility, Human Interaction.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Robot Complexity", C(div * 50 + beta * 30 + gamma * 20)),
            ("Autonomy", C(avgAtt * 0.4 + div * 40 + gamma * 20)),
            ("Intelligence", C(beta * 60 + avgAtt * 0.3 + div * 10)),
            ("Engineering", C(beta * 50 + avgAtt * 0.3 + alpha * 20)),
            ("Mobility", C(avgAtt * 0.5 + div * 30 + beta * 20)),
            ("Human Interaction", C(avgMed * 0.4 + alpha * 30 + 20)),
        };
    }

    public static string DashboardCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var sb = new System.Text.StringBuilder("metric,score\n");
        foreach (var (name, value) in Dashboard(a, m, b, w)) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    /// <summary>Four human-robot interaction preferences derived from the dashboard scores.</summary>
    public static string HumanInteractionCsv(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        double complexity = dashboard[0].Value, autonomy = dashboard[1].Value, intelligence = dashboard[2].Value, human = dashboard[5].Value;
        var sb = new System.Text.StringBuilder("aspect,preference\n");
        sb.AppendLine($"Preferred Robot Personality,{(intelligence >= 60 ? "Analytical assistant" : "Friendly companion")}");
        sb.AppendLine($"Preferred Robot Appearance,{(complexity >= 60 ? "Advanced humanoid" : "Simple functional")}");
        sb.AppendLine($"Preferred Communication Style,{(human >= 60 ? "Conversational and expressive" : "Concise and direct")}");
        sb.AppendLine($"Preferred Interaction Model,{(autonomy >= 60 ? "Autonomous with oversight" : "Direct teleoperation")}");
        return sb.ToString();
    }

    // ---- history ----

    public static string HistoryHeader() =>
        "date,complexity,autonomy,intelligence,engineering,mobility,human_interaction,top_class";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topClass)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{topClass}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topClass)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topClass));
    }
}
