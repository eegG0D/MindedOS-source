using System.IO;

namespace MindedOS.Engine;

/// <summary>A Strong AI domain and how many decoded words matched it.</summary>
public sealed record StrongAiDomainScore(string Domain, int Count, double Percent);

/// <summary>Maps EEG-decoded words to domains via strong_ai_domains.csv. Mirrors <see cref="MasDomains"/>.</summary>
public sealed class StrongAiDomains
{
    private static readonly string[] DefaultDomains =
    {
        "Science", "Engineering", "Programming", "Architecture", "Robotics", "Business", "Research",
    };

    private readonly Dictionary<string, HashSet<string>> _domains = new(StringComparer.OrdinalIgnoreCase);

    public static StrongAiDomains Parse(string text)
    {
        var map = new StrongAiDomains();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var domain = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (domain.Length == 0) continue;
            map._domains[domain] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static StrongAiDomains Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<StrongAiDomainScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "strong_ai_domains.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<StrongAiDomainScore> Detect(IEnumerable<string> words, int top = 7)
    {
        if (_domains.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string domain, int count)>();
        foreach (var (domain, keys) in _domains)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((domain, count));
            total += count;
        }

        var scores = new List<StrongAiDomainScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (domain, _) in raw) scores.Add(new StrongAiDomainScore(domain, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (domain, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new StrongAiDomainScore(domain, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<StrongAiDomainScore> DefaultRanking()
    {
        double even = 100.0 / DefaultDomains.Length;
        return DefaultDomains.Select(d => new StrongAiDomainScore(d, 0, even)).ToList();
    }
}
