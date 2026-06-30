using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Problem Solving program.</summary>
public static class ProblemSolvingPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<ChallengeScore> challenges, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", challenges.Take(8).Select(c => $"{c.Challenge} {c.Percent:0}%"));
        string system =
            "You are a problem-solving analyst working from a person's EEG-decoded words and detected " +
            "challenge types. Output FIVE sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# SOLUTIONS  (possible, alternative, creative, innovative solutions and optimization opportunities)\n" +
            "# SIMULATIONS  (an engineering, scientific, business, robotics and AI scenario, each with the likely approach)\n" +
            "# ROOT CAUSE  (core concerns, recurring obstacles, bottlenecks, limiting assumptions, opportunity areas)\n" +
            "# MULTI SOLUTION  (for the leading challenge: Best, Fastest, Lowest Cost, Most Innovative, Most Reliable solutions)\n" +
            "# FUTURE  (predicted future strengths across science, technology, engineering, AI, research, entrepreneurship, architecture, robotics)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Detected challenges: {list}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the five marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, IReadOnlyList<ChallengeScore> challenges, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", challenges.Take(6).Select(c => $"{c.Challenge} {c.Percent:0}%"));
        string system =
            "You are a cognitive scientist writing a Problem Solving report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Problem Solving Metrics\n## Innovation Analysis\n## Decision Analysis\n## Recommendations\n" +
            "## Future Development Areas. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Challenges: {list}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<ChallengeScore> challenges, double avgAtt, double avgMed, string dominantBand)
    {
        string list = string.Join(", ", challenges.Take(6).Select(c => $"{c.Challenge} {c.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the problem-solving analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Logical Reasoning, 4) Strategy Analysis, 5) Decision Analysis, " +
            "6) Innovation Profile, 7) Problem Simulations, 8) Root Cause Analysis, 9) Future Predictions, 10) Conclusions.";
        string user =
            $"Challenges: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
