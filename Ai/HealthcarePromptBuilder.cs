using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where LM Studio SPECULATES on the EEG-selected drug
/// combination — what illness or set of symptoms the combination might address —
/// given the deterministically chosen drugs and the user's EEG condition. The
/// drug SELECTION is made in code by <see cref="DrugFormulary"/>; the model only
/// speculates on the medical solution.
/// </summary>
public static class HealthcarePromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        IReadOnlyList<DrugPick> picks, string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        var combo = new System.Text.StringBuilder();
        foreach (var p in picks)
            combo.Append($"- {p.Drug} ({p.Class}) — individually treats {p.Treats}\n");

        string system =
            "You are a speculative Healthcare AI in a research sandbox. A brain-computer interface has, " +
            "from a person's EEG, ALREADY SELECTED a fixed combination of drugs from a catalog — you must " +
            "accept that exact combination and not add or remove drugs. Your job is to SPECULATE: reason " +
            "about what illness or cluster of symptoms this particular combination could form an " +
            "experimental medical solution for, and how the agents might complement one another. Be " +
            "clearly speculative ('could', 'might', 'hypothetically'). Output GitHub-flavored MARKDOWN " +
            "with:\n" +
            "## Target illness or symptoms\n" +
            "One paragraph naming the illness/symptom cluster the combination could address and why these " +
            "drugs together fit it.\n\n" +
            "## How the combination might work\n" +
            "A short bullet list: each drug's speculated role in the combined solution.\n\n" +
            "## Cautions\n" +
            "A short bullet list of the obvious risks/interactions to investigate.\n\n" +
            "This is an educational thought experiment, NOT medical advice — never instruct anyone to take " +
            "or combine the drugs, and say so. Output ONLY these sections, no code fences.";

        string user =
            "=== EEG-SELECTED DRUG COMBINATION (fixed — speculate on THIS) ===\n" +
            combo +
            "\n=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n" +
            $"Decoded words: {(string.IsNullOrWhiteSpace(wordSeed) ? "(none)" : wordSeed)}\n" +
            "=== END ===\n\n" +
            "Speculate on the illness this EEG-selected combination could solve, and how — now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
