namespace MindedOS.Core;

/// <summary>The mental profiles a baseline can resolve to.</summary>
public enum MentalProfile
{
    Neutral,
    Focused,
    Flow,
    Relaxed,
    Drowsy,
    Stressed,
}

/// <summary>Classifies a baseline summary into a single dominant mental profile.</summary>
public static class MentalProfileClassifier
{
    /// <param name="avgAttention">0..100 mean attention over the baseline.</param>
    /// <param name="avgMeditation">0..100 mean meditation over the baseline.</param>
    /// <param name="dominant">Dominant band reading over the baseline (may be null).</param>
    public static MentalProfile Classify(double avgAttention, double avgMeditation, BandReading? dominant)
    {
        string band = dominant?.Key ?? "";

        // High-beta dominance with high attention reads as stress vs. focus by calm level.
        if (band is "highBeta" or "lowBeta")
            return avgMeditation < 35 && avgAttention > 60 ? MentalProfile.Stressed
                 : avgAttention > 55 ? MentalProfile.Focused
                 : MentalProfile.Neutral;

        if (band == "highAlpha")
            return avgAttention > 55 ? MentalProfile.Flow : MentalProfile.Relaxed;

        if (band == "lowAlpha")
            return MentalProfile.Relaxed;

        if (band is "theta" or "delta")
            return avgAttention < 35 ? MentalProfile.Drowsy : MentalProfile.Relaxed;

        if (band is "lowGamma" or "midGamma")
            return MentalProfile.Focused;

        // Fall back to attention/meditation balance when no band dominates clearly.
        if (avgAttention >= 70) return MentalProfile.Focused;
        if (avgMeditation >= 70) return MentalProfile.Relaxed;
        if (avgAttention < 30 && avgMeditation < 30) return MentalProfile.Stressed;
        if (avgAttention < 30) return MentalProfile.Drowsy;
        return MentalProfile.Neutral;
    }

    public static string Describe(MentalProfile profile) => profile switch
    {
        MentalProfile.Focused => "Focused — sharp, attentive, ready for demanding tasks.",
        MentalProfile.Flow => "Flow — relaxed concentration and peak creativity.",
        MentalProfile.Relaxed => "Relaxed — calm, settled, low arousal.",
        MentalProfile.Drowsy => "Drowsy — low attention, drifting toward rest.",
        MentalProfile.Stressed => "Stressed — hyper-alert, tense, high beta.",
        _ => "Neutral — balanced baseline state.",
    };
}
