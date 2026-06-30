using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Pattern Recognition content: the per-session pattern
/// CSVs (word/thought/knowledge), the dashboard, and fallbacks for the LM artifacts
/// (hidden patterns, future patterns, research paper, 10-slide deck). Reuses
/// <see cref="NlpContent"/> for shared text helpers. Mirrors the program-45 layout.
/// </summary>
public static class PatternContent
{
    public static string WordPatternsCsv(IReadOnlyList<string> words)
    {
        var freq = Freq(words);
        var sb = new StringBuilder("word,count,type\n");
        foreach (var (word, count) in freq.OrderByDescending(kv => kv.Value))
        {
            string type = count >= 3 ? "recurring" : count == 1 ? "rare" : "common";
            sb.AppendLine($"{word},{count},{type}");
        }
        if (freq.Count == 0) sb.AppendLine("brain,1,rare");
        return sb.ToString();
    }

    public static string ThoughtPatternsCsv(IReadOnlyList<string> words)
    {
        var seqs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        void Count(int size)
        {
            for (int i = 0; i + size <= words.Count; i++)
            {
                string key = string.Join(' ', words.Skip(i).Take(size));
                seqs[key] = seqs.TryGetValue(key, out var c) ? c + 1 : 1;
            }
        }
        Count(2); Count(3);
        var repeated = seqs.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Value).ToList();
        var sb = new StringBuilder("sequence,count\n");
        foreach (var (seq, count) in repeated) sb.AppendLine($"\"{seq}\",{count}");
        if (repeated.Count == 0) sb.AppendLine("\"(no repeated sequences)\",0");
        return sb.ToString();
    }

    public static string KnowledgePatternsCsv(IReadOnlyList<string> words, IReadOnlyList<PatternTopicScore> topics)
    {
        var sb = new StringBuilder("domain,indicator,score\n");
        foreach (var t in topics.Take(10))
        {
            string indicator = t.Count >= 3 ? "expertise" : t.Count >= 1 ? "interest" : "latent";
            sb.AppendLine($"{t.Topic},{indicator},{t.Percent:0.0}");
        }
        return sb.ToString();
    }

    public static string Dashboard(CognitiveSignature sig, IReadOnlyList<PatternTopicScore> topics,
        IReadOnlyList<(string State, double Score)> states)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Pattern Recognition Dashboard");
        sb.AppendLine();
        sb.AppendLine("## Cognitive Signature");
        sb.AppendLine("| Axis | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in sig.Axes()) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Topic Patterns");
        sb.AppendLine("| Topic | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var t in topics.Take(10)) sb.AppendLine($"| {t.Topic} | {t.Percent:0} |");
        sb.AppendLine();
        sb.AppendLine($"Dominant brain state: **{states.OrderByDescending(s => s.Score).First().State}**.");
        return sb.ToString();
    }

    public static string DefaultHiddenPatterns(IReadOnlyList<string> words, IReadOnlyList<PatternTopicScore> topics, CognitiveSignature sig)
    {
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("HIDDEN PATTERNS");
        sb.AppendLine("===============");
        sb.AppendLine($"Unexpected connection: {(topics.Count > 1 ? topics[0].Topic + " ↔ " + topics[1].Topic : "single dominant interest")}.");
        sb.AppendLine($"Rare combination: {string.Join(" + ", concepts.Take(3))}.");
        sb.AppendLine($"Novel signal: high {sig.Axes().OrderByDescending(a => a.Value).First().Name} paired with {sig.Axes().OrderBy(a => a.Value).First().Name}.");
        return sb.ToString();
    }

    public static string DefaultFuturePatterns(IReadOnlyList<PatternTopicScore> topics, CognitiveSignature sig)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "General";
        var sb = new StringBuilder();
        sb.AppendLine("FUTURE PATTERNS");
        sb.AppendLine("===============");
        sb.AppendLine($"Future interests: deepening {top}.");
        sb.AppendLine($"Emerging topics: {string.Join(", ", topics.Skip(1).Take(2).Select(t => t.Topic))}.");
        sb.AppendLine($"Learning direction: leverage strong {sig.Axes().OrderByDescending(a => a.Value).First().Name}.");
        sb.AppendLine($"Innovation potential: {sig.Innovation:0}/100. Research potential: {sig.Curiosity:0}/100.");
        return sb.ToString();
    }

    public static string DefaultResearchMarkdown(
        CognitiveSignature sig, IReadOnlyList<PatternTopicScore> topics, IReadOnlyList<string> words,
        double avgAtt, double avgMed, string dominantBand)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "General";
        var concepts = NlpContent.TopWords(words, 6);
        string topAxis = sig.Axes().OrderByDescending(a => a.Value).First().Name;
        var sb = new StringBuilder();
        sb.AppendLine("# Pattern Recognition Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"The dominant recurring interest is {top}; the strongest cognitive axis is {topAxis}.");
        sb.AppendLine();
        sb.AppendLine("## Brain Statistics");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}. " +
                      $"Signature — Logic {sig.Logic:0}, Creativity {sig.Creativity:0}, Focus {sig.Focus:0}, Innovation {sig.Innovation:0}.");
        sb.AppendLine();
        sb.AppendLine("## Pattern Analysis");
        sb.AppendLine($"Recurring concepts: {string.Join(", ", concepts)}. Top topics: {string.Join(", ", topics.Take(3).Select(t => $"{t.Topic} {t.Percent:0}%"))}.");
        sb.AppendLine();
        sb.AppendLine("## Trend Analysis");
        sb.AppendLine("Across recorded sessions, the cognitive signature is tracked over time to surface growth, decline and stable areas.");
        sb.AppendLine();
        sb.AppendLine("## Future Predictions");
        sb.AppendLine($"Likely trajectory: deeper {top} with rising {topAxis}.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine($"Lean into {topAxis}; schedule deliberate practice in {top}; revisit weaker axes to balance the signature.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        CognitiveSignature sig, IReadOnlyList<PatternTopicScore> topics, IReadOnlyList<string> words,
        double avgAtt, double avgMed, string dominantBand)
    {
        string Topic(int i) => i < topics.Count ? $"{topics[i].Topic} ({topics[i].Percent:0}%)" : "—";
        var concepts = NlpContent.TopWords(words, 3);
        string topAxis = sig.Axes().OrderByDescending(a => a.Value).First().Name;
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Word Patterns", concepts.Count > 0 ? concepts.ToArray() : new[] { "—" }),
            new("Topic Patterns", new[] { Topic(0), Topic(1), Topic(2) }),
            new("Cognitive Signature", new[] { $"Logic {sig.Logic:0}", $"Creativity {sig.Creativity:0}", $"Focus {sig.Focus:0}", $"Innovation {sig.Innovation:0}" }),
            new("Hidden Patterns", new[] { $"Strongest axis: {topAxis}", "Unexpected concept links" }),
            new("Brain States", new[] { $"Dominant: {CognitiveSignature.DominantState(sig, avgAtt, avgMed).State}" }),
            new("Trend Analysis", new[] { "Signature tracked across sessions", "Growth / decline / stable areas" }),
            new("Forecasting", new[] { $"Deepen {(topics.Count > 0 ? topics[0].Topic : "your field")}", $"Innovation potential {sig.Innovation:0}" }),
            new("Conclusions", new[] { "Patterns reveal cognitive signature", "Track over time to guide growth" }),
        };
    }

    private static Dictionary<string, int> Freq(IReadOnlyList<string> words)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }
        return freq;
    }
}
