using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Superintelligence score-sets from EEG averages, band shares and word diversity.
/// Self-contained (no StrongAi dependency). All scores 0–100.
/// </summary>
public static class SuperintelligenceProfile
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

    /// <summary>The six headline dashboard scores (0–100). Order: Cognitive Capability, Innovation, Knowledge Integration, Learning, Systems Thinking, Research Potential.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Cognitive Capability", C(avgAtt * 0.4 + beta * 30 + div * 20)),
            ("Innovation", C(alpha * 40 + gamma * 40 + div * 20)),
            ("Knowledge Integration", C(div * 50 + beta * 30 + avgAtt * 0.2)),
            ("Learning", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Systems Thinking", C(alpha * 40 + beta * 40 + div * 20)),
            ("Research Potential", C(div * 60 + theta * 30 + avgAtt * 0.1)),
        };
    }

    /// <summary>The ten cognitive capabilities (0–100).</summary>
    public static IReadOnlyList<(string Capability, double Value)> Capabilities(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Logical Reasoning", C(beta * 70 + avgAtt * 0.3)),
            ("Analytical Thinking", C(beta * 60 + avgAtt * 0.3 + div * 10)),
            ("Creativity", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Curiosity", C(div * 50 + theta * 30 + gamma * 20)),
            ("Learning Capacity", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Adaptability", C(alpha * 40 + div * 30 + avgMed * 0.3)),
            ("Strategic Thinking", C(avgAtt * 0.4 + alpha * 30 + beta * 20)),
            ("Innovation Potential", C(alpha * 40 + gamma * 40 + div * 20)),
            ("Problem Solving", C(beta * 60 + avgAtt * 0.3 + div * 10)),
            ("Systems Thinking", C(alpha * 40 + beta * 40 + div * 20)),
        };
    }

    public static string CapabilitiesCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var sb = new System.Text.StringBuilder("capability,score\n");
        foreach (var (cap, value) in Capabilities(a, m, b, w)) sb.AppendLine($"{cap},{value:0.0}");
        return sb.ToString();
    }

    public static string KnowledgeIntegrationCsv(
        double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, IReadOnlyList<SuperDomainScore> domains)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        int distinct = Distinct(w);
        int activeDomains = domains.Count(d => d.Count > 0);
        double breadth = C(activeDomains * 10 + div * 40);
        double depth = C((1 - div) * 40 + beta * 40 + a * 0.2);
        double interdisciplinary = C(activeDomains * 8 + alpha * 30 + div * 20);
        double scientific = C(beta * 50 + a * 0.3 + div * 10);
        double engineering = C(beta * 45 + gamma * 25 + a * 0.2);
        double technical = C(beta * 40 + div * 30 + a * 0.2);
        var sb = new System.Text.StringBuilder("metric,score\n");
        sb.AppendLine($"Breadth,{breadth:0.0}");
        sb.AppendLine($"Depth,{depth:0.0}");
        sb.AppendLine($"Interdisciplinary Connections,{interdisciplinary:0.0}");
        sb.AppendLine($"Scientific Interest,{scientific:0.0}");
        sb.AppendLine($"Engineering Interest,{engineering:0.0}");
        sb.AppendLine($"Technical Interest,{technical:0.0}");
        return sb.ToString();
    }

    public static string InnovationCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("metric,score\n");
        sb.AppendLine($"Novelty,{C(gamma * 50 + div * 40 + alpha * 10):0.0}");
        sb.AppendLine($"Idea Generation,{C(alpha * 40 + div * 40 + gamma * 20):0.0}");
        sb.AppendLine($"Concept Combination,{C(div * 50 + alpha * 30 + beta * 20):0.0}");
        sb.AppendLine($"Research Potential,{C(div * 60 + theta * 30 + a * 0.1):0.0}");
        sb.AppendLine($"Inventiveness,{C(gamma * 45 + alpha * 35 + div * 20):0.0}");
        return sb.ToString();
    }

    public static string DomainProfileCsv(IReadOnlyList<SuperDomainScore> domains)
    {
        var sb = new System.Text.StringBuilder("domain,engagement,percent\n");
        foreach (var d in domains)
        {
            string engagement = d.Percent >= 20 ? "High" : d.Percent >= 8 ? "Medium" : "Low";
            sb.AppendLine($"{d.Domain},{engagement},{d.Percent:0.0}");
        }
        if (domains.Count == 0) sb.AppendLine("General,Medium,100.0");
        return sb.ToString();
    }

    // ---- expert profile comparison ----

    private static readonly (string Profile, string[] Keywords)[] ExpertProfiles =
    {
        ("Research Scientist", new[] { "science", "research", "experiment", "theory", "data", "analysis", "hypothesis", "study", "physics", "result" }),
        ("Systems Engineer", new[] { "system", "engineer", "design", "build", "structure", "integrate", "process", "control", "machine", "power" }),
        ("AI Researcher", new[] { "ai", "model", "learn", "neural", "data", "algorithm", "intelligence", "network", "train", "reasoning" }),
        ("Inventor", new[] { "invent", "idea", "create", "novel", "design", "build", "prototype", "concept", "innovation", "tool" }),
        ("Entrepreneur", new[] { "business", "market", "money", "value", "growth", "customer", "product", "strategy", "plan", "trade" }),
        ("Architect", new[] { "design", "build", "structure", "space", "form", "plan", "model", "system", "house", "city" }),
    };

    public static string ExpertComparisonCsv(IReadOnlyList<string> words)
    {
        var distinct = new HashSet<string>(
            words.Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0 && x != "—"),
            StringComparer.OrdinalIgnoreCase);
        var sb = new System.Text.StringBuilder("profile,match_percent,top_overlap\n");
        foreach (var (profile, keywords) in ExpertProfiles)
        {
            var overlap = keywords.Where(distinct.Contains).ToList();
            double match = C(100.0 * overlap.Count / keywords.Length);
            string top = overlap.Count > 0 ? string.Join(" ", overlap.Take(3)) : "general alignment";
            sb.AppendLine($"{profile},{match:0.0},{top}");
        }
        return sb.ToString();
    }

    // ---- historical development tracking ----

    public static string HistoryHeader() =>
        "date,cognitive_capability,innovation,knowledge_integration,learning,systems_thinking,research_potential,distinct_concepts,top_domain";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topDomain)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{Distinct(w)},{topDomain}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topDomain)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topDomain));
    }
}
