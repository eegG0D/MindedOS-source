using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Robot program.</summary>
public static class RobotPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are the cognitive brain of a robot, working from a person's EEG-decoded words. " +
            "Output FOUR sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# BRAIN STATE  (objectives, intentions, commands, strategies, tasks, action plans)\n" +
            "# AUTONOMOUS TASKS  (Markdown bullet list: daily, exploration, research, inspection, learning missions)\n" +
            "# CHAT LOG  (a short human-robot conversation: the robot explains actions and asks a clarifying question)\n" +
            "# EVOLUTION  (future capabilities, new skills, autonomous improvements, hardware + sensor upgrades)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the four marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildAnalysis(
        string wordSeed, int accumulateSeconds, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a robotics analyst writing a Robot analysis report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Brain Analysis\n## Robot Decisions\n## Navigation Analysis\n## Learning Analysis\n" +
            "## Recommendations. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildEngineering(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a robotics engineer writing a Robot engineering report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Robot Designs\n" +
            "## Mechanical Concepts\n## Sensor Configurations\n## Electronics Layouts\n" +
            "## Actuator Suggestions. Be concrete and practical. No code fences.";
        string user =
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the engineering report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the robot system analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Robot Brain State, 4) Command Analysis, 5) Navigation System, " +
            "6) Learning Engine, 7) Personality System, 8) Multi-Robot Coordination, 9) Engineering Concepts, 10) Conclusions.";
        string user =
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildVision(string wordSeed)
    {
        string system =
            "You are a robot's vision system. Identify the single most prominent item in the image and classify it " +
            "as one of: object, tool, person, machine, sign, obstacle. Reply with ONE short line: '<item name> - <type>'. " +
            "No commas, no extra text.";
        string user = "What does the robot camera see? The robot's current intent: " + Seed(wordSeed);
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
