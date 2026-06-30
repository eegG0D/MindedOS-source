using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic reasoning score-sets from EEG averages, band shares and word diversity.
/// Mirrors <see cref="ProblemSolvingProfile"/> math. All scores are 0–100.
/// </summary>
public static class ReasoningProfile
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
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Deductive Reasoning", C(beta * 80 + avgAtt * 0.2)),
            ("Inductive Reasoning", C(alpha * 50 + div * 50)),
            ("Abductive Reasoning", C(alpha * 40 + gamma * 30 + div * 30)),
            ("Sequential Thinking", C(avgAtt * 0.6 + (1 - div) * 40)),
            ("Analytical Thinking", C(beta * 90 + avgAtt * 0.1)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> ProblemSolvingProfileScores(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Solution Generation", C(div * 60 + gamma * 40)),
            ("Alternative Evaluation", C(div * 50 + alpha * 50)),
            ("Complexity Handling", C(beta * 60 + avgAtt * 0.3)),
            ("Strategic Thinking", C(avgAtt * 0.5 + alpha * 30 + beta * 20)),
            ("Optimization Ability", C(beta * 70 + avgAtt * 0.2)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> InferenceAnalysis(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Assumptions", C(div * 50 + theta * 40)),
            ("Conclusions", C(beta * 60 + avgAtt * 0.3)),
            ("Logical Connections", C(beta * 70 + alpha * 20)),
            ("Cause-and-Effect", C(beta * 60 + avgAtt * 0.2 + alpha * 20)),
            ("Reasoning Chains", C(avgAtt * 0.5 + beta * 30 + (1 - div) * 20)),
        };
    }

    public static IReadOnlyList<(string Score, double Value)> ReasoningProfileScores(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Logic", C(beta * 80 + avgAtt * 0.2)),
            ("Analysis", C(beta * 70 + alpha * 20 + avgAtt * 0.1)),
            ("Creativity", C(alpha * 60 + gamma * 50 + div * 20)),
            ("Strategy", C(avgAtt * 0.5 + alpha * 30 + beta * 20)),
            ("Critical Thinking", C(beta * 60 + avgAtt * 0.3 + div * 10)),
            ("Decision Quality", C(avgAtt * 0.5 + avgMed * 0.2 + beta * 30)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> ScientificReasoning(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Observation Quality", C(avgAtt * 0.6 + alpha * 40)),
            ("Experimental Thinking", C(beta * 50 + div * 50)),
            ("Hypothesis Formation", C(div * 60 + gamma * 40)),
            ("Evidence Evaluation", C(beta * 70 + avgAtt * 0.3)),
            ("Research Potential", C(div * 60 + theta * 40)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> EngineeringReasoning(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("System Thinking", C(alpha * 40 + beta * 40 + div * 20)),
            ("Design Thinking", C(alpha * 50 + gamma * 30 + div * 20)),
            ("Optimization Thinking", C(beta * 70 + avgAtt * 0.2)),
            ("Technical Problem Solving", C(beta * 60 + avgAtt * 0.3)),
            ("Innovation Potential", C(gamma * 80 + div * 30)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> MathematicalReasoning(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Logical Structure", C(beta * 80 + avgAtt * 0.2)),
            ("Quantitative Thinking", C(beta * 70 + avgAtt * 0.3)),
            ("Abstract Reasoning", C(alpha * 50 + gamma * 30 + div * 20)),
            ("Pattern Analysis", C(alpha * 40 + beta * 40 + div * 20)),
            ("Computational Thinking", C(beta * 60 + avgAtt * 0.3 + (1 - div) * 10)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> InnovationAnalysis(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Novel Ideas", C(gamma * 80 + div * 30)),
            ("Unusual Associations", C(div * 70 + gamma * 30)),
            ("Inventive Concepts", C(gamma * 70 + alpha * 30 + div * 20)),
            ("Research Opportunities", C(div * 60 + theta * 40)),
        };
    }

    /// <summary>The six dashboard scores (0–100).</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Logic", C(beta * 80 + avgAtt * 0.2)),
            ("Critical Thinking", C(beta * 60 + avgAtt * 0.3 + div * 10)),
            ("Decision Quality", C(avgAtt * 0.5 + avgMed * 0.2 + beta * 30)),
            ("Innovation", C(gamma * 80 + div * 30)),
            ("Scientific Reasoning", C(div * 60 + theta * 40)),
            ("Engineering Reasoning", C(beta * 50 + gamma * 30 + avgAtt * 0.2)),
        };
    }

    public static string ReasoningChainsCsv(double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        int distinct = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        double consistency = C(beta * 60 + avgAtt * 0.3);
        double effectiveness = C(beta * 50 + avgAtt * 0.4);
        var sb = new System.Text.StringBuilder("chain,length,consistency,effectiveness\n");
        sb.AppendLine($"Simple,{System.Math.Clamp(distinct / 8 + 1, 1, 3)},{consistency:0.0},{effectiveness:0.0}");
        sb.AppendLine($"Intermediate,{System.Math.Clamp(distinct / 5 + 2, 2, 6)},{C(consistency - 10):0.0},{C(effectiveness - 5):0.0}");
        sb.AppendLine($"Complex,{System.Math.Clamp(distinct / 3 + 3, 3, 12)},{C(consistency - 20):0.0},{C(effectiveness - 15):0.0}");
        return sb.ToString();
    }

    private static string Csv(string headerA, string headerB, IReadOnlyList<(string, double)> rows)
    {
        var sb = new System.Text.StringBuilder($"{headerA},{headerB}\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string LogicalReasoningCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", LogicalReasoning(a, b, w));
    public static string ProblemSolvingProfileCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", ProblemSolvingProfileScores(a, m, b, w));
    public static string InferenceAnalysisCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", InferenceAnalysis(a, b, w));
    public static string ReasoningProfileCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("score", "value", ReasoningProfileScores(a, m, b, w));
    public static string ScientificReasoningCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", ScientificReasoning(a, b, w));
    public static string EngineeringReasoningCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", EngineeringReasoning(a, b, w));
    public static string MathematicalReasoningCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", MathematicalReasoning(a, b, w));
    public static string InnovationAnalysisCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        Csv("metric", "score", InnovationAnalysis(a, b, w));

    public static string HistoryHeader() =>
        "date,logic,critical_thinking,decision_quality,innovation,scientific,engineering,top_subject";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topSubject)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{topSubject}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topSubject)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topSubject));
    }
}
