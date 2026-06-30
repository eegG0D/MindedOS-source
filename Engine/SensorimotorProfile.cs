using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic sensorimotor score-sets from EEG averages, band shares and word diversity.
/// Mirrors <see cref="RlProfile"/> math. All scores 0–100.
/// </summary>
public static class SensorimotorProfile
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

    public static IReadOnlyList<(string Metric, double Value)> SensoryProcessing(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Visual", C(avgAtt * 0.5 + beta * 30 + gamma * 10)),
            ("Auditory", C(avgAtt * 0.4 + alpha * 30)),
            ("Spatial", C(alpha * 40 + beta * 30 + div * 20)),
            ("Object Awareness", C(avgAtt * 0.5 + gamma * 30)),
            ("Environmental Awareness", C(alpha * 40 + div * 30 + avgAtt * 0.2)),
            ("Situational Awareness", C(avgAtt * 0.4 + beta * 30 + div * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> MotorPlanning(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Action Preparation", C(beta * 60 + avgAtt * 0.3)),
            ("Movement Intentions", C(beta * 50 + avgAtt * 0.3 + div * 10)),
            ("Decision-to-Action", C(beta * 60 + avgAtt * 0.3)),
            ("Goal-Directed Behavior", C(avgAtt * 0.5 + beta * 30 + (1 - div) * 10)),
            ("Sequential Planning", C(avgAtt * 0.4 + alpha * 20 + beta * 30)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> ReactionAnalysis(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Reaction Readiness", C(avgAtt * 0.6 + beta * 30)),
            ("Cognitive Response Speed", C(beta * 60 + avgAtt * 0.3)),
            ("Adaptation Rate", C(div * 50 + gamma * 30 + alpha * 10)),
            ("Task Engagement", C(avgAtt * 0.7 + beta * 10)),
            ("Response Consistency", C(avgMed * 0.3 + (1 - div) * 50)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> CoordinationProfile(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Hand-Eye Coordination", C(avgAtt * 0.5 + beta * 30 + gamma * 10)),
            ("Spatial Coordination", C(alpha * 40 + beta * 30 + div * 20)),
            ("Multi-Step Coordination", C(avgAtt * 0.4 + beta * 30 + (1 - div) * 20)),
            ("Precision Coordination", C(beta * 60 + avgAtt * 0.3)),
            ("Task Synchronization", C(avgMed * 0.3 + avgAtt * 0.3 + beta * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> MotorLearningProfile(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Learning Speed", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Skill Acquisition Rate", C(avgAtt * 0.4 + beta * 30 + div * 10)),
            ("Error Correction Potential", C(beta * 50 + avgMed * 0.2 + div * 10)),
            ("Adaptability", C(div * 60 + gamma * 30 + alpha * 10)),
            ("Repetition Efficiency", C(avgMed * 0.3 + (1 - div) * 50)),
            ("Practice Effectiveness", C(avgAtt * 0.5 + beta * 30)),
        };
    }

    public static IReadOnlyList<(string Skill, double Value)> SkillDevelopment(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Driving", C(avgAtt * 0.5 + beta * 30)),
            ("Robotics", C(beta * 50 + gamma * 30 + div * 10)),
            ("Engineering", C(beta * 50 + avgAtt * 0.3 + alpha * 10)),
            ("Gaming", C(avgAtt * 0.4 + beta * 30 + gamma * 20)),
            ("Sports", C(avgAtt * 0.5 + beta * 20 + gamma * 10)),
            ("Tool Usage", C(beta * 50 + avgAtt * 0.3)),
            ("Programming", C(beta * 60 + avgAtt * 0.2 + div * 10)),
            ("Remote Vehicle Operation", C(avgAtt * 0.5 + beta * 30 + alpha * 10)),
            ("Human-Machine Interaction", C(avgAtt * 0.4 + beta * 30 + div * 20)),
        };
    }

    public static IReadOnlyList<(string State, double Value)> SensorimotorStates(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Focused Action", C(avgAtt * 0.7 + beta * 20)),
            ("Exploration Mode", C(div * 60 + gamma * 40)),
            ("Learning Mode", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Coordination Mode", C(avgAtt * 0.4 + beta * 30 + alpha * 20)),
            ("Adaptive Mode", C(div * 50 + gamma * 30 + alpha * 10)),
            ("Strategic Mode", C(avgAtt * 0.4 + alpha * 30 + beta * 20)),
            ("Reactive Mode", C(beta * 60 + avgAtt * 0.3)),
            ("Flow State", C(avgAtt * 0.4 + avgMed * 0.3 + alpha * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> AdaptationAnalysis(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Learning Curve", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("Adaptation Speed", C(div * 50 + gamma * 30 + alpha * 10)),
            ("Strategy Evolution", C(div * 40 + alpha * 30 + beta * 20)),
            ("Error Recovery", C(beta * 40 + avgMed * 0.3 + div * 10)),
            ("Improvement Rate", C(avgAtt * 0.4 + div * 30 + gamma * 20)),
        };
    }

    /// <summary>The six dashboard scores (0–100). Order: Sensory Awareness, Motor Planning, Coordination, Adaptation, Learning, BMI Readiness.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Sensory Awareness", C(avgAtt * 0.5 + alpha * 30 + div * 10)),
            ("Motor Planning", C(beta * 60 + avgAtt * 0.3)),
            ("Coordination", C(avgAtt * 0.4 + beta * 30 + alpha * 20)),
            ("Adaptation", C(div * 50 + gamma * 30 + alpha * 10)),
            ("Learning", C(avgAtt * 0.4 + div * 30 + beta * 20)),
            ("BMI Readiness", C(avgAtt * 0.4 + beta * 40 + avgMed * 0.1)),
        };
    }

    private static string ScoreCsv(string headerA, string headerB, IReadOnlyList<(string, double)> rows)
    {
        var sb = new System.Text.StringBuilder($"{headerA},{headerB}\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string SensoryProcessingCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("sense", "score", SensoryProcessing(a, m, b, w));
    public static string MotorPlanningCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("metric", "score", MotorPlanning(a, m, b, w));
    public static string ReactionAnalysisCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("metric", "score", ReactionAnalysis(a, m, b, w));
    public static string CoordinationProfileCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("metric", "score", CoordinationProfile(a, m, b, w));
    public static string MotorLearningProfileCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("metric", "score", MotorLearningProfile(a, m, b, w));
    public static string SkillDevelopmentCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("skill", "score", SkillDevelopment(a, m, b, w));
    public static string AdaptationAnalysisCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("metric", "score", AdaptationAnalysis(a, m, b, w));

    public static string SensorimotorStatesCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var states = SensorimotorStates(a, m, b, w);
        int top = 0;
        for (int i = 1; i < states.Count; i++) if (states[i].Value > states[top].Value) top = i;
        var sb = new System.Text.StringBuilder("state,score,dominant\n");
        for (int i = 0; i < states.Count; i++)
            sb.AppendLine($"{states[i].State},{states[i].Value:0.0},{(i == top ? "yes" : "no")}");
        return sb.ToString();
    }

    // ---- history ----

    public static string HistoryHeader() =>
        "date,sensory_awareness,motor_planning,coordination,adaptation,learning,bmi_readiness,top_skill";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topSkill)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{topSkill}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topSkill)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topSkill));
    }
}
