using System.IO;

namespace MindedOS.Engine;

/// <summary>A perceived-interest topic and how many decoded words matched it.</summary>
public sealed record PerceptionScore(string Topic, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to perceived interests via a CSV (topic,keywords).
/// Mirrors <see cref="NlpTopics"/> but takes the CSV file name so one class serves
/// both perception_topics.csv (categories) and perception_objects.csv (objects).
/// </summary>
public sealed class PerceptionTopics
{
    private static readonly string[] DefaultTopics =
    {
        "Artificial Intelligence", "Robotics", "Architecture", "Science", "Engineering",
        "Programming", "Healthcare", "Business", "Education", "Research",
    };

    private readonly Dictionary<string, HashSet<string>> _topics = new(StringComparer.OrdinalIgnoreCase);

    public static PerceptionTopics Parse(string text)
    {
        var map = new PerceptionTopics();
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

    public static PerceptionTopics Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<PerceptionScore> DetectFromFile(string dataDir, IEnumerable<string> words, string fileName)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, fileName));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<PerceptionScore> Detect(IEnumerable<string> words, int top = 10)
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

        var scores = new List<PerceptionScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (topic, _) in raw) scores.Add(new PerceptionScore(topic, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (topic, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new PerceptionScore(topic, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<PerceptionScore> DefaultRanking()
    {
        double even = 100.0 / DefaultTopics.Length;
        return DefaultTopics.Select(t => new PerceptionScore(t, 0, even)).ToList();
    }
}
