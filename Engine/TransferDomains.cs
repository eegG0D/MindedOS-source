using System.IO;

namespace MindedOS.Engine;

/// <summary>A knowledge domain and how many decoded words matched it.</summary>
public sealed record TransferDomainScore(string Domain, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to the 12 knowledge domains via transfer_domains.csv. Self-contained
/// keyword ranker for the Transfer Learning program; also assigns a single concept to its domain.
/// </summary>
public sealed class TransferDomains
{
    private static readonly string[] DefaultDomains =
    {
        "Artificial Intelligence", "Robotics", "Neuroscience", "Engineering", "Architecture", "Mathematics",
        "Physics", "Programming", "Healthcare", "Education", "Business", "Economics",
    };

    private readonly Dictionary<string, HashSet<string>> _domains = new(StringComparer.OrdinalIgnoreCase);

    public static TransferDomains Parse(string text)
    {
        var map = new TransferDomains();
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

    public static TransferDomains Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static TransferDomains LoadFromDir(string dataDir)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "transfer_domains.csv"));
        try { if (File.Exists(path)) return Load(path); }
        catch { /* fall through */ }
        var map = new TransferDomains();
        foreach (var d in DefaultDomains) map._domains[d] = new HashSet<string>();
        return map;
    }

    public static IReadOnlyList<TransferDomainScore> DetectFromFile(string dataDir, IEnumerable<string> words)
        => LoadFromDir(dataDir).Detect(words);

    /// <summary>The domain whose keyword set contains the concept, else "General".</summary>
    public string DomainOf(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        foreach (var (domain, keys) in _domains) if (keys.Contains(w)) return domain;
        return "General";
    }

    public IReadOnlyList<TransferDomainScore> Detect(IEnumerable<string> words, int top = 12)
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

        var scores = new List<TransferDomainScore>();
        if (total == 0)
        {
            double even = 100.0 / Math.Max(raw.Count, 1);
            foreach (var (domain, _) in raw) scores.Add(new TransferDomainScore(domain, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (domain, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new TransferDomainScore(domain, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<TransferDomainScore> DefaultRanking()
    {
        double even = 100.0 / DefaultDomains.Length;
        return DefaultDomains.Select(d => new TransferDomainScore(d, 0, even)).ToList();
    }
}
