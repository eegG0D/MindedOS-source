using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Weak AI math: the dashboard scores, cognitive classification, productivity analysis,
/// benchmarking metrics and the history log. Self-contained. All scores 0–100.
/// </summary>
public static class WeakAiProfile
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

    public static double Productivity(double a, IReadOnlyList<BandReading> b)
    { var (_, _, beta, _) = Shares(b); return C(a * 0.4 + beta * 40 + 10); }

    /// <summary>The six dashboard scores (0–100). Order: Domain Specialization, Cognitive Strength, Productivity, Knowledge Density, Task Focus, Benchmark.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words, double topDomainPercent, int activeDomains)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        double benchmark = C((C(topDomainPercent + 30) + C(beta * 60 + avgAtt * 0.3) + Productivity(avgAtt, bands)) / 3);
        return new (string, double)[]
        {
            ("Domain Specialization", C(topDomainPercent + 30)),
            ("Cognitive Strength", C(beta * 60 + avgAtt * 0.3)),
            ("Productivity", Productivity(avgAtt, bands)),
            ("Knowledge Density", C(div * 70 + activeDomains * 5)),
            ("Task Focus", C(avgAtt * 0.4 + (1 - div) * 30 + beta * 20)),
            ("Benchmark", benchmark),
        };
    }

    public static IReadOnlyList<(string Type, double Value)> Cognitive(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Analytical Thinking", C(beta * 60 + avgAtt * 0.3)),
            ("Creative Thinking", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Strategic Thinking", C(avgAtt * 0.4 + alpha * 30 + beta * 20)),
            ("Technical Thinking", C(beta * 50 + avgAtt * 0.3 + gamma * 20)),
            ("Experimental Thinking", C(gamma * 40 + div * 40 + theta * 20)),
            ("Research Thinking", C(div * 60 + theta * 30 + avgAtt * 0.1)),
        };
    }

    public static string CognitiveClassificationCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var sb = new System.Text.StringBuilder("thinking_type,score\n");
        foreach (var (type, value) in Cognitive(a, m, b, w)) sb.AppendLine($"{type},{value:0.0}");
        return sb.ToString();
    }

    /// <summary>The dominant cognitive thinking type.</summary>
    public static string DominantCognitive(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
        => Cognitive(a, m, b, w).OrderByDescending(c => c.Value).First().Type;

    public static string ProductivityAnalysisCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int taskSuggestions)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        double prod = Productivity(a, b);
        var sb = new System.Text.StringBuilder("metric,value\n");
        sb.AppendLine($"Productivity Score,{prod:0.0}");
        sb.AppendLine($"Focus Pattern,{C(a * 0.4 + (1 - div) * 30 + beta * 20):0.0}");
        sb.AppendLine($"Goal Orientation,{C(a * 0.3 + beta * 30 + 20):0.0}");
        sb.AppendLine($"Task Suggestions,{taskSuggestions}");
        sb.AppendLine($"Improvement Headroom,{C(100 - prod):0.0}");
        return sb.ToString();
    }

    public static string MetricsCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, double topDomainPercent)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("metric,score\n");
        sb.AppendLine($"Recommendation Quality,{C(a * 0.3 + div * 40 + beta * 20):0.0}");
        sb.AppendLine($"Topic Consistency,{C((1 - div) * 50 + a * 0.3 + 10):0.0}");
        sb.AppendLine($"Knowledge Depth,{C(beta * 40 + a * 0.3 + div * 20):0.0}");
        sb.AppendLine($"Domain Specialization,{C(topDomainPercent + 30):0.0}");
        return sb.ToString();
    }

    // ---- multi-session learning ----

    public static string HistoryHeader() =>
        "date,distinct_concepts,domain_specialization,cognitive_strength,productivity,knowledge_density,task_focus,benchmark,top_domain";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, double topDomainPercent, int activeDomains, string topDomain)
    {
        var d = Dashboard(a, m, b, w, topDomainPercent, activeDomains);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{Distinct(w)},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{topDomain}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, double topDomainPercent, int activeDomains, string topDomain)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topDomainPercent, activeDomains, topDomain));
    }
}
