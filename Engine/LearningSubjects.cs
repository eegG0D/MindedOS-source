using System.IO;

namespace MindedOS.Engine;

/// <summary>A subject of interest and how many decoded words matched its keywords.</summary>
public sealed record SubjectScore(string Subject, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to subjects of interest via learning_subjects.csv: each
/// subject owns a keyword set, and a recorded word stream is classified by how
/// frequently its words hit each subject's keywords — yielding ranked interests.
/// Mirrors <see cref="ProfessionMap"/>.
/// </summary>
public sealed class LearningSubjects
{
    private static readonly string[] DefaultSubjects =
    {
        "Science", "Mathematics", "Engineering", "Artificial Intelligence",
        "Architecture", "Robotics", "Programming", "Neuroscience",
        "Business", "Economics",
    };

    private readonly Dictionary<string, HashSet<string>> _subjects = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, HashSet<string>> Subjects => _subjects;

    public static LearningSubjects Parse(string text)
    {
        var map = new LearningSubjects();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("subject", StringComparison.OrdinalIgnoreCase)) continue; // header
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

    public static LearningSubjects Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    /// <summary>Load data\learning_subjects.csv; fall back to an even default ranking.</summary>
    public static IReadOnlyList<SubjectScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "learning_subjects.csv"));
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

    /// <summary>Rank subjects by how often the word stream hits their keywords.</summary>
    public IReadOnlyList<SubjectScore> Detect(IEnumerable<string> words, int top = 10)
    {
        if (_subjects.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long totalMatched = 0;
        var rawCounts = new List<(string subject, int count)>();
        foreach (var (subject, keys) in _subjects)
        {
            int count = 0;
            foreach (var k in keys)
                if (freq.TryGetValue(k, out var c)) count += c;
            rawCounts.Add((subject, count));
            totalMatched += count;
        }

        var scores = new List<SubjectScore>();
        if (totalMatched == 0)
        {
            // No hits: list every subject with an even share so downstream rendering works.
            double even = 100.0 / rawCounts.Count;
            foreach (var (subject, _) in rawCounts)
                scores.Add(new SubjectScore(subject, 0, even));
            return scores.Take(top).ToList();
        }

        foreach (var (subject, count) in rawCounts.OrderByDescending(r => r.count))
            scores.Add(new SubjectScore(subject, count, 100.0 * count / totalMatched));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<SubjectScore> DefaultRanking()
    {
        double even = 100.0 / DefaultSubjects.Length;
        return DefaultSubjects.Select(s => new SubjectScore(s, 0, even)).ToList();
    }
}
