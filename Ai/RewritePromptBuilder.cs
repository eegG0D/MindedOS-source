namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt that asks LM Studio to rewrite a raw EEG-decoded word stream
/// (brain-to-text via the lexicon) into clear, meaningful prose.
/// </summary>
public static class RewritePromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(string wordSeed, int accumulateSeconds)
    {
        string system =
            "You are a skilled editor and interpreter. You receive a raw, noisy stream of English " +
            "words decoded directly from a person's EEG (brain-to-text). The words are individually " +
            "real but unordered and noisy. Rewrite them into clear, coherent, meaningful prose — a " +
            "few well-formed sentences or short paragraphs — that preserves the recurring themes, " +
            "mood and apparent intent of the stream. Do not invent unrelated facts or add commentary. " +
            "Output ONLY the rewritten text, with no preamble, headings or quotes.";

        string user =
            $"The following English words were decoded from a {accumulateSeconds / 60.0:0.#}-minute " +
            "EEG brain scan, in capture order. Rewrite them into meaningful, readable text.\n\n" +
            "=== RAW EEG WORD STREAM ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Rewritten text:";

        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>Trim and strip an accidental surrounding ``` fence from the model reply.</summary>
    public static string CleanReply(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply)) return "";
        var t = reply.Trim();
        if (t.StartsWith("```", StringComparison.Ordinal))
        {
            int nl = t.IndexOf('\n');
            if (nl >= 0) t = t[(nl + 1)..];
            int end = t.LastIndexOf("```", StringComparison.Ordinal);
            if (end >= 0) t = t[..end];
        }
        return t.Trim();
    }
}
