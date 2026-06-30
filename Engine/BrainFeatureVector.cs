using System.Globalization;
using System.IO;

namespace MindedOS.Engine;

/// <summary>
/// An aggregate feature summary of a recorded EEG session, used to compare a
/// human recording against the CPU-generated ("artificial") recording.
/// </summary>
public sealed class BrainFeatureVector
{
    public double AvgAttention { get; init; }
    public double AvgMeditation { get; init; }
    public double BlinkRate { get; init; }     // blinks per minute
    public double AvgSignal { get; init; }
    public double[] BandMeans { get; init; } = new double[8]; // delta..midGamma

    /// <summary>
    /// Normalised, comparable features: five band proportions plus attention,
    /// meditation and (scaled) blink rate. All non-negative so cosine ∈ [0,1].
    /// </summary>
    public double[] ShareVector()
    {
        double total = 0;
        foreach (var b in BandMeans) total += b;
        if (total <= 0) total = 1;

        double delta = BandMeans[0] / total;
        double theta = BandMeans[1] / total;
        double alpha = (BandMeans[2] + BandMeans[3]) / total;
        double beta = (BandMeans[4] + BandMeans[5]) / total;
        double gamma = (BandMeans[6] + BandMeans[7]) / total;

        return new[]
        {
            delta, theta, alpha, beta, gamma,
            AvgAttention / 100.0, AvgMeditation / 100.0,
            Math.Clamp(BlinkRate / 30.0, 0, 1),
        };
    }

    /// <summary>Aggregate a recorded CSV (one row per second) into a feature vector.</summary>
    public static BrainFeatureVector FromCsv(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2) return new BrainFeatureVector();

        double attSum = 0, medSum = 0, sigSum = 0;
        var bandSums = new double[8];
        long n = 0, blinkRows = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var c = lines[i].Split(',');
            if (c.Length < 13) continue;
            double D(int k) => double.TryParse(c[k], NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;

            attSum += D(1); medSum += D(2);
            if (D(3) > 0) blinkRows++;
            sigSum += D(4);
            for (int k = 0; k < 8; k++) bandSums[k] += D(5 + k);
            n++;
        }
        if (n == 0) return new BrainFeatureVector();

        var means = new double[8];
        for (int k = 0; k < 8; k++) means[k] = bandSums[k] / n;
        double minutes = Math.Max(1.0 / 60, n / 60.0);

        return new BrainFeatureVector
        {
            AvgAttention = attSum / n,
            AvgMeditation = medSum / n,
            AvgSignal = sigSum / n,
            BlinkRate = blinkRows / minutes,
            BandMeans = means,
        };
    }
}

/// <summary>Compares two feature vectors and reports how "artificial" one looks.</summary>
public static class ArtificialityComparer
{
    /// <summary>
    /// Cosine similarity of the user's share-vector to the CPU's, as a percentage
    /// (100% = the user's EEG looks exactly like the machine's).
    /// </summary>
    public static double PercentArtificial(BrainFeatureVector user, BrainFeatureVector cpu)
    {
        var a = user.ShareVector();
        var b = cpu.ShareVector();
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        if (na <= 0 || nb <= 0) return 0;
        double cos = dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        return Math.Clamp(cos * 100.0, 0, 100);
    }
}
