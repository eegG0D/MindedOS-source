using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where LM Studio reads the user's EEG-derived condition and
/// decoded words to assess their feelings and write a ONE-PAGE report that
/// explains the user's emotions. The Markdown is rendered to a .pdf.
/// </summary>
public static class EmotionalAiPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are an affective-computing scientist (Emotional AI). From a person's EEG-derived condition " +
            "(focus, calm, dominant band, overall state) and the words decoded from their brain, ASSESS the " +
            "person's feelings and write a clear, warm, one-page emotional report that EXPLAINS their " +
            "emotions — not just naming them, but explaining why each is present and how they relate. Keep " +
            "it to ONE printed page (roughly 350–450 words total). Output GitHub-flavored MARKDOWN with this " +
            "exact structure:\n" +
            "# Emotional Report\n\n" +
            "**Overall feeling:** <one short sentence naming the dominant emotional state>\n\n" +
            "## Emotions detected\n" +
            "A short bullet list — '- <emotion> — <intensity: low/medium/high> — <one line of evidence from " +
            "the readings/words>' — covering the 3–5 emotions present.\n\n" +
            "## What this means\n" +
            "Two short paragraphs that EXPLAIN the emotions: where they come from in the measured signals " +
            "and decoded words, how they interact, and what the person is likely feeling and why.\n\n" +
            "## Gentle note\n" +
            "One or two sentences of supportive, non-clinical perspective. Ground every claim in the " +
            "provided readings; do not invent numbers or diagnose. Output ONLY this report, no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM THE EEG (emotional traces) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Assess this person's feelings and write the one-page emotional report explaining their " +
            "emotions now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
