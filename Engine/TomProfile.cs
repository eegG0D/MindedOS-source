using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Theory of Mind math: the six dashboard scores plus the profile, perspective,
/// decision-style, social-cognition, perspective-taking, human-vs-AI and trend tables, and the
/// history log. Self-contained. All inferred mental-state scores are probabilistic hypotheses, not
/// verified facts. Scores 0–100.
/// </summary>
public static class TomProfile
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

    /// <summary>The six headline dashboard scores (0–100). Order: Goal, Curiosity, Leadership, Collaboration, Innovation, Strategic Thinking.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Goal", C(avgAtt * 0.4 + beta * 30 + div * 20)),
            ("Curiosity", C(div * 50 + theta * 30 + gamma * 20)),
            ("Leadership", C(avgAtt * 0.4 + beta * 30 + alpha * 20)),
            ("Collaboration", C(div * 40 + alpha * 30 + avgMed * 0.3)),
            ("Innovation", C(alpha * 40 + gamma * 40 + div * 20)),
            ("Strategic Thinking", C(avgAtt * 0.4 + alpha * 30 + beta * 20)),
        };
    }

    public static string ProfileCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("attribute,score\n");
        sb.AppendLine($"Goals,{C(a * 0.4 + beta * 30 + div * 20):0.0}");
        sb.AppendLine($"Intentions,{C(a * 0.3 + beta * 30 + div * 20):0.0}");
        sb.AppendLine($"Motivations,{C(a * 0.3 + theta * 30 + div * 20):0.0}");
        sb.AppendLine($"Interests,{C(div * 50 + gamma * 30):0.0}");
        sb.AppendLine($"Priorities,{C(a * 0.4 + (1 - div) * 30 + beta * 20):0.0}");
        sb.AppendLine($"Decision Tendencies,{C(beta * 50 + a * 0.3 + 10):0.0}");
        sb.AppendLine($"Perspective-Taking,{C(alpha * 40 + m * 0.3 + div * 20):0.0}");
        return sb.ToString();
    }

    public static string PerspectiveAnalysisCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("perspective,score,emphasis\n");
        sb.AppendLine($"Personal,{C(a * 0.4 + (1 - div) * 30 + 10):0.0},self-interest & internal motivation");
        sb.AppendLine($"Scientific,{C(beta * 40 + div * 30 + theta * 20):0.0},evidence-based reasoning & curiosity");
        sb.AppendLine($"Engineering,{C(beta * 50 + a * 0.3):0.0},problem solving & optimization");
        sb.AppendLine($"Social,{C(alpha * 40 + m * 0.3 + div * 20):0.0},collaboration & empathy");
        return sb.ToString();
    }

    public static string DecisionStyleCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("style,score\n");
        sb.AppendLine($"Analytical,{C(beta * 60 + a * 0.3):0.0}");
        sb.AppendLine($"Intuitive,{C(alpha * 40 + gamma * 40 + 10):0.0}");
        sb.AppendLine($"Exploratory,{C(div * 50 + theta * 30 + gamma * 20):0.0}");
        sb.AppendLine($"Strategic,{C(a * 0.4 + alpha * 30 + beta * 20):0.0}");
        sb.AppendLine($"Risk-taking,{C(gamma * 50 + div * 30 + 10):0.0}");
        sb.AppendLine($"Risk-avoidance,{C(m * 0.4 + (1 - div) * 40):0.0}");
        return sb.ToString();
    }

    public static string SocialCognitionCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("dimension,score\n");
        sb.AppendLine($"Collaboration Orientation,{C(div * 40 + alpha * 30 + m * 0.3):0.0}");
        sb.AppendLine($"Teamwork Orientation,{C(alpha * 40 + m * 0.3 + div * 20):0.0}");
        sb.AppendLine($"Leadership Indicators,{C(a * 0.4 + beta * 30 + alpha * 20):0.0}");
        sb.AppendLine($"Communication Style,{C(alpha * 40 + div * 30 + a * 0.2):0.0}");
        sb.AppendLine($"Negotiation Style,{C(beta * 30 + alpha * 30 + a * 0.3):0.0}");
        return sb.ToString();
    }

    public static string PerspectiveTakingCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("metric,score\n");
        sb.AppendLine($"Perspective Awareness,{C(alpha * 40 + div * 30 + m * 0.2):0.0}");
        sb.AppendLine($"Context Awareness,{C(a * 0.3 + alpha * 30 + div * 20):0.0}");
        sb.AppendLine($"Cooperative Reasoning,{C(alpha * 40 + m * 0.3 + div * 20):0.0}");
        sb.AppendLine($"Social Understanding,{C(alpha * 50 + m * 0.3):0.0}");
        return sb.ToString();
    }

    private static readonly string[] AiConcepts =
        { "data", "model", "system", "logic", "plan", "design", "build", "learn", "optimize", "process" };

    public static string HumanAiComparisonCsv(IReadOnlyList<string> words)
    {
        var distinct = new HashSet<string>(
            words.Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0 && x != "—"),
            StringComparer.OrdinalIgnoreCase);
        int totalDistinct = Math.Max(distinct.Count, 1);
        int shared = AiConcepts.Count(distinct.Contains);
        int unique = distinct.Count(d => !AiConcepts.Contains(d, StringComparer.OrdinalIgnoreCase));
        double similarity = C(100.0 * shared / AiConcepts.Length);
        var sb = new System.Text.StringBuilder("aspect,value\n");
        sb.AppendLine($"Similarities (%),{similarity:0.0}");
        sb.AppendLine($"Differences (%),{C(100 - similarity):0.0}");
        sb.AppendLine($"Shared Concepts,{shared}");
        sb.AppendLine($"Unique Concepts,{unique}");
        return sb.ToString();
    }

    public static string TrendsCsv(int priorSessions)
    {
        string status = priorSessions > 0 ? "tracked" : "baseline";
        var sb = new System.Text.StringBuilder("trend,status,sessions\n");
        foreach (var t in new[] { "Stable Motivations", "Evolving Goals", "Consistent Beliefs", "Emerging Interests" })
            sb.AppendLine($"{t},{status},{priorSessions + 1}");
        return sb.ToString();
    }

    // ---- long-term cognitive development ----

    public static string HistoryHeader() =>
        "date,distinct_concepts,goal,curiosity,leadership,collaboration,innovation,strategic_thinking,top_intent";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topIntent)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{Distinct(w)},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{topIntent}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topIntent)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topIntent));
    }
}
