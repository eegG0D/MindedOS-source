using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic planning scores derived from EEG averages, band shares and word
/// diversity. Mirrors <see cref="PerceptionProfile"/> math. All scores are 0–100.
/// </summary>
public static class PlanningProfile
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

    public static IReadOnlyList<(string Metric, double Value)> IntentionAnalysis(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Commitment", C(avgAtt * 0.6 + avgMed * 0.2 + beta * 20)),
            ("Focus", C(avgAtt)),
            ("Motivation", C(avgAtt * 0.5 + gamma * 50)),
            ("Persistence", C(avgAtt * 0.4 + (1 - div) * 40 + avgMed * 0.2)),
            ("Strategic Thinking", C(beta * 70 + alpha * 30 + avgAtt * 0.2)),
        };
    }

    public static IReadOnlyList<(string Score, double Value)> PlanningScores(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Strategic Thinking", C(beta * 70 + alpha * 30 + avgAtt * 0.2)),
            ("Organization", C(avgAtt * 0.5 + (1 - div) * 50)),
            ("Goal Clarity", C(avgAtt * 0.6 + (1 - div) * 40)),
            ("Execution Readiness", C(avgAtt * 0.7 + beta * 30)),
            ("Innovation Planning", C(gamma * 80 + div * 30)),
            ("Long-Term Vision", C(alpha * 50 + theta * 30 + div * 30)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> GoalForecasts(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Success Probability", C(avgAtt * 0.5 + beta * 30 + avgMed * 0.2)),
            ("Completion Probability", C(avgAtt * 0.4 + (1 - div) * 40 + avgMed * 0.2)),
            ("Motivation Sustainability", C(avgMed * 0.4 + avgAtt * 0.3 + gamma * 30)),
            ("Resource Sufficiency", C(50 + (avgAtt - 50) * 0.4 + alpha * 20)),
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
            ("Goal Clarity", C(avgAtt * 0.6 + (1 - div) * 40)),
            ("Strategic Thinking", C(beta * 70 + alpha * 30 + avgAtt * 0.2)),
            ("Planning Readiness", C(avgAtt * 0.7 + beta * 30)),
            ("Resource Availability", C(50 + (avgAtt - 50) * 0.4 + alpha * 20)),
            ("Innovation Potential", C(gamma * 80 + div * 30)),
            ("Success Forecast", C(avgAtt * 0.5 + beta * 30 + avgMed * 0.2)),
        };
    }

    private static string Csv(string headerA, string headerB, IReadOnlyList<(string, double)> rows)
    {
        var sb = new System.Text.StringBuilder($"{headerA},{headerB}\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string IntentionCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", IntentionAnalysis(a, m, b, w));
    public static string PlanningScoresCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("score", "value", PlanningScores(a, m, b, w));
    public static string GoalForecastsCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", GoalForecasts(a, m, b, w));

    public static string HistoryHeader() =>
        "date,goal_clarity,strategic_thinking,planning_readiness,resource_availability,innovation_potential,success_forecast,top_domain";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topDomain)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{topDomain}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topDomain)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topDomain));
    }
}
