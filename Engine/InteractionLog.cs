namespace MindedOS.Engine;

/// <summary>
/// Helpers for the Interaction program: saves the recorded EEG word list as a CSV
/// and builds the alternating you↔AI chat transcript (with a deterministic
/// fallback used when LM Studio is offline, so a chat log always exists).
/// </summary>
public static class InteractionLog
{
    /// <summary>The EEG words used as conversation turns (first <paramref name="max"/>).</summary>
    public static IReadOnlyList<string> Turns(IReadOnlyList<string> words, int max = 24)
    {
        var turns = new List<string>();
        foreach (var w in words)
        {
            var word = (w ?? "").Trim();
            if (word.Length == 0 || word == "—") continue;
            turns.Add(word);
            if (turns.Count >= max) break;
        }
        return turns;
    }

    /// <summary>CSV of the recorded EEG list: the turns the brain will say to the AI.</summary>
    public static string ToCsv(IReadOnlyList<string> turns)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("turn,eeg_word\n");
        for (int i = 0; i < turns.Count; i++)
            sb.Append($"{i + 1},{turns[i]}\n");
        return sb.ToString();
    }

    /// <summary>
    /// Deterministic fallback chat: each EEG word becomes a user line, followed by
    /// an AI line — strictly alternating until the recorded list ends.
    /// </summary>
    public static string OfflineTranscript(IReadOnlyList<string> turns)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var word in turns)
        {
            sb.Append($"You: Let me tell you what's on my mind — {word}.\n");
            sb.Append($"AI: \"{word}\" — I hear you. What does it bring up for you?\n");
        }
        if (turns.Count == 0) sb.Append("You: (no words were captured this session)\nAI: Let's try recording again.\n");
        return sb.ToString();
    }
}
