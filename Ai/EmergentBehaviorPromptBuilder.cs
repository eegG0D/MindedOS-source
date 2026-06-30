using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where LM Studio reads the user's EEG-derived condition and
/// decoded words to detect their emergent BEHAVIOR SKEW — the dominant
/// behavioural tendency that emerges from the signals — and writes one paragraph
/// describing it.
/// </summary>
public static class EmergentBehaviorPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are a behavioural scientist studying emergent behaviour. Simple neural signals interacting " +
            "over time give rise to a higher-order behavioural pattern that no single signal contains — the " +
            "person's BEHAVIOR SKEW: the dominant tendency their mind leans toward. From the EEG-derived " +
            "condition (focus, calm, dominant band, profile) and the words decoded from the brain, DETECT " +
            "this person's emergent behavior skew and name it. Output GitHub-flavored MARKDOWN:\n" +
            "# Behavior Skew: <a short name for the detected skew>\n\n" +
            "then EXACTLY ONE paragraph (4–7 sentences) describing the behavior skew — what it is, how it " +
            "emerges from the interplay of the measured signals and decoded words, how it shows up in the " +
            "person's actions, and what it tends toward. Ground every claim in the provided readings; do " +
            "not invent numbers. Output ONLY the title line and the single paragraph — no lists, no extra " +
            "headings, no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM THE EEG (behavioural traces) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Detect this person's emergent behavior skew and write the one paragraph about it now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
