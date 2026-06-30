using System.IO;

namespace MindedOS.Engine;

/// <summary>A robot class and how many decoded words matched it.</summary>
public sealed record RoboticsClassScore(string Class, int Count, double Percent);

/// <summary>Maps EEG-decoded words to robot classes via robotics_classes.csv. Mirrors <see cref="MasDomains"/>.</summary>
public sealed class RoboticsClasses
{
    private static readonly string[] DefaultClasses =
    {
        "Humanoid", "Industrial", "Service", "Medical", "Educational",
        "Agricultural", "Military Simulation", "Research", "Exploration", "Domestic",
    };

    private readonly Dictionary<string, HashSet<string>> _classes = new(StringComparer.OrdinalIgnoreCase);

    public static RoboticsClasses Parse(string text)
    {
        var map = new RoboticsClasses();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var cls = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (cls.Length == 0) continue;
            map._classes[cls] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static RoboticsClasses Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<RoboticsClassScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "robotics_classes.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<RoboticsClassScore> Detect(IEnumerable<string> words, int top = 10)
    {
        if (_classes.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string cls, int count)>();
        foreach (var (cls, keys) in _classes)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((cls, count));
            total += count;
        }

        var scores = new List<RoboticsClassScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (cls, _) in raw) scores.Add(new RoboticsClassScore(cls, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (cls, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new RoboticsClassScore(cls, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<RoboticsClassScore> DefaultRanking()
    {
        double even = 100.0 / DefaultClasses.Length;
        return DefaultClasses.Select(c => new RoboticsClassScore(c, 0, even)).ToList();
    }
}
