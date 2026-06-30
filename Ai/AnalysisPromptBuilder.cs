using System.Text;
using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt for an advanced, accurate brain ANALYSIS (a short study):
/// it hands LM Studio the full measured EEG — every band power with its tier
/// interpretation, plus attention/meditation/profile and the decoded words — and
/// asks for a rigorous, pertinent, honest analysis of the user's brain.
/// </summary>
public static class AnalysisPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation,
        IReadOnlyList<BandReading> bands, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);
        string profileDesc = MentalProfileClassifier.Describe(profile);

        string system =
            "You are a neuroscientist and clinical EEG analyst. From a person's measured EEG you " +
            "produce a rigorous, advanced and ACCURATE analysis — a concise study of their brain. " +
            "Be pertinent and precise: interpret every metric and frequency band from the actual " +
            "numbers given, explain what they indicate about attention, relaxation, stress, fatigue " +
            "and cognitive load, and surface the most important things to know about this brain. " +
            "Stay honest and scientifically accurate: this is single-channel consumer EEG " +
            "(NeuroSky ThinkGear, frontal FP1), eSense attention/meditation are proprietary 0–100 " +
            "indices, and band powers are relative — state these real limitations so nothing is " +
            "overstated. Write in GitHub-flavored MARKDOWN with this structure:\n" +
            "# Brain Analysis\n## Executive summary\n## Attention, meditation & signal\n" +
            "## Frequency band analysis (δ θ α β γ)\n## Cognitive state synthesis\n" +
            "## Most important things to know\n## Accuracy & caveats\n" +
            "Ground EVERY claim in the supplied numbers. Output ONLY the Markdown, no code fences.";

        var sb = new StringBuilder();
        sb.Append("=== MEASURED EEG (").Append($"{accumulateSeconds / 60.0:0.#} min").Append(") ===\n");
        sb.Append($"Attention (eSense): {avgAttention:0}/100 ({focusWord})\n");
        sb.Append($"Meditation (eSense): {avgMeditation:0}/100 ({calmWord})\n");
        sb.Append($"Dominant band: {dominantBand}\n");
        sb.Append($"Classified state: {profile} — {profileDesc}\n\n");
        sb.Append("Band powers (relative units) with tier interpretation:\n");
        foreach (var b in bands)
            sb.Append($"- {b.Name} ({b.Symbol}): {b.Value} — {b.Text}\n");
        sb.Append("\n=== WORDS DECODED FROM THE EEG (eeg_map) ===\n");
        sb.Append(string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed);
        sb.Append("\n=== END ===\n\n");
        sb.Append("Write the advanced, accurate brain analysis now — interpret the actual numbers, " +
                  "give the most pertinent insights, and be honest about the limits of this hardware.");

        return new ArmyPromptBuilder.Prompt(system, sb.ToString());
    }
}
