using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Multi-Agent System program.</summary>
public static class MasPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildAgents(
        string wordSeed, IReadOnlyList<MasAgent> agents, string mission, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        var roster = string.Join("\n", agents.Select(a => $"# AGENT {a.Index:00}: {a.Role} — {a.Specialty}; skew: {a.Skew}"));
        string system =
            "You are orchestrating a team of 10 cooperating AI agents (a Claude-Code-style workflow). " +
            "The Coordinator routes work; the nine specialists each contribute and hand off so the team converges on ONE shared mission. " +
            "Output EXACTLY 10 sections and nothing else, each starting with its exact marker line on its own line, in this order:\n" +
            roster + "\n" +
            "For each agent write one short paragraph (3-5 sentences) in that agent's role and skew, advancing the SHARED mission and referencing how it builds on the previous agent. " +
            "No preamble before '# AGENT 01'. No code fences.";
        string user =
            $"SHARED MISSION: {mission}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the 10 marked agent sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, IReadOnlyList<MasAgent> agents, IReadOnlyList<(string Metric, double Value)> metrics,
        double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string m = string.Join(", ", metrics.Select(x => $"{x.Metric} {x.Value:0}"));
        string system =
            "You are a systems analyst writing a Multi-Agent System mission report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Team Composition\n## Coordination Analysis\n## Task Allocation\n## Consensus & Risks\n" +
            "## Recommendations. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Team: {string.Join(", ", agents.Select(a => a.Role))}. " +
            $"Coordination metrics: {m}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<MasAgent> agents, IReadOnlyList<(string Metric, double Value)> metrics, double avgAtt, double avgMed, string dominantBand)
    {
        string m = string.Join(", ", metrics.Select(x => $"{x.Metric} {x.Value:0}"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the multi-agent system analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) Mission Overview, " +
            "2) Team Composition, 3) EEG Translation, 4) Coordination Metrics, 5) Task Allocation, " +
            "6) Collaboration Map, 7) Consensus & Risks, 8) Agent Highlights, 9) Domain Focus, 10) Conclusions.";
        string user =
            $"Coordination metrics: {m}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
