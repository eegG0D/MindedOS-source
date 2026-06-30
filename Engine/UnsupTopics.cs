using System.IO;

namespace MindedOS.Engine;

/// <summary>A latent topic and how many decoded words matched it.</summary>
public sealed record UnsupTopicScore(string Topic, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to the 10 latent topics via unsup_topics.csv. Self-contained keyword ranker
/// for the Unsupervised Learning program; also assigns a single concept to its best-matching topic.
/// </summary>
public sealed class UnsupTopics
{
    private static readonly string[] DefaultTopics =
    {
        "Science", "Technology", "Engineering", "Artificial Intelligence", "Robotics",
        "Architecture", "Business", "Healthcare", "Education", "Research",
    };

    private readonly Dictionary<string, HashSet<string>> _topics = new(StringComparer.OrdinalIgnoreCase);

    public static UnsupTopics Parse(string text)
    {
        var map = new UnsupTopics();
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

    public static UnsupTopics Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static UnsupTopics LoadFromDir(string dataDir)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "unsup_topics.csv"));
        try { if (File.Exists(path)) return Load(path); }
        catch { /* fall through */ }
        var map = new UnsupTopics();
        foreach (var t in DefaultTopics) map._topics[t] = new HashSet<string>();
        return map;
    }

    public static IReadOnlyList<UnsupTopicScore> DetectFromFile(string dataDir, IEnumerable<string> words)
        => LoadFromDir(dataDir).Detect(words);

    /// <summary>The topic whose keyword set contains the concept, else "General".</summary>
    public string TopicOf(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        foreach (var (topic, keys) in _topics) if (keys.Contains(w)) return topic;
        return "General";
    }

    /// <summary>The keyword set for a topic (for topic_keywords.csv).</summary>
    public IReadOnlyCollection<string> KeywordsOf(string topic) =>
        _topics.TryGetValue(topic, out var keys) ? keys : new HashSet<string>();

    public IReadOnlyList<UnsupTopicScore> Detect(IEnumerable<string> words, int top = 10)
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

        var scores = new List<UnsupTopicScore>();
        if (total == 0)
        {
            double even = 100.0 / Math.Max(raw.Count, 1);
            foreach (var (topic, _) in raw) scores.Add(new UnsupTopicScore(topic, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (topic, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new UnsupTopicScore(topic, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<UnsupTopicScore> DefaultRanking()
    {
        double even = 100.0 / DefaultTopics.Length;
        return DefaultTopics.Select(t => new UnsupTopicScore(t, 0, even)).ToList();
    }
}
