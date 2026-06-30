using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Planning program.</summary>
public static class PlanningPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<PlanningDomainScore> topics, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string topicList = string.Join(", ", topics.Take(8).Select(t => $"{t.Domain} {t.Percent:0}%"));
        string system =
            "You are a strategic planning consultant working from a person's EEG-decoded words and detected " +
            "domains. Output SIX sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# STRATEGIC PLANS  (Markdown ## per domain with concrete plans)\n" +
            "# ROADMAP  (project phases, milestones, deliverables, dependencies, risk factors)\n" +
            "# DECISION SUPPORT  (alternative approaches, recommended actions, potential outcomes, risk assessment)\n" +
            "# SCENARIOS  (Markdown with ## Optimistic, ## Realistic, ## Conservative, ## Experimental)\n" +
            "# RESEARCH PLANS  (research questions, experimental ideas, investigation plans, literature review)\n" +
            "# ADVISOR  (strategic recommendations, next steps, milestone suggestions, efficiency, risk mitigation)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Detected domains: {topicList}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the six marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, IReadOnlyList<PlanningDomainScore> topics, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string topicList = string.Join(", ", topics.Take(6).Select(t => $"{t.Domain} {t.Percent:0}%"));
        string system =
            "You are a strategy analyst writing a Planning report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Goal Analysis\n## Strategic Recommendations\n## Milestones\n## Risk Analysis\n" +
            "## Future Planning. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Domains: {topicList}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<PlanningDomainScore> topics, double avgAtt, double avgMed, string dominantBand)
    {
        string topicList = string.Join(", ", topics.Take(6).Select(t => $"{t.Domain} {t.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the planning analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Goal Identification, 4) Priority Analysis, 5) Strategic Planning, " +
            "6) Resource Planning, 7) Timeline Planning, 8) Opportunity Detection, 9) Forecasting, 10) Conclusions.";
        string user =
            $"Domains: {topicList}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
