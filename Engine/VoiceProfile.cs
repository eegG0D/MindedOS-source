using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Voice Recognition math. In this EEG-simulator environment there is no microphone or
/// speech-to-text engine, so "voice" is modelled from the EEG-decoded word stream and band shares.
/// Produces the dashboard scores, speech/voice/communication/sentiment/profile/correlation/presentation
/// tables and the history log. Self-contained. All scores 0–100.
/// </summary>
public static class VoiceProfile
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

    public static double SpeechRate(int words, int seconds) => seconds > 0 ? words / (seconds / 60.0) : 0;
    public static double Confidence(double a, IReadOnlyList<BandReading> b)
    { var (_, _, beta, _) = Shares(b); return C(a * 0.4 + beta * 30 + 10); }

    /// <summary>The six dashboard scores (0–100). Order: Speech Rate, Confidence, Vocabulary Diversity, Topic Diversity, Communication Balance, EEG Correlation.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words, int seconds, int activeTopics)
    {
        double div = Diversity(words);
        var (theta, alpha, beta, gamma) = Shares(bands);
        return new (string, double)[]
        {
            ("Speech Rate", C(SpeechRate(words.Count, seconds))),
            ("Confidence", Confidence(avgAtt, bands)),
            ("Vocabulary Diversity", C(div * 100)),
            ("Topic Diversity", C(activeTopics * 9 + div * 20)),
            ("Communication Balance", C(alpha * 30 + beta * 30 + div * 30)),
            ("EEG Correlation", C(70 + div * 20)),
        };
    }

    public static string SpeechStatisticsCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int seconds)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        int sentences = Math.Max(1, w.Count / 8);
        var sb = new System.Text.StringBuilder("metric,value\n");
        sb.AppendLine($"Speaking Speed (wpm),{SpeechRate(w.Count, seconds):0.0}");
        sb.AppendLine($"Average Sentence Length,{(double)w.Count / sentences:0.0}");
        sb.AppendLine($"Vocabulary Diversity,{div * 100:0.0}");
        sb.AppendLine($"Speech Complexity,{C(div * 60 + beta * 40):0.0}");
        sb.AppendLine($"Pause Frequency,{C((1 - div) * 40 + (100 - a) * 0.2):0.0}");
        sb.AppendLine($"Verbal Fluency,{C(a * 0.4 + div * 30 + beta * 20):0.0}");
        return sb.ToString();
    }

    public static string VoiceFeaturesCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("feature,value\n");
        sb.AppendLine($"Pitch Variation,{C(gamma * 50 + alpha * 30):0.0}");
        sb.AppendLine($"Volume Variation,{C(beta * 40 + a * 0.3):0.0}");
        sb.AppendLine($"Rhythm,{C(alpha * 40 + theta * 30):0.0}");
        sb.AppendLine($"Speaking Consistency,{C((1 - div) * 50 + m * 0.3):0.0}");
        sb.AppendLine($"Speech Dynamics,{C(gamma * 40 + div * 40):0.0}");
        return sb.ToString();
    }

    public static string CommunicationStyleCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var raw = new (string Name, double V)[]
        {
            ("Technical", C(beta * 50 + a * 0.3)),
            ("Creative", C(alpha * 40 + gamma * 40)),
            ("Analytical", C(beta * 45 + div * 20)),
            ("Educational", C(a * 0.3 + div * 30 + alpha * 20)),
            ("Persuasive", C(a * 0.3 + beta * 30 + gamma * 10)),
            ("Narrative", C(alpha * 30 + theta * 30 + div * 20)),
        };
        double sum = raw.Sum(r => r.V);
        if (sum <= 0) sum = 1;
        var sb = new System.Text.StringBuilder("style,percent\n");
        foreach (var r in raw.OrderByDescending(r => r.V))
            sb.AppendLine($"{r.Name} Communication,{100.0 * r.V / sum:0.0}");
        return sb.ToString();
    }

    /// <summary>The dominant communication style label.</summary>
    public static string DominantStyle(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var raw = new (string Name, double V)[]
        {
            ("Technical", beta * 50 + a * 0.3), ("Creative", alpha * 40 + gamma * 40),
            ("Analytical", beta * 45 + div * 20), ("Educational", a * 0.3 + div * 30 + alpha * 20),
            ("Persuasive", a * 0.3 + beta * 30 + gamma * 10), ("Narrative", alpha * 30 + theta * 30 + div * 20),
        };
        return raw.OrderByDescending(r => r.V).First().Name;
    }

    public static string SentimentCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        double pos = C(50 + alpha * 20 + a * 0.2);
        double neg = C(20 + (100 - a) * 0.15);
        double neu = C(40);
        double s = pos + neg + neu; if (s <= 0) s = 1;
        var sb = new System.Text.StringBuilder("metric,value\n");
        sb.AppendLine($"Positive %,{100.0 * pos / s:0.0}");
        sb.AppendLine($"Neutral %,{100.0 * neu / s:0.0}");
        sb.AppendLine($"Negative %,{100.0 * neg / s:0.0}");
        sb.AppendLine($"Enthusiasm Score,{C(gamma * 40 + a * 0.3 + 10):0.0}");
        sb.AppendLine($"Confidence Score,{Confidence(a, b):0.0}");
        sb.AppendLine($"Curiosity Score,{C(div * 50 + theta * 30):0.0}");
        sb.AppendLine($"Motivation Score,{C(a * 0.4 + gamma * 20 + div * 20):0.0}");
        return sb.ToString();
    }

    public static string SpeakerProfileCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topTopic)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        string style = DominantStyle(a, b, w);
        var sb = new System.Text.StringBuilder("aspect,detail,score\n");
        sb.AppendLine($"Communication Strengths,{style.ToLowerInvariant()} delivery,{C(a * 0.4 + div * 30 + beta * 20):0.0}");
        sb.AppendLine($"Preferred Communication Style,{style},{C(beta * 40 + div * 30 + a * 0.2):0.0}");
        sb.AppendLine($"Knowledge Focus,{topTopic},{C(div * 50 + a * 0.3):0.0}");
        sb.AppendLine($"Presentation Ability,structured & clear,{C(a * 0.4 + beta * 30 + 10):0.0}");
        sb.AppendLine($"Teaching Potential,explains concepts,{C(a * 0.3 + div * 30 + alpha * 20):0.0}");
        sb.AppendLine($"Leadership Indicators,directive & confident,{C(a * 0.4 + beta * 30 + alpha * 10):0.0}");
        return sb.ToString();
    }

    public static string VoiceEegCorrelationCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int activeTopics)
    {
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("metric,score\n");
        sb.AppendLine($"Similarity Score,{C(70 + div * 20):0.0}");
        sb.AppendLine($"Concept Alignment,{C(72 + div * 18):0.0}");
        sb.AppendLine($"Topic Overlap,{C(activeTopics * 8 + 40):0.0}");
        sb.AppendLine($"Cognitive Consistency,{C(a * 0.4 + (1 - div) * 30 + 30):0.0}");
        return sb.ToString();
    }

    public static string PresentationEvaluationCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("criterion,score\n");
        sb.AppendLine($"Clarity,{C(a * 0.4 + (1 - div) * 20 + beta * 20):0.0}");
        sb.AppendLine($"Structure,{C(a * 0.3 + beta * 30 + 20):0.0}");
        sb.AppendLine($"Confidence,{Confidence(a, b):0.0}");
        sb.AppendLine($"Technical Accuracy,{C(beta * 50 + a * 0.3):0.0}");
        sb.AppendLine($"Engagement,{C(gamma * 40 + alpha * 30 + a * 0.2):0.0}");
        sb.AppendLine($"Persuasiveness,{C(a * 0.3 + beta * 30 + gamma * 10):0.0}");
        return sb.ToString();
    }

    public static string SpeakerComparisonCsv(double a, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        double div = Diversity(w);
        double style = C(a * 0.4 + div * 30 + 10);
        double vocab = C(div * 100);
        double expertise = C(a * 0.3 + div * 40 + 10);
        double effectiveness = C((style + vocab + expertise) / 3);
        var sb = new System.Text.StringBuilder("speaker,communication_style,vocabulary_richness,topic_expertise,effectiveness\n");
        sb.AppendLine($"You,{style:0.0},{vocab:0.0},{expertise:0.0},{effectiveness:0.0}");
        sb.AppendLine($"Baseline Presenter,68.0,62.0,65.0,65.0");
        sb.AppendLine($"Expert Communicator,82.0,80.0,85.0,82.0");
        return sb.ToString();
    }

    // ---- voice memory database ----

    public static string HistoryHeader() =>
        "date,distinct_words,speech_rate,confidence,vocabulary_diversity,topic_diversity,communication_balance,eeg_correlation,dominant_style";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int seconds, int activeTopics)
    {
        var d = Dashboard(a, m, b, w, seconds, activeTopics);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{Distinct(w)},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{DominantStyle(a, b, w)}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int seconds, int activeTopics)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, seconds, activeTopics));
    }
}
