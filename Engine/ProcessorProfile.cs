using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic processing score-sets from EEG averages, band shares and word
/// diversity. Mirrors <see cref="ProblemSolvingProfile"/> math. Scores are 0–100
/// except the throughput per-minute rates, which are raw values.
/// </summary>
public static class ProcessorProfile
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

    public static IReadOnlyList<(string Metric, double Value)> InputProcessing(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        double len = words.Count;
        return new (string, double)[]
        {
            ("Input Complexity", C(div * 60 + alpha * 40)),
            ("Information Density", C(System.Math.Min(len, 100) * 0.6 + div * 40)),
            ("Signal Diversity", C(div * 100)),
            ("Concept Diversity", C(div * 90 + gamma * 10)),
            ("Processing Load", C(avgAtt * 0.5 + div * 30 + beta * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> ProcessingPipeline(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Information Intake", C(avgAtt * 0.6 + div * 40)),
            ("Concept Analysis", C(beta * 70 + avgAtt * 0.3)),
            ("Pattern Matching", C(alpha * 50 + beta * 30 + div * 20)),
            ("Prioritization", C(avgAtt * 0.7 + (1 - div) * 30)),
            ("Decision Preparation", C(avgAtt * 0.5 + avgMed * 0.2 + beta * 30)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> ThroughputAnalysis(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words, int seconds)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        double minutes = System.Math.Max(1.0, seconds / 60.0);
        int distinct = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        double conceptsPerMin = distinct / minutes;
        double thoughtsPerMin = words.Count / minutes;
        return new (string, double)[]
        {
            ("Concepts per Minute", System.Math.Round(conceptsPerMin, 1)),
            ("Thoughts per Minute", System.Math.Round(thoughtsPerMin, 1)),
            ("Processing Efficiency", C(avgAtt * 0.6 + beta * 30)),
            ("Cognitive Throughput", C(System.Math.Min(thoughtsPerMin * 2, 100))),
            ("Mental Workload", C(avgAtt * 0.4 + div * 40 + beta * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> ProcessingSpeed(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Fast Processing", C(avgAtt * 0.6 + beta * 40)),
            ("Slow Processing", C((100 - avgAtt) * 0.5 + theta * 40)),
            ("Delayed Transitions", C((1 - div) * 60 + theta * 30)),
            ("Efficient Transitions", C(div * 60 + avgAtt * 0.3)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> LogicProcessing(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Logical Reasoning", C(beta * 80 + avgAtt * 0.2)),
            ("Sequential Reasoning", C(avgAtt * 0.6 + (1 - div) * 40)),
            ("Analytical Depth", C(beta * 70 + alpha * 20 + avgAtt * 0.1)),
            ("Problem Decomposition", C(beta * 60 + avgAtt * 0.3)),
            ("Structured Thinking", C(beta * 50 + avgAtt * 0.3 + (1 - div) * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> ParallelProcessing(
        IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Parallel Thought Streams", C(div * 80 + gamma * 20)),
            ("Multi-topic Processing", C(div * 90)),
            ("Context Switching", C(div * 70 + gamma * 30)),
            ("Concurrent Concept Handling", C(div * 60 + alpha * 40)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> MemoryProcessing(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Short-term Processing", C(avgAtt * 0.5 + theta * 40)),
            ("Working Memory Utilization", C(theta * 60 + avgAtt * 0.3)),
            ("Concept Retention", C(theta * 50 + avgMed * 0.3 + (1 - div) * 20)),
            ("Recall Indicators", C(theta * 40 + alpha * 30 + avgMed * 0.3)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> SchedulerAnalysis(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Priority Assignment", C(avgAtt * 0.6 + beta * 30)),
            ("Task Switching", C(div * 70 + gamma * 30)),
            ("Resource Allocation", C(avgAtt * 0.5 + beta * 30 + (1 - div) * 20)),
            ("Scheduling Efficiency", C(avgAtt * 0.6 + beta * 20 + (1 - div) * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> DecisionProcessing(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Decision Formation", C(avgAtt * 0.5 + beta * 40)),
            ("Decision Complexity", C(div * 60 + beta * 30)),
            ("Decision Confidence", C(avgAtt * 0.5 + avgMed * 0.3 + beta * 20)),
            ("Decision Consistency", C((1 - div) * 50 + avgAtt * 0.4 + avgMed * 0.1)),
        };
    }

    /// <summary>The seven dashboard scores (all 0–100).</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Processing Speed", C(avgAtt * 0.6 + beta * 40)),
            ("Processing Efficiency", C(avgAtt * 0.6 + beta * 30)),
            ("Throughput", C(avgAtt * 0.4 + div * 40 + beta * 20)),
            ("Parallel Processing", C(div * 80 + gamma * 20)),
            ("Logic", C(beta * 80 + avgAtt * 0.2)),
            ("Memory", C(theta * 60 + avgAtt * 0.3)),
            ("Decision", C(avgAtt * 0.5 + avgMed * 0.3 + beta * 20)),
        };
    }

    private static string Csv(string headerA, string headerB, IReadOnlyList<(string, double)> rows)
    {
        var sb = new System.Text.StringBuilder($"{headerA},{headerB}\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string InputProcessingCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", InputProcessing(a, b, w));
    public static string ProcessingPipelineCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", ProcessingPipeline(a, m, b, w));
    public static string ThroughputAnalysisCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int seconds) =>
        Csv("metric", "value", ThroughputAnalysis(a, b, w, seconds));
    public static string ProcessingSpeedCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", ProcessingSpeed(a, b, w));
    public static string LogicProcessingCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", LogicProcessing(a, b, w));
    public static string ParallelProcessingCsv(IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", ParallelProcessing(b, w));
    public static string MemoryProcessingCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", MemoryProcessing(a, m, b, w));
    public static string SchedulerAnalysisCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", SchedulerAnalysis(a, b, w));
    public static string DecisionProcessingCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", DecisionProcessing(a, m, b, w));

    public static string HistoryHeader() =>
        "date,processing_speed,processing_efficiency,throughput,parallel_processing,logic,memory,decision,top_core";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topCore)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{d[6].Value:0},{topCore}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topCore)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topCore));
    }
}
