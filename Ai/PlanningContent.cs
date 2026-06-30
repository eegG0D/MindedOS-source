using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Planning content: the structural CSVs (goals,
/// priorities, timeline, resources, opportunities, tasks, project rankings), the
/// dashboard, and fallbacks for the LM artifacts (six narratives, research paper,
/// 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class PlanningContent
{
    private static readonly string[] Horizons = { "short-term", "medium-term", "long-term" };

    public static string IdentifiedGoalsCsv(IReadOnlyList<string> words)
    {
        var top = NlpContent.TopWords(words, 12);
        var sb = new StringBuilder("goal,horizon\n");
        for (int i = 0; i < top.Count; i++)
            sb.AppendLine($"Advance {top[i]},{Horizons[i % Horizons.Length]}");
        if (top.Count == 0) sb.AppendLine("Define a clear goal,short-term");
        return sb.ToString();
    }

    public static string PriorityAnalysisCsv(IReadOnlyList<PlanningDomainScore> topics)
    {
        var sb = new StringBuilder("objective,priority,urgency\n");
        for (int i = 0; i < topics.Count; i++)
        {
            string priority = i == 0 ? "high" : i <= 2 ? "medium" : "low";
            string urgency = i == 0 ? "urgent" : "strategic";
            sb.AppendLine($"Progress in {topics[i].Domain},{priority},{urgency}");
        }
        if (topics.Count == 0) sb.AppendLine("Clarify objectives,high,urgent");
        return sb.ToString();
    }

    public static string TimelinePlansCsv(IReadOnlyList<PlanningDomainScore> topics)
    {
        string top = topics.Count > 0 ? topics[0].Domain : "your focus";
        string second = topics.Count > 1 ? topics[1].Domain : top;
        var sb = new StringBuilder("scope,plan\n");
        sb.AppendLine($"daily,Spend focused time on {top}");
        sb.AppendLine($"weekly,Complete one concrete step in {top}");
        sb.AppendLine($"monthly,Ship a milestone in {top} and explore {second}");
        sb.AppendLine($"yearly,Reach mastery in {top}");
        return sb.ToString();
    }

    public static string ResourceRequirementsCsv(IReadOnlyList<PlanningDomainScore> topics)
    {
        string top = topics.Count > 0 ? topics[0].Domain : "the field";
        var sb = new StringBuilder("category,requirement\n");
        sb.AppendLine($"Knowledge,Fundamentals of {top}");
        sb.AppendLine($"Skills,Core practical skills for {top}");
        sb.AppendLine("Software,Tools relevant to the work");
        sb.AppendLine("Hardware,A capable workstation");
        sb.AppendLine("Research materials,Key texts and papers");
        sb.AppendLine("Human resources,Mentors or collaborators");
        return sb.ToString();
    }

    public static string OpportunityAnalysisCsv(IReadOnlyList<PlanningDomainScore> topics)
    {
        var sb = new StringBuilder("type,opportunity,score\n");
        string top = topics.Count > 0 ? topics[0].Domain : "your field";
        double s = topics.Count > 0 ? topics[0].Percent : 50;
        sb.AppendLine($"Business,A venture grounded in {top},{s:0}");
        sb.AppendLine($"Research,An open question in {top},{s:0}");
        sb.AppendLine($"Innovation,A novel application of {top},{s:0}");
        sb.AppendLine($"Learning,A focused course in {top},{s:0}");
        sb.AppendLine($"Collaboration,A team project in {top},{s:0}");
        return sb.ToString();
    }

    public static string TaskBreakdownCsv(IReadOnlyList<string> words)
    {
        var top = NlpContent.TopWords(words, 5);
        var sb = new StringBuilder("goal,task,subtask\n");
        foreach (var g in top)
        {
            sb.AppendLine($"Advance {g},Research {g},Read and take notes");
            sb.AppendLine($"Advance {g},Build with {g},Ship a small prototype");
        }
        if (top.Count == 0) sb.AppendLine("Define a goal,Clarify scope,Write it down");
        return sb.ToString();
    }

    public static string ProjectRankingsCsv(IReadOnlyList<PlanningDomainScore> topics)
    {
        var sb = new StringBuilder("project,feasibility,impact,complexity,resource_cost,innovation\n");
        for (int i = 0; i < topics.Count; i++)
        {
            double p = topics[i].Percent;
            double feas = System.Math.Clamp(80 - i * 5, 0, 100);
            double impact = System.Math.Clamp(p + 30, 0, 100);
            double complexity = System.Math.Clamp(40 + i * 6, 0, 100);
            double cost = System.Math.Clamp(35 + i * 5, 0, 100);
            double innov = System.Math.Clamp(p + 20, 0, 100);
            sb.AppendLine($"{topics[i].Domain} project,{feas:0},{impact:0},{complexity:0},{cost:0},{innov:0}");
        }
        if (topics.Count == 0) sb.AppendLine("Primary project,70,60,50,45,55");
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<PlanningDomainScore> topics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Planning Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Score | Value |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Strategic Domains");
        sb.AppendLine("| Domain | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var t in topics.Take(10)) sb.AppendLine($"| {t.Domain} | {t.Percent:0} |");
        return sb.ToString();
    }

    public static string DefaultStrategicPlans(IReadOnlyList<PlanningDomainScore> topics, IReadOnlyList<string> words)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Strategic Plans");
        sb.AppendLine();
        foreach (var t in topics.Take(10))
        {
            sb.AppendLine($"## {t.Domain}");
            sb.AppendLine($"- Build fundamentals, then ship one applied project in {t.Domain.ToLowerInvariant()}.");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string DefaultRoadmap(IReadOnlyList<PlanningDomainScore> topics)
    {
        string top = topics.Count > 0 ? topics[0].Domain : "the project";
        var sb = new StringBuilder();
        sb.AppendLine("# Project Roadmap");
        sb.AppendLine();
        sb.AppendLine("## Phases");
        sb.AppendLine("- Discovery → Design → Build → Test → Ship");
        sb.AppendLine("## Milestones");
        sb.AppendLine($"- M1 fundamentals of {top}; M2 prototype; M3 release");
        sb.AppendLine("## Deliverables");
        sb.AppendLine("- A working prototype and a short write-up");
        sb.AppendLine("## Dependencies");
        sb.AppendLine("- Knowledge, tools and time");
        sb.AppendLine("## Risk factors");
        sb.AppendLine("- Scope creep, unclear goals, resource gaps");
        return sb.ToString();
    }

    public static string DefaultDecisionSupport(IReadOnlyList<PlanningDomainScore> topics)
    {
        string top = topics.Count > 0 ? topics[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("DECISION SUPPORT REPORT");
        sb.AppendLine("=======================");
        sb.AppendLine($"Alternative approaches: depth-first in {top}, or breadth-first across domains.");
        sb.AppendLine($"Recommended action: commit to {top} fundamentals first.");
        sb.AppendLine("Potential outcomes: faster mastery vs broader exposure.");
        sb.AppendLine("Risk assessment: over-commitment; mitigate with weekly reviews.");
        return sb.ToString();
    }

    public static string DefaultScenarios(IReadOnlyList<PlanningDomainScore> topics)
    {
        string top = topics.Count > 0 ? topics[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("# Future Scenarios");
        sb.AppendLine();
        sb.AppendLine("## Optimistic");
        sb.AppendLine($"- Rapid progress and a shipped project in {top}.");
        sb.AppendLine("## Realistic");
        sb.AppendLine($"- Steady, consistent advancement in {top}.");
        sb.AppendLine("## Conservative");
        sb.AppendLine("- Slow but safe progress with minimal risk.");
        sb.AppendLine("## Experimental");
        sb.AppendLine($"- Bold cross-domain bets combining {top} with adjacent fields.");
        return sb.ToString();
    }

    public static string DefaultResearchPlans(IReadOnlyList<PlanningDomainScore> topics)
    {
        string top = topics.Count > 0 ? topics[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("# Research Plans");
        sb.AppendLine();
        sb.AppendLine("## Research questions");
        sb.AppendLine($"- What are the open problems in {top}?");
        sb.AppendLine("## Experimental ideas");
        sb.AppendLine($"- A small study testing one hypothesis in {top}.");
        sb.AppendLine("## Investigation plans");
        sb.AppendLine("- Define method, collect data, analyze, report.");
        sb.AppendLine("## Literature review suggestions");
        sb.AppendLine($"- Survey foundational and recent work in {top}.");
        return sb.ToString();
    }

    public static string DefaultAdvisor(IReadOnlyList<PlanningDomainScore> topics, IReadOnlyList<(string Score, double Value)> dashboard)
    {
        string top = topics.Count > 0 ? topics[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("PLANNING ADVISOR REPORT");
        sb.AppendLine("=======================");
        sb.AppendLine($"Strategic recommendation: focus on {top}.");
        sb.AppendLine("Next steps: pick one goal, break it into tasks, schedule them.");
        sb.AppendLine("Milestone suggestion: ship one small deliverable this month.");
        sb.AppendLine("Efficiency improvement: batch similar tasks; review weekly.");
        sb.AppendLine("Risk mitigation: keep scope small and outcomes measurable.");
        return sb.ToString();
    }

    public static string DefaultResearchMarkdown(
        IReadOnlyList<PlanningDomainScore> topics, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = topics.Count > 0 ? topics[0].Domain : "General";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Planning Analysis Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"The plan centers on {top}, built from recurring concepts: {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Goal Analysis");
        sb.AppendLine($"Short, medium and long-term goals are framed around {top} and adjacent domains.");
        sb.AppendLine();
        sb.AppendLine("## Strategic Recommendations");
        sb.AppendLine($"Commit to {top} fundamentals, then ship an applied project.");
        sb.AppendLine();
        sb.AppendLine("## Milestones");
        sb.AppendLine("- M1 fundamentals; M2 prototype; M3 release.");
        sb.AppendLine();
        sb.AppendLine("## Risk Analysis");
        sb.AppendLine("Primary risks: unclear scope and resource gaps; mitigate with weekly reviews.");
        sb.AppendLine();
        sb.AppendLine("## Future Planning");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}. Continue to track goals over time.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<PlanningDomainScore> topics, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Domain(int i) => i < topics.Count ? $"{topics[i].Domain} ({topics[i].Percent:0}%)" : "—";
        var concepts = NlpContent.TopWords(words, 3);
        string top = topics.Count > 0 ? topics[0].Domain : "your field";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Goal Identification", concepts.Count > 0 ? concepts.Select(c => $"Advance {c}").ToArray() : new[] { "Define a goal" }),
            new("Priority Analysis", new[] { $"High: {Domain(0)}", $"Medium: {Domain(1)}" }),
            new("Strategic Planning", new[] { Domain(0), Domain(1), Domain(2) }),
            new("Resource Planning", new[] { "Knowledge & skills", "Software & hardware", "Mentors" }),
            new("Timeline Planning", new[] { "Daily / weekly", "Monthly / yearly milestones" }),
            new("Opportunity Detection", new[] { $"Business & research in {top}", "Innovation & collaboration" }),
            new("Forecasting", new[] { "Success & completion probability", "Motivation sustainability" }),
            new("Conclusions", new[] { "Turn EEG concepts into a roadmap", "Track goals over time" }),
        };
    }
}
