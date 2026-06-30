using System.IO;

namespace MindedOS.Engine;

/// <summary>A quantum topic/interest and how many decoded words matched it.</summary>
public sealed record QuantumScore(string Topic, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to quantum concepts or interests via a CSV (topic,keywords).
/// Mirrors <see cref="PerceptionTopics"/> — takes the CSV file name so one class serves
/// both quantum_concepts.csv and quantum_interests.csv.
/// </summary>
public sealed class QuantumTopics
{
    private static readonly string[] DefaultTopics =
    {
        "Quantum Computing", "Quantum Information", "Quantum Algorithms", "Quantum Simulation",
        "Quantum Cryptography", "Quantum Machine Learning", "Quantum Networking",
        "Quantum Error Correction", "Quantum Optimization",
    };

    private readonly Dictionary<string, HashSet<string>> _topics = new(StringComparer.OrdinalIgnoreCase);

    public static QuantumTopics Parse(string text)
    {
        var map = new QuantumTopics();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var topic = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (topic.Length == 0) continue;
            map._topics[topic] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static QuantumTopics Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<QuantumScore> DetectFromFile(string dataDir, IEnumerable<string> words, string fileName)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, fileName));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<QuantumScore> Detect(IEnumerable<string> words, int top = 9)
    {
        if (_topics.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string topic, int count)>();
        foreach (var (topic, keys) in _topics)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((topic, count));
            total += count;
        }

        var scores = new List<QuantumScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (topic, _) in raw) scores.Add(new QuantumScore(topic, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (topic, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new QuantumScore(topic, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<QuantumScore> DefaultRanking()
    {
        double even = 100.0 / DefaultTopics.Length;
        return DefaultTopics.Select(t => new QuantumScore(t, 0, even)).ToList();
    }
}
