using System.IO;

namespace MindedOS.Engine;

/// <summary>A profession and how many decoded words matched its keywords.</summary>
public sealed record ProfessionScore(string Profession, int Count, double Percent);

/// <summary>
/// Maps EEG-decoded words to professions via profession_map.csv: each profession
/// owns a keyword set, and a recorded word stream is classified by how frequently
/// its words hit each profession's keywords — yielding the top professions.
/// </summary>
public sealed class ProfessionMap
{
    private readonly Dictionary<string, HashSet<string>> _professions = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, HashSet<string>> Professions => _professions;

    public static ProfessionMap Parse(string text)
    {
        var map = new ProfessionMap();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("profession", StringComparison.OrdinalIgnoreCase)) continue; // header
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var profession = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (profession.Length == 0) continue;
            map._professions[profession] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static ProfessionMap Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    /// <summary>Rank professions by how often the word stream hits their keywords.</summary>
    public IReadOnlyList<ProfessionScore> Analyze(IEnumerable<string> words, int top = 10)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        var scores = new List<ProfessionScore>();
        long totalMatched = 0;
        var raw = new List<(string prof, int count)>();
        foreach (var (prof, keys) in _professions)
        {
            int count = 0;
            foreach (var k in keys)
                if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((prof, count));
            totalMatched += count;
        }
        if (totalMatched == 0) totalMatched = 1;

        foreach (var (prof, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new ProfessionScore(prof, count, 100.0 * count / totalMatched));

        return scores.Take(top).ToList();
    }

    /// <summary>Read the "word" column from a recorded Big Data CSV.</summary>
    public static List<string> WordsFromCsv(string path)
    {
        var words = new List<string>();
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0) return words;

        var header = lines[0].Split(',');
        int wordCol = Array.FindIndex(header, h => h.Trim().Equals("word", StringComparison.OrdinalIgnoreCase));
        if (wordCol < 0) wordCol = 1; // big-data format is t_sec,word,...

        for (int i = 1; i < lines.Length; i++)
        {
            var c = lines[i].Split(',');
            if (c.Length > wordCol) words.Add(c[wordCol].Trim());
        }
        return words;
    }
}
