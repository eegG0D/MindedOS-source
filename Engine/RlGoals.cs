using System.IO;

namespace MindedOS.Engine;

/// <summary>A learning goal and how many decoded words matched it.</summary>
public sealed record RlGoalScore(string Goal, int Count, double Percent);

/// <summary>Maps EEG-decoded words to learning goals via rl_goals.csv. Mirrors <see cref="MasDomains"/>.</summary>
public sealed class RlGoals
{
    private static readonly string[] DefaultGoals =
    {
        "Learn Programming", "Learn AI", "Learn Robotics", "Learn Engineering",
        "Learn Mathematics", "Learn Architecture", "Build Projects",
    };

    private readonly Dictionary<string, HashSet<string>> _goals = new(StringComparer.OrdinalIgnoreCase);

    public static RlGoals Parse(string text)
    {
        var map = new RlGoals();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var goal = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (goal.Length == 0) continue;
            map._goals[goal] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static RlGoals Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<RlGoalScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "rl_goals.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<RlGoalScore> Detect(IEnumerable<string> words, int top = 7)
    {
        if (_goals.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string goal, int count)>();
        foreach (var (goal, keys) in _goals)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((goal, count));
            total += count;
        }

        var scores = new List<RlGoalScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (goal, _) in raw) scores.Add(new RlGoalScore(goal, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (goal, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new RlGoalScore(goal, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<RlGoalScore> DefaultRanking()
    {
        double even = 100.0 / DefaultGoals.Length;
        return DefaultGoals.Select(g => new RlGoalScore(g, 0, even)).ToList();
    }
}
