using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Strong AI content: the knowledge-base .db snapshot, goal hierarchy,
/// knowledge graph, dashboard, and fallbacks for the LM artifacts (seven narratives, three reports,
/// a deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class StrongAiContent
{
    private static readonly string[] Agents =
        { "Scientist", "Engineer", "Architect", "Programmer", "Research", "Strategist", "Inventor", "Analyst", "Planner", "Educator" };

    public static string KnowledgeBaseDb(IReadOnlyList<string> words, IReadOnlyList<StrongAiDomainScore> domains)
    {
        var concepts = NlpContent.TopWords(words, 10);
        if (concepts.Count == 0) concepts = new List<string> { "concept" };
        var doms = domains.Select(d => d.Domain).ToList();
        if (doms.Count == 0) doms.Add("General");
        var sb = new StringBuilder();
        sb.AppendLine("-- cognitive_knowledge_base.db (plaintext SQL-style snapshot)");
        sb.AppendLine();
        sb.AppendLine("CREATE TABLE concepts (id INTEGER, name TEXT);");
        for (int i = 0; i < concepts.Count; i++) sb.AppendLine($"INSERT INTO concepts VALUES ({i + 1}, '{Escape(concepts[i])}');");
        sb.AppendLine();
        sb.AppendLine("CREATE TABLE facts (id INTEGER, statement TEXT);");
        for (int i = 0; i < concepts.Count && i < 6; i++) sb.AppendLine($"INSERT INTO facts VALUES ({i + 1}, '{Escape(concepts[i])} is a recurring concept');");
        sb.AppendLine();
        sb.AppendLine("CREATE TABLE relationships (id INTEGER, source TEXT, target TEXT);");
        for (int i = 0; i + 1 < concepts.Count && i < 6; i++) sb.AppendLine($"INSERT INTO relationships VALUES ({i + 1}, '{Escape(concepts[i])}', '{Escape(concepts[i + 1])}');");
        sb.AppendLine();
        sb.AppendLine("CREATE TABLE goals (id INTEGER, domain TEXT);");
        for (int i = 0; i < doms.Count; i++) sb.AppendLine($"INSERT INTO goals VALUES ({i + 1}, '{Escape(doms[i])}');");
        sb.AppendLine();
        sb.AppendLine("CREATE TABLE ideas (id INTEGER, idea TEXT);");
        for (int i = 0; i < concepts.Count && i < 5; i++) sb.AppendLine($"INSERT INTO ideas VALUES ({i + 1}, 'Explore {Escape(concepts[i])}');");
        sb.AppendLine();
        sb.AppendLine("CREATE TABLE memories (id INTEGER, note TEXT);");
        sb.AppendLine($"INSERT INTO memories VALUES (1, 'Session captured {words.Count} words across {concepts.Count} concepts');");
        return sb.ToString();
    }

    private static string Escape(string s) => s.Replace("'", "''");

    public static string GoalHierarchyCsv(IReadOnlyList<StrongAiDomainScore> domains, IReadOnlyList<(string Score, double Value)> dashboard)
    {
        double planning = dashboard.Count > 4 ? dashboard[4].Value : 50;
        var sb = new StringBuilder("goal,priority,progress,dependencies,status\n");
        for (int i = 0; i < domains.Count; i++)
        {
            string priority = i == 0 ? "High" : i < 3 ? "Medium" : "Low";
            double progress = System.Math.Clamp(planning - i * 5, 0, 100);
            string deps = i == 0 ? "-" : domains[i - 1].Domain;
            string status = progress >= 60 ? "in_progress" : "planned";
            sb.AppendLine($"Advance {domains[i].Domain},{priority},{progress:0},{deps},{status}");
        }
        if (domains.Count == 0) sb.AppendLine("Advance General,High,50,-,planned");
        return sb.ToString();
    }

    public static string KnowledgeGraphMd(IReadOnlyList<string> words, IReadOnlyList<StrongAiDomainScore> domains)
    {
        var concepts = NlpContent.TopWords(words, 5);
        if (concepts.Count == 0) concepts = new List<string> { "core" };
        var doms = domains.Take(4).Select(d => d.Domain).ToList();
        if (doms.Count == 0) doms.Add("General");
        var sb = new StringBuilder();
        sb.AppendLine("# Strong AI Knowledge Graph");
        sb.AppendLine();
        sb.AppendLine("Concepts, relationships, goals, projects and research areas:");
        sb.AppendLine();
        for (int i = 0; i < doms.Count; i++)
        {
            string next = i + 1 < doms.Count ? doms[i + 1] : "Synthesis";
            sb.AppendLine($"- **{doms[i]}** → relates to → **{next}**");
        }
        sb.AppendLine();
        sb.AppendLine($"Core concepts: {string.Join(" · ", concepts)}");
        sb.AppendLine();
        sb.AppendLine($"Research areas: {string.Join(", ", doms)}");
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<StrongAiDomainScore> domains)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Strong AI Cognitive Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Metric | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Domains");
        sb.AppendLine("| Domain | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var d in domains.Take(7)) sb.AppendLine($"| {d.Domain} | {d.Percent:0} |");
        return sb.ToString();
    }

    // ---- LM fallbacks: narratives ----

    public static string DefaultReasoning(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("REASONING RESULTS");
        sb.AppendLine("=================");
        sb.AppendLine($"Deductive: conclusions follow from the recurring concepts ({string.Join(", ", concepts.Take(3).DefaultIfEmpty("the focus"))}).");
        sb.AppendLine("Inductive: emerging patterns suggest a consistent theme and trend.");
        sb.AppendLine("Abductive: the most likely explanation is a focused interest in the dominant topic.");
        return sb.ToString();
    }

    public static string DefaultSelfReflection(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SELF-REFLECTION REPORT");
        sb.AppendLine("======================");
        sb.AppendLine($"Thinking patterns: reasoning {dashboard[1].Value:0}, creativity {dashboard[2].Value:0}.");
        sb.AppendLine("Decision patterns: balanced between analysis and exploration.");
        sb.AppendLine("Learning habits: steady, concept-driven.");
        sb.AppendLine($"Strengths: problem solving {dashboard[5].Value:0}; Weaknesses: consistency over long sessions.");
        return sb.ToString();
    }

    public static string DefaultCreativity(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string top = concepts.Count > 0 ? concepts[0] : "the focus";
        var sb = new StringBuilder();
        sb.AppendLine("CREATIVITY REPORT");
        sb.AppendLine("=================");
        sb.AppendLine($"Invention: a tool that applies {top} in a new context.");
        sb.AppendLine("Product idea: a focused assistant around the recurring concepts.");
        sb.AppendLine("Scientific theory: a hypothesis linking the dominant themes.");
        sb.AppendLine("Engineering/architecture concept: a system built around the leading domain.");
        sb.AppendLine("Research direction: deepen the most frequent concept.");
        return sb.ToString();
    }

    public static string DefaultDecision(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DECISION ANALYSIS");
        sb.AppendLine("=================");
        sb.AppendLine("Alternatives: focus deeply vs. explore broadly.");
        sb.AppendLine($"Risk assessment: planning {dashboard[4].Value:0}; moderate risk either way.");
        sb.AppendLine("Expected outcomes: focusing accelerates mastery; exploring broadens options.");
        sb.AppendLine("Recommendation: focus now, schedule exploration later.");
        return sb.ToString();
    }

    public static string DefaultAgentReports(IReadOnlyList<StrongAiDomainScore> domains, IReadOnlyList<string> words)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "the focus";
        var concepts = NlpContent.TopWords(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("# Multi-Agent Reports");
        sb.AppendLine();
        foreach (var agent in Agents)
            sb.AppendLine($"- **{agent} Agent:** contributes a {agent.ToLowerInvariant()} perspective on {top} ({string.Join(", ", concepts.Take(2).DefaultIfEmpty("the concepts"))}).");
        sb.AppendLine();
        sb.AppendLine("The agents share findings and collaborate toward a unified plan.");
        return sb.ToString();
    }

    public static string DefaultWorldModel(IReadOnlyList<StrongAiDomainScore> domains)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# World Model");
        sb.AppendLine();
        foreach (var area in new[] { "Technology", "Science", "Society", "Economics", "Education", "Engineering" })
            sb.AppendLine($"- **{area}:** relationships, dependencies and trends relevant to the user's focus.");
        sb.AppendLine();
        sb.AppendLine($"Leading domain: {(domains.Count > 0 ? domains[0].Domain : "General")}; opportunities follow the strongest interests.");
        return sb.ToString();
    }

    public static string DefaultFuturePredictions(IReadOnlyList<StrongAiDomainScore> domains)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("FUTURE PREDICTIONS");
        sb.AppendLine("==================");
        sb.AppendLine($"Learning growth: steady gains anchored in {top}.");
        sb.AppendLine($"Research directions: deeper work in {top} and adjacent areas.");
        sb.AppendLine("Technology interests: tools that amplify the recurring concepts.");
        sb.AppendLine("Project opportunities: build something concrete in the leading domain.");
        return sb.ToString();
    }

    // ---- LM fallbacks: documents ----

    public static string DefaultProblemSolvingMarkdown(IReadOnlyList<StrongAiDomainScore> domains, IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string focus = concepts.Count > 0 ? concepts[0] : "the focus";
        var sb = new StringBuilder();
        sb.AppendLine("# Problem Solving Report");
        sb.AppendLine();
        foreach (var dom in new[] { "Science", "Engineering", "Programming", "Architecture", "Robotics", "Business", "Research" })
        {
            sb.AppendLine($"## {dom}");
            sb.AppendLine($"Challenge: apply {focus} within {dom.ToLowerInvariant()}. Solution: a focused, iterative plan with measurable steps.");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string DefaultResearchOpportunitiesMarkdown(IReadOnlyList<StrongAiDomainScore> domains, IReadOnlyList<string> words)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "General";
        var concepts = NlpContent.TopWords(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("# Research Opportunities");
        sb.AppendLine();
        sb.AppendLine("## Research Opportunities");
        sb.AppendLine($"Open questions in {top} around {string.Join(", ", concepts.Take(2).DefaultIfEmpty("the concepts"))}.");
        sb.AppendLine();
        sb.AppendLine("## Hypotheses");
        sb.AppendLine("Testable statements linking the recurring concepts to outcomes.");
        sb.AppendLine();
        sb.AppendLine("## Proposed Experiments");
        sb.AppendLine("Small, controlled studies to validate the hypotheses.");
        sb.AppendLine();
        sb.AppendLine("## Suggested Publications");
        sb.AppendLine("A short report or article summarizing the findings.");
        return sb.ToString();
    }

    public static string DefaultAnalysisMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<StrongAiDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "General";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Strong AI Analysis Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A Strong-AI-inspired cognitive framework from a 3-minute EEG. Leading domain: {top}; recurring concepts {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Cognitive Profile");
        sb.AppendLine($"Intelligence profile {dashboard[0].Value:0}; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Reasoning Analysis");
        sb.AppendLine($"Reasoning {dashboard[1].Value:0}; deductive, inductive and abductive paths are exercised.");
        sb.AppendLine();
        sb.AppendLine("## Memory Analysis");
        sb.AppendLine($"Memory utilization {dashboard[6].Value:0}; long-term memory accumulates across sessions.");
        sb.AppendLine();
        sb.AppendLine("## Creativity Analysis");
        sb.AppendLine($"Creativity {dashboard[2].Value:0}; novel combinations emerge from the recurring concepts.");
        sb.AppendLine();
        sb.AppendLine("## Future Opportunities");
        sb.AppendLine($"Research potential {dashboard[7].Value:0}; pursue focused projects in {top}.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<StrongAiDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Domain(int i) => i < domains.Count ? $"{domains[i].Domain} ({domains[i].Percent:0}%)" : "—";
        var concepts = NlpContent.TopWords(words, 3);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Knowledge Base", new[] { "Concepts, facts, relationships", "Goals, ideas, memories" }),
            new("Reasoning Engine", new[] { $"Reasoning {dashboard[1].Value:0}", "Deductive, inductive, abductive" }),
            new("Memory System", new[] { $"Memory {dashboard[6].Value:0}", "Long-term recall & clustering" }),
            new("Creativity Analysis", new[] { $"Creativity {dashboard[2].Value:0}", "Inventions & ideas" }),
            new("Multi-Agent System", new[] { "Scientist, Engineer, Architect…", "10 collaborating agents" }),
            new("World Model", new[] { "Technology, science, society", "Economics, education, engineering" }),
            new("Future Predictions", new[] { Domain(0), Domain(1), Domain(2) }),
            new("Conclusions", new[] { "EEG → unified cognitive model", "Learn, reason, create, plan" }),
        };
    }
}
