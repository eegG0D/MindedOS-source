using System.IO;

namespace MindedOS.Engine;

/// <summary>A task category and how many decoded words matched it.</summary>
public sealed record TaskCategoryScore(string Category, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to the 10 task categories via task_categories.csv. Self-contained keyword
/// ranker for the Task Automation program; also assigns a single concept to its best-matching category.
/// </summary>
public sealed class TaskCategories
{
    private static readonly string[] DefaultCategories =
    {
        "Programming", "Engineering", "Research", "Education", "Writing",
        "Robotics", "Artificial Intelligence", "Architecture", "Business", "Personal Development",
    };

    private readonly Dictionary<string, HashSet<string>> _categories = new(StringComparer.OrdinalIgnoreCase);

    public static TaskCategories Parse(string text)
    {
        var map = new TaskCategories();
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

    public static TaskCategories Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static TaskCategories LoadFromDir(string dataDir)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "task_categories.csv"));
        try { if (File.Exists(path)) return Load(path); }
        catch { /* fall through */ }
        var map = new TaskCategories();
        foreach (var c in DefaultCategories) map._categories[c] = new HashSet<string>();
        return map;
    }

    public static IReadOnlyList<TaskCategoryScore> DetectFromFile(string dataDir, IEnumerable<string> words)
        => LoadFromDir(dataDir).Detect(words);

    /// <summary>The category whose keyword set contains the concept, else "Personal Development".</summary>
    public string CategoryOf(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        foreach (var (category, keys) in _categories) if (keys.Contains(w)) return category;
        return "Personal Development";
    }

    public IReadOnlyList<TaskCategoryScore> Detect(IEnumerable<string> words, int top = 10)
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

        var scores = new List<TaskCategoryScore>();
        if (total == 0)
        {
            double even = 100.0 / Math.Max(raw.Count, 1);
            foreach (var (category, _) in raw) scores.Add(new TaskCategoryScore(category, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (category, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new TaskCategoryScore(category, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<TaskCategoryScore> DefaultRanking()
    {
        double even = 100.0 / DefaultCategories.Length;
        return DefaultCategories.Select(c => new TaskCategoryScore(c, 0, even)).ToList();
    }
}
