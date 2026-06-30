using System.IO;
using System.Globalization;

namespace MindedOS.Engine;

/// <summary>One IoT mapping row: a raw EEG amplitude, its word, and the robot command.</summary>
public sealed record IotEntry(int Eeg, string Word, string Command);

/// <summary>
/// The IoT robot-control map (eeg_map_iot.csv): column A is a raw EEG amplitude,
/// column B the English word, column C the robot command (LEFT/RIGHT/UP/DOWN…).
/// A live EEG reading is matched to the nearest row and that command is streamed
/// to the robot over the COM port.
/// </summary>
public sealed class IotCommandMap
{
    private readonly List<IotEntry> _entries = new();

    public IReadOnlyList<IotEntry> Entries => _entries;

    public static IotCommandMap Parse(string text)
    {
        var map = new IotCommandMap();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("raw_eeg", StringComparison.OrdinalIgnoreCase)) continue; // header
            var cols = line.Split(',');
            if (cols.Length < 3) continue;
            if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var eeg)) continue;
            var word = cols[1].Trim();
            var command = cols[2].Trim();
            if (command.Length == 0) continue;
            map._entries.Add(new IotEntry(eeg, word, command));
        }
        return map;
    }

    public static IotCommandMap Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    /// <summary>The entry whose EEG value is nearest the live reading (null if empty).</summary>
    public IotEntry? Nearest(double reading)
    {
        IotEntry? best = null;
        double bestDist = double.MaxValue;
        foreach (var e in _entries)
        {
            double dist = Math.Abs(e.Eeg - reading);
            if (dist < bestDist) { bestDist = dist; best = e; }
        }
        return best;
    }

    /// <summary>The wire form sent to the robot: the command terminated by a newline.</summary>
    public static string Wire(string command) => command.Trim().ToUpperInvariant() + "\n";
}
