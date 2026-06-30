using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Turing Test math: a human thought profile from EEG compared against a synthetic AI
/// profile, plus human/machine-likeness, the cognitive/reasoning/creativity/knowledge comparisons,
/// probabilities, the blind-judge simulation, leaderboard, multi-model and artificial-brain tables,
/// and the history log. Self-contained. All scores 0–100.
/// </summary>
public static class TuringProfile
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

    private static int Distinct(IReadOnlyList<string> words) =>
        words.Where(x => x.Trim().Length > 0 && x.Trim() != "—").Distinct(StringComparer.OrdinalIgnoreCase).Count();

    // ---- core human scalars ----

    public static double Creativity(IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    { var (_, a, _, g) = Shares(b); return C(a * 50 + g * 40 + Diversity(w) * 10); }

    public static double Logic(double att, IReadOnlyList<BandReading> b)
    { var (_, _, beta, _) = Shares(b); return C(beta * 60 + att * 0.3); }

    public static double Curiosity(IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    { var (t, _, _, g) = Shares(b); return C(Diversity(w) * 50 + t * 30 + g * 20); }

    public static double Emotional(IReadOnlyList<BandReading> b)
    { var (t, a, _, _) = Shares(b); return C(a * 40 + t * 30 + 15); }

    public static double HumanLikeness(double att, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
        => C(Diversity(w) * 40 + Creativity(b, w) * 0.25 + Emotional(b) * 0.25 + 10);

    public static double Authenticity(IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
        => C(Diversity(w) * 50 + Emotional(b) * 0.3 + 10);

    public static double MachineLikeness(IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (_, _, beta, gamma) = Shares(b);
        double div = Diversity(w);
        double predictability = C(100 - div * 60 - gamma * 20);
        double repetition = C(100 - div * 70);
        double formulaic = C(beta * 30 + (1 - div) * 50);
        double structured = C(beta * 40 + (1 - div) * 20 + 20);
        return C((predictability + repetition + formulaic + structured) / 4);
    }

    /// <summary>The six dashboard scores (0–100). Order: Human-Likeness, Machine-Likeness, Creativity, Logic, Curiosity, Authenticity.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
        => new (string, double)[]
        {
            ("Human-Likeness", HumanLikeness(avgAtt, bands, words)),
            ("Machine-Likeness", MachineLikeness(bands, words)),
            ("Creativity", Creativity(bands, words)),
            ("Logic", Logic(avgAtt, bands)),
            ("Curiosity", Curiosity(bands, words)),
            ("Authenticity", Authenticity(bands, words)),
        };

    // ---- profile + likeness tables ----

    public static string HumanThoughtProfileCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("attribute,score\n");
        sb.AppendLine($"Vocabulary Diversity,{C(div * 100):0.0}");
        sb.AppendLine($"Concept Diversity,{C(Distinct(w) * 8):0.0}");
        sb.AppendLine($"Logical Consistency,{Logic(a, b):0.0}");
        sb.AppendLine($"Creativity Indicators,{Creativity(b, w):0.0}");
        sb.AppendLine($"Curiosity Indicators,{Curiosity(b, w):0.0}");
        sb.AppendLine($"Technical Thinking,{C(beta * 50 + a * 0.3 + gamma * 20):0.0}");
        sb.AppendLine($"Abstract Thinking,{C(alpha * 50 + gamma * 30 + div * 20):0.0}");
        return sb.ToString();
    }

    public static string HumanLikenessCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (_, alpha, _, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("metric,score\n");
        sb.AppendLine($"Human-Likeness,{HumanLikeness(a, b, w):0.0}");
        sb.AppendLine($"Naturalness,{C(div * 40 + alpha * 30 + 15):0.0}");
        sb.AppendLine($"Authenticity,{Authenticity(b, w):0.0}");
        sb.AppendLine($"Originality,{C(gamma * 40 + div * 40 + alpha * 20):0.0}");
        return sb.ToString();
    }

    public static string MachineLikenessCsv(IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (_, _, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("metric,score\n");
        sb.AppendLine($"Predictability,{C(100 - div * 60 - gamma * 20):0.0}");
        sb.AppendLine($"Repetition,{C(100 - div * 70):0.0}");
        sb.AppendLine($"Formulaic Thinking,{C(beta * 30 + (1 - div) * 50):0.0}");
        sb.AppendLine($"Structured Response,{C(beta * 40 + (1 - div) * 20 + 20):0.0}");
        return sb.ToString();
    }

    // ---- human vs AI comparisons (AI columns are synthetic constants) ----

    private static string Compare(string header, params (string Dim, double Human, double Ai)[] rows)
    {
        var sb = new System.Text.StringBuilder(header + "\n");
        foreach (var (dim, h, ai) in rows) sb.AppendLine($"{dim},{C(h):0.0},{C(ai):0.0}");
        return sb.ToString();
    }

    public static string CognitiveComparisonCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        return Compare("dimension,human,ai",
            ("Creativity", Creativity(b, w), 65),
            ("Logic", Logic(a, b), 90),
            ("Flexibility", div * 60 + alpha * 30, 58),
            ("Innovation", alpha * 40 + gamma * 40 + div * 20, 66),
            ("Curiosity", Curiosity(b, w), 54),
            ("Emotional Expression", Emotional(b), 28),
            ("Technical Depth", beta * 50 + a * 0.3 + gamma * 20, 86));
    }

    public static string ReasoningComparisonCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        return Compare("dimension,human,ai",
            ("Problem Solving", beta * 60 + a * 0.3, 84),
            ("Abstract Reasoning", alpha * 50 + gamma * 30 + div * 20, 80),
            ("Scientific Thinking", beta * 45 + a * 0.3 + div * 10, 82),
            ("Engineering Thinking", beta * 45 + gamma * 25 + a * 0.2, 83),
            ("Strategic Thinking", a * 0.4 + alpha * 30 + beta * 20, 78));
    }

    public static string CreativityChallengeCsv(IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        return Compare("dimension,human,ai",
            ("Originality", gamma * 40 + div * 40 + alpha * 20, 60),
            ("Novelty", gamma * 45 + div * 35 + 10, 58),
            ("Detail", beta * 40 + 40, 88),
            ("Imagination", alpha * 50 + gamma * 30 + div * 20, 62));
    }

    public static string KnowledgeComparisonCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        double div = Diversity(w);
        int distinct = Distinct(w);
        return Compare("dimension,human,ai",
            ("Breadth of Concepts", distinct * 7, 90),
            ("Knowledge Diversity", div * 100, 85),
            ("Subject Variety", distinct * 6, 88),
            ("Learning Indicators", a * 0.4 + div * 40, 80));
    }

    // ---- probabilities ----

    public static string TuringProbabilityCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        double h = HumanLikeness(a, b, w);
        double mach = MachineLikeness(b, w);
        double hybrid = C(100 - Math.Abs(h - mach));
        double sum = h + mach + hybrid;
        if (sum <= 0) sum = 1;
        var sb = new System.Text.StringBuilder("outcome,percent\n");
        sb.AppendLine($"Probability Human,{100.0 * h / sum:0.0}");
        sb.AppendLine($"Probability AI,{100.0 * mach / sum:0.0}");
        sb.AppendLine($"Hybrid Probability,{100.0 * hybrid / sum:0.0}");
        return sb.ToString();
    }

    // ---- blind judge simulation ----

    public static string JudgeResultsCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        double h = HumanLikeness(a, b, w);
        // The judge guesses "Human" when a sample looks human-like; we know the true labels.
        var samples = new (string Actual, double Score)[]
        {
            ("Human", h), ("AI", 100 - h), ("Human", h - 5), ("AI", 100 - h + 5), ("Human", h + 3),
        };
        var sb = new System.Text.StringBuilder("round,actual,judge_guess,correct,confidence\n");
        for (int i = 0; i < samples.Length; i++)
        {
            string guess = samples[i].Score >= 50 ? "Human" : "AI";
            bool correct = guess == samples[i].Actual;
            double conf = C(Math.Abs(samples[i].Score - 50) * 2);
            sb.AppendLine($"{i + 1},{samples[i].Actual},{guess},{(correct ? "yes" : "no")},{conf:0.0}");
        }
        return sb.ToString();
    }

    // ---- multi-model + leaderboard + artificial brain ----

    public static string MultiModelResultsCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        double h = HumanLikeness(a, b, w);
        var sb = new System.Text.StringBuilder("model,human_likeness_vs_model,verdict\n");
        foreach (var (model, offset) in new[] { ("local-llm", 0.0), ("model-b", -6.0), ("historical-ai", 8.0) })
        {
            double score = C(h + offset);
            sb.AppendLine($"{model},{score:0.0},{(score >= 50 ? "more human" : "more AI")}");
        }
        return sb.ToString();
    }

    public static string LeaderboardCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var sb = new System.Text.StringBuilder("category,session,score\n");
        sb.AppendLine($"Most Human Session,current,{HumanLikeness(a, b, w):0.0}");
        sb.AppendLine($"Most AI-Like Session,current,{MachineLikeness(b, w):0.0}");
        sb.AppendLine($"Most Creative Session,current,{Creativity(b, w):0.0}");
        sb.AppendLine($"Most Logical Session,current,{Logic(a, b):0.0}");
        var (_, alpha, _, gamma) = Shares(b);
        sb.AppendLine($"Most Innovative Session,current,{C(alpha * 40 + gamma * 40 + Diversity(w) * 20):0.0}");
        return sb.ToString();
    }

    public static string ArtificialBrainComparisonCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        double h = HumanLikeness(a, b, w);
        var sb = new System.Text.StringBuilder("source,human_likeness,creativity,logic\n");
        sb.AppendLine($"Human EEG,{h:0.0},{Creativity(b, w):0.0},{Logic(a, b):0.0}");
        sb.AppendLine($"Artificial EEG (cpu_eeg),{C(h - 25):0.0},{C(Creativity(b, w) - 15):0.0},{C(Logic(a, b) + 8):0.0}");
        sb.AppendLine($"AI Text,{C(100 - h):0.0},65.0,90.0");
        return sb.ToString();
    }

    // ---- history ----

    public static string HistoryHeader() =>
        "date,distinct_concepts,human_likeness,machine_likeness,creativity,logic,curiosity,authenticity";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{Distinct(w)},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w));
    }
}
