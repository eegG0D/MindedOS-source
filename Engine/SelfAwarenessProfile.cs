using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic self-awareness score-sets from EEG averages, band shares and word diversity.
/// Mirrors <see cref="RlProfile"/> math. All scores 0–100.
/// </summary>
public static class SelfAwarenessProfile
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

    /// <summary>The seven dashboard scores (0–100). Order: Focus, Reflection, Curiosity, Creativity, Consistency, Learning, Innovation.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Focus", C(avgAtt * 0.7 + beta * 20)),
            ("Reflection", C(avgMed * 0.4 + theta * 30 + alpha * 20)),
            ("Curiosity", C(div * 60 + gamma * 40)),
            ("Creativity", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Consistency", C(avgMed * 0.3 + (1 - div) * 50)),
            ("Learning", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Innovation", C(gamma * 70 + div * 30)),
        };
    }

    public static IReadOnlyList<(string Strength, double Value)> StrengthProfile(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Creativity", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Logic", C(beta * 80 + avgAtt * 0.2)),
            ("Focus", C(avgAtt * 0.7 + beta * 20)),
            ("Curiosity", C(div * 60 + gamma * 40)),
            ("Innovation", C(gamma * 70 + div * 30)),
            ("Learning", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Adaptability", C(div * 60 + gamma * 30 + alpha * 10)),
            ("Planning", C(avgAtt * 0.4 + alpha * 30 + beta * 20)),
        };
    }

    public static IReadOnlyList<(string GoalType, double Value)> GoalAnalysis(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Personal", C(avgMed * 0.4 + alpha * 30 + 20)),
            ("Technical", C(beta * 60 + avgAtt * 0.3)),
            ("Educational", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Research", C(div * 50 + theta * 30 + avgAtt * 0.2)),
            ("Creative", C(alpha * 50 + gamma * 40 + div * 10)),
        };
    }

    private static string ScoreCsv(string headerA, string headerB, IReadOnlyList<(string, double)> rows)
    {
        var sb = new System.Text.StringBuilder($"{headerA},{headerB}\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string StrengthProfileCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("strength", "score", StrengthProfile(a, m, b, w));
    public static string GoalAnalysisCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("goal_type", "score", GoalAnalysis(a, m, b, w));

    /// <summary>Growth opportunities = the lowest strengths, framed as improvement areas with a priority.</summary>
    public static string GrowthOpportunitiesCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var labels = new Dictionary<string, string>
        {
            ["Creativity"] = "Creative exploration", ["Logic"] = "Structured reasoning", ["Focus"] = "Learning consistency",
            ["Curiosity"] = "Knowledge expansion", ["Innovation"] = "Research opportunities", ["Learning"] = "Skill development",
            ["Adaptability"] = "Flexibility practice", ["Planning"] = "Project completion",
        };
        var lowest = StrengthProfile(a, m, b, w).OrderBy(s => s.Value).Take(5).ToList();
        var sb = new System.Text.StringBuilder("opportunity,score,priority\n");
        for (int i = 0; i < lowest.Count; i++)
        {
            string opp = labels.TryGetValue(lowest[i].Strength, out var l) ? l : lowest[i].Strength;
            string priority = i == 0 ? "High" : i < 3 ? "Medium" : "Low";
            sb.AppendLine($"{opp},{lowest[i].Value:0.0},{priority}");
        }
        return sb.ToString();
    }

    // ---- history ----

    public static string HistoryHeader() =>
        "date,focus,reflection,curiosity,creativity,consistency,learning,innovation,top_domain";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topDomain)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{d[6].Value:0},{topDomain}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topDomain)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topDomain));
    }
}
