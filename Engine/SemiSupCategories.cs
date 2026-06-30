using System.IO;

namespace MindedOS.Engine;

/// <summary>A classification category and how many decoded words matched it.</summary>
public sealed record SemiSupCategoryScore(string Category, int Count, double Percent);

/// <summary>Maps EEG-decoded words to categories via semi_sup_categories.csv. Mirrors <see cref="MasDomains"/>.</summary>
public sealed class SemiSupCategories
{
    private static readonly string[] DefaultCategories =
    {
        "Science", "Engineering", "Architecture", "Robotics", "Artificial Intelligence",
        "Mathematics", "Business", "Healthcare", "Education", "Creativity",
    };

    private readonly Dictionary<string, HashSet<string>> _categories = new(StringComparer.OrdinalIgnoreCase);

    public static SemiSupCategories Parse(string text)
    {
        var map = new SemiSupCategories();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var category = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (category.Length == 0) continue;
            map._categories[category] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static SemiSupCategories Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<SemiSupCategoryScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "semi_sup_categories.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<SemiSupCategoryScore> Detect(IEnumerable<string> words, int top = 10)
    {
        if (_categories.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string category, int count)>();
        foreach (var (category, keys) in _categories)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((category, count));
            total += count;
        }

        var scores = new List<SemiSupCategoryScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (category, _) in raw) scores.Add(new SemiSupCategoryScore(category, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (category, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new SemiSupCategoryScore(category, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<SemiSupCategoryScore> DefaultRanking()
    {
        double even = 100.0 / DefaultCategories.Length;
        return DefaultCategories.Select(c => new SemiSupCategoryScore(c, 0, even)).ToList();
    }
}
