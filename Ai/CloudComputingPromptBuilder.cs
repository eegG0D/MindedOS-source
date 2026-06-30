using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where LM Studio acts as a cloud-computing research scientist
/// that evaluates a brain's cloud-computing reasoning from its EEG-decoded words —
/// hunting the smallest flaws/errors and scoring how scientific the brain is.
/// </summary>
public static class CloudComputingPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);

        string system =
            "You are a senior CLOUD COMPUTING research scientist and examiner. A person tried to reason " +
            "about cloud computing using only their brain; their EEG was decoded into English words (via " +
            "eeg_map). Because you know cloud computing deeply (distributed systems, scaling, consistency, " +
            "latency, multi-tenancy, security, cost, serverless, networking, fault tolerance), you can " +
            "judge whether the EEG-decoded reasoning is right. EVALUATE whether these words reflect valid " +
            "cloud-computing reasoning and whether the brain identified any FLAWS or ERRORS in cloud " +
            "computing — even the smallest ones. Then SCORE how scientific the brain is about cloud " +
            "computing. Output rigorous Markdown:\n" +
            "# Cloud Computing Brain Evaluation\n" +
            "**SCIENTIFIC SCORE: <0-100>** — one-line justification.\n" +
            "## What the brain's words suggest about cloud computing\n" +
            "## Valid insights detected\n" +
            "## Smallest flaws or errors in cloud computing the brain touched on (or could have)\n" +
            "## How scientific the reasoning is\n" +
            "## Verdict — can this brain solve cloud-computing problems from EEG alone?\n" +
            "Be honest and scientific: EEG-decoded words are noisy, so map them generously to concepts but " +
            "do not invent reasoning that isn't supported. Ground every point in the actual words. Output " +
            "ONLY the Markdown, no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile}\n\n" +
            "=== WORDS DECODED FROM THE EEG (the brain's cloud-computing reasoning) ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Evaluate and score whether this brain can solve cloud-computing problems from EEG alone.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
