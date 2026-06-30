using System.IO;

namespace MindedOS.Engine;

/// <summary>A reasoning subject and how many decoded words matched it.</summary>
public sealed record ReasoningSubjectScore(string Subject, int Count, double Percent);

/// <summary>Maps EEG-decoded words to subjects via reasoning_subjects.csv. Mirrors <see cref="ChallengeTopics"/>.</summary>
public sealed class ReasoningSubjects
{
    private static readonly string[] DefaultSubjects =
    {
        "Artificial Intelligence", "Robotics", "Architecture", "Programming", "Engineering",
        "Science", "Healthcare", "Business", "Education",
    };

    private readonly Dictionary<string, HashSet<string>> _subjects = new(StringComparer.OrdinalIgnoreCase);

    public static ReasoningSubjects Parse(string text)
    {
        var map = new ReasoningSubjects();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var subject = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (subject.Length == 0) continue;
            map._subjects[subject] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static ReasoningSubjects Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<ReasoningSubjectScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "reasoning_subjects.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<ReasoningSubjectScore> Detect(IEnumerable<string> words, int top = 9)
    {
        if (_subjects.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string subject, int count)>();
        foreach (var (subject, keys) in _subjects)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((subject, count));
            total += count;
        }

        var scores = new List<ReasoningSubjectScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (subject, _) in raw) scores.Add(new ReasoningSubjectScore(subject, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (subject, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new ReasoningSubjectScore(subject, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<ReasoningSubjectScore> DefaultRanking()
    {
        double even = 100.0 / DefaultSubjects.Length;
        return DefaultSubjects.Select(t => new ReasoningSubjectScore(t, 0, even)).ToList();
    }
}
