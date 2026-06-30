using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Asks LM Studio to describe what choices a person's brain made during a session,
/// from the tallied EEG-mapped choices.
/// </summary>
public static class ChoiceDescriptorPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(IReadOnlyList<ChoiceTally> tally, int seconds)
    {
        string system =
            "You are a behavioral analyst. During a session a person's EEG was mapped, moment by " +
            "moment, into a sequence of CHOICES (e.g. Engage, Reflect, Decide, Rest, Avoid, Confirm, " +
            "Observe). Given how often each choice was made, write a concise, insightful description of " +
            "WHAT CHOICES THEIR BRAIN MADE and what it suggests about their decision-making, engagement " +
            "and state during the session. Be specific and honest that this is consumer EEG. Plain prose " +
            "(2–4 short paragraphs), no headings, no code fences.";

        var sb = new StringBuilder();
        sb.Append($"Session length: {seconds / 60.0:0.#} minutes.\n");
        sb.Append("Choices made (most → least frequent):\n");
        if (tally.Count == 0) sb.Append("(none)\n");
        foreach (var t in tally) sb.Append($"- {t.Choice}: {t.Count} times ({t.Percent:0.0}%)\n");
        sb.Append("\nDescribe what choices were made and what they reveal.");

        return new ArmyPromptBuilder.Prompt(system, sb.ToString());
    }
}
