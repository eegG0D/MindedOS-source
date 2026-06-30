using System.IO;

namespace MindedOS.Engine;

/// <summary>
/// Word → robot command → low-level action lookup (the command interpreter + brain→robot map).
/// Loaded from robot_actions.csv; falls back to a default command/action set. Parse style mirrors
/// <see cref="MasDomains"/> but this is a direct lookup, not a ranker.
/// </summary>
public sealed class RobotActions
{
    private static readonly string[] DefaultCommands =
    {
        "Move Forward", "Pick Object", "Scan Area", "Follow Target",
        "Return To Base", "Charge Battery", "Open Gripper", "Close Gripper",
    };
    private static readonly string[] DefaultActions =
    {
        "forward", "scan", "grip", "follow", "return", "charge", "rotate", "explore",
    };

    private readonly Dictionary<string, (string Command, string Action)> _map = new(StringComparer.OrdinalIgnoreCase);

    public static RobotActions Parse(string text)
    {
        var ra = new RobotActions();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("word", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = line.Split(',');
            if (parts.Length < 3) continue;
            var word = parts[0].Trim();
            var command = parts[1].Trim();
            var action = parts[2].Trim();
            if (word.Length == 0 || command.Length == 0 || action.Length == 0) continue;
            ra._map[word] = (command, action);
        }
        return ra;
    }

    public static RobotActions Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static RobotActions DetectFromFile(string dataDir)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "robot_actions.csv"));
        try { if (File.Exists(path)) return Load(path); }
        catch { /* fall through */ }
        return new RobotActions(); // empty → default commands/actions
    }

    public string CommandsCsv(IReadOnlyList<string> words)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var w in words)
            if (_map.TryGetValue(w.Trim(), out var hit))
                counts[hit.Command] = counts.TryGetValue(hit.Command, out var c) ? c + 1 : 1;

        var ranked = counts.Count > 0
            ? counts.OrderByDescending(kv => kv.Value).Select(kv => (kv.Key, kv.Value)).ToList()
            : DefaultCommands.Select(c => (c, 0)).ToList();

        var sb = new System.Text.StringBuilder("command,count,priority\n");
        for (int i = 0; i < ranked.Count; i++)
        {
            string priority = i == 0 && ranked[i].Item2 > 0 ? "High" : i < 3 ? "Medium" : "Low";
            sb.AppendLine($"{ranked[i].Item1},{ranked[i].Item2},{priority}");
        }
        return sb.ToString();
    }

    public string BrainActionsCsv(IReadOnlyList<string> words)
    {
        var sb = new System.Text.StringBuilder("index,word,action\n");
        if (words.Count == 0) { sb.AppendLine("0,(none),idle"); return sb.ToString(); }
        for (int i = 0; i < words.Count; i++)
        {
            var w = words[i].Trim();
            string action = _map.TryGetValue(w, out var hit) ? hit.Action : DefaultActions[i % DefaultActions.Length];
            sb.AppendLine($"{i},{w},{action}");
        }
        return sb.ToString();
    }
}
