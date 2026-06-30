using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt asking LM Studio for a 6-slide deck explaining the user's
/// Ambient User Experience — what they are experiencing right now — inferred from
/// their EEG condition and decoded words. Output is a simple, easily-parsed slide
/// format that <see cref="PptxArticleWriter.ParseSlides"/> turns into a .pptx.
/// </summary>
public static class SlidesPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);
        string profileDesc = MentalProfileClassifier.Describe(profile);

        string system =
            "You are a UX strategist and cognitive scientist. From a user's EEG-derived condition " +
            "and the words decoded from their brain, create a clear 6-slide presentation that " +
            "explains their AMBIENT USER EXPERIENCE — what the user is experiencing right now and " +
            "how calm, context-aware, adaptive technology (ambient computing that fades into the " +
            "background) would respond to them.\n" +
            "Output EXACTLY 6 slides, each in this format and nothing else:\n" +
            "SLIDE <n>: <slide title>\n- <bullet>\n- <bullet>\n(3 to 5 concise bullets per slide). " +
            "No preamble, no closing remarks, no markdown headings — only the SLIDE blocks.\n" +
            "Suggested arc: 1) Title & overview, 2) What you are experiencing now (your state), " +
            "3) Your ambient signals (the EEG reading), 4) How an ambient system adapts to you, " +
            "5) Recommendations for a better ambient experience, 6) Summary.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Average attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Average meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile} — {profileDesc}\n\n" +
            "=== WORDS DECODED FROM THE EEG ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Write the 6 slides explaining this user's Ambient User Experience now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
