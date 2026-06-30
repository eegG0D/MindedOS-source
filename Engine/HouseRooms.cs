using System.IO;

namespace MindedOS.Engine;

/// <summary>A house room and how many decoded words matched it.</summary>
public sealed record HouseRoomScore(string Room, int Count, double Percent);

/// <summary>Maps EEG-decoded words to house rooms via house_rooms.csv. Mirrors <see cref="MasDomains"/>.</summary>
public sealed class HouseRooms
{
    private static readonly string[] DefaultRooms =
    {
        "Living Room", "Bedroom", "Office", "Kitchen", "Workshop",
        "Entertainment Room", "Laboratory", "Study Room",
    };

    private readonly Dictionary<string, HashSet<string>> _rooms = new(StringComparer.OrdinalIgnoreCase);

    public static HouseRooms Parse(string text)
    {
        var map = new HouseRooms();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("topic", StringComparison.OrdinalIgnoreCase)) continue;
            int comma = line.IndexOf(',');
            if (comma <= 0) continue;
            var room = line[..comma].Trim();
            var keywords = line[(comma + 1)..]
                .Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant());
            if (room.Length == 0) continue;
            map._rooms[room] = new HashSet<string>(keywords);
        }
        return map;
    }

    public static HouseRooms Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    public static IReadOnlyList<HouseRoomScore> DetectFromFile(string dataDir, IEnumerable<string> words)
    {
        var path = MindedOS.Core.DataFile.Resolve(Path.Combine(dataDir, "house_rooms.csv"));
        try { if (File.Exists(path)) return Load(path).Detect(words); }
        catch { /* fall through */ }
        return DefaultRanking();
    }

    public IReadOnlyList<HouseRoomScore> Detect(IEnumerable<string> words, int top = 8)
    {
        if (_rooms.Count == 0) return DefaultRanking();

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        long total = 0;
        var raw = new List<(string room, int count)>();
        foreach (var (room, keys) in _rooms)
        {
            int count = 0;
            foreach (var k in keys) if (freq.TryGetValue(k, out var c)) count += c;
            raw.Add((room, count));
            total += count;
        }

        var scores = new List<HouseRoomScore>();
        if (total == 0)
        {
            double even = 100.0 / raw.Count;
            foreach (var (room, _) in raw) scores.Add(new HouseRoomScore(room, 0, even));
            return scores.Take(top).ToList();
        }
        foreach (var (room, count) in raw.OrderByDescending(r => r.count))
            scores.Add(new HouseRoomScore(room, count, 100.0 * count / total));
        return scores.Take(top).ToList();
    }

    private static IReadOnlyList<HouseRoomScore> DefaultRanking()
    {
        double even = 100.0 / DefaultRooms.Length;
        return DefaultRooms.Select(r => new HouseRoomScore(r, 0, even)).ToList();
    }
}
