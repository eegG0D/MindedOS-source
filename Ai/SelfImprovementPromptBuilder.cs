using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt for a personalized self-improvement article: it combines the
/// user's EEG-derived condition (mental state) with the decoded word stream and
/// asks LM Studio for insightful, actionable advice on what to improve and how.
/// </summary>
public static class SelfImprovementPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);
        string profileDesc = MentalProfileClassifier.Describe(profile);

        string system =
            "You are an insightful performance coach and cognitive scientist. You are given a " +
            "person's EEG-derived mental condition plus a stream of words decoded from their brain " +
            "activity. Write a warm, insightful and genuinely actionable SELF-IMPROVEMENT article " +
            "in GitHub-flavored MARKDOWN. Ground every recommendation in their measured condition " +
            "and the themes in their word stream. Structure:\n" +
            "# <an encouraging title>\n" +
            "**Your reading** — one paragraph interpreting their current cognitive condition.\n" +
            "## What this means\n## Where to focus your improvement\n## A practical 7-day plan\n" +
            "## Habits to build\n## Conclusion\n" +
            "(use '##' for headings, '-' for list items, **bold** for key advice). Be specific, kind, " +
            "non-clinical, and practical. ~600-900 words. Output ONLY the Markdown, no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (measured over " + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Average attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Average meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile} — {profileDesc}\n\n" +
            "=== WORDS DECODED FROM THE EEG ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Using BOTH the condition and the decoded words, write the personalized self-improvement " +
            "article now — tell them what to improve and exactly how.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
