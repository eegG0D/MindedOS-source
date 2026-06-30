using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Computes a cognition score (1–200%) from the EEG — how calculative, powerful
/// and analytical the brain is — from attention and the fast (beta/gamma) bands.
/// </summary>
public static class CognitionIndex
{
    public static double Compute(double avgAttention, double avgMeditation, IReadOnlyList<BandReading> bands)
    {
        double total = 0;
        foreach (var b in bands) total += b.Value;
        if (total <= 0) total = 1;

        double Val(string key)
        {
            foreach (var b in bands) if (b.Key == key) return b.Value;
            return 0;
        }

        double beta = (Val("lowBeta") + Val("highBeta")) / total;
        double gamma = (Val("lowGamma") + Val("midGamma")) / total;
        double analytical = beta + gamma;            // 0..1, fast-band cognition

        // attention drives the calculative/focused contribution; fast bands the
        // analytical/powerful one. Can exceed 100% for exceptional brains.
        double raw = avgAttention * 1.0 + analytical * 120.0;
        return Math.Clamp(raw, 1, 200);
    }

    public static string Tier(double score) => score switch
    {
        < 50 => "Resting",
        < 90 => "Capable",
        < 120 => "Sharp",
        < 160 => "Powerful",
        _ => "Exceptional",
    };

    public static string DefaultAssessment(double score, double avgAttention) =>
        $"This brain's cognition index is {score:0}% on a 1–200% scale ({Tier(score)}). It reflects " +
        $"focus of {avgAttention:0}/100 combined with the share of fast analytical (beta/gamma) activity " +
        "in the spectrum. Higher scores mean a more calculative, analytical and powerful cognitive state; " +
        "scores above 100% indicate unusually engaged, fast-band-dominant cognition. (Single-channel " +
        "consumer EEG — a directional indicator, not a clinical measure.)";
}
