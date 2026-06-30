using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>Deterministic sentiment readout (percentages sum to 100).</summary>
public sealed record Sentiment(
    double Positive, double Negative, double Neutral,
    double Motivation, double Confidence, double Curiosity);

/// <summary>The deterministic NLP statistics for a session.</summary>
public sealed record NlpProfile(
    int TotalWords, int UniqueWords, double Complexity, double Creativity,
    double Technical, double CognitiveDiversity, Sentiment Sentiment)
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
        return (Share("theta"),
                Share("lowAlpha") + Share("highAlpha"),
                Share("lowBeta") + Share("highBeta"),
                Share("lowGamma") + Share("midGamma"));
    }

    private static double Diversity(IReadOnlyList<string> words)
    {
        int n = words.Count;
        if (n == 0) return 0;
        int distinct = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        return (double)distinct / n;
    }

    public static NlpProfile Compute(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands,
        IReadOnlyList<string> words, IReadOnlyList<TopicScore> topics)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        int n = words.Count;
        int distinct = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        double diversity = Diversity(words);
        double avgLen = n > 0 ? words.Average(w => w.Length) : 0;

        double complexity = C(avgLen * 8 + diversity * 40);
        double creativity = C(alpha * 60 + gamma * 50 + diversity * 40);
        double technical = C(beta * 80 + avgAtt * 0.3);
        double cogDiversity = C(diversity * 100);

        // sentiment: positivity from calm + engagement, negativity from fast/low-calm
        double pos = C(avgMed * 0.6 + avgAtt * 0.2 + gamma * 30);
        double neg = C((100 - avgMed) * 0.4 + beta * 40);
        double neu = C(avgMed * 0.2 + theta * 30);
        double s = pos + neg + neu; if (s <= 0) { pos = neu = 33.3; neg = 33.4; s = 100; }
        pos = pos / s * 100; neg = neg / s * 100; neu = neu / s * 100;

        double motivation = C(avgAtt * 0.7 + gamma * 30);
        double confidence = C(avgAtt * 0.5 + avgMed * 0.3 + beta * 20);
        double curiosity = C(diversity * 60 + theta * 40);

        var sentiment = new Sentiment(pos, neg, neu, motivation, confidence, curiosity);
        return new NlpProfile(n, distinct, complexity, creativity, technical, cogDiversity, sentiment);
    }

    /// <summary>Six ordered thought-structure metrics (0–100).</summary>
    public static IReadOnlyList<(string Metric, double Value)> ThoughtMetrics(
        double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double diversity = Diversity(words);
        double avgLen = words.Count > 0 ? words.Average(w => w.Length) : 0;
        return new (string, double)[]
        {
            ("Vocabulary Diversity", C(diversity * 100)),
            ("Concept Density", C(avgLen * 10)),
            ("Language Complexity", C(avgLen * 8 + diversity * 40)),
            ("Idea Connectivity", C(beta * 60 + (1 - diversity) * 40)),
            ("Logical Consistency", C(beta * 50 + avgAtt * 0.5)),
            ("Topic Transitions", C(diversity * 70)),
        };
    }

    /// <summary>Six communication-style percentages, normalized to sum 100.</summary>
    public static IReadOnlyList<(string Style, double Percent)> CommunicationProfile(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double diversity = Diversity(words);
        var raw = new (string Style, double V)[]
        {
            ("Analytical", beta * 100 + avgAtt * 0.3),
            ("Creative", alpha * 100 + gamma * 50 + diversity * 30),
            ("Technical", beta * 80 + avgAtt * 0.2),
            ("Emotional", (100 - avgMed) * 0.5 + theta * 30),
            ("Logical", avgAtt * 0.6 + beta * 30),
            ("Visionary", gamma * 100 + theta * 30 + diversity * 20),
        };
        double sum = raw.Sum(r => r.V);
        if (sum <= 0) return raw.Select(r => (r.Style, 100.0 / raw.Length)).ToList();
        return raw.Select(r => (r.Style, r.V / sum * 100)).ToList();
    }

    public static string TopicsCsv(IReadOnlyList<TopicScore> topics)
    {
        var sb = new System.Text.StringBuilder("topic,percent\n");
        foreach (var t in topics) sb.AppendLine($"{Escape(t.Topic)},{t.Percent:0.0}");
        return sb.ToString();
    }

    public static string ThoughtCsv(double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var sb = new System.Text.StringBuilder("metric,value\n");
        foreach (var (metric, value) in ThoughtMetrics(avgAtt, bands, words))
            sb.AppendLine($"{Escape(metric)},{value:0.0}");
        return sb.ToString();
    }

    public static string CommunicationCsv(double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var sb = new System.Text.StringBuilder("style,percent\n");
        foreach (var (style, percent) in CommunicationProfile(avgAtt, avgMed, bands, words))
            sb.AppendLine($"{Escape(style)},{percent:0.0}");
        return sb.ToString();
    }

    public static string HistoryHeader() =>
        "date,total_words,unique_words,top_topics,positive,negative,neutral,complexity,creativity,technical,cognitive_diversity";

    public static string HistoryRow(NlpProfile p, IReadOnlyList<TopicScore> topics)
    {
        string top = string.Join(" | ", topics.Take(3).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{p.TotalWords},{p.UniqueWords},\"{top}\",{p.Sentiment.Positive:0},{p.Sentiment.Negative:0}," +
               $"{p.Sentiment.Neutral:0},{p.Complexity:0},{p.Creativity:0},{p.Technical:0},{p.CognitiveDiversity:0}";
    }

    public static void AppendHistory(string path, NlpProfile p, IReadOnlyList<TopicScore> topics)
    {
        bool isNew = !File.Exists(path);
        using var w = new StreamWriter(path, append: true);
        if (isNew) w.WriteLine(HistoryHeader());
        w.WriteLine(HistoryRow(p, topics));
    }

    private static string Escape(string s) => s.Contains(',') ? $"\"{s}\"" : s;
}
