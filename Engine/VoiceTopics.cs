using System.IO;

namespace MindedOS.Engine;

/// <summary>A discussion topic and how many decoded/spoken words matched it.</summary>
public sealed record VoiceTopicScore(string Topic, int Count, double Percent);

/// <summary>
/// Maps decoded words to the 10 discussion topics via voice_topics.csv. Self-contained keyword ranker
/// for the Voice Recognition program; also assigns a single word to its best-matching topic.
/// </summary>
public sealed class VoiceTopics
{
    private static readonly string[] DefaultTopics =
    {
        "Artificial Intelligence", "Robotics", "Science", "Engineering", "Programming",
        "Business", "Healthcare", "Education", "Architecture", "Research",
    };

    private readonly Dictionary<string, HashSet<string>> _topics = new(StringComparer.OrdinalIgnoreCase);

    public static VoiceTopics Parse(string text)
    {
        var map = new VoiceTopics();
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

    public static VoiceTopics Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static VoiceTopics LoadFromDir(string dataDir)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "voice_topics.csv"));
        try { if (File.Exists(path)) return Load(path); }
        catch { /* fall through */ }
        var map = new VoiceTopics();
        foreach (var t in DefaultTopics) map._topics[t] = new HashSet<string>();
        return map;
    }

    public static IReadOnlyList<VoiceTopicScore> DetectFromFile(string dataDir, IEnumerable<string> words)
        => LoadFromDir(dataDir).Detect(words);

    /// <summary>True if the word is a keyword of any topic (used to flag technical terms).</summary>
    public bool IsTechnicalTerm(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        foreach (var keys in _topics.Values) if (keys.Contains(w)) return true;
        return false;
    }

    public IReadOnlyList<VoiceTopicScore> Detect(IEnumerable<string> words, int top = 10)
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

        var scores = new List<VoiceTopicScore>();
        if (total == 0)
        {
            double even = 100.0 / Math.Max(raw.Count, 1);
            foreach (var (topic, _) in raw) scores.Add(new VoiceTopicScore(topic, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (topic, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new VoiceTopicScore(topic, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<VoiceTopicScore> DefaultRanking()
    {
        double even = 100.0 / DefaultTopics.Length;
        return DefaultTopics.Select(t => new VoiceTopicScore(t, 0, even)).ToList();
    }
}
