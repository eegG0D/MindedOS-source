using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Asks LM Studio to validate and explain a measured cognition score from the
/// EEG-decoded words and metrics — how calculative, powerful and analytical the
/// brain is.
/// </summary>
public static class CognitionPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile, double score)
    {
        string system =
            "You are a cognitive scientist. A person's EEG was decoded into English words while their " +
            "cognitive state was measured. Their COGNITION INDEX — how calculative, powerful and " +
            "analytical their brain is — has been computed on a 1–200% scale. Write a short, sharp " +
            "assessment (2–3 paragraphs) that VALIDATES and EXPLAINS this cognition score from the metrics " +
            "and the decoded words: how analytical/calculative/powerful the brain looks, what supports the " +
            "score, and one tip to raise it. Be honest that this is consumer EEG. Plain prose, no headings, " +
            "no code fences.";

        string user =
            $"Measured COGNITION INDEX: {score:0}% (1–200% scale).\n" +
            $"Attention/focus: {avgAttention:0}/100. Meditation/calm: {avgMeditation:0}/100. " +
            $"Dominant band: {dominantBand}. State: {profile}.\n\n" +
            "=== WORDS DECODED FROM THE EEG ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Validate and explain the cognition score now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
