using MindedOS.Core;
using MindedOS.Sensor;

namespace MindedOS.Engine;

/// <summary>
/// Subscribes to the one shared <see cref="IEegSource"/> and keeps the latest
/// value of every named signal. Triggers and the UI read current values by name
/// instead of each re-parsing the stream.
/// </summary>
public sealed class SignalHub
{
    private readonly RawLexicon _lexicon;
    private readonly Dictionary<string, double> _values = new();
    private readonly object _gate = new();

    public SignalHub(IEegSource source, RawLexicon lexicon)
    {
        _lexicon = lexicon;
        source.Event += OnEvent;
    }

    /// <summary>Raised after each event with the signal name that changed.</summary>
    public event Action<string>? Updated;

    public int Attention { get; private set; }
    public int Meditation { get; private set; }
    public int SignalNoise { get; private set; } = 200;
    public int LastRaw { get; private set; }
    public int[] LastRawChannels { get; private set; } = Array.Empty<int>();
    public string[] LastChannelWords { get; private set; } = Array.Empty<string>();
    public int LastBlink { get; private set; }
    public string CurrentWord { get; private set; } = "—";
    public BandPowers Bands { get; private set; }

    public string FocusWord => BandInterpreter.FocusWord(Attention);
    public string CalmWord => BandInterpreter.CalmWord(Meditation);
    public bool Contact => SignalNoise < 200;

    /// <summary>Plain-language read of the poor-signal value (0 = clean … 200 = off-head).</summary>
    public string SignalQuality => SignalNoise switch
    {
        <= 0 => "clean",
        < 51 => "good",
        < 100 => "fair",
        < 200 => "poor",
        _ => "no contact",
    };

    /// <summary>The most recent raw sample converted to microvolts.</summary>
    public double Microvolts => BandInterpreter.RawToMicrovolts(LastRaw);

    /// <summary>All EEG channels' latest raw amplitude as one line, e.g.
    /// "c1:18 c2:-4 c3:22 …" — populated from the 16-channel OpenBCI frame.</summary>
    public string RawChannelSummary() => ChannelLine(c => LastRawChannels[c].ToString());

    /// <summary>Each EEG channel's amplitude mapped through the eeg_map.csv
    /// lexicon to a word, e.g. "c1:focus c2:calm c3:drift …".</summary>
    public string ChannelWordSummary() =>
        LastChannelWords.Length == 0 ? "—" : ChannelLine(c => LastChannelWords[c]);

    /// <summary>Join "cN:value" for every channel in the latest frame.</summary>
    private string ChannelLine(Func<int, string> value)
    {
        int n = LastRawChannels.Length;
        if (n == 0) return "—";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < n; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append('c').Append(i + 1).Append(':').Append(value(i));
        }
        return sb.ToString();
    }

    /// <summary>The eight band powers as one compact line, e.g. "δ40k θ.4M α-80k …".</summary>
    public string BandSummary()
    {
        var b = Bands;
        return $"δ{Short(b.Delta)} θ{Short(b.Theta)} " +
               $"α-{Short(b.LowAlpha)} α+{Short(b.HighAlpha)} " +
               $"β-{Short(b.LowBeta)} β+{Short(b.HighBeta)} " +
               $"γ-{Short(b.LowGamma)} γ+{Short(b.MidGamma)}";
    }

    /// <summary>Shorten a band power into k/M shorthand so the status bar stays readable.</summary>
    private static string Short(long value) =>
        value >= 1_000_000 ? $"{value / 1_000_000.0:0.0}M" :
        value >= 1_000 ? $"{value / 1_000.0:0}k" :
        value.ToString();

    public double GetSignal(string name)
    {
        lock (_gate)
            return _values.TryGetValue(name, out var v) ? v : 0;
    }

    private void Set(string name, double value)
    {
        lock (_gate) _values[name] = value;
        Updated?.Invoke(name);
    }

    private void OnEvent(EegEvent e)
    {
        switch (e)
        {
            case AttentionEvent a:
                Attention = a.Level;
                Set("attention", a.Level);
                break;
            case MeditationEvent m:
                Meditation = m.Level;
                Set("meditation", m.Level);
                break;
            case SignalEvent s:
                SignalNoise = s.Noise;
                Set("signal", s.Noise);
                break;
            case BlinkEvent b:
                LastBlink = b.Strength;
                Set("blink", b.Strength);
                break;
            case RawEvent r:
                LastRaw = r.Amplitude;
                if (_lexicon.IsLoaded) CurrentWord = _lexicon.WordFor(r.Amplitude);
                Set("raw", r.Amplitude);
                break;
            case RawFrameEvent rf:
                LastRawChannels = rf.Amplitudes;
                if (_lexicon.IsLoaded)
                {
                    var words = new string[rf.Amplitudes.Length];
                    for (int c = 0; c < words.Length; c++)
                        words[c] = _lexicon.WordFor(rf.Amplitudes[c]);
                    LastChannelWords = words;
                }
                Set("rawChannels", rf.Amplitudes.Length);
                break;
            case SpectrumEvent sp:
                Bands = sp.Bands;
                Set("delta", sp.Bands.Delta);
                Set("theta", sp.Bands.Theta);
                Set("lowAlpha", sp.Bands.LowAlpha);
                Set("highAlpha", sp.Bands.HighAlpha);
                Set("lowBeta", sp.Bands.LowBeta);
                Set("highBeta", sp.Bands.HighBeta);
                Set("lowGamma", sp.Bands.LowGamma);
                Set("midGamma", sp.Bands.MidGamma);
                break;
        }
    }
}
