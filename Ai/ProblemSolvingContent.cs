using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Problem Solving content: the challenge/solver/
/// knowledge CSVs, the dashboard, and fallbacks for the LM artifacts (five
/// narratives, research paper, 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class ProblemSolvingContent
{
    public static string ChallengeProfileCsv(IReadOnlyList<ChallengeScore> challenges)
    {
        var sb = new StringBuilder("challenge,percent\n");
        foreach (var c in challenges) sb.AppendLine($"{c.Challenge},{c.Percent:0.0}");
        if (challenges.Count == 0) sb.AppendLine("General,100.0");
        return sb.ToString();
    }

    public static string SolverProfileCsv(IReadOnlyList<(string Archetype, double Value)> archetypes)
    {
        var sb = new StringBuilder("archetype,score,dominant\n");
        double max = archetypes.Count > 0 ? archetypes.Max(a => a.Value) : 0;
        bool flagged = false;
        foreach (var (archetype, value) in archetypes)
        {
            bool dom = !flagged && value >= max;
            if (dom) flagged = true;
            sb.AppendLine($"{archetype},{value:0.0},{(dom ? "yes" : "no")}");
        }
        if (archetypes.Count == 0) sb.AppendLine("Analyst,50.0,yes");
        return sb.ToString();
    }

    public static string KnowledgeExtractionCsv(IReadOnlyList<string> words, IReadOnlyList<ChallengeScore> challenges)
    {
        var sb = new StringBuilder("concept,type\n");
        foreach (var w in NlpContent.TopWords(words, 10)) sb.AppendLine($"{w},useful concept");
        foreach (var c in challenges.Take(3).Where(c => c.Count > 0)) sb.AppendLine($"{c.Challenge},frequent solution domain");
        if (NlpContent.TopWords(words, 1).Count == 0) sb.AppendLine("problem,learning opportunity");
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<ChallengeScore> challenges)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Problem Solving Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Score | Value |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Challenge Profile");
        sb.AppendLine("| Challenge | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var c in challenges.Take(10)) sb.AppendLine($"| {c.Challenge} | {c.Percent:0} |");
        return sb.ToString();
    }

    public static string DefaultSolutionGeneration(IReadOnlyList<ChallengeScore> challenges, IReadOnlyList<string> words)
    {
        string top = challenges.Count > 0 ? challenges[0].Challenge : "the problem";
        var concepts = NlpContent.TopWords(words, 5);
        var sb = new StringBuilder();
        sb.AppendLine("SOLUTION GENERATION");
        sb.AppendLine("===================");
        sb.AppendLine($"Possible solutions: direct approaches grounded in {top}.");
        sb.AppendLine($"Alternative solutions: reframings using {string.Join(", ", concepts)}.");
        sb.AppendLine("Creative solutions: unexpected combinations of the recurring concepts.");
        sb.AppendLine($"Innovative approaches: a novel method drawn from {top}.");
        sb.AppendLine("Optimization opportunities: simplify, automate, and remove bottlenecks.");
        return sb.ToString();
    }

    public static string DefaultSimulations(IReadOnlyList<ChallengeScore> challenges)
    {
        string top = challenges.Count > 0 ? challenges[0].Challenge : "the field";
        var sb = new StringBuilder();
        sb.AppendLine("PROBLEM SIMULATIONS");
        sb.AppendLine("===================");
        sb.AppendLine($"Engineering scenario: a complex build in {top} — likely a structured, component-first approach.");
        sb.AppendLine("Scientific scenario: a hypothesis is formed, tested and revised.");
        sb.AppendLine("Business scenario: trade-offs are weighed against cost and impact.");
        sb.AppendLine("Robotics scenario: a sense-plan-act design is proposed.");
        sb.AppendLine("AI scenario: data, model and evaluation are reasoned through.");
        return sb.ToString();
    }

    public static string DefaultRootCause(IReadOnlyList<ChallengeScore> challenges, IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 5);
        var sb = new StringBuilder();
        sb.AppendLine("ROOT CAUSE ANALYSIS");
        sb.AppendLine("===================");
        sb.AppendLine($"Core concerns: the recurring concepts {string.Join(", ", concepts)}.");
        sb.AppendLine("Recurring obstacles: unclear scope and competing priorities.");
        sb.AppendLine("Bottlenecks: limited time and focus.");
        sb.AppendLine("Limiting assumptions: assuming one right answer.");
        sb.AppendLine($"Opportunity areas: deeper work in {(challenges.Count > 0 ? challenges[0].Challenge : "your field")}.");
        return sb.ToString();
    }

    public static string DefaultMultiSolution(IReadOnlyList<ChallengeScore> challenges)
    {
        string top = challenges.Count > 0 ? challenges[0].Challenge : "the challenge";
        var sb = new StringBuilder();
        sb.AppendLine("MULTI-SOLUTION REPORT");
        sb.AppendLine("=====================");
        sb.AppendLine($"For the leading challenge ({top}):");
        sb.AppendLine("Best Solution: the most balanced approach across quality and effort.");
        sb.AppendLine("Fastest Solution: the quickest path to a working result.");
        sb.AppendLine("Lowest Cost Solution: the cheapest viable approach.");
        sb.AppendLine("Most Innovative Solution: the boldest, most novel approach.");
        sb.AppendLine("Most Reliable Solution: the safest, best-tested approach.");
        return sb.ToString();
    }

    public static string DefaultFuturePredictions(IReadOnlyList<ChallengeScore> challenges)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FUTURE CHALLENGE PREDICTIONS");
        sb.AppendLine("============================");
        string top = challenges.Count > 0 ? challenges[0].Challenge : "your field";
        sb.AppendLine($"Predicted future strengths center on {top}.");
        sb.AppendLine($"Emerging strength areas: {string.Join(", ", challenges.Skip(1).Take(3).Select(c => c.Challenge))}.");
        sb.AppendLine("Across Science, Technology, Engineering, AI, Research, Entrepreneurship, Architecture and Robotics,");
        sb.AppendLine($"the strongest trajectory is toward {top}.");
        return sb.ToString();
    }

    public static string DefaultResearchMarkdown(
        IReadOnlyList<ChallengeScore> challenges, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = challenges.Count > 0 ? challenges[0].Challenge : "General";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Problem Solving Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"The solver gravitates toward {top} challenges, with recurring focus on {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Problem Solving Metrics");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}. Top challenges: {string.Join(", ", challenges.Take(3).Select(c => $"{c.Challenge} {c.Percent:0}%"))}.");
        sb.AppendLine();
        sb.AppendLine("## Innovation Analysis");
        sb.AppendLine("Innovation is driven by recombining recurring concepts into novel approaches.");
        sb.AppendLine();
        sb.AppendLine("## Decision Analysis");
        sb.AppendLine("Decisions balance speed, confidence and risk awareness.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine($"Commit to {top} fundamentals; practice decomposing problems; ship small solutions often.");
        sb.AppendLine();
        sb.AppendLine("## Future Development Areas");
        sb.AppendLine($"Deepen {top} and strengthen weaker reasoning modes over time.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<ChallengeScore> challenges, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Ch(int i) => i < challenges.Count ? $"{challenges[i].Challenge} ({challenges[i].Percent:0}%)" : "—";
        var concepts = NlpContent.TopWords(words, 3);
        string top = challenges.Count > 0 ? challenges[0].Challenge : "your field";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Logical Reasoning", new[] { "Deductive & inductive", "Analytical & systems thinking" }),
            new("Strategy Analysis", new[] { "Logical analysis", "Pattern recognition", "Optimization" }),
            new("Decision Analysis", new[] { "Speed & confidence", "Risk awareness" }),
            new("Innovation Profile", new[] { $"Innovation in {top}", "Invention & discovery potential" }),
            new("Problem Simulations", new[] { "Engineering / scientific", "Business / robotics / AI" }),
            new("Root Cause Analysis", new[] { "Core concerns & bottlenecks", "Limiting assumptions" }),
            new("Future Predictions", new[] { Ch(0), Ch(1), Ch(2) }),
            new("Conclusions", new[] { "How you solve problems", "Where to grow next" }),
        };
    }
}
