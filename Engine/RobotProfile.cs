using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic robot score-sets from EEG averages, band shares and word diversity.
/// Mirrors <see cref="RlProfile"/> math. Scores 0–100 (navigation weights 0–100 too).
/// </summary>
public static class RobotProfile
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

    public static IReadOnlyList<(string Metric, double Value)> RobotState(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Focus Level", C(avgAtt * 0.7 + beta * 20)),
            ("Task Confidence", C(avgAtt * 0.4 + beta * 30 + avgMed * 0.1)),
            ("Decision Stability", C(avgMed * 0.3 + (1 - div) * 40 + beta * 20)),
            ("Exploration Level", C(div * 60 + gamma * 40)),
            ("Learning Level", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Curiosity Level", C(div * 60 + gamma * 40)),
            ("Goal Commitment", C(avgAtt * 0.4 + avgMed * 0.2 + (1 - div) * 30)),
        };
    }

    /// <summary>Seven personality traits as percentages summing to ~100.</summary>
    public static IReadOnlyList<(string Trait, double Value)> Personality(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        var raw = new (string, double)[]
        {
            ("Assistant", C(avgMed * 0.4 + avgAtt * 0.2 + 20)),
            ("Researcher", C(div * 50 + theta * 30 + 20)),
            ("Explorer", C(div * 60 + gamma * 30 + 10)),
            ("Engineer", C(beta * 60 + avgAtt * 0.2)),
            ("Scientist", C(beta * 40 + div * 30 + alpha * 20)),
            ("Inventor", C(gamma * 60 + div * 30)),
            ("Teacher", C(avgMed * 0.3 + alpha * 30 + 20)),
        };
        double sum = 0;
        foreach (var (_, v) in raw) sum += v;
        if (sum <= 0) sum = 1;
        return raw.Select(r => (r.Item1, 100.0 * r.Item2 / sum)).ToList();
    }

    public static IReadOnlyList<(string Skill, double Value)> Skills(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Navigation", C(avgAtt * 0.4 + beta * 30 + div * 20)),
            ("Inspection", C(avgAtt * 0.5 + beta * 30)),
            ("Monitoring", C(avgAtt * 0.5 + avgMed * 0.2)),
            ("Research", C(div * 50 + theta * 30 + avgAtt * 0.2)),
            ("Data Collection", C(beta * 50 + avgAtt * 0.3)),
            ("Maintenance", C(avgMed * 0.3 + beta * 40 + (1 - div) * 10)),
            ("Assistance", C(avgMed * 0.4 + avgAtt * 0.2 + alpha * 20)),
        };
    }

    /// <summary>Eight movements with a 0–100 weight and a High/Medium/Low priority.</summary>
    public static IReadOnlyList<(string Movement, double Weight)> NavigationPlan(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Forward", C(avgAtt * 0.7 + beta * 20)),
            ("Backward", C((100 - avgAtt) * 0.4)),
            ("Left", C(alpha * 50 + div * 20)),
            ("Right", C(beta * 40 + div * 20)),
            ("Stop", C(avgMed * 0.4 + (1 - div) * 30)),
            ("Rotate", C(div * 50 + gamma * 30)),
            ("Explore", C(div * 60 + gamma * 40)),
            ("Return Home", C(avgMed * 0.3 + (1 - div) * 40)),
        };
    }

    /// <summary>The seven dashboard scores (0–100). Order: Confidence, Goal Commitment, Battery, Learning, Exploration, Curiosity, Decision Stability.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Confidence", C(avgAtt * 0.4 + beta * 30 + avgMed * 0.1)),
            ("Goal Commitment", C(avgAtt * 0.4 + avgMed * 0.2 + (1 - div) * 30)),
            ("Battery", C(avgMed * 0.5 + 50)),
            ("Learning", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Exploration", C(div * 60 + gamma * 40)),
            ("Curiosity", C(div * 60 + gamma * 40)),
            ("Decision Stability", C(avgMed * 0.3 + (1 - div) * 40 + beta * 20)),
        };
    }

    // ---- CSV builders ----

    private static string ScoreCsv(string headerA, string headerB, IReadOnlyList<(string, double)> rows)
    {
        var sb = new System.Text.StringBuilder($"{headerA},{headerB}\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string RobotStateCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("metric", "score", RobotState(a, m, b, w));
    public static string PersonalityCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("trait", "percent", Personality(a, m, b, w));
    public static string SkillsCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("skill", "score", Skills(a, m, b, w));

    public static string NavigationPlanCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var sb = new System.Text.StringBuilder("movement,weight,priority\n");
        foreach (var (movement, weight) in NavigationPlan(a, m, b, w))
        {
            string priority = weight >= 66 ? "High" : weight >= 33 ? "Medium" : "Low";
            sb.AppendLine($"{movement},{weight:0.0},{priority}");
        }
        return sb.ToString();
    }

    // ---- history ----

    public static string HistoryHeader() =>
        "date,confidence,goal_commitment,battery,learning,exploration,curiosity,decision_stability,top_personality";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topPersonality)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{d[6].Value:0},{topPersonality}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topPersonality)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topPersonality));
    }
}
