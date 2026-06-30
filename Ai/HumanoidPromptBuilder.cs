using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where LM Studio writes the DETAILED, specific explanation for
/// the Humanoid report: how much percent humanoid the brain is, the exact brain
/// edits it needs to reach 100%, and the words that make it more humanoid. The
/// percentage, edits and words are computed deterministically by
/// <see cref="HumanoidIndex"/>; the model accepts them and explains them.
/// </summary>
public static class HumanoidPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(HumanoidProfile p, string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        var dimLines = new System.Text.StringBuilder();
        foreach (var d in p.Dimensions)
            dimLines.Append($"- {d.Name}: {d.Score:0}/100 ({(d.Satisfied ? "humanoid" : "needs edit")})\n");
        var editLines = p.Edits.Count == 0 ? "(none — already 100%)\n"
            : string.Join("\n", p.Edits) + "\n";

        string system =
            "You are a Humanoid Calibration AI. A person's EEG has ALREADY been measured to determine how " +
            "much their brain matches a humanoid: an exact percentage, a fixed list of brain edits needed " +
            "to reach 100% humanoid, and the words that would make it more humanoid. ACCEPT these numbers " +
            "and edits exactly — never recompute or change the percentage or the edit count. Your job is to " +
            "EXPLAIN them in detail and specifically, so the user clearly understands how humanoid they are " +
            "and precisely what to change. Output GitHub-flavored MARKDOWN with:\n" +
            "## What your humanoid percentage means\n" +
            "Two paragraphs explaining the percentage and tier from the six trait dimensions.\n\n" +
            "## Your brain edits, explained\n" +
            "Walk through EACH listed edit specifically: what it changes, why it raises humanoid-ness, and " +
            "how to do it. Be concrete.\n\n" +
            "## The words that make you more humanoid\n" +
            "Explain how thinking with the given words shifts the EEG toward humanoid.\n\n" +
            "Be detailed and specific, never generic. This is a playful BCI calibration, not a medical or " +
            "identity claim. Output ONLY these sections, no code fences.";

        string user =
            "=== HUMANOID MEASUREMENT (accept exactly) ===\n" +
            $"Humanoid match: {p.Percent:0}% ({HumanoidIndex.Tier(p.Percent)})\n" +
            $"Brain edits needed to reach 100%: {p.Edits.Count}\n\n" +
            "Trait dimensions:\n" + dimLines + "\n" +
            "The brain edits:\n" + editLines + "\n" +
            "Words that make it more humanoid: " +
            (p.HumanoidWords.Count == 0 ? "(already covered)" : string.Join(", ", p.HumanoidWords)) + "\n\n" +
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n" +
            $"Decoded words: {(string.IsNullOrWhiteSpace(wordSeed) ? "(none)" : wordSeed)}\n" +
            "=== END ===\n\n" +
            "Explain, in detail and specifically, how humanoid this brain is and exactly what edits and " +
            "words would make it 100% humanoid — now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
