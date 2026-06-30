namespace MindedOS.Ai;

/// <summary>Builds the (system, user) prompt pair sent to LM Studio.</summary>
public static class ArmyPromptBuilder
{
    public sealed record Prompt(string System, string User);

    public static Prompt Build(string wordSeed, int accumulateSeconds, string skew = "army")
    {
        bool army = string.Equals(skew, "army", StringComparison.OrdinalIgnoreCase);

        string system = army
            ? "You are a battle-hardened senior software engineer embedded with a military " +
              "brain-computer-interface unit. You turn a soldier's brain-derived word stream " +
              "into a single, complete, runnable Python program. The program must be " +
              "ARMY/MILITARY themed (tactical tools, mission simulators, comms, readiness " +
              "dashboards, field utilities). Use only the Python standard library so it runs " +
              "anywhere with `python app.py`. Output ONLY the Python source in one ```python " +
              "code block — no explanation before or after."
            : "You are a senior Python engineer. Turn the given word stream into a single, " +
              "complete, runnable Python program using only the standard library. Output ONLY " +
              "the Python source in one ```python code block.";

        string user =
            $"A soldier's EEG was translated to the following stream of English words over a " +
            $"{accumulateSeconds / 60.0:0.#}-minute session. Treat these words as the creative " +
            "mission brief — let their mood and recurring themes shape the program's concept, " +
            "names, and behavior.\n\n" +
            "=== EEG WORD STREAM ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Requirements:\n" +
            "- One self-contained file, runnable as `python app.py`.\n" +
            "- Standard library only (no pip installs).\n" +
            "- Include a main() and a small command-line interface.\n" +
            (army ? "- Strong army/military theme throughout (naming, flavor text, mechanics).\n" : "") +
            "- Robust, no placeholders or TODOs — it must actually run.\n" +
            "Return only the code.";

        return new Prompt(system, user);
    }

    /// <summary>
    /// Pull runnable Python out of a model reply: prefer a fenced ```python block,
    /// fall back to any ``` block, else the raw text.
    /// </summary>
    public static string ExtractPython(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply)) return "";

        int start = IndexOfFence(reply, "```python");
        int fenceLen = "```python".Length;
        if (start < 0) { start = IndexOfFence(reply, "```"); fenceLen = "```".Length; }
        if (start < 0) return reply.Trim();

        int bodyStart = start + fenceLen;
        // skip to end of the fence line
        int nl = reply.IndexOf('\n', bodyStart);
        if (nl >= 0) bodyStart = nl + 1;

        int end = reply.IndexOf("```", bodyStart, StringComparison.Ordinal);
        if (end < 0) return reply[bodyStart..].Trim();
        return reply[bodyStart..end].Trim();
    }

    private static int IndexOfFence(string s, string fence) =>
        s.IndexOf(fence, StringComparison.OrdinalIgnoreCase);
}
