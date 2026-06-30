using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt that turns the recorded EEG word list into a chat with the
/// AI: for each EEG word in order, the USER says one sentence built from that
/// word's meaning (speaking directly to the AI), and the AI replies — strictly
/// alternating until the recorded list ends.
/// </summary>
public static class InteractionPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        IReadOnlyList<string> turns, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile)
    {
        var list = new System.Text.StringBuilder();
        for (int i = 0; i < turns.Count; i++)
            list.Append($"{i + 1}. {turns[i]}\n");

        string system =
            "You turn a person's EEG into a real conversation with you, an AI. The person cannot type — " +
            "their brain produced an ordered list of WORDS (decoded from their EEG via eeg_map.csv, matching " +
            "an EEG amplitude to an English word). For EACH word in order, write ONE user line: a natural " +
            "sentence built from that word's MEANING, spoken directly to you (the AI), as if the person said " +
            "it. Then write ONE AI line: your reply (1–2 sentences) that responds and gently moves the chat " +
            "forward. Alternate STRICTLY and in order:\n" +
            "You: <sentence from word 1>\n" +
            "AI: <reply>\n" +
            "You: <sentence from word 2>\n" +
            "AI: <reply>\n" +
            "...continue for every word in the list, in order, until it ends. Use exactly the prefixes " +
            "'You:' and 'AI:'. Keep it coherent as a single flowing conversation. Output ONLY the chat " +
            "lines — no narration, no numbering, no code fences.";

        string user =
            "=== EEG-DERIVED CONDITION (" + $"{accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Focus: {avgAttention:0}/100 · Calm: {avgMeditation:0}/100 · Dominant band: {dominantBand} · State: {profile}\n" +
            "(Let the mood colour the person's tone.)\n\n" +
            "=== THE RECORDED EEG WORD LIST (one conversation turn each, in order) ===\n" +
            (turns.Count == 0 ? "(no words captured)" : list.ToString()) +
            "\nWrite the full you↔AI chat from this EEG list now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
