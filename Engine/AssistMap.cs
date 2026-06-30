using System.IO;
using System.Globalization;

namespace MindedOS.Engine;

/// <summary>One assistant mapping row: an EEG amplitude, its English word, and the offered service.</summary>
public sealed record AssistEntry(int Eeg, string Word, string Service);

/// <summary>An offer the assistant made for one matched EEG reading.</summary>
public sealed record AssistOffer(int Eeg, double Reading, string Word, string Service);

/// <summary>
/// The Intelligent Assistant map (eeg_map_assist.csv): column A is an EEG
/// amplitude, column B the English word, column C the service the assistant
/// offers. A live EEG reading is matched to the nearest row; three readings give
/// three distinct offers per try.
/// </summary>
public sealed class AssistMap
{
    private readonly List<AssistEntry> _entries = new();

    public IReadOnlyList<AssistEntry> Entries => _entries;

    public static AssistMap Parse(string text)
    {
        var map = new AssistMap();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("eeg,", StringComparison.OrdinalIgnoreCase)) continue; // header
            var cols = line.Split(',');
            if (cols.Length < 3) continue;
            if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var eeg)) continue;
            var word = cols[1].Trim();
            // the service may itself contain commas — keep everything after the 2nd comma
            var service = string.Join(",", cols[2..]).Trim();
            if (service.Length == 0) continue;
            map._entries.Add(new AssistEntry(eeg, word, service));
        }
        return map;
    }

    public static AssistMap Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    /// <summary>The entry whose EEG value is nearest the reading, optionally skipping used rows.</summary>
    public AssistEntry? Nearest(double reading, ISet<int>? used = null)
    {
        AssistEntry? best = null;
        double bestDist = double.MaxValue;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (used is not null && used.Contains(i)) continue;
            double dist = Math.Abs(_entries[i].Eeg - reading);
            if (dist < bestDist) { bestDist = dist; best = _entries[i]; }
        }
        return best;
    }

    /// <summary>
    /// Match three EEG readings to three DISTINCT offers (each reading takes the
    /// nearest row not already offered), returning the assistant's services.
    /// </summary>
    public IReadOnlyList<AssistOffer> ThreeOffers(IReadOnlyList<double> readings)
    {
        var offers = new List<AssistOffer>();
        var used = new HashSet<int>();
        foreach (var r in readings)
        {
            int idx = NearestIndex(r, used);
            if (idx < 0) break;
            used.Add(idx);
            var e = _entries[idx];
            offers.Add(new AssistOffer(e.Eeg, r, e.Word, e.Service));
            if (offers.Count == 3) break;
        }
        return offers;
    }

    private int NearestIndex(double reading, ISet<int> used)
    {
        int best = -1;
        double bestDist = double.MaxValue;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (used.Contains(i)) continue;
            double dist = Math.Abs(_entries[i].Eeg - reading);
            if (dist < bestDist) { bestDist = dist; best = i; }
        }
        return best;
    }
}
