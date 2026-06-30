using System.IO;

namespace MindedOS.Engine;

/// <summary>A problem-solving challenge type and how many decoded words matched it.</summary>
public sealed record ChallengeScore(string Challenge, int Count, double Percent);

/// <summary>Maps EEG-decoded words to challenge types via challenge_topics.csv. Mirrors <see cref="PlanningTopics"/>.</summary>
public sealed class ChallengeTopics
{
    private static readonly string[] DefaultChallenges =
    {
        "Engineering", "Scientific", "Mathematical", "Programming", "Business",
        "Research", "Design", "Robotics", "AI", "Architecture",
    };

    private readonly Dictionary<string, HashSet<string>> _challenges = new(StringComparer.OrdinalIgnoreCase);

    public static ChallengeTopics Parse(string text)
    {
        var map = new ChallengeTopics();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var challenge = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (challenge.Length == 0) continue;
            map._challenges[challenge] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static ChallengeTopics Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<ChallengeScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "challenge_topics.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<ChallengeScore> Detect(IEnumerable<string> words, int top = 10)
    {
        if (_challenges.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string challenge, int count)>();
        foreach (var (challenge, keys) in _challenges)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((challenge, count));
            total += count;
        }

        var scores = new List<ChallengeScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (challenge, _) in raw) scores.Add(new ChallengeScore(challenge, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (challenge, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new ChallengeScore(challenge, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<ChallengeScore> DefaultRanking()
    {
        double even = 100.0 / DefaultChallenges.Length;
        return DefaultChallenges.Select(t => new ChallengeScore(t, 0, even)).ToList();
    }
}
