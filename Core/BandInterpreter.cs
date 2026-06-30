namespace MindedOS.Core;

/// <summary>One band's plain-language reading at the current power level.</summary>
public sealed record BandReading(
    string Key, string Symbol, string Name, string Text,
    int Tier, double Intensity, long Value);

/// <summary>
/// Translates raw band powers into tiered, human-readable descriptions.
/// Ported from BandInterpreter / BAND_PROFILES in translator.php.
/// </summary>
public static class BandInterpreter
{
    private sealed record Profile(string Key, string Symbol, string Name, long[] Ceilings, string[] Descriptions);

    private static readonly Profile[] Profiles =
    {
        new("delta", "δ", "Delta", new long[]{100_000, 5_000_000}, new[]{
            "Low delta: Mentally alert or stressed",
            "Medium delta: Deep relaxation or light sleep",
            "High delta: Deep sleep or unconsciousness"}),
        new("theta", "θ", "Theta", new long[]{100_000, 5_000_000}, new[]{
            "Low theta: Distracted or anxious state",
            "Medium theta: Meditation or creative thinking",
            "High theta: Dream-like states or deep meditation"}),
        new("lowAlpha", "α−", "Low Alpha", new long[]{50_000, 2_000_000}, new[]{
            "Low alphaLow: Mentally tense or agitated",
            "Medium alphaLow: Calm and alert",
            "High alphaLow: Deep relaxation or passive awareness"}),
        new("highAlpha", "α+", "High Alpha", new long[]{50_000, 2_000_000}, new[]{
            "Low alphaHigh: Lack of mental coordination",
            "Medium alphaHigh: Coordinated relaxation and focus",
            "High alphaHigh: Flow state or peak creativity"}),
        new("lowBeta", "β−", "Low Beta", new long[]{30_000, 1_000_000}, new[]{
            "Low betaLow: Daydreaming or disengaged",
            "Medium betaLow: Focused thinking and attention",
            "High betaLow: Intense focus or anxiety"}),
        new("highBeta", "β+", "High Beta", new long[]{30_000, 1_000_000}, new[]{
            "Low betaHigh: Relaxed cognitive state",
            "Medium betaHigh: Alertness and logical thinking",
            "High betaHigh: Hyper-alert or stressed mind"}),
        new("lowGamma", "γ−", "Low Gamma", new long[]{10_000, 500_000}, new[]{
            "Low gammaLow: Low cognitive load",
            "Medium gammaLow: Moderate learning or memory use",
            "High gammaLow: High-level information processing"}),
        new("midGamma", "γ+", "Mid Gamma", new long[]{10_000, 500_000}, new[]{
            "Low gammaMid: Minimal sensory integration",
            "Medium gammaMid: Cognitive engagement",
            "High gammaMid: Heightened consciousness or insight"}),
    };

    private static int TierOf(long value, long[] ceilings) =>
        value <= ceilings[0] ? 0 : value <= ceilings[1] ? 1 : 2;

    public static IReadOnlyList<BandReading> Interpret(BandPowers bands)
    {
        var result = new List<BandReading>(Profiles.Length);
        foreach (var p in Profiles)
        {
            long value = bands[p.Key];
            int tier = TierOf(value, p.Ceilings);
            result.Add(new BandReading(
                p.Key, p.Symbol, p.Name, p.Descriptions[tier],
                tier, (double)value / p.Ceilings[1], value));
        }
        return result;
    }

    public static BandReading? DominantBand(IReadOnlyList<BandReading> readings)
    {
        BandReading? best = null;
        foreach (var r in readings)
            if (best is null || r.Intensity > best.Intensity) best = r;
        return best;
    }

    public static string FocusWord(int level) => level switch
    {
        < 20 => "Unfocused",
        < 45 => "Drifting",
        < 70 => "Engaged",
        < 88 => "Focused",
        _ => "Locked in",
    };

    public static string CalmWord(int level) => level switch
    {
        < 20 => "Restless",
        < 45 => "Unsettled",
        < 70 => "Easing",
        < 88 => "Calm",
        _ => "Deeply calm",
    };

    public static double RawToMicrovolts(int amplitude) =>
        amplitude * 1.8 / 4096 / 2000 * 1_000_000;
}
