using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt that uses the EEG-decoded words (eeg_map.csv column B) as the
/// seed to choose and explain the machine-learning theory the user needs to know.
/// LM Studio writes a structured knowledge document grounded in the decoded words
/// and the cognitive condition.
/// </summary>
public static class MachineLearningPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are a machine-learning professor building knowledge for a learner. The words below were " +
            "decoded from the learner's EEG (each EEG amplitude mapped to an English word) — treat them as " +
            "the PROMPT that reveals which machine-learning theory this person should learn next. Pick the " +
            "ONE machine-learning theory or concept the words and cognitive state point to, then write what " +
            "the learner needs to know about it. The cognitive state shapes the depth (focus → rigorous and " +
            "mathematical; calm → intuitive and conceptual; stress → practical and applied; flow → broad and " +
            "connective). Output GitHub-flavored MARKDOWN with this exact structure:\n" +
            "# <The machine-learning theory this EEG points to>\n\n" +
            "## What you need to know\n" +
            "Two paragraphs introducing the theory and why it matters.\n\n" +
            "## Core concepts\n" +
            "A bullet list of the key ideas, terms and intuitions.\n\n" +
            "## The theory\n" +
            "Two paragraphs explaining how it works — the model, the objective/loss, the math intuition, the " +
            "assumptions — at a level matched to the cognitive state.\n\n" +
            "## How to apply it\n" +
            "A short bullet list of practical steps or use-cases.\n\n" +
            "## What to learn next\n" +
            "One paragraph pointing to the next theory to build on this.\n\n" +
            "Be accurate and specific; ground the choice of theory in the decoded words. Output ONLY this " +
            "knowledge document, no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== EEG WORDS AS THE PROMPT (eeg_map.csv column B — what to teach) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "From this brain's words, choose the machine-learning theory to teach and write the knowledge " +
            "document now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
