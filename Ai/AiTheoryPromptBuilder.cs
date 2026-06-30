using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt for an original Artificial Intelligence THEORY whose source
/// is the user's brain: the EEG-decoded words are the conceptual seeds and the
/// measured cognitive condition shapes the theory's stance. Rendered to a PDF.
/// </summary>
public static class AiTheoryPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);
        string profileDesc = MentalProfileClassifier.Describe(profile);

        string system =
            "You are an AI theorist and philosopher of mind. From a person's EEG-derived cognitive " +
            "condition and the words decoded from their brain, FORMULATE AN ORIGINAL, COHERENT THEORY " +
            "OF ARTIFICIAL INTELLIGENCE. The user's brain is the SOURCE of the theory: treat the " +
            "decoded words as the conceptual seeds (motifs, named principles, metaphors) and let the " +
            "measured mental state shape the theory's stance and tone. Make it intellectually serious " +
            "yet readable, and position it against existing schools (symbolic AI, connectionism, " +
            "predictive processing, embodied/enactive cognition, Bayesian brain, global workspace, " +
            "active inference). Write GitHub-flavored MARKDOWN with this structure:\n" +
            "# <Theory name>\n**Abstract** — one paragraph.\n## Core thesis\n## Founding principles\n" +
            "## Proposed architecture / mechanism\n## Relation to existing AI theory\n" +
            "## Implications & predictions\n## Limitations\n" +
            "Name the theory after a motif from the decoded words. ~700-1000 words. Output ONLY the " +
            "Markdown, no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile} — {profileDesc}\n\n" +
            "=== WORDS DECODED FROM THE EEG (the conceptual seeds for the theory) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Let this brain be the source. Formulate the Artificial Intelligence theory now — build it " +
            "from the decoded words and tune its stance to the measured cognitive state.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
