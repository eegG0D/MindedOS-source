using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Transfer Learning math: the six dashboard scores, the five skill-transfer scores,
/// cognitive adaptation, the human-vs-AI transfer comparison, learning evolution and the history log.
/// Self-contained. All scores 0–100.
/// </summary>
public static class TransferProfile
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

    /// <summary>The six headline dashboard scores (0–100). Order: Knowledge, Transfer, Adaptability, Innovation, Learning Acceleration, Cross-Domain Potential.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words, int activeDomains)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Knowledge Score", C(avgAtt * 0.4 + div * 40 + beta * 20)),
            ("Transfer Score", C(div * 40 + beta * 30 + alpha * 20)),
            ("Adaptability Score", C(alpha * 40 + div * 30 + avgMed * 0.3)),
            ("Innovation Score", C(alpha * 40 + gamma * 40 + div * 20)),
            ("Learning Acceleration Score", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Cross-Domain Potential Score", C(activeDomains * 8 + div * 30 + alpha * 20)),
        };
    }

    /// <summary>The five skill-transfer scores. Order: Technical, Analytical, Creative, Research, Innovation.</summary>
    public static IReadOnlyList<(string Name, double Value)> SkillTransferScores(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Technical Transfer", C(beta * 50 + avgAtt * 0.3 + div * 10)),
            ("Analytical Transfer", C(beta * 60 + avgAtt * 0.3)),
            ("Creative Transfer", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Research Transfer", C(div * 60 + theta * 30 + avgAtt * 0.1)),
            ("Innovation Transfer", C(alpha * 40 + gamma * 40 + div * 20)),
        };
    }

    public static string CognitiveAdaptationCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("metric,score\n");
        sb.AppendLine($"Adaptability,{C(alpha * 40 + div * 30 + m * 0.3):0.0}");
        sb.AppendLine($"Generalization Ability,{C(div * 50 + alpha * 30 + a * 0.2):0.0}");
        sb.AppendLine($"Knowledge Flexibility,{C(div * 40 + alpha * 40):0.0}");
        sb.AppendLine($"Learning Efficiency,{C(a * 0.4 + beta * 30 + div * 20):0.0}");
        sb.AppendLine($"Conceptual Transfer Ability,{C(div * 50 + beta * 30 + alpha * 20):0.0}");
        return sb.ToString();
    }

    private static readonly string[] AiConcepts =
        { "data", "model", "system", "logic", "plan", "design", "build", "learn", "optimize", "process" };

    public static string HumanAiTransferCsv(IReadOnlyList<string> words)
    {
        var distinct = new HashSet<string>(
            words.Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0 && x != "—"),
            StringComparer.OrdinalIgnoreCase);
        int shared = AiConcepts.Count(distinct.Contains);
        int unique = distinct.Count(d => !AiConcepts.Contains(d, StringComparer.OrdinalIgnoreCase));
        var sb = new System.Text.StringBuilder("aspect,value\n");
        sb.AppendLine($"Shared Concepts,{shared}");
        sb.AppendLine($"Unique Concepts,{unique}");
        sb.AppendLine($"Transfer Opportunities,{Math.Max(shared + unique / 2, 1)}");
        return sb.ToString();
    }

    public static string LearningEvolutionCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int priorSessions)
    {
        string status = priorSessions > 0 ? "tracked" : "baseline";
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("metric,value,status,sessions\n");
        sb.AppendLine($"Skill Growth,{C(beta * 40 + div * 30 + a * 0.2):0.0},{status},{priorSessions + 1}");
        sb.AppendLine($"Knowledge Growth,{C(a * 0.4 + div * 40):0.0},{status},{priorSessions + 1}");
        sb.AppendLine($"Transfer Ability Growth,{C(div * 50 + beta * 30):0.0},{status},{priorSessions + 1}");
        sb.AppendLine($"Adaptability Growth,{C(alpha * 40 + div * 30 + m * 0.3):0.0},{status},{priorSessions + 1}");
        return sb.ToString();
    }

    // ---- transfer learning memory database ----

    public static string HistoryHeader() =>
        "date,distinct_concepts,knowledge,transfer,adaptability,innovation,learning_acceleration,cross_domain,top_domain";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int activeDomains, string topDomain)
    {
        var d = Dashboard(a, m, b, w, activeDomains);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{Distinct(w)},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{topDomain}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int activeDomains, string topDomain)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, activeDomains, topDomain));
    }
}
