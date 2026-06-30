using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Smart House program.</summary>
public static class SmartHousePromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<HouseRoomScore> rooms, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", rooms.Take(8).Select(r => $"{r.Room} {r.Percent:0}%"));
        string system =
            "You are a smart-home intelligence engine working from a person's EEG-decoded words and room preferences. " +
            "Output FIVE sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# ENERGY  (energy savings, cost savings, efficiency improvements)\n" +
            "# SECURITY  (door monitoring, camera placement, motion zones, alerts, night profile)\n" +
            "# ASSISTANT  (a short house-assistant conversation: recommend automation, explain energy, suggest improvements)\n" +
            "# HOME DESIGN  (Markdown bullets: furniture, workspaces, learning spaces, entertainment, robotics station, device placement)\n" +
            "# SIMULATION  (daily usage, energy consumption, automation workflows, occupancy patterns)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Room preferences: {list}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the five marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildFuturePlan(
        string wordSeed, IReadOnlyList<HouseRoomScore> rooms, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", rooms.Take(5).Select(r => $"{r.Room} {r.Percent:0}%"));
        string system =
            "You are a smart-home planner writing a future smart-home plan in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Future Upgrades\n" +
            "## Automation Expansion\n## New Devices\n## AI Assistants\n## Robotics Integration. " +
            "Be concrete. No code fences.";
        string user =
            $"Room preferences: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the plan with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildAnalysis(
        string wordSeed, int accumulateSeconds, IReadOnlyList<HouseRoomScore> rooms, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", rooms.Take(5).Select(r => $"{r.Room} {r.Percent:0}%"));
        string system =
            "You are a smart-home analyst writing an analysis report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Automation Recommendations\n## Energy Analysis\n## Security Analysis\n" +
            "## Future Planning. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Room preferences: {list}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<HouseRoomScore> rooms, double avgAtt, double avgMed, string dominantBand)
    {
        string list = string.Join(", ", rooms.Take(5).Select(r => $"{r.Room} {r.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the smart-house analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Home Preferences, 4) Environmental Controls, 5) Device Recommendations, " +
            "6) Energy Optimization, 7) Security Analysis, 8) Automation Workflows, 9) Future Smart Home Plan, 10) Conclusions.";
        string user =
            $"Room preferences: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
