using System.IO;

namespace MindedOS.Engine;

/// <summary>A choice and how often it was made during a session.</summary>
public sealed record ChoiceTally(string Choice, int Count, double Percent);

/// <summary>Counts and ranks the choices recorded in a choices CSV.</summary>
public static class ChoiceLog
{
    public static IReadOnlyList<ChoiceTally> Tally(IEnumerable<string> choices)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var raw in choices)
        {
            var c = raw.Trim();
            if (c.Length == 0) continue;
            freq[c] = freq.TryGetValue(c, out var n) ? n + 1 : 1;
            total++;
        }
        if (total == 0) return Array.Empty<ChoiceTally>();

        return freq.OrderByDescending(kv => kv.Value)
                   .Select(kv => new ChoiceTally(kv.Key, kv.Value, 100.0 * kv.Value / total))
                   .ToList();
    }

    /// <summary>Read the "choice" column from a recorded choices CSV and tally it.</summary>
    public static IReadOnlyList<ChoiceTally> TallyFromCsv(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2) return Array.Empty<ChoiceTally>();

        var header = lines[0].Split(',');
        int col = Array.FindIndex(header, h => h.Trim().Equals("choice", StringComparison.OrdinalIgnoreCase));
        if (col < 0) col = 1; // format is t_sec,choice,...

        var choices = new List<string>();
        for (int i = 1; i < lines.Length; i++)
        {
            var c = lines[i].Split(',');
            if (c.Length > col) choices.Add(c[col].Trim());
        }
        return Tally(choices);
    }
}
