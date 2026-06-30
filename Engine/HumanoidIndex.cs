using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>One humanoid trait dimension, its score, and the edit to perfect it.</summary>
public sealed record HumanoidDimension(
    string Name, double Score, bool Satisfied, string Edit, IReadOnlyList<string> Words);

/// <summary>The full humanoid assessment: percentage, dimensions, edits and words.</summary>
public sealed record HumanoidProfile(
    double Percent, IReadOnlyList<HumanoidDimension> Dimensions,
    IReadOnlyList<string> Edits, IReadOnlyList<string> HumanoidWords);

/// <summary>
/// Measures how much the user's brain matches a HUMANOID (0–100%) across six
/// human-defining trait dimensions, and computes the exact brain edits needed to
/// reach 100% and the words that would make it more humanoid. Everything here is
/// deterministic — LM Studio only writes the detailed explanation.
/// </summary>
public static class HumanoidIndex
{
    private const double Threshold = 60; // a dimension at/above this is "humanoid enough"

    // Word triggers per word-based dimension (everyday English the lexicon decodes).
    private static readonly (string name, string[] words)[] WordDims =
    {
        ("Emotional resonance", new[] { "love", "hope", "fear", "joy", "sad", "anger", "feel", "calm" }),
        ("Social communication", new[] { "talk", "speak", "word", "friend", "share", "help", "listen", "smile" }),
        ("Sensorimotor embodiment", new[] { "hand", "face", "walk", "move", "see", "hear", "touch", "eye" }),
    };

    public static HumanoidProfile Compute(double avgAttention, double avgMeditation,
        IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var freq = WordFreq(words);
        double total = 0;
        foreach (var b in bands) total += b.Value;
        if (total <= 0) total = 1;
        double Val(string key) { foreach (var b in bands) if (b.Key == key) return b.Value; return 0; }

        var dims = new List<HumanoidDimension>();

        // 1–3: the three word-based human traits
        foreach (var (name, triggers) in WordDims)
        {
            int hits = 0;
            var missing = new List<string>();
            foreach (var t in triggers)
            {
                if (freq.TryGetValue(t, out var c) && c > 0) hits += c;
                else missing.Add(t);
            }
            double score = Math.Clamp(Math.Min(hits, 4) / 4.0 * 100, 0, 100);
            bool ok = score >= Threshold;
            // words that would make it more humanoid for this trait (the missing triggers)
            var suggest = missing.Count > 0 ? missing.GetRange(0, Math.Min(4, missing.Count)) : new List<string>();
            string edit = ok ? "" :
                $"Edit — {name}: think with {Join(suggest)} so your decoded EEG carries this human trait.";
            dims.Add(new HumanoidDimension(name, score, ok, edit, suggest));
        }

        // 4: balanced arousal — humanoid focus sits in a human midrange (~55)
        double arousal = Math.Clamp(100 - Math.Abs(avgAttention - 55) * 2.2, 0, 100);
        dims.Add(new HumanoidDimension("Balanced arousal", arousal, arousal >= Threshold,
            arousal >= Threshold ? "" :
            (avgAttention > 55
                ? "Edit — Balanced arousal: lower over-focus toward a calm human midrange (~55)."
                : "Edit — Balanced arousal: raise engagement toward a human midrange (~55)."),
            Array.Empty<string>()));

        // 5: reflective calm — alpha presence is the human resting signature
        double alpha = (Val("lowAlpha") + Val("highAlpha")) / total;
        double calm = Math.Clamp(alpha * 100 * 2.0, 0, 100);
        dims.Add(new HumanoidDimension("Reflective calm", calm, calm >= Threshold,
            calm >= Threshold ? "" :
            "Edit — Reflective calm: rest into an alpha state (relax, breathe) to add the human resting rhythm.",
            Array.Empty<string>()));

        // 6: lexical richness — humans range widely in thought
        int distinct = DistinctCount(words);
        double richness = Math.Clamp(distinct / 30.0 * 100, 0, 100);
        dims.Add(new HumanoidDimension("Lexical richness", richness, richness >= Threshold,
            richness >= Threshold ? "" :
            "Edit — Lexical richness: let your mind wander across more topics to widen the decoded vocabulary.",
            Array.Empty<string>()));

        double percent = Math.Round(dims.Average(d => d.Score));
        var edits = dims.Where(d => !d.Satisfied).Select(d => d.Edit).ToList();
        var humanoidWords = dims.SelectMany(d => d.Words).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new HumanoidProfile(percent, dims, edits, humanoidWords);
    }

