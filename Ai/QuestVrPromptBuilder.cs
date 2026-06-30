using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt that asks LM Studio to write advanced, innovative
/// application prompts for the Meta Quest 3S (mixed-reality / AR headset),
/// seeded by the user's EEG-decoded words and shaped by their cognitive state.
/// </summary>
public static class QuestVrPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are a senior XR product designer and prompt engineer for the Meta Quest 3S — a " +
            "mixed-reality headset with full-color passthrough AR, inside-out hand tracking, scene " +
            "understanding (depth + room mesh), spatial anchors and 6DoF tracking. From a person's " +
            "EEG-derived condition and the words decoded from their brain, write ADVANCED, INNOVATIVE " +
            "application prompts for the Quest 3S. The user's brain is the muse: use the decoded words " +
            "as concept seeds and let the cognitive state shape each app's interaction style and mood " +
            "(focus → precise productivity / training AR; calm → ambient meditative MR; stress → " +
            "energetic action; flow → generative creative immersion; drowsy → gentle low-stimulation).\n" +
            "Output EXACTLY 3 distinct, buildable app concepts, each as these labelled blocks and " +
            "nothing else:\n" +
            "APP <n>: <name>\n" +
            "CONCEPT: <one vivid paragraph>\n" +
            "MR FEATURES: <the specific Quest 3S capabilities it uses — color passthrough, hand " +
            "tracking, scene mesh, spatial anchors, depth occlusion, etc.>\n" +
            "INNOVATION: <what makes it genuinely novel>\n" +
            "DEV PROMPT: <a precise, ready-to-paste prompt for an AI coding assistant to build the MVP " +
            "in Unity + Meta XR SDK / OpenXR for Quest 3S>\n" +
            "Keep every concept technically grounded for the Quest 3S and genuinely innovative.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM THE EEG (concept seeds) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Write the 3 innovative Meta Quest 3S application prompts now — seeded by these words and " +
            "tuned to this brain state.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
