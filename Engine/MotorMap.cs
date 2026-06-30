using System.IO;

namespace MindedOS.Engine;

/// <summary>
/// Word → movement command lookup for the Sensorimotor program (the EEG→motor mapping).
/// Loaded from eeg_map_motor.csv; unmapped words cycle the 8 default commands. Parse style mirrors
/// <see cref="RobotActions"/> (a direct lookup, not a ranker).
/// </summary>
public sealed class MotorMap
{
    private static readonly string[] DefaultCommands =
        { "Up", "Down", "Left", "Right", "Forward", "Backward", "Stop", "Action" };
    private static readonly string[] Devices =
        { "Robot", "Robotic Arm", "Wheelchair", "IoT", "Bluetooth", "COM Port" };

    private readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);

    public static MotorMap Parse(string text)
    {
        var mm = new MotorMap();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("word", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = line.Split(',');
            if (parts.Length < 3) continue;
            var word = parts[0].Trim();
            var command = parts[2].Trim();
            if (word.Length == 0 || command.Length == 0) continue;
            mm._map[word] = command;
        }
        return mm;
    }

    public static MotorMap Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static MotorMap DetectFromFile(string dataDir)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "eeg_map_motor.csv"));
        try { if (File.Exists(path)) return Load(path); }
        catch { /* fall through */ }
        return new MotorMap(); // empty → default command cycle
    }

    private string CommandFor(string word, int i) =>
        _map.TryGetValue(word.Trim(), out var c) ? c : DefaultCommands[i % DefaultCommands.Length];

    public string MotorCommandsCsv(IReadOnlyList<string> words)
    {
        var sb = new System.Text.StringBuilder("index,word,command\n");
        if (words.Count == 0) { sb.AppendLine("0,(none),Stop"); return sb.ToString(); }
        for (int i = 0; i < words.Count; i++)
            sb.AppendLine($"{i},{words[i].Trim()},{CommandFor(words[i], i)}");
        return sb.ToString();
    }

    public string BmiControlLogCsv(IReadOnlyList<string> words)
    {
        var sb = new System.Text.StringBuilder("step,device,command,status\n");
        if (words.Count == 0) { sb.AppendLine("1,Robot,Stop,idle"); return sb.ToString(); }
        for (int i = 0; i < words.Count; i++)
        {
            string device = Devices[i % Devices.Length];
            string status = i % 4 == 0 ? "ack" : "sent";
            sb.AppendLine($"{i + 1},{device},{CommandFor(words[i], i)},{status}");
        }
        return sb.ToString();
    }
}
