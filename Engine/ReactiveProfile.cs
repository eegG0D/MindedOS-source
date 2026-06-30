using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic reactive (present-moment) scores from EEG averages, band shares and
/// word diversity. Mirrors <see cref="ProcessorProfile"/> math. All scores are 0–100.
/// Memory-less: no history methods.
/// </summary>
public static class ReactiveProfile
{
    private static double C(double v) => Math.Clamp(v, 0, 100);

    private static (double theta, double alpha, double beta, double gamma) Shares(IReadOnlyList<BandReading> bands)
    {
        double total = 0;
        foreach (var b in bands) total += b.Value;
        if (total <= 0) total = 1;
        double Share(string key)
        {
            foreach (var b in bands) if (b.Key == key) return b.Value / total;
            return 0;
        }
        return (Share("theta"), Share("lowAlpha") + Share("highAlpha"),
                Share("lowBeta") + Share("highBeta"), Share("lowGamma") + Share("midGamma"));
    }

    private static double Diversity(IReadOnlyList<string> words)
    {
        int n = words.Count;
        if (n == 0) return 0;
        return (double)words.Distinct(StringComparer.OrdinalIgnoreCase).Count() / n;
    }

    /// <summary>The ten current cognitive states (0–100).</summary>
    public static IReadOnlyList<(string State, double Value)> CurrentStates(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Focused", C(avgAtt)),
            ("Distracted", C((100 - avgAtt) * 0.6 + div * 40)),
            ("Analytical", C(beta * 80 + avgAtt * 0.2)),
            ("Creative", C(alpha * 60 + gamma * 50 + div * 20)),
            ("Curious", C(div * 60 + theta * 40)),
            ("Exploratory", C(div * 80 + theta * 20)),
            ("Learning", C(avgAtt * 0.5 + div * 50)),
            ("Problem Solving", C(beta * 60 + avgAtt * 0.3)),
            ("Innovative", C(gamma * 80 + div * 30)),
            ("Relaxed", C(avgMed)),
        };
    }

    /// <summary>Four attention-response metrics (0–100).</summary>
    public static IReadOnlyList<(string Metric, double Value)> AttentionResponse(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Attention Level", C(avgAtt)),
            ("Attention Stability", C(avgAtt * 0.7 + (1 - div) * 30)),
            ("Attention Shifts", C(div * 80 + gamma * 20)),
            ("Response Readiness", C(avgAtt * 0.6 + beta * 40)),
        };
    }

    /// <summary>Five live dashboard scores (0–100).</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Current Focus", C(avgAtt)),
            ("Current Curiosity", C(div * 60 + theta * 40)),
            ("Current Logic", C(beta * 80 + avgAtt * 0.2)),
            ("Current Creativity", C(alpha * 60 + gamma * 50 + div * 20)),
            ("Current Awareness", C(avgAtt * 0.5 + alpha * 30 + avgMed * 0.2)),
        };
    }

    public static string CurrentStateCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var states = CurrentStates(a, m, b, w);
        double max = states.Count > 0 ? states.Max(x => x.Value) : 0;
        bool flagged = false;
        var sb = new System.Text.StringBuilder("state,score,dominant\n");
        foreach (var (state, value) in states)
        {
            bool dom = !flagged && value >= max;
            if (dom) flagged = true;
            sb.AppendLine($"{state},{value:0.0},{(dom ? "yes" : "no")}");
        }
        return sb.ToString();
    }

    public static string AttentionResponseCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var sb = new System.Text.StringBuilder("metric,score\n");
        foreach (var (metric, value) in AttentionResponse(a, b, w)) sb.AppendLine($"{metric},{value:0.0}");
        return sb.ToString();
    }
}
