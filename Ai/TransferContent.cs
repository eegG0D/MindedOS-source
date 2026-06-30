using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Transfer Learning content: the knowledge profile, transfer map, concept
/// graph, cross-domain expansion, learning acceleration, career analysis, project ideas, the preview
/// scorecard, and fallbacks for the LM artifacts (four narratives, a report and a 10-slide deck).
/// Self-contained; reuses only <see cref="NlpContent"/>.
/// </summary>
public static class TransferContent
{
    // Predefined transfer pairs among the 12 domains.
    private static readonly (string Source, string Target)[] TransferPairs =
    {
        ("Programming", "Robotics"),
        ("Engineering", "Architecture"),
        ("Mathematics", "Artificial Intelligence"),
        ("Artificial Intelligence", "Healthcare"),
        ("Physics", "Engineering"),
        ("Business", "Economics"),
    };

    private static List<string> Concepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "concept", "skill", "idea", "method", "model", "pattern" };
    }

    private static Dictionary<string, double> PercentMap(IReadOnlyList<TransferDomainScore> domains) =>
        domains.ToDictionary(d => d.Domain, d => d.Percent, StringComparer.OrdinalIgnoreCase);

    // ---- knowledge profile ----

    public static string KnowledgeProfileCsv(IReadOnlyList<string> words, IReadOnlyList<TransferDomainScore> domains, TransferDomains domainObj)
    {
        var concepts = Concepts(words, 8);
        var sb = new StringBuilder("aspect,detail,score\n");
        foreach (var c in concepts.Take(4)) sb.AppendLine($"Skill,{c},{System.Math.Clamp(85 - concepts.IndexOf(c) * 6, 30, 90)}");
        foreach (var c in concepts.Take(3)) sb.AppendLine($"Concept,{c},{System.Math.Clamp(80 - concepts.IndexOf(c) * 5, 30, 88)}");
        foreach (var d in domains.Where(d => d.Count > 0).Take(3)) sb.AppendLine($"Knowledge Domain,{d.Domain},{d.Percent:0.0}");
        if (domains.All(d => d.Count == 0) && domains.Count > 0) sb.AppendLine($"Knowledge Domain,{domains[0].Domain},{domains[0].Percent:0.0}");
        foreach (var c in concepts.Skip(2).Take(2)) sb.AppendLine($"Interest,{c},70");
        sb.AppendLine($"Expertise Indicator,{(domains.Count > 0 ? domains[0].Domain : "General")},{(domains.Count > 0 ? domains[0].Percent : 50):0.0}");
        sb.AppendLine($"Learning Pattern,concept diversity,{System.Math.Clamp(concepts.Count * 8, 20, 95)}");
        return sb.ToString();
    }

    // ---- transfer learning map ----

    public static string TransferLearningMapCsv(IReadOnlyList<TransferDomainScore> domains)
    {
        var pct = PercentMap(domains);
        double P(string d) => pct.TryGetValue(d, out var v) ? v : 0;
        var sb = new StringBuilder("source_domain,target_domain,transfer_score\n");
        foreach (var (src, tgt) in TransferPairs)
        {
            double score = System.Math.Clamp(40 + (P(src) + P(tgt)) / 2, 0, 100);
            sb.AppendLine($"{src},{tgt},{score:0.0}");
        }
        return sb.ToString();
    }

    // ---- concept transfer graph ----

    public static string ConceptTransferGraphMd(IReadOnlyList<string> words, IReadOnlyList<TransferDomainScore> domains)
    {
        var concepts = Concepts(words, 6);
        var doms = domains.Take(4).Select(d => d.Domain).ToList();
        if (doms.Count == 0) doms.Add("General");
        var sb = new StringBuilder();
        sb.AppendLine("# Concept Transfer Graph");
        sb.AppendLine();
        sb.AppendLine("## Existing Concepts");
        foreach (var c in concepts.Take(4)) sb.AppendLine($"- {c}");
        sb.AppendLine();
        sb.AppendLine("## New Concepts");
        foreach (var d in doms) sb.AppendLine($"- applications in {d}");
        sb.AppendLine();
        sb.AppendLine("## Similar Concepts");
        for (int i = 0; i + 1 < concepts.Count; i++) sb.AppendLine($"- **{concepts[i]}** ~ **{concepts[i + 1]}**");
        sb.AppendLine();
        sb.AppendLine("## Complementary Concepts");
        sb.AppendLine($"- {string.Join(" + ", concepts.Take(3))} → a combined capability");
        return sb.ToString();
    }

    // ---- multi-domain expansion ----

    public static string CrossDomainExpansionCsv(IReadOnlyList<TransferDomainScore> domains, IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 6);
        var sb = new StringBuilder("domain,application,potential\n");
        for (int i = 0; i < domains.Count; i++)
        {
            string concept = concepts[i % concepts.Count];
            double potential = System.Math.Clamp(40 + domains[i].Percent, 0, 100);
            sb.AppendLine($"{domains[i].Domain},apply '{concept}' to {domains[i].Domain.ToLowerInvariant()},{potential:0.0}");
        }
        return sb.ToString();
    }

    // ---- learning acceleration ----

    public static string LearningAccelerationCsv(IReadOnlyList<TransferDomainScore> domains)
    {
        var sb = new StringBuilder("subject,acceleration,reason\n");
        foreach (var d in domains.OrderByDescending(d => d.Percent))
        {
            string accel = d.Percent >= 20 ? "Fast" : d.Percent >= 8 ? "Moderate" : "Standard";
            string reason = d.Percent >= 8 ? "aligns with current concepts" : "partial foundation";
            sb.AppendLine($"{d.Domain},{accel},{reason}");
        }
        return sb.ToString();
    }

    // ---- career transfer ----

    public static string CareerTransferCsv(IReadOnlyList<TransferDomainScore> domains)
    {
        string[] buckets = { "Best Match", "Best Match", "Emerging", "Future", "Research" };
        var sb = new StringBuilder("career,category,match\n");
        var ranked = domains.OrderByDescending(d => d.Percent).ToList();
        for (int i = 0; i < ranked.Count; i++)
        {
            string bucket = buckets[System.Math.Min(i, buckets.Length - 1)];
            sb.AppendLine($"{ranked[i].Domain} specialist,{bucket},{ranked[i].Percent:0.0}");
        }
        return sb.ToString();
    }

    // ---- project generator ----

    public static string TransferProjectsMd(IReadOnlyList<string> words, IReadOnlyList<TransferDomainScore> domains)
    {
        var concepts = Concepts(words, 5);
        var sb = new StringBuilder();
        sb.AppendLine("# Transfer Projects");
        sb.AppendLine();
        int count = System.Math.Clamp(concepts.Count, 3, 5);
        for (int i = 0; i < count; i++)
        {
            string concept = concepts[i];
            string domain = i < domains.Count ? domains[i].Domain : "General";
            sb.AppendLine($"## Project {i + 1}: {concept} → {domain}");
            sb.AppendLine($"- **Title:** Applying {concept} in {domain}");
            sb.AppendLine($"- **Description:** Transfer the recurring concept '{concept}' into a {domain.ToLowerInvariant()} project.");
            sb.AppendLine($"- **Objectives:** prove the concept, build a prototype, measure results.");
            sb.AppendLine($"- **Required Skills:** {string.Join(", ", concepts.Take(3))}");
            sb.AppendLine($"- **Expected Outcomes:** a working {domain.ToLowerInvariant()} artifact and a reusable method.");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // ---- preview scorecard ----

    public static string Scorecard(IReadOnlyList<(string Score, double Value)> dashboard, string topDomain)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TRANSFER LEARNING DASHBOARD");
        sb.AppendLine("===========================");
        sb.AppendLine($"Strongest domain: {topDomain}");
        foreach (var (name, value) in dashboard)
        {
            int filled = (int)System.Math.Round(value / 5.0);
            string bar = new string('█', System.Math.Clamp(filled, 0, 20)) + new string('░', System.Math.Clamp(20 - filled, 0, 20));
            sb.AppendLine($"{name,-30} {bar} {value:0}");
        }
        return sb.ToString();
    }

    // ---- LM fallbacks: narratives ----

    public static string DefaultSkillTransfer(IReadOnlyList<(string Name, double Value)> skillScores, IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("SKILL TRANSFER REPORT");
        sb.AppendLine("=====================");
        foreach (var (name, value) in skillScores) sb.AppendLine($"{name}: {value:0}");
        sb.AppendLine();
        sb.AppendLine($"Existing strengths: {string.Join(", ", concepts)}.");
        sb.AppendLine("Transferable skills: analytical and technical methods carry across domains.");
        sb.AppendLine("Hidden competencies: pattern recognition applies beyond its original context.");
        sb.AppendLine("Cross-domain abilities: the recurring concepts bridge multiple fields.");
        return sb.ToString();
    }

    public static string DefaultKnowledgeReuse(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("KNOWLEDGE REUSE REPORT");
        sb.AppendLine("======================");
        sb.AppendLine($"Reusable concepts: {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine("Reusable problem-solving methods: decompose, prototype, iterate.");
        sb.AppendLine("Reusable mental models: systems thinking and abstraction.");
        sb.AppendLine("Reusable learning strategies: spaced practice and active recall.");
        return sb.ToString();
    }

    public static string DefaultResearchTransfer(IReadOnlyList<TransferDomainScore> domains, IReadOnlyList<string> words)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var concepts = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("RESEARCH TRANSFER OPPORTUNITIES");
        sb.AppendLine("===============================");
        sb.AppendLine($"New research topics: deeper study of {concepts[0]} within {top}.");
        sb.AppendLine($"Interdisciplinary studies: combine {top} with an adjacent domain.");
        sb.AppendLine("Technology applications: turn the concepts into a tool or system.");
        sb.AppendLine("Engineering applications: prototype the strongest idea.");
        return sb.ToString();
    }

    public static string DefaultInnovationTransfer(IReadOnlyList<TransferDomainScore> domains, IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("INNOVATION TRANSFER REPORT");
        sb.AppendLine("==========================");
        sb.AppendLine($"New inventions: a tool built around {concepts[0]}.");
        sb.AppendLine("New technologies: a system that scales the recurring concepts.");
        sb.AppendLine("New products: a focused offering for the leading domain.");
        sb.AppendLine("New scientific theories: a hypothesis linking the dominant themes.");
        sb.AppendLine("New engineering projects: a concrete build in the strongest area.");
        return sb.ToString();
    }

    // ---- LM fallback: research report (.docx) ----

    public static string DefaultReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<TransferDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "General";
        var concepts = Concepts(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Transfer Learning Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A transfer-learning analysis from a 3-minute EEG. Strongest domain: {top}; knowledge {dashboard[0].Value:0}, transfer {dashboard[1].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Knowledge Profile");
        sb.AppendLine($"Recurring concepts {string.Join(", ", concepts.Take(3))}; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Transfer Analysis");
        sb.AppendLine($"Knowledge transfers across {string.Join(", ", domains.Take(3).Select(d => d.Domain))}; adaptability {dashboard[2].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Innovation Opportunities");
        sb.AppendLine($"Innovation {dashboard[3].Value:0}; the concepts seed new inventions, products and engineering projects.");
        sb.AppendLine();
        sb.AppendLine("## Research Opportunities");
        sb.AppendLine($"Interdisciplinary studies combine {top} with adjacent fields; learning acceleration {dashboard[4].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine("Reuse the strongest methods, target fast-to-learn adjacent subjects, and prototype a transfer project.");
        return sb.ToString();
    }

    // ---- LM fallback: 10-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<TransferDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = Concepts(words, 3);
        string Dom(int i) => i < domains.Count ? $"{domains[i].Domain} ({domains[i].Percent:0}%)" : "—";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", $"Recurring: {string.Join(", ", concepts)}" }),
            new("Knowledge Profile", new[] { $"Knowledge {dashboard[0].Value:0}", $"Strongest: {Dom(0)}" }),
            new("Transfer Mapping", new[] { "Programming → Robotics", "Mathematics → AI, AI → Healthcare" }),
            new("Skill Transfer", new[] { $"Transfer {dashboard[1].Value:0}", "Technical, analytical, creative, research" }),
            new("Cognitive Adaptation", new[] { $"Adaptability {dashboard[2].Value:0}", "Generalization & flexibility" }),
            new("Innovation Opportunities", new[] { $"Innovation {dashboard[3].Value:0}", "Inventions, technologies, products" }),
            new("Career Analysis", new[] { Dom(0), Dom(1), "Best-match & emerging careers" }),
            new("Future Learning Paths", new[] { $"Acceleration {dashboard[4].Value:0}", "Fast-to-learn adjacent subjects" }),
            new("Conclusions", new[] { "EEG → transferable knowledge", "Bridges between domains" }),
        };
    }
}
