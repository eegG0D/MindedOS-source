using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Task Automation math: the dashboard scores, productivity metrics and the history
/// log. Self-contained. All scores 0–100.
/// </summary>
public static class TaskProfile
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

    /// <summary>The four headline scores (0–100). Order: Productivity, Automation, Project Progress, Agent Activity.</summary>
    public static IReadOnlyList<(string Score, double Value)> Scores(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Productivity Score", C(avgAtt * 0.4 + beta * 40 + 10)),
            ("Automation Score", C(beta * 40 + div * 40 + gamma * 20)),
            ("Project Progress", C(avgAtt * 0.3 + beta * 30 + (1 - div) * 20)),
            ("Agent Activity", C(div * 50 + beta * 30 + avgAtt * 0.2)),
        };
    }

    public static string ProductivityMetricsCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("metric,value\n");
        sb.AppendLine($"Productivity Potential,{C(a * 0.4 + beta * 40 + 10):0.0}");
        sb.AppendLine($"Completion Probability,{C(a * 0.3 + (1 - div) * 30 + m * 0.2 + beta * 20):0.0}");
        sb.AppendLine($"Cognitive Load,{C(beta * 50 + div * 30 + a * 0.2):0.0}");
        sb.AppendLine($"Focus Requirement,{C(beta * 60 + a * 0.3):0.0}");
        sb.AppendLine($"Resource Demand,{C(div * 50 + beta * 30 + 10):0.0}");
        return sb.ToString();
    }

    // ---- historical task learning ----

    public static string HistoryHeader() =>
        "date,total_tasks,completed,productivity,automation,project_progress,agent_activity,top_category";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w,
        int totalTasks, int completed, string topCategory)
    {
        var s = Scores(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{totalTasks},{completed},{s[0].Value:0},{s[1].Value:0},{s[2].Value:0},{s[3].Value:0},{topCategory}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w,
        int totalTasks, int completed, string topCategory)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, totalTasks, completed, topCategory));
    }
}
