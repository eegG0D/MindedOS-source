using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Multi-Agent System content: the mission brief / domain-scores
/// CSVs, the team dashboard, and fallbacks for the LM artifacts (10 agent contributions,
/// mission report, 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class MasContent
{
    /// <summary>A one-line mission derived from the most frequent decoded words.</summary>
    public static string MissionLine(IReadOnlyList<string> words)
    {
        var top = NlpContent.TopWords(words, 3);
        if (top.Count == 0) return "Advance the team's shared objective from the decoded focus.";
        string rest = top.Count > 1 ? string.Join(", ", top.Skip(1)) : "the available context";
        return $"Advance \"{top[0]}\" using {rest}.";
    }

    public static IReadOnlyList<(string Aspect, string Detail)> MissionBrief(IReadOnlyList<string> words)
    {
        var top = NlpContent.TopWords(words, 5);
        string obj = top.Count > 0 ? top[0] : "the focus";
        string scope = top.Count > 1 ? string.Join(", ", top.Skip(1).Take(3)) : "the decoded concepts";
        return new (string, string)[]
        {
            ("Objective", $"Advance {obj}"),
            ("Scope", scope),
            ("Priority", "Coordinate the 10 agents toward a single outcome"),
            ("Success Criterion", $"A validated, documented result aligned with {obj}"),
        };
    }

    public static string MissionBriefCsv(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder("aspect,detail\n");
        foreach (var (aspect, detail) in MissionBrief(words)) sb.AppendLine($"{aspect},\"{detail}\"");
        return sb.ToString();
    }

    public static string DomainScoresCsv(IReadOnlyList<MasDomainScore> domains)
    {
        var sb = new StringBuilder("domain,score\n");
        foreach (var d in domains) sb.AppendLine($"{d.Domain},{d.Percent:0.0}");
        if (domains.Count == 0) sb.AppendLine("General,100.0");
        return sb.ToString();
    }

    public static string Dashboard(
        IReadOnlyList<(string Metric, double Value)> metrics, IReadOnlyList<MasAgent> agents, IReadOnlyList<MasDomainScore> domains)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Multi-Agent System Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Coordination Metric | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (metric, value) in metrics) sb.AppendLine($"| {metric} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Team");
        sb.AppendLine("| Agent | Role | Specialty |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var a in agents) sb.AppendLine($"| {a.Index} | {a.Role} | {a.Specialty} |");
        sb.AppendLine();
        sb.AppendLine("## Focus Domains");
        sb.AppendLine("| Domain | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var d in domains.Take(9)) sb.AppendLine($"| {d.Domain} | {d.Percent:0} |");
        return sb.ToString();
    }

    /// <summary>Ten deterministic contributions, one per agent, cooperating on the shared mission.</summary>
    public static IReadOnlyList<string> DefaultAgentContributions(IReadOnlyList<MasAgent> agents, IReadOnlyList<string> words)
    {
        string mission = MissionLine(words);
        var concepts = NlpContent.TopWords(words, 4);
        string focus = concepts.Count > 0 ? string.Join(", ", concepts) : "the decoded focus";
        var list = new List<string>();
        foreach (var a in agents)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"As the {a.Role} ({a.Skew}), I focus on {a.Specialty}.");
            sb.AppendLine($"Mission: {mission}");
            sb.AppendLine($"Working from the recurring concepts ({focus}), I contribute my part and hand off to the next agent so the team converges on one outcome.");
            list.Add(sb.ToString().Trim());
        }
        return list;
    }

    public static string DefaultResearchMarkdown(
        IReadOnlyList<MasAgent> agents, IReadOnlyList<(string Metric, double Value)> metrics,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = NlpContent.TopWords(words, 6);
        string mission = MissionLine(words);
        var sb = new StringBuilder();
        sb.AppendLine("# Multi-Agent System Mission Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A team of {agents.Count} cooperating agents pursued one mission: {mission}");
        sb.AppendLine();
        sb.AppendLine("## Team Composition");
        sb.AppendLine($"Roles: {string.Join(", ", agents.Select(a => a.Role))}. Each agent has a fixed specialty and skew and reports through the Coordinator.");
        sb.AppendLine();
        sb.AppendLine("## Coordination Analysis");
        sb.AppendLine($"Coordination metrics — {string.Join(", ", metrics.Select(m => $"{m.Metric} {m.Value:0}"))}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Task Allocation");
        sb.AppendLine("Tasks flow Research → Analyze → Strategy → Design → Build → Test → Document, each owned by the best-fit agent and dependent on the prior step.");
        sb.AppendLine();
        sb.AppendLine("## Consensus & Risks");
        sb.AppendLine("Decisions are reached by team consensus; the Critic stress-tests assumptions and surfaces risks before implementation.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine($"Concentrate the team on {(concepts.Count > 0 ? concepts[0] : "the leading concept")} and keep the agent hand-offs tight to raise throughput.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<MasAgent> agents, IReadOnlyList<(string Metric, double Value)> metrics,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string Metric(int i) => i < metrics.Count ? $"{metrics[i].Metric} {metrics[i].Value:0}" : "—";
        return new List<SlideContent>
        {
            new("Mission Overview", new[] { MissionLine(words), $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100" }),
            new("Team Composition", new[] { $"{agents.Count} cooperating agents", "Coordinator + 9 specialists" }),
            new("EEG Translation", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—", $"Dominant band {dominantBand}" }),
            new("Coordination Metrics", new[] { Metric(0), Metric(1), Metric(2) }),
            new("Task Allocation", new[] { "Research → Analyze → Strategy", "Design → Build → Test → Document" }),
            new("Collaboration Map", new[] { "Coordinator routes work", "Agents hand off in sequence" }),
            new("Consensus & Risks", new[] { "Consensus-driven decisions", "Critic surfaces risks early" }),
            new("Agent Highlights", new[] { "Researcher & Analyst gather insight", "Engineer & Implementer build" }),
            new("Domain Focus", new[] { Metric(3), Metric(4), Metric(5) }),
            new("Conclusions", new[] { "One mission, ten agents", "Coordinated toward a single outcome" }),
        };
    }
}
