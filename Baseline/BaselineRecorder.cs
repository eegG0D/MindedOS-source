using System.IO;
using System.Text.Json;
using MindedOS.Core;
using MindedOS.Sensor;

namespace MindedOS.Baseline;

/// <summary>Summary of a completed baseline recording, persisted between runs.</summary>
public sealed record BaselineResult(
    DateTime RecordedAt,
    double AvgAttention,
    double AvgMeditation,
    string DominantBandKey,
    MentalProfile Profile,
    int RawSamples);

/// <summary>
/// Records a fixed-length window of EEG (default 5 minutes) from the shared
/// source, averages the metrics, derives the dominant band and mental profile,
/// and reports progress for the UI overlay.
/// </summary>
public sealed class BaselineRecorder
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(5);

    private readonly IEegSource _source;
    private readonly TimeSpan _duration;

    private long _attSum, _medSum, _attCount, _medCount, _rawCount;
    private readonly long[] _bandSums = new long[8];
    private long _bandFrames;

    public BaselineRecorder(IEegSource source, TimeSpan? duration = null)
    {
        _source = source;
        _duration = duration ?? DefaultDuration;
    }

    /// <summary>Fired roughly 10x/sec with progress in [0,1].</summary>
    public event Action<double>? Progress;

    public async Task<BaselineResult> RecordAsync(CancellationToken ct = default)
    {
        void Handler(EegEvent e)
        {
            switch (e)
            {
                case AttentionEvent a: Interlocked.Add(ref _attSum, a.Level); Interlocked.Increment(ref _attCount); break;
                case MeditationEvent m: Interlocked.Add(ref _medSum, m.Level); Interlocked.Increment(ref _medCount); break;
                case RawEvent: Interlocked.Increment(ref _rawCount); break;
                case SpectrumEvent sp:
                    lock (_bandSums)
                    {
                        _bandSums[0] += sp.Bands.Delta;
                        _bandSums[1] += sp.Bands.Theta;
                        _bandSums[2] += sp.Bands.LowAlpha;
                        _bandSums[3] += sp.Bands.HighAlpha;
                        _bandSums[4] += sp.Bands.LowBeta;
                        _bandSums[5] += sp.Bands.HighBeta;
                        _bandSums[6] += sp.Bands.LowGamma;
                        _bandSums[7] += sp.Bands.MidGamma;
                        _bandFrames++;
                    }
                    break;
            }
        }

        _source.Event += Handler;
        try
        {
            var start = DateTime.UtcNow;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var elapsed = DateTime.UtcNow - start;
                double frac = Math.Clamp(elapsed / _duration, 0, 1);
                Progress?.Invoke(frac);
                if (frac >= 1) break;
                await Task.Delay(100, ct);
            }
        }
        finally
        {
            _source.Event -= Handler;
        }

        double avgAtt = _attCount > 0 ? (double)_attSum / _attCount : 0;
        double avgMed = _medCount > 0 ? (double)_medSum / _medCount : 0;

        BandPowers meanBands = default;
        BandReading? dominant = null;
        lock (_bandSums)
        {
            if (_bandFrames > 0)
            {
                long F = _bandFrames;
                meanBands = new BandPowers(
                    (int)(_bandSums[0] / F), (int)(_bandSums[1] / F),
                    (int)(_bandSums[2] / F), (int)(_bandSums[3] / F),
                    (int)(_bandSums[4] / F), (int)(_bandSums[5] / F),
                    (int)(_bandSums[6] / F), (int)(_bandSums[7] / F));
                dominant = BandInterpreter.DominantBand(BandInterpreter.Interpret(meanBands));
            }
        }

        var profile = MentalProfileClassifier.Classify(avgAtt, avgMed, dominant);
        return new BaselineResult(
            DateTime.UtcNow, avgAtt, avgMed,
            dominant?.Key ?? "none", profile, (int)_rawCount);
    }

    // ---- Persistence ------------------------------------------------------
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "mindedOS", "baseline.json");

    public static void Save(BaselineResult result, string? path = null)
    {
        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(result,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    public static BaselineResult? Load(string? path = null)
    {
        path ??= DefaultPath;
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<BaselineResult>(File.ReadAllText(path)); }
        catch { return null; }
    }
}
