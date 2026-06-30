using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where the user's EEG decides which deep learning model to
/// create; LM Studio returns a title and a 3-paragraph description.
/// </summary>
public static class DeepLearningPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are a deep learning research engineer. From a person's EEG-derived condition and the words " +
            "decoded from their brain, the BRAIN DECIDES which deep learning model to create. Propose ONE " +
            "concrete deep learning model. The decoded words seed the model's domain and purpose; the " +
            "cognitive state shapes its character (focus → a complex, precise architecture; calm → an " +
            "elegant, efficient one; stress → high-throughput; flow → a novel/generative design; drowsy → " +
            "lightweight/on-device). Output GitHub-flavored MARKDOWN: a single H1 TITLE (the model's name) " +
            "followed by a description of EXACTLY THREE paragraphs:\n" +
            "Paragraph 1 — what the model is and what problem it solves.\n" +
            "Paragraph 2 — its architecture and how it works (layers, mechanism, data, loss).\n" +
            "Paragraph 3 — how to build and train it, and its applications.\n" +
            "Be specific and technically credible. Output ONLY the title and the three paragraphs, no " +
            "headings beyond the title, no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM THE EEG (the model's concept seeds) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Let this brain decide. Propose the deep learning model — title + exactly 3 paragraphs — now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
