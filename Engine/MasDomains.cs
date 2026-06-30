using System.IO;

namespace MindedOS.Engine;

/// <summary>A team focus domain and how many decoded words matched it.</summary>
public sealed record MasDomainScore(string Domain, int Count, double Percent);

/// <summary>Maps EEG-decoded words to team focus domains via mas_domains.csv. Mirrors <see cref="ReasoningSubjects"/>.</summary>
public sealed class MasDomains
{
    private static readonly string[] DefaultDomains =
    {
        "Artificial Intelligence", "Robotics", "Software Engineering", "Research", "Design",
        "Operations", "Strategy", "Quality", "Knowledge Management",
    };

    private readonly Dictionary<string, HashSet<string>> _domains = new(StringComparer.OrdinalIgnoreCase);

    public static MasDomains Parse(string text)
    {
        var map = new MasDomains();
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

    public static MasDomains Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<MasDomainScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "mas_domains.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<MasDomainScore> Detect(IEnumerable<string> words, int top = 9)
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

        var scores = new List<MasDomainScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (domain, _) in raw) scores.Add(new MasDomainScore(domain, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (domain, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new MasDomainScore(domain, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<MasDomainScore> DefaultRanking()
    {
        double even = 100.0 / DefaultDomains.Length;
        return DefaultDomains.Select(d => new MasDomainScore(d, 0, even)).ToList();
    }
}
