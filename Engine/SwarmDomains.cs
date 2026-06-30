using System.IO;

namespace MindedOS.Engine;

/// <summary>A swarm role/domain and how many decoded words matched it.</summary>
public sealed record SwarmDomainScore(string Domain, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to the 8 swarm roles via swarm_domains.csv. Self-contained keyword ranker
/// for the Swarm Intelligence program; also assigns a single concept to its best-matching role.
/// </summary>
public sealed class SwarmDomains
{
    private static readonly string[] DefaultDomains =
    {
        "Inventor", "Engineer", "Researcher", "Scientist",
        "Architect", "Entrepreneur", "Strategist", "Educator",
    };

    private readonly Dictionary<string, HashSet<string>> _domains = new(StringComparer.OrdinalIgnoreCase);

    public static SwarmDomains Parse(string text)
    {
        var map = new SwarmDomains();
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

    public static SwarmDomains Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static SwarmDomains LoadFromDir(string dataDir)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "swarm_domains.csv"));
        try { if (File.Exists(path)) return Load(path); }
        catch { /* fall through */ }
        var map = new SwarmDomains();
        foreach (var d in DefaultDomains) map._domains[d] = new HashSet<string>();
        return map;
    }

    public static IReadOnlyList<SwarmDomainScore> DetectFromFile(string dataDir, IEnumerable<string> words)
        => LoadFromDir(dataDir).Detect(words);

    /// <summary>The swarm role whose keyword set contains the concept, else "Generalist".</summary>
    public string DomainOf(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        foreach (var (domain, keys) in _domains) if (keys.Contains(w)) return domain;
        return "Generalist";
    }

    public IReadOnlyList<SwarmDomainScore> Detect(IEnumerable<string> words, int top = 8)
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

        var scores = new List<SwarmDomainScore>();
        if (total == 0)
        {
            double even = 100.0 / Math.Max(raw.Count, 1);
            foreach (var (domain, _) in raw) scores.Add(new SwarmDomainScore(domain, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (domain, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new SwarmDomainScore(domain, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<SwarmDomainScore> DefaultRanking()
    {
        double even = 100.0 / DefaultDomains.Length;
        return DefaultDomains.Select(d => new SwarmDomainScore(d, 0, even)).ToList();
    }
}
