using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>A virtual swarm agent derived from a decoded concept.</summary>
public sealed record SwarmAgent(string Id, string Concept, string Domain, double Confidence, double Influence);

/// <summary>
/// Deterministic, offline-safe Swarm Intelligence content: virtual agents, the interaction network,
/// idea colony, knowledge swarm, roles, the knowledge ecosystem, the global noosphere, the preview
/// scorecard, and fallbacks for the LM artifacts (four narratives, two reports and a 10-slide deck).
/// Self-contained; reuses only <see cref="NlpContent"/>.
/// </summary>
public static class SwarmContent
{
    private static readonly string[] ColonyTypes =
        { "Worker", "Explorer", "Inventor", "Research", "Builder", "Analyst" };

    private static readonly string[] LeadershipRoles =
        { "Leader", "Coordinator", "Explorer", "Specialist", "Supporter" };

    private static List<string> Concepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "idea", "concept", "signal", "pattern", "goal", "system" };
    }

    // ---- agents + network ----

    public static IReadOnlyList<SwarmAgent> BuildAgents(IReadOnlyList<string> words, SwarmDomains domains, int max = 12)
    {
        var concepts = Concepts(words, max);
        var agents = new List<SwarmAgent>();
        for (int i = 0; i < concepts.Count; i++)
        {
            double confidence = Math.Clamp(95 - i * 5, 30, 95);
            double influence = Math.Clamp(100 - i * 7, 20, 100);
            agents.Add(new SwarmAgent($"A{i + 1}", concepts[i], domains.DomainOf(concepts[i]), confidence, influence));
        }
        return agents;
    }

    public static string SwarmAgentsCsv(IReadOnlyList<SwarmAgent> agents)
    {
        var sb = new StringBuilder("agent_id,concept,priority,confidence,knowledge_domain,influence_score\n");
        for (int i = 0; i < agents.Count; i++)
        {
            string priority = i == 0 ? "High" : i < 4 ? "Medium" : "Low";
            var ag = agents[i];
            sb.AppendLine($"{ag.Id},{ag.Concept},{priority},{ag.Confidence:0.0},{ag.Domain},{ag.Influence:0.0}");
        }
        if (agents.Count == 0) sb.AppendLine("A1,idea,High,80.0,Generalist,80.0");
        return sb.ToString();
    }

    public static string SwarmNetworkCsv(IReadOnlyList<SwarmAgent> agents)
    {
        string[] interactions = { "cooperation", "competition", "consensus", "information_sharing", "collective_learning" };
        var sb = new StringBuilder("source_agent,target_agent,interaction,weight\n");
        for (int i = 0; i < agents.Count; i++)
        {
            var src = agents[i];
            var dst = agents[(i + 1) % Math.Max(agents.Count, 1)];
            if (agents.Count < 2) break;
            string kind = interactions[i % interactions.Length];
            double weight = Math.Clamp((src.Influence + dst.Influence) / 2 - i, 10, 100);
            sb.AppendLine($"{src.Id},{dst.Id},{kind},{weight:0.0}");
        }
        if (agents.Count < 2) sb.AppendLine("A1,A1,cooperation,50.0");
        return sb.ToString();
    }

    // ---- idea colony ----

    public static string IdeaColonyCsv(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 12);
        var sb = new StringBuilder("concept,agent_type,generation,fitness\n");
        for (int i = 0; i < concepts.Count; i++)
        {
            string type = ColonyTypes[i % ColonyTypes.Length];
            int generation = 1 + (i % 3);
            double fitness = Math.Clamp(90 - i * 4, 20, 90);
            sb.AppendLine($"{concepts[i]},{type},{generation},{fitness:0.0}");
        }
        return sb.ToString();
    }

    // ---- knowledge swarm ----

    public static string KnowledgeSwarmCsv(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 10);
        string[] roles = { "Dominant Idea", "Influential Concept", "Knowledge Hub", "Information Bottleneck", "Emerging Discovery" };
        var sb = new StringBuilder("concept,role,score\n");
        for (int i = 0; i < concepts.Count; i++)
        {
            string role = roles[i % roles.Length];
            double score = Math.Clamp(95 - i * 6, 20, 95);
            sb.AppendLine($"{concepts[i]},{role},{score:0.0}");
        }
        return sb.ToString();
    }

    // ---- swarm roles (leadership) ----

    public static string SwarmRolesCsv(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 12);
        var sb = new StringBuilder("concept,role\n");
        for (int i = 0; i < concepts.Count; i++)
            sb.AppendLine($"{concepts[i]},{LeadershipRoles[i % LeadershipRoles.Length]}");
        return sb.ToString();
    }

    // ---- knowledge ecosystem ----

    public static string KnowledgeEcosystemMd(IReadOnlyList<string> words, IReadOnlyList<SwarmDomainScore> domains)
    {
        var concepts = Concepts(words, 6);
        var doms = domains.Take(5).Select(d => d.Domain).ToList();
        if (doms.Count == 0) doms.Add("Generalist");
        var sb = new StringBuilder();
        sb.AppendLine("# Swarm Knowledge Ecosystem");
        sb.AppendLine();
        sb.AppendLine("## Idea Relationships");
        for (int i = 0; i + 1 < concepts.Count; i++)
            sb.AppendLine($"- **{concepts[i]}** ↔ **{concepts[i + 1]}**");
        sb.AppendLine();
        sb.AppendLine("## Knowledge Pathways");
        sb.AppendLine($"- {string.Join(" → ", concepts)}");
        sb.AppendLine();
        sb.AppendLine("## Agent Interactions");
        foreach (var d in doms) sb.AppendLine($"- **{d}** agents share findings with the colony.");
        sb.AppendLine();
        sb.AppendLine("## Discovery Chains");
        sb.AppendLine($"- {string.Join(" ⇒ ", doms)} ⇒ Emergent insight");
        return sb.ToString();
    }

    // ---- global noosphere ----

    public static string GlobalNoosphereCsv(IReadOnlyList<string> words, SwarmDomains domains)
    {
        var concepts = Concepts(words, 16);
        var sb = new StringBuilder("concept,assigned_role,influence,community\n");
        for (int i = 0; i < concepts.Count; i++)
        {
            string role = domains.DomainOf(concepts[i]);
            double influence = Math.Clamp(100 - i * 5, 20, 100);
            int community = (i % 4) + 1;
            sb.AppendLine($"{concepts[i]},{role},{influence:0.0},C{community}");
        }
        return sb.ToString();
    }

    // ---- preview scorecard ----

    public static string Scorecard(IReadOnlyList<(string Score, double Value)> dashboard, int agents, string topRole)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SWARM INTELLIGENCE DASHBOARD");
        sb.AppendLine("============================");
        sb.AppendLine($"Agents: {agents}   ·   Dominant role: {topRole}");
        foreach (var (name, value) in dashboard)
        {
            int filled = (int)Math.Round(value / 5.0);
            string bar = new string('█', Math.Clamp(filled, 0, 20)) + new string('░', Math.Clamp(20 - filled, 0, 20));
            sb.AppendLine($"{name,-24} {bar} {value:0}");
        }
        return sb.ToString();
    }

    // ---- LM fallbacks: narratives ----

    public static string DefaultCollectiveIntelligence(IReadOnlyList<string> words, IReadOnlyList<SwarmDomainScore> domains)
    {
        var concepts = Concepts(words, 5);
        string top = domains.Count > 0 ? domains[0].Domain : "the colony";
        var sb = new StringBuilder();
        sb.AppendLine("COLLECTIVE INTELLIGENCE REPORT");
        sb.AppendLine("==============================");
        sb.AppendLine($"Shared concepts: {string.Join(", ", concepts.Take(3))} recur across the swarm.");
        sb.AppendLine($"Common goals: the agents converge toward {top}.");
        sb.AppendLine("Emerging solutions: combining the dominant concepts yields concrete next steps.");
        sb.AppendLine("Knowledge convergence: influence concentrates on a few high-confidence hubs.");
        return sb.ToString();
    }

    public static string DefaultEmergentBehavior(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("EMERGENT BEHAVIOR");
        sb.AppendLine("=================");
        sb.AppendLine($"Unexpected solutions: pairing {concepts[0]} with {(concepts.Count > 1 ? concepts[1] : "a distant idea")} produces a novel approach.");
        sb.AppendLine("Novel combinations: low-frequency concepts recombine into fresh directions.");
        sb.AppendLine("Creative discoveries: the swarm surfaces ideas no single agent held.");
        sb.AppendLine("Emergent innovations: feedback among agents amplifies the strongest concept.");
        return sb.ToString();
    }

    public static string DefaultInnovationSwarm(IReadOnlyList<SwarmDomainScore> domains, IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 4);
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("INNOVATION SWARM REPORT");
        sb.AppendLine("=======================");
        sb.AppendLine($"New inventions: a tool that operationalizes {concepts[0]}.");
        sb.AppendLine($"Research directions: deeper study of {top} and adjacent concepts.");
        sb.AppendLine("Engineering concepts: a system that links the dominant ideas.");
        sb.AppendLine("Business opportunities: a focused product around the recurring theme.");
        sb.AppendLine("AI systems: an agent that automates the strongest workflow.");
        return sb.ToString();
    }

    public static string DefaultSwarmForecast(IReadOnlyList<SwarmDomainScore> domains)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("SWARM FORECAST");
        sb.AppendLine("==============");
        sb.AppendLine($"Future ideas: continued growth around {top}.");
        sb.AppendLine("Emerging interests: adjacent domains begin to attract attention.");
        sb.AppendLine("Research opportunities: open questions cluster near the dominant hubs.");
        sb.AppendLine("Innovation potential: high where diverse concepts overlap.");
        return sb.ToString();
    }

    // ---- LM fallback: distributed problem solving (.docx) ----

    public static string DefaultSolutionMarkdown(IReadOnlyList<SwarmDomainScore> domains, IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 3);
        string focus = concepts[0];
        var sb = new StringBuilder();
        sb.AppendLine("# Swarm Solution Report");
        sb.AppendLine();
        foreach (var dom in new[] { "Engineering", "Robotics", "AI", "Architecture", "Science" })
        {
            sb.AppendLine($"## {dom}");
            sb.AppendLine($"Challenge: apply {focus} within {dom.ToLowerInvariant()}. Swarm solution: agents cooperate, share partial results and converge on an iterative, measurable plan.");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // ---- LM fallback: research paper (.docx) ----

    public static string DefaultResearchMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<SwarmDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand, int agents)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "Generalist";
        var concepts = Concepts(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Swarm Intelligence Research");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A swarm-intelligence analysis from a 3-minute EEG. {agents} agents formed; collective intelligence {dashboard[0].Value:0}, dominant role {top}.");
        sb.AppendLine();
        sb.AppendLine("## Methodology");
        sb.AppendLine($"Decoded concepts ({string.Join(", ", concepts.Take(3))}) became virtual agents that cooperate, compete and share information; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Swarm Analysis");
        sb.AppendLine($"Influence concentrates on high-confidence hubs; diversity {dashboard[3].Value:0}, consensus {dashboard[4].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Results");
        sb.AppendLine($"Innovation {dashboard[2].Value:0}, discovery {dashboard[5].Value:0}; emergent combinations surface from low-frequency concepts.");
        sb.AppendLine();
        sb.AppendLine("## Discussion");
        sb.AppendLine($"Collaboration {dashboard[1].Value:0}; the human swarm is most creative where it overlaps an artificial swarm in the hybrid mode.");
        sb.AppendLine();
        sb.AppendLine("## Conclusions");
        sb.AppendLine($"Pursue the dominant hubs in {top}, recombine distant concepts, and grow the colony with more sessions.");
        return sb.ToString();
    }

    // ---- LM fallback: 10-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<SwarmDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand, int agents)
    {
        var concepts = Concepts(words, 3);
        string Role(int i) => i < domains.Count ? $"{domains[i].Domain} ({domains[i].Percent:0}%)" : "—";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", $"Recurring: {string.Join(", ", concepts)}" }),
            new("Agent Creation", new[] { $"{agents} virtual agents", "Idea, concept, skill, goal, strategy" }),
            new("Swarm Formation", new[] { "Cooperation & competition", "Consensus & information sharing" }),
            new("Collective Intelligence", new[] { $"Collective intelligence {dashboard[0].Value:0}", $"Collaboration {dashboard[1].Value:0}" }),
            new("Knowledge Ecosystem", new[] { "Idea relationships & pathways", "Agent interactions & discovery chains" }),
            new("Emergent Behaviors", new[] { $"Discovery {dashboard[5].Value:0}", "Novel combinations & creative discoveries" }),
            new("Innovation Analysis", new[] { $"Innovation {dashboard[2].Value:0}", "Inventions, research, AI systems" }),
            new("Forecasting", new[] { Role(0), Role(1), Role(2) }),
            new("Conclusions", new[] { "EEG → agents → collective intelligence", "Grows with more sessions & users" }),
        };
    }
}
