using System.IO;

namespace MindedOS.Engine;

/// <summary>A career strength and how many decoded words matched it.</summary>
public sealed record CareerScore(string Career, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to the 8 career strengths via supervised_careers.csv.
/// Self-contained keyword ranker for the Supervised Learning program.
/// </summary>
public sealed class SupervisedCareers
{
    private static readonly string[] DefaultCareers =
    {
        "Engineering", "Science", "Programming", "Architecture",
        "Research", "Design", "Education", "Entrepreneurship",
    };

    private readonly Dictionary<string, HashSet<string>> _careers = new(StringComparer.OrdinalIgnoreCase);

    public static SupervisedCareers Parse(string text)
    {
        var map = new SupervisedCareers();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var career = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (career.Length == 0) continue;
            map._careers[career] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static SupervisedCareers Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<CareerScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "supervised_careers.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<CareerScore> Detect(IEnumerable<string> words, int top = 8)
    {
        if (_careers.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string career, int count)>();
        foreach (var (career, keys) in _careers)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((career, count));
            total += count;
        }

        var scores = new List<CareerScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (career, _) in raw) scores.Add(new CareerScore(career, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (career, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new CareerScore(career, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<CareerScore> DefaultRanking()
    {
        double even = 100.0 / DefaultCareers.Length;
        return DefaultCareers.Select(c => new CareerScore(c, 0, even)).ToList();
    }
}
