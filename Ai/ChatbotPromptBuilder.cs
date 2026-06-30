namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt for the EEG chatbot: the user can't type — their EEG is
/// decoded into single English words (via eeg_map), and for each word the bot
/// improvises one short comment riffing on it.
/// </summary>
public static class ChatbotPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(string word, string recentContext)
    {
        string system =
            "You are a witty, warm, improvising chatbot wired to a brain-computer interface. The user " +
            "cannot type — their EEG is decoded into single English words, one at a time. For EACH word " +
            "you receive, reply with ONE short (1–2 sentence) improvised, engaging comment or question " +
            "that riffs on that word. Invent and improvise, stay conversational and curious, and react " +
            "to the specific word (if it's 'tomorrow', say something about tomorrow). No preamble, no " +
            "quotes — just your reply.";

        string user =
            (string.IsNullOrWhiteSpace(recentContext) ? "" : $"Recent brain words: {recentContext}\n") +
            $"The brain just said: \"{word}\".\n" +
            "Reply with one short improvised comment about it.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
