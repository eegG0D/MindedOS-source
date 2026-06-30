using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>One contestant brain (human or AI) and its scientific EEG score.</summary>
public sealed record Contestant(
    string Name, double Score, double AvgAttention, double AvgMeditation,
    string DominantBand, MentalProfile Profile, string Seed, int DistinctWords);

/// <summary>
/// Scores a brain's EEG for scientific reasoning and runs the Human-vs-AI duel.
/// Science favours analytical fast-band (beta/gamma) activity, sustained focus and
/// lexical variety in the decoded words. The winner is decided deterministically
/// from the two EEG lists; LM Studio only writes the verdict.
/// </summary>
public static class ScienceDuel
{
    /// <summary>Scientific-EEG score, ~1–100, from the condition and decoded words.</summary>
    public static double Score(double avgAttention, double avgMeditation,
        IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        double total = 0;
        foreach (var b in bands) total += b.Value;
        if (total <= 0) total = 1;

        double Val(string key)
        {
            foreach (var b in bands) if (b.Key == key) return b.Value;
            return 0;
        }

        double fast = (Val("lowBeta") + Val("highBeta") + Val("lowGamma") + Val("midGamma")) / total; // 0..1
        int distinct = DistinctCount(words);
        double variety = Math.Min(distinct, 50) / 50.0; // 0..1 lexical richness

        double raw = avgAttention * 0.45 + fast * 100.0 * 0.40 + variety * 15.0;
        return Math.Clamp(raw, 1, 100);
    }

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

    /// <summary>The winning contestant (higher score wins; human wins exact ties).</summary>
    public static Contestant Winner(Contestant human, Contestant ai) =>
        ai.Score > human.Score ? ai : human;

    public static double Margin(Contestant a, Contestant b) => Math.Abs(a.Score - b.Score);

    /// <summary>Deterministic offline verdict used when LM Studio is unavailable.</summary>
    public static string OfflineVerdict(Contestant human, Contestant ai)
    {
        var w = Winner(human, ai);
        return
            $"On the subject of science, **{w.Name} wins** with a scientific-EEG score of {w.Score:0} " +
            $"versus {(w.Name == human.Name ? ai.Score : human.Score):0} (margin {Margin(human, ai):0}). " +
            "The score rewards analytical fast-band (beta/gamma) activity, sustained focus and lexical " +
            $"variety. {human.Name} showed focus {human.AvgAttention:0} and {human.DistinctWords} distinct " +
            $"ideas; the AI (processor) brain showed focus {ai.AvgAttention:0} and {ai.DistinctWords}. " +
            "The more analytical, focused and varied EEG is judged the more scientific.";
    }

    /// <summary>Render the saved duel report (both lists + scores + verdict).</summary>
    public static string ToMarkdown(Contestant human, Contestant ai, string verdict, int seconds)
    {
        var w = Winner(human, ai);
        var sb = new System.Text.StringBuilder();
        sb.Append("# Human vs AI — The Most Scientific EEG\n\n");
        sb.Append($"**Subject:** Science  ·  **Each brain recorded:** {seconds / 60.0:0.#} min\n\n");
        sb.Append($"## 🏆 Winner: {w.Name} (scientific-EEG {w.Score:0} / 100)\n\n");
        sb.Append("| Contestant | Scientific-EEG score | Focus | Calm | Dominant band | Distinct ideas | State |\n");
        sb.Append("|---|---|---|---|---|---|---|\n");
        sb.Append(Row(human));
        sb.Append(Row(ai));
        sb.Append("\n## Verdict (subject: science)\n");
        sb.Append(verdict.Trim()).Append('\n');
        sb.Append("\n## The EEG lists\n");
        sb.Append($"**{human.Name} EEG (decoded):** {Short(human.Seed)}\n\n");
        sb.Append($"**{ai.Name} EEG (decoded):** {Short(ai.Seed)}\n");
        return sb.ToString();
    }

    private static string Row(Contestant c) =>
        $"| {c.Name} | {c.Score:0} | {c.AvgAttention:0} | {c.AvgMeditation:0} | {c.DominantBand} | {c.DistinctWords} | {c.Profile} |\n";

    private static string Short(string seed)
    {
        seed = (seed ?? "").Trim();
        if (seed.Length == 0) return "(none captured)";
        return seed.Length <= 200 ? seed : seed[..200] + "…";
    }
}
