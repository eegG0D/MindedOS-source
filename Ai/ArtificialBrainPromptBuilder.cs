using System.Text;
using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt for studying an EEG stream that was SYNTHESIZED from the
/// computer's processor (the "artificial brain"), asking LM Studio to explain how
/// this machine-generated EEG differs from real human EEG, grounded in the
/// measured band numbers.
/// </summary>
public static class ArtificialBrainPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation,
        IReadOnlyList<BandReading> bands, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a computational neuroscientist. You are given an EEG-LIKE stream that was NOT " +
            "recorded from a person — it was synthesized from a computer's processor activity (CPU " +
            "load → attention, idleness → meditation, garbage-collection events → 'blinks', and a " +
            "deterministic square/jagged raw waveform). This is an 'artificial brain'. Study the " +
            "supplied numbers and write a clear, accurate, educational explanation of HOW THIS " +
            "MACHINE-GENERATED EEG DIFFERS FROM REAL HUMAN EEG. Cover: band distribution (human " +
            "resting EEG is alpha/theta-rich with a clear posterior alpha rhythm, whereas this stream " +
            "is beta/gamma-dominant), signal morphology (organic, sinusoidal, 1/f spectrum in humans " +
            "vs deterministic/digital here), variability and noise, the meaning of 'blink' (eye " +
            "movement vs GC pause), the absence of biological rhythms, and what is genuinely " +
            "informative vs merely metaphorical. Be honest and precise. Write GitHub-flavored " +
            "MARKDOWN:\n# Artificial Brain EEG Study\n## What the computer generated\n" +
            "## How it differs from human EEG\n## Band-by-band comparison\n## What this tells us\n" +
            "## Caveats\nGround every claim in the numbers below. Output ONLY the Markdown.";

        var sb = new StringBuilder();
        sb.Append("=== ARTIFICIAL (PROCESSOR-DERIVED) EEG, measured over ")
          .Append($"{accumulateSeconds / 60.0:0.#} min ===\n");
        sb.Append($"Attention (from CPU load): {avgAttention:0}/100\n");
        sb.Append($"Meditation (from CPU idle): {avgMeditation:0}/100\n");
        sb.Append($"Dominant band: {dominantBand}\n");
        sb.Append($"Classifier output (human-trained, applied to the machine): {profile}\n\n");
        sb.Append("Band powers (relative units) with the human-oriented tier interpretation:\n");
        foreach (var b in bands)
            sb.Append($"- {b.Name} ({b.Symbol}): {b.Value} — {b.Text}\n");
        sb.Append("\n=== WORDS DECODED FROM THE MACHINE STREAM (same eeg_map lexicon) ===\n");
        sb.Append(string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed);
        sb.Append("\n=== END ===\n\n");
        sb.Append("Explain what the computer generated and exactly how it differs from human EEG, " +
                  "using the numbers above.");

        return new ArmyPromptBuilder.Prompt(system, sb.ToString());
    }
}
