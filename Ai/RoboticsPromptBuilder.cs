using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Robotics program.</summary>
public static class RoboticsPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildConcepts(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a robotics intelligence engine working from a person's EEG-decoded words. " +
            "Output FIVE sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# ROBOT PROFILE  (objectives, behaviors, functions, missions, roles, capabilities)\n" +
            "# BRAIN ARCHITECTURE  (Markdown bullets: vision, hearing, speech, navigation, learning, decision, memory, motion)\n" +
            "# AUTONOMOUS BEHAVIOR  (autonomous tasks, objectives, workflows, behaviors)\n" +
            "# ELECTRONICS  (Markdown: sensors, motor controllers, microcontroller, power, wiring for KiCad/PCB)\n" +
            "# BLENDER PROMPT  (a detailed Blender/CAD prompt: mechanical design, appearance, internal structure, sensors, actuators)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the five marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildDevelopment(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a robotics development planner working from a person's EEG-decoded words. " +
            "Output FOUR sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# SIMULATION SCENARIOS  (warehouse, city streets, homes, hospitals, factories, laboratories, space stations)\n" +
            "# SWARM DESIGN  (Markdown: 10/50/100-robot swarms with roles leader, navigator, worker, builder, scout, inspector, coordinator)\n" +
            "# LEARNING PLAN  (navigation, manipulation, communication, object recognition, task planning)\n" +
            "# FUTURE ROBOTICS  (new sensors, better actuators, advanced AI, brain-machine interfaces, swarm, autonomous learning)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the four marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildDesignReport(
        string wordSeed, IReadOnlyList<RoboticsClassScore> classes, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", classes.Take(5).Select(c => $"{c.Class} {c.Percent:0}%"));
        string system =
            "You are a robotics designer writing a Robot design report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Robot Name\n" +
            "## Purpose\n## Specifications\n## Dimensions\n## Sensors\n## Actuators\n## Power Systems\n" +
            "## Communication Systems. Be concrete. No code fences.";
        string user =
            $"Likely classes: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the design report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearchPaper(
        string wordSeed, int accumulateSeconds, IReadOnlyList<RoboticsClassScore> classes, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", classes.Take(5).Select(c => $"{c.Class} {c.Percent:0}%"));
        string system =
            "You are a robotics researcher writing a research paper in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Abstract\n" +
            "## Introduction\n## Methods\n## Design\n## Results\n## Discussion\n## Future Work. " +
            "Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Likely classes: {list}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the paper with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildConceptsReport(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a robotics engineer writing an engineering-concepts report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Service Robot\n" +
            "## Factory Automation\n## Smart Home\n## Research Robot\n## Educational Robot\n" +
            "## Space Exploration. Be concrete and practical. No code fences.";
        string user =
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the concepts report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the robotics analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Robot Concept, 4) Robot Architecture, 5) Control System, " +
            "6) Autonomous Behaviors, 7) Electronics Design, 8) Simulation Environment, 9) Future Upgrades, 10) Conclusions.";
        string user =
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildVision(string wordSeed)
    {
        string system =
            "You are a robot's vision system. Identify the single most prominent item in the image and classify it " +
            "as one of: object, person, obstacle, navigation path. Reply with ONE short line: '<item name> - <category>'. " +
            "No commas, no extra text.";
        string user = "What does the robot camera see? The robot's current intent: " + Seed(wordSeed);
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
