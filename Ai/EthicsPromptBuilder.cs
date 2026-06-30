using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where LM Studio writes the eight explanatory slides of the
/// Ethical AI deck — why the brain is ethical and what its ethical aspect looks
/// like — given the deterministically computed ethical percentage. The opening
/// title slide and the score slide are produced deterministically by
/// <see cref="MindedOS.Engine.EthicsIndex"/>, so the model only fills bullets.
/// </summary>
public static class EthicsPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds, double ethicalScore, string scoreTier,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile,
        IReadOnlyList<string> slideTitles)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        var titleLines = new System.Text.StringBuilder();
        for (int i = 0; i < slideTitles.Count; i++)
            titleLines.Append($"SLIDE {i + 1}: {slideTitles[i]}\n");

        string system =
            "You are an Ethical AI: a neuroethicist who explains the ethical aspect of a brain from its " +
            "EEG. The brain was recorded for ten minutes and its ethical potential has ALREADY been measured " +
            "deterministically — you must accept that exact percentage and explain it, never re-score it. " +
            "Write EXACTLY EIGHT presentation slides that explain WHY this brain is ethical and HOW its " +
            "ethical aspect appears in the signals and decoded words. Use these exact slide titles in this " +
            "order. For EACH slide output:\n" +
            "SLIDE n: <the given title>\n" +
            "- <3 to 4 concise bullets, each one clear sentence>\n\n" +
            "Ground every bullet in the provided readings (calm/meditation, focus, dominant band, decoded " +
            "words) and tie it back to the measured ethical percentage. Be warm and precise, never preachy " +
            "and never diagnostic. Output ONLY the eight SLIDE blocks — no intro, no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min recording) ===\n" +
            $"MEASURED ETHICAL POTENTIAL: {ethicalScore:0}% (0–100%, tier: {scoreTier}) — accept and explain this.\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM THE EEG (moral/behavioural traces) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Write these eight slides, in this exact order and with these exact titles:\n" +
            titleLines +
            "\nExplain the ethical aspect of this brain now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
