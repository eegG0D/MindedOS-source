using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic problem-solving scores from EEG averages, band shares and word
/// diversity. Mirrors <see cref="PlanningProfile"/> math. All scores are 0–100.
/// </summary>
public static class ProblemSolvingProfile
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

    public static IReadOnlyList<(string Metric, double Value)> LogicalReasoning(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Logical Consistency", C(beta * 70 + avgAtt * 0.3)),
            ("Deductive Reasoning", C(beta * 80 + avgAtt * 0.2)),
            ("Inductive Reasoning", C(alpha * 50 + div * 50)),
            ("Analytical Thinking", C(beta * 90 + avgAtt * 0.1)),
            ("Sequential Thinking", C(avgAtt * 0.6 + (1 - div) * 40)),
            ("Systems Thinking", C(alpha * 40 + gamma * 40 + div * 30)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> ProblemDecomposition(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Break Down Problems", C(avgAtt * 0.6 + beta * 30)),
            ("Identify Root Causes", C(beta * 60 + avgAtt * 0.3)),
            ("Detect Dependencies", C(alpha * 40 + avgAtt * 0.4 + div * 20)),
            ("Prioritize Tasks", C(avgAtt * 0.7 + (1 - div) * 30)),
            ("Create Action Plans", C(avgAtt * 0.5 + beta * 30 + (1 - div) * 20)),
        };
    }

    public static IReadOnlyList<(string Strategy, double Value)> StrategyAnalysis(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Trial and Error", C(div * 70 + (100 - avgAtt) * 0.2)),
            ("Logical Analysis", C(beta * 80 + avgAtt * 0.2)),
            ("Pattern Recognition", C(alpha * 50 + beta * 30 + div * 20)),
            ("Creative Exploration", C(alpha * 50 + gamma * 50 + div * 20)),
            ("Optimization", C(beta * 60 + avgAtt * 0.3)),
            ("Risk Assessment", C(avgAtt * 0.4 + avgMed * 0.3 + beta * 20)),
            ("Decision Confidence", C(avgAtt * 0.5 + avgMed * 0.3 + beta * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> InnovationProfile(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Innovation", C(gamma * 80 + div * 30)),
            ("Invention Potential", C(gamma * 70 + alpha * 30 + div * 20)),
            ("Discovery Potential", C(div * 50 + gamma * 50)),
            ("Research Potential", C(div * 60 + theta * 40)),
            ("Engineering Creativity", C(beta * 50 + gamma * 40 + div * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> DecisionAnalysis(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Decision Speed", C(avgAtt * 0.6 + beta * 30)),
            ("Decision Confidence", C(avgAtt * 0.5 + avgMed * 0.3 + beta * 20)),
            ("Alternative Evaluation", C(div * 60 + alpha * 40)),
            ("Outcome Prediction", C(beta * 50 + avgAtt * 0.3 + alpha * 20)),
            ("Risk Awareness", C(avgMed * 0.4 + avgAtt * 0.3 + beta * 20)),
        };
    }

    /// <summary>The seven dashboard scores.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Problem Solving", C(beta * 50 + avgAtt * 0.3 + div * 20)),
            ("Logic", C(beta * 80 + avgAtt * 0.2)),
            ("Innovation", C(gamma * 80 + div * 30)),
            ("Decision", C(avgAtt * 0.5 + avgMed * 0.3 + beta * 20)),
            ("Research", C(div * 60 + theta * 40)),
            ("Engineering", C(beta * 50 + gamma * 30 + avgAtt * 0.2)),
            ("Optimization", C(beta * 60 + avgAtt * 0.3)),
        };
    }

    /// <summary>Ten solver-archetype scores.</summary>
    public static IReadOnlyList<(string Archetype, double Value)> SolverArchetypes(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Analyst", C(beta * 90 + avgAtt * 0.1)),
            ("Inventor", C(gamma * 80 + div * 30)),
            ("Engineer", C(beta * 60 + avgAtt * 0.3)),
            ("Researcher", C(div * 60 + theta * 40)),
            ("Strategist", C(avgAtt * 0.5 + alpha * 40 + beta * 20)),
            ("Optimizer", C(beta * 70 + avgAtt * 0.2)),
            ("Explorer", C(div * 80 + theta * 20)),
            ("Builder", C(avgAtt * 0.6 + (1 - div) * 40)),
            ("Visionary", C(gamma * 70 + alpha * 30 + div * 20)),
            ("Integrator", C(alpha * 50 + beta * 30 + div * 30)),
        };
    }

    private static string Csv(string headerA, string headerB, IReadOnlyList<(string, double)> rows)
    {
        var sb = new System.Text.StringBuilder($"{headerA},{headerB}\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string LogicalReasoningCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", LogicalReasoning(a, m, b, w));
    public static string ProblemDecompositionCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", ProblemDecomposition(a, b, w));
    public static string StrategyAnalysisCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("strategy", "score", StrategyAnalysis(a, m, b, w));
    public static string InnovationProfileCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", InnovationProfile(a, b, w));
    public static string DecisionAnalysisCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", DecisionAnalysis(a, m, b, w));

    public static string HistoryHeader() =>
        "date,problem_solving,logic,innovation,decision,research,engineering,optimization,top_challenge";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topChallenge)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{d[6].Value:0},{topChallenge}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topChallenge)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topChallenge));
    }
}
