using System.IO;

namespace MindedOS.Engine;

/// <summary>An NLP topic and how many decoded words matched its keywords.</summary>
public sealed record TopicScore(string Topic, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to NLP topics via nlp_topics.csv (topic,keywords).
/// Mirrors <see cref="LearningSubjects"/> / <see cref="ProfessionMap"/>.
/// </summary>
public sealed class NlpTopics
{
    private static readonly string[] DefaultTopics =
    {
        "Science", "Technology", "Engineering", "Mathematics", "Artificial Intelligence",
        "Architecture", "Robotics", "Programming", "Business", "Economics",
        "Healthcare", "Education", "Philosophy",
    };

    private readonly Dictionary<string, HashSet<string>> _topics = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, HashSet<string>> Topics => _topics;

    public static NlpTopics Parse(string text)
    {
        var map = new NlpTopics();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue; // header
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

    public static NlpTopics Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    /// <summary>Load data\nlp_topics.csv; fall back to an even default ranking.</summary>
    public static IReadOnlyList<TopicScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "nlp_topics.csv"));
        try
        {
            if (File.Exists(path)) return Load(path).Detect(words);
        }
        catch
        {
            // fall through to defaults
        }
        return DefaultRanking();
    }

    /// <summary>Rank topics by how often the word stream hits their keywords.</summary>
    public IReadOnlyList<TopicScore> Detect(IEnumerable<string> words, int top = 13)
    {
        if (_topics.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long totalMatched = 0;
        var rawCounts = new List<(string topic, int count)>();
        foreach (var (topic, keys) in _topics)
        {
            int count = 0;
            foreach (var k in keys)
                if (freq.TryGetValue(k, out var c)) count += c;
            rawCounts.Add((topic, count));
            totalMatched += count;
        }

        var scores = new List<TopicScore>();
        if (totalMatched == 0)
        {
            double even = 100.0 / rawCounts.Count;
            foreach (var (topic, _) in rawCounts)
                scores.Add(new TopicScore(topic, 0, even));
            return scores.Take(top).ToList();
        }

        foreach (var (topic, count) in rawCounts.OrderByDescending(r => r.count))
            scores.Add(new TopicScore(topic, count, 100.0 * count / totalMatched));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<TopicScore> DefaultRanking()
    {
        double even = 100.0 / DefaultTopics.Length;
        return DefaultTopics.Select(t => new TopicScore(t, 0, even)).ToList();
    }
}
