using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Supervised Learning math: EEG feature extraction, simulated model training,
/// predictions, classification, evaluation, skill scores, and the append-only logs. Self-contained.
/// "Training" here is a deterministic simulation (this app ships no ML library); all values derive
/// from EEG averages, band shares and word diversity. Scores 0–100.
/// </summary>
public static class SupervisedProfile
{
    public static readonly string[] Models =
        { "Decision Tree", "Random Forest", "Logistic Regression", "Neural Network", "Support Vector Machine" };

    public static readonly string[] Labels =
        { "Focused", "Creative", "Analytical", "Learning", "Problem Solving", "Innovative", "Curious", "Distracted" };

    public static readonly string[] Modes =
        { "Research Mode", "Engineering Mode", "Learning Mode", "Creative Mode", "Scientific Mode", "Strategic Mode" };

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

    // ---- feature extraction (8 features) ----

    /// <summary>The eight extracted EEG features as (name, value) rows. Dominant Band is a textual code.</summary>
    public static IReadOnlyList<(string Name, string Value)> Features(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, string dominantBand)
    {
        double mean = 0, variance = 0;
        if (bands.Count > 0)
        {
            mean = bands.Average(b => b.Value);
            variance = bands.Average(b => (b.Value - mean) * (b.Value - mean));
        }
        double peak = bands.Count > 0 ? bands.OrderByDescending(b => b.Value).First().Value : 0;
        var (theta, alpha, beta, gamma) = Shares(bands);
        return new (string, string)[]
        {
            ("Mean Signal", $"{mean:0.0}"),
            ("Variance", $"{variance:0.0}"),
            ("Peak Frequency", $"{peak:0.0}"),
            ("Dominant Band", dominantBand),
            ("Alpha Activity", $"{alpha * 100:0.0}"),
            ("Beta Activity", $"{beta * 100:0.0}"),
            ("Theta Activity", $"{theta * 100:0.0}"),
            ("Gamma Activity", $"{gamma * 100:0.0}"),
        };
    }

    public static string FeatureVectorsCsv(double a, double m, IReadOnlyList<BandReading> b, string dominantBand)
    {
        var sb = new System.Text.StringBuilder("feature,value\n");
        foreach (var (name, value) in Features(a, m, b, dominantBand)) sb.AppendLine($"{name},{value}");
        return sb.ToString();
    }

    // ---- prediction scores ----

    public static double FocusScore(double a, IReadOnlyList<BandReading> bands)
    {
        var (_, _, beta, _) = Shares(bands);
        return C(beta * 60 + a * 0.4);
    }

    public static double CreativityScore(IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (_, alpha, _, gamma) = Shares(bands);
        return C(alpha * 50 + gamma * 40 + Diversity(words) * 10);
    }

    public static double ProductivityScore(double a, IReadOnlyList<BandReading> bands)
    {
        var (_, _, beta, _) = Shares(bands);
        return C(a * 0.4 + beta * 40 + 20);
    }

    /// <summary>The predicted label from the strongest signal (deterministic).</summary>
    public static string PredictLabel(double a, double m, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        if (a < 35) return "Distracted";
        if (beta >= alpha && beta >= theta && beta >= gamma) return a > 60 ? "Focused" : "Analytical";
        if (gamma >= alpha && gamma >= theta) return "Innovative";
        if (alpha >= theta) return div > 0.6 ? "Creative" : "Problem Solving";
        return div > 0.6 ? "Curious" : "Learning";
    }

