namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt that asks LM Studio to write a structured research article
/// (in Markdown, so mindedOS can render it to a cleanly formatted .docx).
/// </summary>
public static class ArticlePromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(string wordSeed, int accumulateSeconds)
    {
        string system =
            "You are a research scientist and academic writer. You are given a noisy stream of " +
            "English words decoded from a person's EEG (brain-to-text); treat it as the thematic " +
            "seed for a paper. Write a complete, coherent research article in GitHub-flavored " +
            "MARKDOWN with this structure:\n" +
            "# <Title>\n" +
            "**Abstract** — one paragraph.\n" +
            "## Introduction\n## <2-4 body sections>\n## Discussion\n## Conclusion\n## References\n" +
            "(use '##' for section headings, '-' for reference/list items, and **bold** for emphasis). " +
            "Formal academic tone, ~600-900 words, self-consistent. Output ONLY the Markdown, no " +
            "code fences, no commentary.";

        string user =
            $"The following English words were decoded from a {accumulateSeconds / 60.0:0.#}-minute " +
            "EEG brain scan. Use their recurring themes and mood as the subject of the article.\n\n" +
            "=== RAW EEG WORD STREAM ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Write the full research article in Markdown now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
