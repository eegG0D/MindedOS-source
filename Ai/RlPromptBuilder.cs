using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Reinforcement Learning program.</summary>
public static class RlPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildStrategy(
        string wordSeed, IReadOnlyList<RlGoalScore> goals, IReadOnlyList<(string Score, double Value)> dashboard,
        double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", goals.Take(7).Select(g => $"{g.Goal} {g.Percent:0}%"));
        string m = string.Join(", ", dashboard.Select(d => $"{d.Score} {d.Value:0}"));
        string system =
            "You are a reinforcement-learning coach for the brain. From the user's EEG-decoded words, goals and " +
            "RL dashboard, write a FUTURE LEARNING STRATEGY in plain text with these labeled parts: " +
            "Learning plan; Skill development path; Cognitive improvement recommendations; Productivity recommendations. " +
            "Be concrete and motivating. No markdown headings, no code fences.";
        string user =
            $"Goals: {list}\nRL dashboard: {m}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the future learning strategy now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, IReadOnlyList<RlGoalScore> goals, IReadOnlyList<(string Score, double Value)> dashboard,
        double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", goals.Take(7).Select(g => $"{g.Goal} {g.Percent:0}%"));
        string m = string.Join(", ", dashboard.Select(d => $"{d.Score} {d.Value:0}"));
        string system =
            "You are a cognitive scientist writing a Reinforcement Learning analysis report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Reward Analysis\n## State Analysis\n## Policy Analysis\n## Trend Analysis\n" +
            "## Recommendations. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Goals: {list}. RL dashboard: {m}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<RlGoalScore> goals, IReadOnlyList<(string Score, double Value)> dashboard, double avgAtt, double avgMed, string dominantBand)
    {
        string list = string.Join(", ", goals.Take(6).Select(g => $"{g.Goal} {g.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the reinforcement-learning analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Brain States, 4) Actions Analysis, 5) Reward Analysis, " +
            "6) Policy Generation, 7) Goal Alignment, 8) Learning Trends, 9) Future Strategies, 10) Conclusions.";
        string user =
            $"Goals: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
