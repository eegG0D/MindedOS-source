using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where LM Studio turns the user's EEG into an engineering
/// concept and writes a PDF article that explains it — and whose description is
/// written to be used directly as a prompt for Claude Code (to build the project)
/// and Blender (to model it in 3D).
/// </summary>
public static class GearPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are a mechanical/systems engineer and technical writer. From a person's EEG-derived " +
            "condition and the words decoded from their brain, choose ONE concrete engineering concept the " +
            "person could build — a mechanism, machine, gear train, structure or device (think gears, " +
            "linkages, actuators, frames, drivetrains). The decoded words seed the concept; the cognitive " +
            "state shapes it (focus → precise/high-tolerance mechanism; calm → elegant/efficient; stress → " +
            "rugged/high-load; flow → inventive/novel; drowsy → simple/low-power). Write an ARTICLE that " +
            "EXPLAINS the concept AND is structured so its description can be used directly as a build " +
            "prompt. Output GitHub-flavored MARKDOWN with this exact structure:\n" +
            "# <Engineering Concept / Project Title>\n\n" +
            "## The Concept\n" +
            "Two paragraphs explaining what it is, the engineering principle behind it, and how it works " +
            "(forces, motion, gear ratios, materials, tolerances as relevant).\n\n" +
            "## How It Works\n" +
            "A short bullet list of the key components and how they interact.\n\n" +
            "## Claude Code Prompt\n" +
            "A self-contained paragraph written as an instruction to Claude Code: build the project " +
            "(parametric model script, simulation, or control code) — state language/library, the " +
            "parameters, and what to produce, so it can be pasted straight into Claude Code.\n\n" +
            "## Blender Prompt\n" +
            "A self-contained paragraph written as an instruction to model the concept in Blender: the " +
            "geometry, parts, dimensions/ratios, modifiers and layout — pasteable straight into a " +
            "Blender-driving tool.\n\n" +
            "## Why This Fits Your Brain\n" +
            "One short paragraph tying the concept to the measured EEG condition and decoded words.\n\n" +
            "Be specific and technically credible; use real numbers (ratios, counts, sizes) where natural. " +
            "Output ONLY the article, no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM THE EEG (the concept's seeds) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Choose this brain's engineering concept and write the article — explaining it and giving the " +
            "Claude Code and Blender prompts to build it — now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
