using System.IO;

namespace MindedOS.Engine;

/// <summary>A virtual brain core and how many decoded words matched it.</summary>
public sealed record CoreScore(string Core, int Count, double Percent);

/// <summary>Maps EEG-decoded words to brain cores via core_topics.csv. Mirrors <see cref="ChallengeTopics"/>.</summary>
public sealed class CoreTopics
{
    private static readonly string[] DefaultCores =
    {
        "Logic", "Creativity", "Learning", "Research", "Memory", "Innovation",
    };

    private readonly Dictionary<string, HashSet<string>> _cores = new(StringComparer.OrdinalIgnoreCase);

    public static CoreTopics Parse(string text)
    {
        var map = new CoreTopics();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var core = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (core.Length == 0) continue;
            map._cores[core] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static CoreTopics Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<CoreScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "core_topics.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<CoreScore> Detect(IEnumerable<string> words, int top = 6)
    {
        if (_cores.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string core, int count)>();
        foreach (var (core, keys) in _cores)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((core, count));
            total += count;
        }

        var scores = new List<CoreScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (core, _) in raw) scores.Add(new CoreScore(core, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (core, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new CoreScore(core, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<CoreScore> DefaultRanking()
    {
        double even = 100.0 / DefaultCores.Length;
        return DefaultCores.Select(t => new CoreScore(t, 0, even)).ToList();
    }
}
