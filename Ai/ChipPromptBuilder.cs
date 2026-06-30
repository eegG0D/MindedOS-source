using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt that asks LM Studio to write a very detailed KiCad chip/PCB
/// design prompt — usable directly, or pasted into Claude Code to scaffold the
/// project — seeded by the user's EEG-decoded words and cognitive state.
/// </summary>
public static class ChipPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are a senior IC/PCB design engineer and prompt engineer for KiCad. From a person's " +
            "EEG-derived condition and the words decoded from their brain, write ONE VERY DETAILED chip " +
            "design prompt for KiCad — precise enough to use directly, or to paste into Claude Code so " +
            "it can scaffold the KiCad project (via SKiDL / kicad-skip / pcbnew Python or by writing the " +
            ".kicad_sch/.kicad_pcb). The decoded words seed the chip's purpose and codename; the " +
            "cognitive state shapes its character (focus → complex precise digital; calm → low-power " +
            "analog/sensor; stress → high-speed; flow → novel mixed-signal; drowsy → minimal/ultra-low-" +
            "power). Output EXACTLY these labelled sections and nothing else:\n" +
            "CHIP: <codename> — one-line purpose\n" +
            "SPECIFICATION: function, board/process, supply voltage, clock, package, key parameters\n" +
            "BLOCK DIAGRAM: a textual block diagram (blocks + how they connect)\n" +
            "COMPONENTS: a concrete BOM (reference designators, parts, values — MCU/ICs, passives, " +
            "connectors, regulators)\n" +
            "SCHEMATIC: net-by-net description (power rails, ground, decoupling, clock, reset, I/O, buses)\n" +
            "KICAD STEPS: step-by-step (new project, schematic capture, footprint assignment, netlist, " +
            "PCB layout, design rules, gerbers)\n" +
            "CLAUDE CODE DEV PROMPT: a precise, ready-to-paste prompt telling Claude Code exactly how to " +
            "generate this KiCad project programmatically.\n" +
            "Be specific, realistic and richly detailed throughout.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM THE EEG (concept seeds) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Write the very detailed KiCad chip design prompt now — seeded by these words and tuned to " +
            "this brain state.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
