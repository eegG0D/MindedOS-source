namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt where LM Studio writes column C of the Limited Memory
/// Machine's instruction table: one short machine command per decoded EEG word,
/// in row order, so the recorded EEG becomes a runnable command memory.
/// </summary>
public static class MemoryCommandPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(IReadOnlyList<string> words)
    {
        var list = new System.Text.StringBuilder();
        for (int i = 0; i < words.Count; i++)
            list.Append($"{i + 1}. {words[i]}\n");

        string system =
            "You write the instruction memory for a limited-memory machine (a robot/agent with no memory " +
            "of its own). You are given an ordered list of words decoded from a person's EEG. For EACH " +
            "word, in order, output ONE short imperative machine command derived from the word's meaning — " +
            "uppercase verbs like 'MOVE FORWARD', 'TURN LEFT', 'MOVE UP', 'GRAB', 'RELEASE', 'WAIT', " +
            "'SCAN', 'STORE'. Output EXACTLY one command per word, one per line, in the same order, with no " +
            "numbering, no blank lines and no extra text. The number of lines MUST equal the number of words.";

        string user =
            "Words (one machine command each, in order):\n" +
            (words.Count == 0 ? "(none)" : list.ToString()) +
            "\nWrite the command per word now — one per line, in order.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>Parse the model's reply into one command per line (stripped of numbering).</summary>
    public static IReadOnlyList<string> ParseCommands(string reply)
    {
        var commands = new List<string>();
        foreach (var raw in (reply ?? "").Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            // strip "1." / "1)" / "- " prefixes
            int i = 0;
            while (i < line.Length && (char.IsDigit(line[i]) || line[i] is '.' or ')' or '-' or ' ')) i++;
            var cmd = (i > 0 && i < line.Length ? line[i..] : line).Trim();
            if (cmd.Length > 0) commands.Add(cmd);
        }
        return commands;
    }
}