    public static string PredictMode(IReadOnlyList<BandReading> bands, IReadOnlyList<string> words, double a)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        if (beta >= alpha && a > 55) return "Engineering Mode";
        if (gamma >= beta) return "Creative Mode";
        if (alpha >= theta && div > 0.6) return "Research Mode";
        if (theta > alpha) return "Learning Mode";
        return beta > gamma ? "Scientific Mode" : "Strategic Mode";
    }

    public static string PredictionResultsCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string dominantBand)
    {
        string label = PredictLabel(a, m, b, w);
        string mode = PredictMode(b, w, a);
        string learning = Diversity(w) > 0.6 ? "Explorer" : "Focused Learner";
        var sb = new System.Text.StringBuilder("brain_state,cognitive_category,learning_profile,productivity_score,creativity_score,focus_score\n");
        sb.AppendLine($"{label},{mode},{learning},{ProductivityScore(a, b):0.0},{CreativityScore(b, w):0.0},{FocusScore(a, b):0.0}");
        return sb.ToString();
    }

    // ---- classification with confidence ----

    public static string ClassificationResultsCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var raw = new (string Mode, double Score)[]
        {
            ("Research Mode", C(div * 50 + alpha * 30 + theta * 20)),
            ("Engineering Mode", C(beta * 50 + a * 0.3 + gamma * 10)),
            ("Learning Mode", C(theta * 40 + a * 0.3 + div * 20)),
            ("Creative Mode", C(alpha * 40 + gamma * 40 + div * 10)),
            ("Scientific Mode", C(beta * 45 + a * 0.3 + div * 10)),
            ("Strategic Mode", C(a * 0.4 + alpha * 30 + beta * 20)),
        };
        double sum = raw.Sum(r => r.Score);
        if (sum <= 0) sum = 1;
        var sb = new System.Text.StringBuilder("mode,confidence\n");
        foreach (var r in raw.OrderByDescending(r => r.Score))
            sb.AppendLine($"{r.Mode},{100.0 * r.Score / sum:0.0}");
        return sb.ToString();
    }

    // ---- evaluation (per model) ----

    public static double BaseAccuracy(double a, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (_, _, beta, _) = Shares(bands);
        return C(62 + Diversity(words) * 20 + a * 0.1 + beta * 10);
    }

    public static string ModelEvaluationCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        double baseAcc = BaseAccuracy(a, b, w);
        // deterministic per-model offsets (no randomness)
        var offsets = new[] { -6.0, 4.0, -2.0, 6.0, 0.0 };
        var sb = new System.Text.StringBuilder("model,accuracy,precision,recall,f1,confidence\n");
        for (int i = 0; i < Models.Length; i++)
        {
            double acc = C(baseAcc + offsets[i]);
            double prec = C(acc - 2);
            double rec = C(acc - 4);
            double f1 = prec + rec > 0 ? 2 * prec * rec / (prec + rec) : 0;
            double conf = C(acc - 1);
            sb.AppendLine($"{Models[i]},{acc:0.0},{prec:0.0},{rec:0.0},{f1:0.0},{conf:0.0}");
        }
        return sb.ToString();
    }

    // ---- learning progress, skills ----

    public static string LearningProgressCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var sb = new System.Text.StringBuilder("metric,value\n");
        sb.AppendLine($"Focus Improvement,{FocusScore(a, b):0.0}");
        sb.AppendLine($"Creativity Improvement,{CreativityScore(b, w):0.0}");
        sb.AppendLine($"Learning Growth,{C(a * 0.4 + Diversity(w) * 40):0.0}");
        sb.AppendLine($"Cognitive Development,{C(a * 0.3 + m * 0.2 + Diversity(w) * 30):0.0}");
        return sb.ToString();
    }

    public static string SkillPredictionsCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("skill,score\n");
        sb.AppendLine($"Problem Solving Potential,{C(beta * 60 + a * 0.3 + div * 10):0.0}");
        sb.AppendLine($"Research Potential,{C(div * 60 + theta * 30 + a * 0.1):0.0}");
        sb.AppendLine($"Innovation Potential,{C(alpha * 40 + gamma * 40 + div * 20):0.0}");
        sb.AppendLine($"Technical Aptitude,{C(beta * 50 + a * 0.3 + gamma * 20):0.0}");
        sb.AppendLine($"Learning Capacity,{C(a * 0.4 + div * 30 + beta * 20):0.0}");
        return sb.ToString();
    }

    // ---- multi-session population analysis ----

    public static string PopulationAnalysisCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int priorSessions)
    {
        double baseAcc = BaseAccuracy(a, b, w);
        int sessions = priorSessions + 1;
        var sb = new System.Text.StringBuilder("model_type,sessions,avg_accuracy,top_label\n");
        sb.AppendLine($"Population Model,{sessions},{C(baseAcc - 3):0.0},{PredictLabel(a, m, b, w)}");
        sb.AppendLine($"User Model,{sessions},{baseAcc:0.0},{PredictLabel(a, m, b, w)}");
        sb.AppendLine($"Group Model,{sessions},{C(baseAcc - 1):0.0},{PredictLabel(a, m, b, w)}");
        return sb.ToString();
    }

    // ---- append-only logs ----

    public static string TrainingHistoryHeader() =>
        "date,distinct_concepts,predicted_label,predicted_mode,base_accuracy,focus,creativity,productivity";

    public static string TrainingHistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{Distinct(w)},{PredictLabel(a, m, b, w)},{PredictMode(b, w, a)},{BaseAccuracy(a, b, w):0},{FocusScore(a, b):0},{CreativityScore(b, w):0},{ProductivityScore(a, b):0}";
    }

    public static void AppendTrainingHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
        => Append(path, TrainingHistoryHeader(), TrainingHistoryRow(a, m, b, w));

    public static void AppendFeedback(string path, string predictedLabel, string status)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Append(path, "date,predicted_label,feedback,relabeled_to", $"{date},{predictedLabel},{status},-");
    }

    public static void AppendBrainDatabase(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topCareer)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string row = $"{date},{PredictLabel(a, m, b, w)},{BaseAccuracy(a, b, w):0},{PredictMode(b, w, a)},{Distinct(w)},{topCareer},pending";
        Append(path, "date,label,top_feature_accuracy,prediction,distinct_concepts,top_career,user_feedback", row);
    }

    private static void Append(string path, string header, string row)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(header);
        writer.WriteLine(row);
    }
}
