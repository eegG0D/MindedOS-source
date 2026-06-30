using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where LM Studio, as a science judge, writes the verdict for
/// the Human-vs-AI EEG duel: which brain's EEG is the most scientific and who
/// wins. The two contestants and their deterministic scientific-EEG scores are
/// computed in code by <see cref="ScienceDuel"/>; the model accepts the winner
/// and explains it on the subject of science.
/// </summary>
public static class HumanVsAiPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(Contestant human, Contestant ai, int accumulateSeconds)
    {
        var winner = ScienceDuel.Winner(human, ai);

        string system =
            "You are a rigorous science judge refereeing a duel between two brains' EEG: a HUMAN brain and " +
            "an AI brain (an artificial EEG synthesized from a computer's processor). The subject is " +
            "SCIENCE. Each EEG has ALREADY been scored deterministically for scientific quality (analytical " +
            "fast-band activity, sustained focus, lexical variety) — you must ACCEPT the given scores and " +
            "the stated winner; do not recompute or overturn them. Explain, on the subject of science, why " +
            "the winning EEG is the more scientific, comparing the two EEG lists and conditions fairly. " +
            "Output GitHub-flavored MARKDOWN:\n" +
            "## Why the winner is the most scientific\n" +
            "Two short paragraphs comparing the human and AI EEG on scientific merit and naming the winner.\n\n" +
            "## What each brain brought\n" +
            "A short bullet list — one bullet for the human, one for the AI — on their scientific strengths " +
            "and weaknesses from the readings.\n\n" +
            "End with the line: '**Winner on science: <name>**'. Output ONLY these sections, no code fences.";

        string user =
            "=== DUEL (subject: science · each brain recorded " + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"DETERMINISTIC WINNER (accept this): {winner.Name}\n\n" +
            Block(human) +
            Block(ai) +
            "Write the science verdict explaining why " + winner.Name + " has the most scientific EEG — now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }

    private static string Block(Contestant c) =>
        $"--- {c.Name} ---\n" +
        $"Scientific-EEG score: {c.Score:0}/100\n" +
        $"Focus (attention): {c.AvgAttention:0}/100\n" +
        $"Calm (meditation): {c.AvgMeditation:0}/100\n" +
        $"Dominant EEG band: {c.DominantBand}\n" +
        $"Overall state: {c.Profile}\n" +
        $"Distinct decoded ideas: {c.DistinctWords}\n" +
        $"EEG words: {(string.IsNullOrWhiteSpace(c.Seed) ? "(none)" : c.Seed)}\n\n";
}
