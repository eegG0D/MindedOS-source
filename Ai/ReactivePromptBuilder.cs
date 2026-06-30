using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Reactive Machine program (present-moment only).</summary>
public static class ReactivePromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, string dominantState, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a Reactive Machine AI: you respond ONLY to the present input, with no memory of past " +
            "sessions and no historical context. From the current EEG-decoded words and state, output EIGHT " +
            "sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# REACTIVE ANALYSIS  (immediate reaction to the present input)\n" +
            "# SITUATION RESPONSES  (responses to technical, engineering, learning, research and innovation situations)\n" +
            "# PROBLEM SOLVER  (problems of interest, areas of concern, topics needing attention right now)\n" +
            "# RESEARCH SUGGESTIONS  (papers, subjects, experiments, projects to pursue now)\n" +
            "# INNOVATION IDEAS  (inventions, software concepts, engineering designs, research proposals, AI systems)\n" +
            "# ARCHITECTURE  (building, city, infrastructure concepts from the current state)\n" +
            "# ROBOTICS  (robot concepts, automation ideas, control systems, AI agents)\n" +
            "# ACTION RECOMMENDATIONS  (what to learn, build, research and improve next)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Current state: {dominantState}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, profile {profile}.\n\n" +
            "=== EEG WORD STREAM (present input) ===\n" + Seed(wordSeed) + "\n=== END ===\nReact now and write the eight marked sections.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, string dominantState, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a Reactive Machine analyst writing a present-moment report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## State Analysis\n## Opportunity Analysis\n## Action Recommendations\n## Innovation Recommendations\n" +
            "## Conclusions. React only to the present input; no historical context. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Current state: {dominantState}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, profile {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        string dominantState, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the reactive machine analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Current State, 4) Attention Analysis, 5) Opportunity Detection, " +
            "6) Decision Engine, 7) Innovation Engine, 8) Research Suggestions, 9) Action Recommendations, 10) Conclusions.";
        string user =
            $"Current state: {dominantState}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
