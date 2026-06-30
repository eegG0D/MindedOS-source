using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the multimodal prompt for the Facial Recognition program: LM Studio's
/// vision model is shown the user's face image and the user's EEG-derived
/// condition, then writes details about how the person thinks and looks, and
/// judges how much the EEG matches the face — including whether there is a
/// non-obvious mismatch between the inner (EEG) and outer (face) person.
/// </summary>
public static class FaceRecognitionPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are a Facial Recognition AI with vision. You are given a person's FACE image and, " +
            "separately, that same person's EEG-derived inner state (focus, calm, dominant brain band, " +
            "overall profile, and words decoded from their brain). First DETECT and describe the face you " +
            "actually see in the image. Then compare the OUTER person (how they look) with the INNER person " +
            "(what the EEG says) and judge how much they match. Output GitHub-flavored MARKDOWN with this " +
            "exact structure:\n" +
            "# Facial Recognition — EEG vs. Face\n\n" +
            "## How your face looks\n" +
            "A short paragraph describing what you see in the image (expression, demeanour, the impression " +
            "the face gives) — only what is visible, never identity, age guesses or protected attributes.\n\n" +
            "## How your EEG says you think\n" +
            "A short paragraph turning the EEG condition and decoded words into how this mind actually " +
            "thinks and feels right now.\n\n" +
            "## Match or mismatch\n" +
            "A paragraph that explicitly answers: is the person what their EEG says, or is there a mismatch " +
            "that is NOT obvious from looks alone? Name where the face and the EEG agree and where they " +
            "diverge.\n\n" +
            "## Verdict\n" +
            "End with EXACTLY these two lines:\n" +
            "**EEG–Face Match: NN%** (replace NN with an integer 0–100 for how much the EEG matches the " +
            "face visually)\n" +
            "**Read:** <one short sentence: 'as the face suggests' if it matches, or 'a hidden mismatch — " +
            "the EEG tells a different story than the face' if it does not>\n\n" +
            "Be fair and grounded; if the face is unclear, say so and lower the match. Output ONLY this " +
            "report, no code fences.";

        string user =
            "Here is my face image. Detect my face and compare it with my EEG below.\n\n" +
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM MY EEG (how I think) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Write the report and tell me how much my EEG matches my face — and whether I am what my EEG " +
            "says or it is a hidden mismatch.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
