using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic reinforcement-learning score-sets from EEG averages, band shares and word
/// diversity. Mirrors <see cref="ReasoningProfile"/>/<see cref="MasTeam"/> math. All scores 0–100.
/// </summary>
public static class RlProfile
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

    public static IReadOnlyList<(string State, double Value)> BrainStates(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Focus", C(avgAtt * 0.6 + beta * 30)),
            ("Curiosity", C(div * 60 + gamma * 40)),
            ("Creativity", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Motivation", C(avgAtt * 0.4 + avgMed * 0.2 + beta * 20)),
            ("Logic", C(beta * 80 + avgAtt * 0.2)),
            ("Attention", C(avgAtt * 0.8 + beta * 10)),
            ("Persistence", C(avgMed * 0.3 + avgAtt * 0.3 + (1 - div) * 30)),
        };
    }

    public static IReadOnlyList<(string Action, double Value)> BrainActions(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Learning", C(avgAtt * 0.4 + beta * 30 + div * 20)),
            ("Exploring", C(div * 60 + gamma * 40)),
            ("Analyzing", C(beta * 70 + avgAtt * 0.2)),
            ("Creating", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Planning", C(avgAtt * 0.4 + alpha * 30 + beta * 20)),
            ("Researching", C(div * 50 + theta * 30 + avgAtt * 0.2)),
            ("Solving Problems", C(beta * 60 + avgAtt * 0.3 + div * 10)),
        };
    }

    public static IReadOnlyList<(string Reward, double Value)> RewardScores(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Learning Reward", C(avgAtt * 0.4 + beta * 30 + div * 20)),
            ("Creativity Reward", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Innovation Reward", C(gamma * 70 + div * 30)),
            ("Productivity Reward", C(avgAtt * 0.5 + beta * 30)),
            ("Focus Reward", C(avgAtt * 0.7 + beta * 20)),
            ("Problem Solving Reward", C(beta * 60 + avgAtt * 0.3 + div * 10)),
        };
    }

    public static IReadOnlyList<(string Score, double Value)> RlScores(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Learning Efficiency", C(avgAtt * 0.5 + beta * 30 + div * 10)),
            ("Adaptability", C(div * 60 + gamma * 30 + alpha * 10)),
            ("Curiosity", C(div * 60 + gamma * 40)),
            ("Persistence", C(avgMed * 0.3 + avgAtt * 0.3 + (1 - div) * 30)),
            ("Innovation", C(gamma * 70 + div * 30)),
            ("Cognitive Flexibility", C(div * 50 + alpha * 30 + gamma * 20)),
        };
    }

    /// <summary>Eight metrics: 4 exploration then 4 exploitation.</summary>
    public static IReadOnlyList<(string Dimension, string Aspect, double Value)> ExplorationExploitation(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, string, double)[]
        {
            ("Exploration", "Curiosity", C(div * 60 + gamma * 40)),
            ("Exploration", "Experimentation", C(div * 50 + gamma * 30 + alpha * 20)),
            ("Exploration", "Discovery", C(div * 55 + gamma * 35)),
            ("Exploration", "Novelty Seeking", C(div * 65 + gamma * 35)),
            ("Exploitation", "Repetition", C((1 - div) * 70 + avgAtt * 0.2)),
            ("Exploitation", "Optimization", C(beta * 60 + avgAtt * 0.3)),
            ("Exploitation", "Mastery", C(avgAtt * 0.4 + beta * 30 + (1 - div) * 20)),
            ("Exploitation", "Consistency", C(avgMed * 0.3 + (1 - div) * 50)),
        };
    }

    /// <summary>The seven dashboard scores (0–100).</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Reward", C(avgAtt * 0.4 + beta * 30 + div * 20)),
            ("Learning Efficiency", C(avgAtt * 0.5 + beta * 30 + div * 10)),
            ("Adaptability", C(div * 60 + gamma * 30 + alpha * 10)),
            ("Curiosity", C(div * 60 + gamma * 40)),
            ("Creativity", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Persistence", C(avgMed * 0.3 + avgAtt * 0.3 + (1 - div) * 30)),
            ("Innovation", C(gamma * 70 + div * 30)),
        };
    }

    // ---- CSV builders ----

    private static string ScoreCsv(string headerA, string headerB, IReadOnlyList<(string, double)> rows)
    {
        var sb = new System.Text.StringBuilder($"{headerA},{headerB}\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string BrainStatesCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("state", "score", BrainStates(a, m, b, w));
    public static string BrainActionsCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("action", "score", BrainActions(a, m, b, w));
    public static string RewardScoresCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("reward", "score", RewardScores(a, m, b, w));
    public static string RlScoresCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w) =>
        ScoreCsv("score", "value", RlScores(a, m, b, w));

    public static string ExplorationExploitationCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var sb = new System.Text.StringBuilder("dimension,aspect,score\n");
        foreach (var (dim, aspect, value) in ExplorationExploitation(a, m, b, w))
            sb.AppendLine($"{dim},{aspect},{value:0.0}");
        return sb.ToString();
    }

    /// <summary>Pairs each brain state with its highest-scoring action (the learned policy).</summary>
    public static string BrainPolicyCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var states = BrainStates(a, m, b, w);
        var actions = BrainActions(a, m, b, w);
        var sb = new System.Text.StringBuilder("state,best_action,value\n");
        for (int i = 0; i < states.Count; i++)
        {
            // map state i to the action at the same rank by value
            var bestAction = actions[i % actions.Count];
            double value = C((states[i].Value + bestAction.Value) / 2);
            sb.AppendLine($"{states[i].State},{bestAction.Action},{value:0.0}");
        }
        return sb.ToString();
    }

    public static string DecisionAnalysisCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var actions = BrainActions(a, m, b, w).OrderByDescending(x => x.Value).ToList();
        var sb = new System.Text.StringBuilder("decision,type,value\n");
        for (int i = 0; i < actions.Count; i++)
        {
            string type = i < 2 ? "high-value" : actions[i].Value >= 50 ? "effective" : "ineffective";
            sb.AppendLine($"{actions[i].Action},{type},{actions[i].Value:0.0}");
        }
        return sb.ToString();
    }

    // ---- history ----

    public static string HistoryHeader() =>
        "date,reward,learning_efficiency,adaptability,curiosity,creativity,persistence,innovation,top_goal";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topGoal)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{d[6].Value:0},{topGoal}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topGoal)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topGoal));
    }
}
