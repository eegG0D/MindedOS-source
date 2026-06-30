using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Superintelligence content: the 100 grand challenges, the knowledge
/// graph, the preview scorecard, and fallbacks for the LM artifacts (four narratives, the research
/// council, the research report and the 12-slide deck). Self-contained; reuses only <see cref="NlpContent"/>.
/// </summary>
public static class SuperintelligenceContent
{
    private static readonly string[] Council =
        { "Scientist", "Engineer", "Architect", "Economist", "Researcher", "Roboticist", "Programmer", "Educator", "Analyst", "Inventor" };

    private static readonly string[] ChallengeCategories =
        { "Science", "Technology", "Engineering", "AI", "Space", "Healthcare", "Education", "Environment" };

    private static List<string> Concepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "knowledge", "reasoning", "discovery", "systems" };
    }

    /// <summary>Exactly 100 numbered research challenges across the 8 categories, seeded by EEG concepts.</summary>
    public static string GrandChallenges(IReadOnlyList<string> words, IReadOnlyList<SuperDomainScore> domains)
    {
        var concepts = Concepts(words, 12);
        string topDomain = domains.Count > 0 ? domains[0].Domain : "your field";
        string[] templates =
        {
            "Design a breakthrough in {cat} that advances {concept}.",
            "Build a system that connects {concept} with {domain} to solve a {cat} problem.",
            "Discover a new principle of {concept} with applications in {cat}.",
            "Create an open dataset and method to study {concept} for {cat}.",
            "Invent a tool that scales {concept} across {cat}.",
            "Model how {concept} reshapes the future of {cat}.",
            "Reduce the cost of {cat} progress by rethinking {concept}.",
            "Unify {concept} and {domain} into a single {cat} framework.",
        };
        var sb = new StringBuilder();
        sb.AppendLine("GRAND CHALLENGES — 100 research challenges inspired by your EEG concepts");
        sb.AppendLine("======================================================================");
        for (int i = 0; i < 100; i++)
        {
            string cat = ChallengeCategories[i % ChallengeCategories.Length];
            string concept = concepts[i % concepts.Count];
            string tmpl = templates[i % templates.Length];
            string text = tmpl.Replace("{cat}", cat).Replace("{concept}", concept).Replace("{domain}", topDomain);
            sb.AppendLine($"{i + 1}. [{cat}] {text}");
        }
        return sb.ToString();
    }

    public static string KnowledgeGraphMd(IReadOnlyList<string> words, IReadOnlyList<SuperDomainScore> domains)
    {
        var concepts = Concepts(words, 6);
        var doms = domains.Take(5).Select(d => d.Domain).ToList();
        if (doms.Count == 0) doms.Add("General");
        var sb = new StringBuilder();
        sb.AppendLine("# Superintelligence Knowledge Graph");
        sb.AppendLine();
        sb.AppendLine("## Concepts");
        foreach (var c in concepts) sb.AppendLine($"- {c}");
        sb.AppendLine();
        sb.AppendLine("## Topics");
        foreach (var d in doms) sb.AppendLine($"- {d}");
        sb.AppendLine();
        sb.AppendLine("## Relationships");
        for (int i = 0; i < doms.Count; i++)
        {
            string next = i + 1 < doms.Count ? doms[i + 1] : "Synthesis";
            sb.AppendLine($"- **{doms[i]}** → relates to → **{next}**");
        }
        sb.AppendLine();
        sb.AppendLine("## Dependencies");
        for (int i = 0; i + 1 < concepts.Count && i < 5; i++)
            sb.AppendLine($"- **{concepts[i]}** depends on → **{concepts[i + 1]}**");
        sb.AppendLine();
        sb.AppendLine("## Opportunities");
        foreach (var d in doms) sb.AppendLine($"- Deepen **{d}** to open new research directions.");
        return sb.ToString();
    }

    /// <summary>The in-app preview scorecard (the six headline scores with bar indicators).</summary>
    public static string Scorecard(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SUPERINTELLIGENCE DASHBOARD");
        sb.AppendLine("===========================");
        foreach (var (name, value) in dashboard)
        {
            int filled = (int)Math.Round(value / 5.0); // 0..20
            string bar = new string('█', Math.Clamp(filled, 0, 20)) + new string('░', Math.Clamp(20 - filled, 0, 20));
            sb.AppendLine($"{name,-24} {bar} {value:0}");
        }
        return sb.ToString();
    }

    // ---- LM fallbacks: narratives ----

    public static string DefaultProblemSolving(IReadOnlyList<SuperDomainScore> domains, IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 3);
        string focus = concepts[0];
        var sb = new StringBuilder();
        sb.AppendLine("PROBLEM SOLVING REPORT");
        sb.AppendLine("======================");
        foreach (var dom in new[] { "Science", "Engineering", "Robotics", "Artificial Intelligence", "Architecture", "Mathematics", "Business" })
        {
            sb.AppendLine($"## {dom}");
            sb.AppendLine($"Challenge: apply {focus} within {dom.ToLowerInvariant()}. Approach: an iterative, measurable plan that combines the recurring concepts into a concrete solution.");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string DefaultSystemsThinking(IReadOnlyList<SuperDomainScore> domains, IReadOnlyList<string> words)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "the focus";
        var concepts = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("SYSTEMS THINKING REPORT");
        sb.AppendLine("=======================");
        sb.AppendLine($"Cause and effect: changes in {concepts[0]} propagate to {top} outcomes.");
        sb.AppendLine($"Network thinking: {string.Join(", ", concepts)} form an interconnected web of ideas.");
        sb.AppendLine("Interconnected concepts: the recurring themes reinforce one another across domains.");
        sb.AppendLine($"Hierarchical reasoning: high-level goals in {top} decompose into concrete sub-problems.");
        sb.AppendLine("Complex system understanding: feedback loops and dependencies shape the whole.");
        return sb.ToString();
    }

    public static string DefaultFutureKnowledge(IReadOnlyList<SuperDomainScore> domains)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("FUTURE KNOWLEDGE SIMULATION");
        sb.AppendLine("===========================");
        sb.AppendLine("## 1-Year Horizon");
        sb.AppendLine($"Skills acquired: foundations of {top}. Knowledge growth: steady. Research opportunities: a first focused project.");
        sb.AppendLine();
        sb.AppendLine("## 5-Year Horizon");
        sb.AppendLine($"Skills acquired: advanced {top} plus an adjacent domain. Knowledge growth: compounding. Research opportunities: original contributions.");
        sb.AppendLine();
        sb.AppendLine("## 10-Year Horizon");
        sb.AppendLine($"Skills acquired: mastery and cross-domain synthesis. Knowledge growth: leadership-level. Research opportunities: define new directions in {top}.");
        return sb.ToString();
    }

    public static string DefaultGrowthRecommendations(IReadOnlyList<SuperDomainScore> domains)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("COGNITIVE GROWTH RECOMMENDATIONS");
        sb.AppendLine("================================");
        sb.AppendLine($"Learning efficiency: use spaced repetition and active recall on {top} fundamentals.");
        sb.AppendLine("Critical thinking: practice steelmanning opposing views before deciding.");
        sb.AppendLine("Research methodology: state a hypothesis, design a small test, record results.");
        sb.AppendLine("Creativity: combine two unrelated concepts daily and sketch the result.");
        sb.AppendLine("Scientific reasoning: quantify claims and seek disconfirming evidence.");
        sb.AppendLine("Engineering design: prototype early, iterate on measurable feedback.");
        return sb.ToString();
    }

    public static string DefaultResearchCouncil(IReadOnlyList<SuperDomainScore> domains, IReadOnlyList<string> words)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "the focus";
        var concepts = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("# Artificial Research Council");
        sb.AppendLine();
        sb.AppendLine($"Ten specialist agents analyze the translated EEG concepts ({string.Join(", ", concepts.Take(2))}) and contribute recommendations.");
        sb.AppendLine();
        foreach (var agent in Council)
            sb.AppendLine($"- **{agent}:** offers a {agent.ToLowerInvariant()} perspective on {top}, recommending a concrete next step grounded in the recurring concepts.");
        sb.AppendLine();
        sb.AppendLine("The council converges on a shared roadmap toward higher knowledge, creativity and innovation.");
        return sb.ToString();
    }

    // ---- LM fallback: research report (.docx) ----

    public static string DefaultResearchReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<SuperDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "General";
        var concepts = Concepts(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Superintelligence Research Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A Superintelligence research profile from a 3-minute EEG. Leading domain: {top}; recurring concepts {string.Join(", ", concepts.Take(3))}. Cognitive capability {dashboard[0].Value:0}, innovation {dashboard[1].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Cognitive Analysis");
        sb.AppendLine($"Cognitive capability {dashboard[0].Value:0}; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}. Reasoning, analysis and systems thinking are exercised together.");
        sb.AppendLine();
        sb.AppendLine("## Innovation Analysis");
        sb.AppendLine($"Innovation {dashboard[1].Value:0}; novel combinations emerge from the recurring concepts, with strong potential for invention and research.");
        sb.AppendLine();
        sb.AppendLine("## Knowledge Integration Analysis");
        sb.AppendLine($"Knowledge integration {dashboard[2].Value:0}; concepts connect across {string.Join(", ", domains.Take(3).Select(d => d.Domain))}.");
        sb.AppendLine();
        sb.AppendLine("## Future Simulations");
        sb.AppendLine($"Projected 1/5/10-year paths deepen {top} and adjacent domains, compounding skills and research opportunities.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine("Pursue focused projects, combine concepts deliberately, and validate ideas with small experiments.");
        return sb.ToString();
    }

    // ---- LM fallback: 12-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<SuperDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Domain(int i) => i < domains.Count ? $"{domains[i].Domain} ({domains[i].Percent:0}%)" : "—";
        var concepts = Concepts(words, 3);
        string top = domains.Count > 0 ? domains[0].Domain : "General";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", $"Recurring: {string.Join(", ", concepts)}" }),
            new("Cognitive Capabilities", new[] { $"Cognitive capability {dashboard[0].Value:0}", $"Learning {dashboard[3].Value:0}" }),
            new("Knowledge Integration", new[] { $"Integration {dashboard[2].Value:0}", $"Across {Domain(0)}, {Domain(1)}" }),
            new("Innovation Assessment", new[] { $"Innovation {dashboard[1].Value:0}", "Novelty, idea generation, inventiveness" }),
            new("Problem Solving Analysis", new[] { "Science, Engineering, Robotics, AI", "Architecture, Mathematics, Business" }),
            new("Systems Thinking", new[] { $"Systems thinking {dashboard[4].Value:0}", "Cause-effect, networks, hierarchy" }),
            new("Research Council Results", new[] { "Scientist, Engineer, Architect, Economist…", "10 specialist agents" }),
            new("Future Simulations", new[] { $"1 / 5 / 10-year paths in {top}", "Skills, knowledge, opportunities" }),
            new("Expert Comparisons", new[] { "Research Scientist, Systems Engineer", "AI Researcher, Inventor, Entrepreneur, Architect" }),
            new("Recommendations", new[] { "Learning efficiency & research method", "Creativity & engineering design" }),
            new("Conclusions", new[] { "EEG → measurable cognitive profile", "Pathways toward higher knowledge & innovation" }),
        };
    }
}
