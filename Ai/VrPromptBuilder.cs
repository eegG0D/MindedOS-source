using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Virtual Reality program. Self-contained.</summary>
public static class VrPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string Ctx(double a, double m, string band, MentalProfile p) =>
        $"EEG: attention {a:0}/100, calm {m:0}/100, dominant band {band}, state {p}.";

    /// <summary>World bundle → virtual worlds, world blueprints, VR experiences.</summary>
    public static ArmyPromptBuilder.Prompt BuildWorlds(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a VR world generator working from a person's EEG-decoded concepts. " +
            "Output THREE sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# VIRTUAL WORLDS  (10 environment types: futuristic cities, space colonies, research facilities, underwater/ancient civilizations, fantasy worlds, smart megacities, floating cities, robotics labs, AI worlds)\n" +
            "# WORLD BLUEPRINTS  (Markdown ## Geography, ## Buildings, ## Transportation, ## Technology, ## Economy, ## Society, ## Infrastructure, ## Energy Systems, ## Communication Systems)\n" +
            "# VR EXPERIENCES  (discovery missions, space exploration, engineering challenges, AI research, education, historical reconstructions, innovation workshops)\n" +
            "No preamble before the first marker. No code fences.";
        string user = Ctx(avgAtt, avgMed, dominantBand, profile) + "\n\n=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the three marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>Creative bundle → architecture prompts, VR story, training worlds, Meta Quest prompts.</summary>
    public static ArmyPromptBuilder.Prompt BuildCreative(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a VR creative director working from a person's EEG-decoded concepts. " +
            "Output FOUR sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# ARCHITECTURE PROMPTS  (detailed prompts for Blender, Unreal Engine, Unity, Godot — cities, buildings, research labs, transportation networks)\n" +
            "# VR STORY  (Markdown ## Main Storyline, ## Side Quests, ## Missions, ## Challenges, ## Exploration Objectives)\n" +
            "# TRAINING WORLDS  (educational simulations for AI, robotics, programming, engineering, neuroscience, architecture, mathematics)\n" +
            "# METAQUEST PROMPTS  (highly detailed prompts for Meta Quest, Unreal Engine VR, Unity XR, Godot XR)\n" +
            "No preamble before the first marker. No code fences.";
        string user = Ctx(avgAtt, avgMed, dominantBand, profile) + "\n\n=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the four marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>Innovation bundle → innovation simulations, emotional worlds, shared worlds.</summary>
    public static ArmyPromptBuilder.Prompt BuildInnovation(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a VR innovation engine working from a person's EEG-decoded concepts. " +
            "Output THREE sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# INNOVATION SIMULATIONS  (new technologies, scientific breakthroughs, engineering concepts, smart cities, future transportation)\n" +
            "# EMOTIONAL WORLDS  (calm, productive, creative, exploration and scientific environments tuned to the EEG state)\n" +
            "# SHARED WORLDS  (shared interests, common themes, collaborative opportunities across users)\n" +
            "No preamble before the first marker. No code fences.";
        string user = Ctx(avgAtt, avgMed, dominantBand, profile) + "\n\n=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the three marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The VR research report — five level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, string theme, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a VR researcher writing an analysis report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## World Analysis\n## Character Analysis\n## Experience Analysis\n## Future Applications. " +
            "Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. World theme: {theme}. " + Ctx(avgAtt, avgMed, dominantBand, profile) +
            "\n\n=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 10-slide presentation — exactly 10 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(string theme, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the VR analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) World Generation, 4) Character Generation, 5) VR Experiences, " +
            "6) Educational Worlds, 7) AI Companions, 8) Innovation Simulations, 9) Future Applications, 10) Conclusions.";
        string user = $"World theme: {theme}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
