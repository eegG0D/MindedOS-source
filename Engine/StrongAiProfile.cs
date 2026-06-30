using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Strong AI score-sets from EEG averages, band shares and word diversity.
/// Mirrors <see cref="RlProfile"/> math. All scores 0–100.
/// </summary>
public static class StrongAiProfile
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

    /// <summary>The eight dashboard scores (0–100). Order: Intelligence Profile, Reasoning, Creativity, Learning, Planning, Problem Solving, Memory Utilization, Research Potential.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Intelligence Profile", C(avgAtt * 0.4 + beta * 30 + div * 20)),
            ("Reasoning", C(beta * 70 + avgAtt * 0.3)),
            ("Creativity", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Learning", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Planning", C(avgAtt * 0.4 + alpha * 30 + beta * 20)),
            ("Problem Solving", C(beta * 60 + avgAtt * 0.3 + div * 10)),
            ("Memory Utilization", C(avgMed * 0.3 + (1 - div) * 40 + beta * 20)),
            ("Research Potential", C(div * 60 + theta * 30 + avgAtt * 0.1)),
        };
    }

    public static IReadOnlyList<(string Type, double Value)> CognitiveSimulation(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Analytical Thinking", C(beta * 70 + avgAtt * 0.3)),
            ("Creative Thinking", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Strategic Thinking", C(avgAtt * 0.4 + alpha * 30 + beta * 20)),
            ("Technical Thinking", C(beta * 60 + avgAtt * 0.2 + div * 10)),
            ("Systems Thinking", C(alpha * 40 + beta * 40 + div * 20)),
            ("Abstract Thinking", C(alpha * 50 + gamma * 30 + div * 20)),
        };
    }

    public static string CognitiveSimulationCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var sb = new System.Text.StringBuilder("thinking_type,score\n");
        foreach (var (type, value) in CognitiveSimulation(a, m, b, w)) sb.AppendLine($"{type},{value:0.0}");
        return sb.ToString();
    }

    public static string LearningProgressCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var d = Dashboard(a, m, b, w);
        int distinct = w.Where(x => x.Trim().Length > 0 && x.Trim() != "—").Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var sb = new System.Text.StringBuilder("metric,value\n");
        sb.AppendLine($"Knowledge Growth,{d[3].Value:0.0}");
        sb.AppendLine($"Skill Development,{d[5].Value:0.0}");
        sb.AppendLine($"Topic Evolution,{d[7].Value:0.0}");
        sb.AppendLine($"Vocabulary,{distinct}");
        return sb.ToString();
    }

    // ---- long-term memory ----

    public static string MemoryHeader() =>
        "date,intelligence,reasoning,creativity,learning,planning,problem_solving,memory_utilization,research_potential,top_domain";

    public static string MemoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topDomain)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{d[6].Value:0},{d[7].Value:0},{topDomain}";
    }

    public static void AppendMemory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topDomain)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(MemoryHeader());
        writer.WriteLine(MemoryRow(a, m, b, w, topDomain));
    }
}
