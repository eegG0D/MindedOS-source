using System.Globalization;
using System.IO;
using MindedOS.Core;
using MindedOS.Sensor;

namespace MindedOS.Engine;

/// <summary>
/// Records an EEG source's stats to a CSV (one row per second) for a fixed window
/// and returns the aggregate <see cref="BrainFeatureVector"/>. Used to capture both
/// the CPU "artificial brain" and the human recording for comparison.
/// </summary>
public sealed class EegCsvRecorder
{
    public async Task<BrainFeatureVector> RecordAsync(
        IEegSource source, int seconds, string csvPath,
        Action<double>? progress = null, CancellationToken ct = default)
    {
        int attention = 0, meditation = 0, blink = 0, signal = 200;
        BandPowers bands = default;
        long blinkRows = 0;

        void OnEvent(EegEvent e)
        {
            switch (e)
            {
                case AttentionEvent a: attention = a.Level; break;
                case MeditationEvent m: meditation = m.Level; break;
                case BlinkEvent b: blink = b.Strength; break;
                case SignalEvent s: signal = s.Noise; break;
                case SpectrumEvent sp: bands = sp.Bands; break;
            }
        }

        if (source.State != LinkState.Streaming) await source.ConnectAsync(ct);
        source.Event += OnEvent;

        var rows = new List<string>
        {
            "t_sec,attention,meditation,blink,signal,delta,theta,lowAlpha,highAlpha,lowBeta,highBeta,lowGamma,midGamma"
        };
        long attSum = 0, medSum = 0, sigSum = 0, n = 0;
        var bandSums = new double[8];

        try
        {
            var start = DateTime.UtcNow;
            var window = TimeSpan.FromSeconds(Math.Max(1, seconds));
            for (int t = 0; ; t++)
            {
                ct.ThrowIfCancellationRequested();

                int blinkNow = blink; blink = 0; // blink is momentary; consume it per row
                rows.Add(string.Create(CultureInfo.InvariantCulture,
                    $"{t},{attention},{meditation},{blinkNow},{signal},{bands.Delta},{bands.Theta},{bands.LowAlpha},{bands.HighAlpha},{bands.LowBeta},{bands.HighBeta},{bands.LowGamma},{bands.MidGamma}"));

                attSum += attention; medSum += meditation; sigSum += signal;
                if (blinkNow > 0) blinkRows++;
                bandSums[0] += bands.Delta; bandSums[1] += bands.Theta;
                bandSums[2] += bands.LowAlpha; bandSums[3] += bands.HighAlpha;
                bandSums[4] += bands.LowBeta; bandSums[5] += bands.HighBeta;
                bandSums[6] += bands.LowGamma; bandSums[7] += bands.MidGamma;
                n++;

                var elapsed = DateTime.UtcNow - start;
                progress?.Invoke(Math.Clamp(elapsed / window, 0, 1));
                if (elapsed >= window) break;
                await Task.Delay(1000, ct);
            }
        }
        finally
        {
            source.Event -= OnEvent;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(csvPath)!);
        File.WriteAllLines(csvPath, rows);

        var means = new double[8];
        for (int k = 0; k < 8; k++) means[k] = n > 0 ? bandSums[k] / n : 0;
        double minutes = Math.Max(1.0 / 60, n / 60.0);

        return new BrainFeatureVector
        {
            AvgAttention = n > 0 ? (double)attSum / n : 0,
            AvgMeditation = n > 0 ? (double)medSum / n : 0,
            AvgSignal = n > 0 ? (double)sigSum / n : 0,
            BlinkRate = blinkRows / minutes,
            BandMeans = means,
        };
    }
}
