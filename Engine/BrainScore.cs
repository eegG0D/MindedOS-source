using System.IO;

namespace MindedOS.Engine;

/// <summary>A user's EEG "subject" scores (0–100) for the neural-network leaderboard.</summary>
public sealed record BrainScores(
    string User,
    double Positivity,
    double Advance,
    double Particular,
    double Overall);

/// <summary>
/// Turns a recorded EEG feature vector into comparable "subject" scores and ranks
/// a folder of per-user CSVs into a leaderboard — a network of brains.
/// </summary>
public static class BrainScorer
{
    public static BrainScores Score(string user, BrainFeatureVector v)
    {
        var s = v.ShareVector(); // [delta, theta, alpha, beta, gamma, att/100, med/100, blink]
        double alpha = s[2], beta = s[3], gamma = s[4];
        double maxBandShare = Math.Max(Math.Max(s[0], s[1]), Math.Max(alpha, Math.Max(beta, gamma)));
        double signalQuality = Math.Clamp((200.0 - v.AvgSignal) / 2.0, 0, 100); // 0 noise → 100

        // Positivity: calm + relaxed alpha. Advance: focus + fast bands.
        // Particular: clean signal + a sharply dominant band. Overall: their mean.
        double positivity = Math.Clamp(v.AvgMeditation * 0.7 + alpha * 100 * 0.6, 0, 100);
        double advance = Math.Clamp(v.AvgAttention * 0.7 + (beta + gamma) * 100 * 0.5, 0, 100);
        double particular = Math.Clamp(signalQuality * 0.5 + maxBandShare * 100 * 0.5, 0, 100);
        double overall = Math.Clamp((positivity + advance + particular) / 3.0, 0, 100);

        return new BrainScores(user, positivity, advance, particular, overall);
    }

    /// <summary>Scan a folder of per-user CSVs and rank them best-brain-first.</summary>
    public static IReadOnlyList<BrainScores> RankFolder(string folder)
    {
        var list = new List<BrainScores>();
        if (!Directory.Exists(folder)) return list;

        foreach (var path in Directory.EnumerateFiles(folder, "*.csv"))
        {
            try
            {
                var vec = BrainFeatureVector.FromCsv(path);
                list.Add(Score(Path.GetFileNameWithoutExtension(path), vec));
            }
            catch { /* skip unreadable/foreign csv */ }
        }
        return list.OrderByDescending(s => s.Overall).ToList();
    }
}
