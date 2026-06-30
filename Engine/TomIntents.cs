using System.IO;

namespace MindedOS.Engine;

/// <summary>An intent category and how many decoded words matched it.</summary>
public sealed record TomIntentScore(string Intent, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to the 9 intent categories via tom_intents.csv. Self-contained keyword
/// ranker for the Theory of Mind program. Inferred intents are probabilistic hypotheses, not facts.
/// </summary>
public sealed class TomIntents
{
    private static readonly string[] DefaultIntents =
    {
        "Learning", "Exploration", "Innovation", "Creativity", "Leadership",
        "Research", "Problem Solving", "Design", "Communication",
    };

    private readonly Dictionary<string, HashSet<string>> _intents = new(StringComparer.OrdinalIgnoreCase);

    public static TomIntents Parse(string text)
    {
        var map = new TomIntents();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var intent = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (intent.Length == 0) continue;
            map._intents[intent] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static TomIntents Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<TomIntentScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "tom_intents.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<TomIntentScore> Detect(IEnumerable<string> words, int top = 9)
    {
        if (_intents.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string intent, int count)>();
        foreach (var (intent, keys) in _intents)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((intent, count));
            total += count;
        }

        var scores = new List<TomIntentScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (intent, _) in raw) scores.Add(new TomIntentScore(intent, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (intent, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new TomIntentScore(intent, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<TomIntentScore> DefaultRanking()
    {
        double even = 100.0 / DefaultIntents.Length;
        return DefaultIntents.Select(i => new TomIntentScore(i, 0, even)).ToList();
    }
}
