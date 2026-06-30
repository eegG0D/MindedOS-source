using System.IO;

namespace MindedOS.Engine;

/// <summary>One drug in the catalog and the EEG-words that select it.</summary>
public sealed record DrugEntry(string Drug, string Class, string Treats, IReadOnlyList<string> Keywords);

/// <summary>A drug the EEG selected, with how many decoded words pointed at it.</summary>
public sealed record DrugPick(string Drug, string Class, string Treats, int Hits);

/// <summary>
/// The Healthcare AI formulary: the drug catalog AND the EEG→drug mapping, loaded
/// from eeg_map_drugs.csv (drug,class,treats,keywords). The recorded EEG-word
/// stream deterministically SELECTS which drugs to combine — drugs whose keywords
/// the brain hit most come first; the combination is then handed to LM Studio to
/// speculate on. Selection never depends on the language model.
/// </summary>
public sealed class DrugFormulary
{
    private readonly List<DrugEntry> _drugs = new();

    public IReadOnlyList<DrugEntry> Drugs => _drugs;

    public static DrugFormulary Parse(string text)
    {
        var f = new DrugFormulary();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("drug,", StringComparison.OrdinalIgnoreCase)) continue; // header
            var cols = line.Split(',');
            if (cols.Length < 4) continue;
            var drug = cols[0].Trim();
            var cls = cols[1].Trim();
            var treats = cols[2].Trim();
            var keywords = cols[3]
                .Split(new[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant()).ToList();
            if (drug.Length == 0) continue;
            f._drugs.Add(new DrugEntry(drug, cls, treats, keywords));
        }
        return f;
    }

    public static DrugFormulary Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    /// <summary>
    /// Deterministically select <paramref name="count"/> drugs to combine: rank by
    /// how often the decoded word stream hit each drug's keywords, breaking ties by
    /// catalog order, and always returning exactly <paramref name="count"/> drugs
    /// (filling from the catalog if the brain produced too few hits).
    /// </summary>
    public IReadOnlyList<DrugPick> Select(IEnumerable<string> words, int count = 4)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = (w ?? "").Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }

        var scored = new List<(DrugEntry entry, int hits, int order)>();
        for (int i = 0; i < _drugs.Count; i++)
        {
            int hits = 0;
            foreach (var k in _drugs[i].Keywords)
                if (freq.TryGetValue(k, out var c)) hits += c;
            scored.Add((_drugs[i], hits, i));
        }

        count = Math.Clamp(count, 1, _drugs.Count == 0 ? 1 : _drugs.Count);
        return scored
            .OrderByDescending(s => s.hits)
            .ThenBy(s => s.order)
            .Take(count)
            .Select(s => new DrugPick(s.entry.Drug, s.entry.Class, s.entry.Treats, s.hits))
            .ToList();
    }

    /// <summary>Deterministic offline speculation used when LM Studio is unavailable.</summary>
    public static string OfflineSpeculation(IReadOnlyList<DrugPick> picks)
    {
        var names = string.Join(" + ", picks.Select(p => p.Drug));
        var targets = string.Join("; ", picks.Select(p => p.Treats).Distinct());
        return
            $"This brain selected the combination **{names}**. Individually they address {targets}. " +
            "Speculatively, layering these agents could target an illness whose symptoms span those " +
            "domains at once — for example a viral upper-respiratory illness presenting with pain, " +
            "congestion, poor sleep and low immunity. The combination is a hypothesis to investigate, " +
            "not a regimen: real interactions, doses, contraindications and timing must be checked by " +
            "professionals before anything is ever combined.";
    }

    /// <summary>Render the saved Healthcare report (selection + speculation + disclaimer).</summary>
    public static string ToMarkdown(IReadOnlyList<DrugPick> picks, string illnessSpeculation,
        double avgAttention, double avgMeditation, string domKey, string seed)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("# Healthcare AI — Experimental Drug Combination\n\n");
        sb.Append("> **EXPERIMENTAL SPECULATION — NOT MEDICAL ADVICE.** This is an EEG-driven thought " +
                  "experiment in a brain-computer-interface OS. Do not combine, dose, or take any drugs " +
                  "based on it. Consult a licensed clinician or pharmacist.\n\n");
        sb.Append("## The EEG-selected combination\n");
        sb.Append("| Drug | Class | Individually treats | EEG hits |\n|---|---|---|---|\n");
        foreach (var p in picks)
            sb.Append($"| {p.Drug} | {p.Class} | {p.Treats} | {p.Hits} |\n");
        sb.Append("\n## Speculated medical solution\n");
        sb.Append(illnessSpeculation.Trim()).Append("\n\n");
        sb.Append("## Brain context\n");
        sb.Append($"- Focus (attention): {avgAttention:0}/100\n");
        sb.Append($"- Calm (meditation): {avgMeditation:0}/100\n");
        sb.Append($"- Dominant EEG band: {domKey}\n");
        sb.Append($"- Decoded words: {(string.IsNullOrWhiteSpace(seed) ? "(none)" : seed)}\n");
        return sb.ToString();
    }
}
