using System.Globalization;
using System.IO;

namespace MindedOS.Engine;

/// <summary>
/// Learning-session metrics (0–100) derived from a recorded study CSV: how focused,
/// logical, mindful and "in flow" the learner was while studying a subject.
/// </summary>
public sealed record LearningStats(
    double Focus,
    double LogicReasoning,
    double LogicBricks,
    double LogicalThoughts,
    double Mindfulness,
    double FlowState)
{
    public double Overall => (Focus + LogicReasoning + LogicBricks + LogicalThoughts + Mindfulness + FlowState) / 6.0;

    /// <summary>Compute the learning stats from an EegCsvRecorder CSV (one row/second).</summary>
    public static LearningStats FromCsv(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2) return new LearningStats(0, 0, 0, 0, 0, 0);

        double attSum = 0, medSum = 0, attSumSq = 0;
        var band = new double[8];
        long n = 0, flow = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var c = lines[i].Split(',');
            if (c.Length < 13) continue;
            double D(int k) => double.TryParse(c[k], NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;

            double att = D(1), med = D(2);
            attSum += att; medSum += med; attSumSq += att * att;
            for (int k = 0; k < 8; k++) band[k] += D(5 + k);
            if (att > 55 && med > 55) flow++;       // relaxed concentration = flow
            n++;
        }
        if (n == 0) return new LearningStats(0, 0, 0, 0, 0, 0);

        double meanAtt = attSum / n, meanMed = medSum / n;
        double attStd = Math.Sqrt(Math.Max(0, attSumSq / n - meanAtt * meanAtt));

        double total = band.Sum(); if (total <= 0) total = 1;
        double alpha = (band[2] + band[3]) / total;
        double beta = (band[4] + band[5]) / total;
        double gamma = (band[6] + band[7]) / total;

        double C(double v) => Math.Clamp(v, 0, 100);
        double stability = C(100 - attStd * 2);     // steady focus = structured logic

        return new LearningStats(
            Focus:           C(meanAtt),
            LogicReasoning:  C(meanAtt * 0.5 + beta * 100 * 0.7),
            LogicBricks:     C(beta * 100 * 0.6 + stability * 0.4),
            LogicalThoughts: C((beta + gamma) * 100 * 0.8 + meanAtt * 0.2),
            Mindfulness:     C(meanMed * 0.7 + alpha * 100 * 0.6),
            FlowState:       C(100.0 * flow / n));
    }
}
