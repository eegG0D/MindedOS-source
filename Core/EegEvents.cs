namespace MindedOS.Core;

/// <summary>Discriminated set of events an EEG source can emit.</summary>
public abstract record EegEvent;

public sealed record SignalEvent(int Noise) : EegEvent;          // 0 = clean, 200 = no contact
public sealed record AttentionEvent(int Level) : EegEvent;        // 0..100
public sealed record MeditationEvent(int Level) : EegEvent;       // 0..100
public sealed record BlinkEvent(int Strength) : EegEvent;         // 0..255
public sealed record RawEvent(int Amplitude) : EegEvent;          // primary channel, signed 16-bit
public sealed record RawFrameEvent(int[] Amplitudes) : EegEvent;  // one signed-16-bit sample per EEG channel
public sealed record SpectrumEvent(BandPowers Bands) : EegEvent;

/// <summary>The eight EEG band powers (delta…mid-gamma).</summary>
public readonly record struct BandPowers(
    int Delta, int Theta,
    int LowAlpha, int HighAlpha,
    int LowBeta, int HighBeta,
    int LowGamma, int MidGamma)
{
    public int this[string key] => key switch
    {
        "delta" => Delta,
        "theta" => Theta,
        "lowAlpha" => LowAlpha,
        "highAlpha" => HighAlpha,
        "lowBeta" => LowBeta,
        "highBeta" => HighBeta,
        "lowGamma" => LowGamma,
        "midGamma" => MidGamma,
        _ => 0,
    };
}