    public static string Tier(double pct) => pct switch
    {
        < 40 => "Machine-leaning",
        < 60 => "Part-humanoid",
        < 80 => "Largely humanoid",
        < 100 => "Near-humanoid",
        _ => "Fully humanoid",
    };

    public static int DistinctCount(IReadOnlyList<string> words)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = (w ?? "").Trim();
            if (word.Length == 0 || word == "—") continue;
            set.Add(word.ToLowerInvariant());
        }
        return set.Count;
    }

    private static Dictionary<string, int> WordFreq(IReadOnlyList<string> words)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = (w ?? "").Trim().ToLowerInvariant();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }
        return freq;
    }

    private static string Join(IReadOnlyList<string> w) => w.Count == 0 ? "richer human words" : string.Join(", ", w);

    /// <summary>Deterministic offline explanation when LM Studio is unavailable.</summary>
    public static string OfflineExplanation(HumanoidProfile p)
    {
        var weak = p.Dimensions.Where(d => !d.Satisfied).Select(d => d.Name).ToList();
        string weakText = weak.Count == 0 ? "every human trait is already satisfied" : string.Join(", ", weak);
        return
            $"Your brain matches a humanoid at {p.Percent:0}% ({Tier(p.Percent)}). The score is the average of " +
            "six human-defining trait dimensions measured from your EEG and decoded words. The dimensions " +
            $"still below the humanoid threshold are: {weakText}. Applying the {p.Edits.Count} brain edit(s) " +
            "listed — each one lifting a weak dimension to the human range — would bring you to 100% humanoid. " +
            (p.HumanoidWords.Count > 0
                ? $"Thinking with words such as {string.Join(", ", p.HumanoidWords.Take(8))} makes your EEG read as more humanoid."
                : "Your decoded vocabulary already carries the human traits.");
    }

    /// <summary>Render the full humanoid report Markdown (later turned into a PDF).</summary>
    public static string ToMarkdown(HumanoidProfile p, string detailedExplanation,
        double avgAttention, double avgMeditation, string domKey, string seed)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("# Humanoid Match Report\n\n");
        sb.Append($"## You are {p.Percent:0}% humanoid ({Tier(p.Percent)})\n\n");
        sb.Append($"Brain edits needed to reach 100% humanoid: **{p.Edits.Count}**\n\n");

        sb.Append("## Trait dimensions\n");
        sb.Append("| Dimension | Score | Humanoid? |\n|---|---|---|\n");
        foreach (var d in p.Dimensions)
            sb.Append($"| {d.Name} | {d.Score:0}/100 | {(d.Satisfied ? "yes" : "needs edit")} |\n");

        sb.Append("\n## The brain edits to reach 100%\n");
        if (p.Edits.Count == 0) sb.Append("- None — your brain is already 100% humanoid.\n");
        else foreach (var e in p.Edits) sb.Append($"- {e}\n");

        sb.Append("\n## Words that make you more humanoid\n");
        sb.Append(p.HumanoidWords.Count == 0
            ? "- (your decoded words already cover the human traits)\n"
            : "- " + string.Join("\n- ", p.HumanoidWords) + "\n");

        sb.Append("\n## Detailed explanation\n");
        sb.Append(detailedExplanation.Trim()).Append('\n');

        sb.Append("\n## Brain context\n");
        sb.Append($"- Focus (attention): {avgAttention:0}/100\n");
        sb.Append($"- Calm (meditation): {avgMeditation:0}/100\n");
        sb.Append($"- Dominant EEG band: {domKey}\n");
        sb.Append($"- Decoded words: {(string.IsNullOrWhiteSpace(seed) ? "(none)" : seed)}\n");
        return sb.ToString();
    }
}
