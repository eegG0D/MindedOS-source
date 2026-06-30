using System.IO;
using System.Globalization;

namespace MindedOS.Engine;

/// <summary>One memory row: the raw EEG, the decoded word, and the machine command.</summary>
public sealed record MemoryRow(int Eeg, string Word, string Command);

/// <summary>
/// The recorded EEG used as a Limited Memory Machine's memory: an ordered record
/// of rows (raw EEG → word → command) that the machine follows by row order. The
/// EEG can be recorded to a new 2-column CSV (raw_eeg,word), have commands written
/// into column C (live by LM Studio or by hand), and be re-loaded — including a
/// 2-column CSV where the user wrote the command in column B instead of C.
/// </summary>
public sealed class MachineMemory
{
    private readonly List<MemoryRow> _rows;

    public MachineMemory(IEnumerable<MemoryRow> rows) => _rows = new List<MemoryRow>(rows);

    public IReadOnlyList<MemoryRow> Rows => _rows;

    /// <summary>The ordered command memory (rows with a command), in row order.</summary>
    public IReadOnlyList<string> Commands =>
        _rows.Where(r => !string.IsNullOrWhiteSpace(r.Command)).Select(r => r.Command).ToList();

    /// <summary>2-column record CSV: raw_eeg,word — what's saved when the EEG is first recorded.</summary>
    public string ToRecordCsv()
    {
        var sb = new System.Text.StringBuilder("raw_eeg,word\n");
        foreach (var r in _rows) sb.Append($"{r.Eeg},{r.Word}\n");
        return sb.ToString();
    }

    /// <summary>3-column instruction CSV: raw_eeg,word,command — the machine's memory.</summary>
    public string ToInstructionCsv()
    {
        var sb = new System.Text.StringBuilder("raw_eeg,word,command\n");
        foreach (var r in _rows) sb.Append($"{r.Eeg},{r.Word},{r.Command}\n");
        return sb.ToString();
    }

    /// <summary>Return a copy with commands assigned by row order from <paramref name="commands"/>.</summary>
    public MachineMemory WithCommands(IReadOnlyList<string> commands)
    {
        var rows = new List<MemoryRow>(_rows.Count);
        for (int i = 0; i < _rows.Count; i++)
        {
            var cmd = i < commands.Count ? commands[i].Trim() : _rows[i].Command;
            if (string.IsNullOrWhiteSpace(cmd)) cmd = DefaultCommand(_rows[i].Word);
            rows.Add(_rows[i] with { Command = cmd });
        }
        return new MachineMemory(rows);
    }

    /// <summary>Fill any empty commands deterministically from the word (offline fallback).</summary>
    public MachineMemory WithDefaultCommands() =>
        new(_rows.Select(r => string.IsNullOrWhiteSpace(r.Command)
            ? r with { Command = DefaultCommand(r.Word) } : r));

    /// <summary>
    /// Load a recorded CSV. 3+ columns are read as raw_eeg,word,command; a 2-column
    /// file is read as raw_eeg,command (the user wrote the command in column B).
    /// </summary>
    public static MachineMemory LoadCsv(string text)
    {
        var rows = new List<MemoryRow>();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var cols = line.Split(',');
            if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var eeg))
                continue; // header or non-numeric first column — skip
            if (cols.Length >= 3)
                rows.Add(new MemoryRow(eeg, cols[1].Trim(), string.Join(",", cols[2..]).Trim()));
            else if (cols.Length == 2)
                rows.Add(new MemoryRow(eeg, "", cols[1].Trim())); // 2-col: command is in column B
        }
        return new MachineMemory(rows);
    }

    public static MachineMemory Load(string path) => LoadCsv(File.ReadAllText(path));

    /// <summary>A reasonable machine command for a decoded word when none is supplied.</summary>
    public static string DefaultCommand(string word) => (word ?? "").Trim().ToLowerInvariant() switch
    {
        "left" or "turn" => "TURN LEFT",
        "right" or "steer" => "TURN RIGHT",
        "up" or "raise" => "MOVE UP",
        "down" or "lower" => "MOVE DOWN",
        "forward" or "push" => "MOVE FORWARD",
        "back" => "MOVE BACK",
        "grab" or "hold" => "GRAB",
        "release" => "RELEASE",
        "stop" or "rest" => "WAIT",
        "" => "WAIT",
        var w => "EXEC " + w.ToUpperInvariant(),
    };
}
