using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe NLP content: text assembly, the token/vocabulary/
/// recorded CSV builders, the knowledge graph, the dashboard, and fallbacks for
/// every LM artifact (POS, NER, semantic report, questions, chat, research paper,
/// 10-slide deck). Mirrors <see cref="LearningContent"/>.
/// </summary>
public static class NlpContent
{
    private static readonly HashSet<string> Pronouns =
        new(StringComparer.OrdinalIgnoreCase) { "i", "you", "he", "she", "it", "we", "they", "me", "him", "her", "them", "us" };
    private static readonly HashSet<string> Conjunctions =
        new(StringComparer.OrdinalIgnoreCase) { "and", "or", "but", "so", "yet", "nor", "because", "although", "while", "if" };

    // ---- text assembly ----

    public static IReadOnlyList<string> Sentences(IReadOnlyList<string> words, int perSentence = 8)
    {
        var sentences = new List<string>();
        for (int i = 0; i < words.Count; i += perSentence)
        {
            var chunk = words.Skip(i).Take(perSentence).ToList();
            if (chunk.Count == 0) continue;
            string s = string.Join(' ', chunk);
            sentences.Add(char.ToUpper(s[0]) + s[1..] + ".");
        }
        return sentences;
    }

    public static string Paragraphs(IReadOnlyList<string> words)
    {
        var sentences = Sentences(words);
        var sb = new StringBuilder();
        for (int i = 0; i < sentences.Count; i++)
        {
            sb.Append(sentences[i]).Append(' ');
            if ((i + 1) % 4 == 0) sb.Append("\n\n");
        }
        return sb.ToString().Trim();
    }

    public static string TranslatedText(string header, IReadOnlyList<string> words)
    {
        string body = words.Count == 0 ? "(no words captured)" : Paragraphs(words);
        return header + body;
    }

    // ---- tabular builders ----

    public static string RecordedEegCsv(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder("index,word\n");
        for (int i = 0; i < words.Count; i++) sb.AppendLine($"{i},{words[i]}");
        return sb.ToString();
    }

    public static string TokensCsv(IReadOnlyList<string> words)
    {
        var freq = Freq(words);
        var sb = new StringBuilder("token,count\n");
        foreach (var (token, count) in freq.OrderByDescending(kv => kv.Value))
            sb.AppendLine($"{token},{count}");
        return sb.ToString();
    }

    public static string VocabularyCsv(IReadOnlyList<string> words, IReadOnlyList<TopicScore> topics)
    {
        var freq = Freq(words);
        var sb = new StringBuilder("word,count,category\n");
        foreach (var (word, count) in freq.OrderByDescending(kv => kv.Value))
            sb.AppendLine($"{word},{count},{Category(word)}");
        return sb.ToString();
    }

    // ---- knowledge graph + dashboard ----

    public static string KnowledgeGraph(IReadOnlyList<TopicScore> topics, IReadOnlyList<string> words)
    {
        var top = topics.Take(4).Select(t => t.Topic).ToList();
        if (top.Count == 0) top.Add("General");
        var concepts = TopWords(words, 5);
        var sb = new StringBuilder();
        sb.AppendLine("# Brain Knowledge Graph");
        sb.AppendLine();
        sb.AppendLine("Relationships between words, concepts, topics and themes:");
        sb.AppendLine();
        for (int i = 0; i < top.Count; i++)
        {
            string next = i + 1 < top.Count ? top[i + 1] : "Synthesis";
            sb.AppendLine($"- **{top[i]}** → relates to → **{next}**");
        }
        sb.AppendLine();
        if (concepts.Count > 0)
            sb.AppendLine($"Core concepts: {string.Join(" · ", concepts)}");
        sb.AppendLine();
        sb.AppendLine($"Pathway: {string.Join(" → ", top)} → Synthesis");
        return sb.ToString();
    }

    public static string Dashboard(NlpProfile p, IReadOnlyList<TopicScore> topics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# NLP Statistics");
        sb.AppendLine();
        sb.AppendLine($"- Total words: {p.TotalWords}");
        sb.AppendLine($"- Unique words: {p.UniqueWords}");
        sb.AppendLine($"- Topics detected: {topics.Count(t => t.Count > 0)}");
        sb.AppendLine($"- Sentiment: +{p.Sentiment.Positive:0}% / -{p.Sentiment.Negative:0}% / ={p.Sentiment.Neutral:0}%");
        sb.AppendLine($"- Complexity: {p.Complexity:0} · Creativity: {p.Creativity:0} · Technical: {p.Technical:0}");
        sb.AppendLine($"- Cognitive diversity: {p.CognitiveDiversity:0}");
        return sb.ToString();
    }

    // ---- LM fallbacks ----

    public static string DefaultPosCsv(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder("word,pos\n");
        foreach (var w in words.Distinct(StringComparer.OrdinalIgnoreCase))
            sb.AppendLine($"{w},{GuessPos(w)}");
        if (words.Count == 0) sb.AppendLine("brain,NOUN");
        return sb.ToString();
    }

    public static string DefaultEntitiesCsv(IReadOnlyList<string> words, IReadOnlyList<TopicScore> topics)
    {
        var set = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder("entity,type\n");
        int rows = 0;
        foreach (var t in topics.Where(t => t.Count > 0))
            foreach (var w in TopWords(words, 20))
                if (set.Contains(w)) { sb.AppendLine($"{w},{t.Topic}"); if (++rows >= 30) break; }
        if (rows == 0)
            foreach (var w in TopWords(words, 5)) sb.AppendLine($"{w},CONCEPT");
        if (sb.ToString().Trim().Split('\n').Length <= 1) sb.AppendLine("brain,CONCEPT");
        return sb.ToString();
    }

    public static string DefaultSemanticReport(NlpProfile p, IReadOnlyList<TopicScore> topics, IReadOnlyList<string> words)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "General";
        var concepts = TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("SEMANTIC ANALYSIS REPORT");
        sb.AppendLine("========================");
        sb.AppendLine();
        sb.AppendLine($"Main themes: {string.Join(", ", topics.Take(3).Select(t => t.Topic))}.");
        sb.AppendLine($"Hidden themes: {string.Join(", ", topics.Skip(3).Take(2).Select(t => t.Topic))}.");
        sb.AppendLine($"Subject interests: {top}.");
        sb.AppendLine($"Cognitive focus: complexity {p.Complexity:0}/100, technical {p.Technical:0}/100.");
        sb.AppendLine($"Dominant concepts: {string.Join(", ", concepts)}.");
        return sb.ToString();
    }

    public static string DefaultQuestions(IReadOnlyList<string> words, IReadOnlyList<TopicScore> topics)
    {
        var concepts = TopWords(words, 12);
        if (concepts.Count == 0) concepts = new List<string> { "thought", "idea", "mind" };
        var topicNames = topics.Select(t => t.Topic).ToList();
        if (topicNames.Count == 0) topicNames.Add("Science");
        string[] templates =
        {
            "What is the relationship between {0} and {1}?",
            "How could {0} be applied in {2}?",
            "Why does {0} matter for {2}?",
            "What research could deepen understanding of {0}?",
            "How would you teach {0} to a beginner?",
            "What experiment would test the role of {0}?",
            "How does {0} connect to {1} in {2}?",
            "What is the next step after learning {0}?",
        };
        var sb = new StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            string a = concepts[i % concepts.Count];
            string b = concepts[(i + 1) % concepts.Count];
            string t = topicNames[i % topicNames.Count];
            string tpl = templates[i % templates.Length];
            sb.AppendLine($"{i + 1}. {string.Format(tpl, a, b, t)}");
        }
        return sb.ToString();
    }

    public static string DefaultChatLog(IReadOnlyList<string> words, IReadOnlyList<TopicScore> topics)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "your thoughts";
        var concepts = TopWords(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("BRAIN CHAT LOG");
        sb.AppendLine("==============");
        sb.AppendLine();
        sb.AppendLine("Q: What is on my mind?");
        sb.AppendLine($"A: Your decoded words center on {top}" +
                      (concepts.Count > 0 ? $", especially {string.Join(", ", concepts)}." : "."));
        sb.AppendLine();
        sb.AppendLine("Q: What should I explore next?");
        sb.AppendLine($"A: Dig deeper into {top} and connect it to the recurring ideas above.");
        return sb.ToString();
    }

    public static string DefaultResearchMarkdown(
        NlpProfile p, IReadOnlyList<TopicScore> topics, IReadOnlyList<string> words,
        double avgAtt, double avgMed, string dominantBand)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "General";
        var concepts = TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# NLP Analysis of Translated EEG");
        sb.AppendLine();
        sb.AppendLine("## Abstract");
        sb.AppendLine($"This report applies natural language processing to a 3-minute EEG-derived word stream. The dominant theme is {top}; sentiment is {p.Sentiment.Positive:0}% positive.");
        sb.AppendLine();
        sb.AppendLine("## Introduction");
        sb.AppendLine($"The brain's decoded vocabulary ({p.UniqueWords} unique of {p.TotalWords} words) is treated as natural language and analyzed for theme, sentiment and structure.");
        sb.AppendLine();
        sb.AppendLine("## Methods");
        sb.AppendLine($"EEG was decoded to English via eeg_map, tokenized, and ranked across {topics.Count} topics. Bands summarized to a dominant band of {dominantBand}; attention {avgAtt:0}/100, calm {avgMed:0}/100.");
        sb.AppendLine();
        sb.AppendLine("## Results");
        sb.AppendLine($"Top topics: {string.Join(", ", topics.Take(3).Select(t => $"{t.Topic} {t.Percent:0}%"))}. Core concepts: {string.Join(", ", concepts)}. Complexity {p.Complexity:0}, creativity {p.Creativity:0}, technical {p.Technical:0}.");
        sb.AppendLine();
        sb.AppendLine("## Discussion");
        sb.AppendLine($"The profile suggests a {top}-oriented cognitive focus with cognitive diversity {p.CognitiveDiversity:0}/100.");
        sb.AppendLine();
        sb.AppendLine("## Conclusion");
        sb.AppendLine($"EEG-derived language can be processed with standard NLP to reveal {top} as the user's leading theme.");
        sb.AppendLine();
        sb.AppendLine("## References");
        sb.AppendLine("1. mindedOS eeg_map lexicon. 2. Consumer single-channel EEG — directional, not clinical.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        NlpProfile p, IReadOnlyList<TopicScore> topics, double avgAtt, double avgMed, string dominantBand)
    {
        string Topic(int i) => i < topics.Count ? $"{topics[i].Topic} ({topics[i].Percent:0}%)" : "—";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{p.TotalWords} words", $"{p.UniqueWords} unique" }),
            new("Vocabulary Statistics", new[] { $"Cognitive diversity {p.CognitiveDiversity:0}", $"Complexity {p.Complexity:0}" }),
            new("Topic Detection", new[] { Topic(0), Topic(1), Topic(2) }),
            new("Sentiment Analysis", new[] { $"Positive {p.Sentiment.Positive:0}%", $"Negative {p.Sentiment.Negative:0}%", $"Neutral {p.Sentiment.Neutral:0}%" }),
            new("Semantic Analysis", new[] { $"Lead theme {(topics.Count > 0 ? topics[0].Topic : "General")}", $"Confidence {p.Sentiment.Confidence:0}" }),
            new("Communication Style", new[] { $"Technical {p.Technical:0}", $"Creativity {p.Creativity:0}" }),
            new("Brain Themes", new[] { Topic(0), Topic(1), Topic(2) }),
            new("Research Opportunities", new[] { $"Deepen {(topics.Count > 0 ? topics[0].Topic : "your field")}", "Form testable questions" }),
            new("Conclusions", new[] { "EEG decoded to language", "NLP reveals themes & sentiment", "Use it to guide learning" }),
        };
    }

    /// <summary>
    /// Pull a CSV block that starts at a marker line (e.g. "# POS") up to the next
    /// line starting with "# ", dropping the marker. Returns the block prefixed with
    /// <paramref name="expectedHeader"/> if the first data line isn't already it.
    /// Returns null if the marker is absent or the block has no data rows.
    /// </summary>
    public static string? ExtractCsvSection(string text, string marker, string expectedHeader)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var lines = text.Replace("\r\n", "\n").Split('\n');
        int start = Array.FindIndex(lines, l => l.Trim().StartsWith(marker, StringComparison.OrdinalIgnoreCase));
        if (start < 0) return null;
        var body = new List<string>();
        for (int i = start + 1; i < lines.Length; i++)
        {
            var t = lines[i].Trim();
            if (t.StartsWith("# ")) break;
            if (t.Length == 0) continue;
            body.Add(t);
        }
        // drop a duplicated header line if the model repeated it
        if (body.Count > 0 && body[0].Replace(" ", "").Equals(expectedHeader.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
            body.RemoveAt(0);
        if (body.Count == 0) return null;
        return expectedHeader + "\n" + string.Join("\n", body) + "\n";
    }

    /// <summary>
    /// Return the text between <paramref name="marker"/> (exclusive) and
    /// <paramref name="endMarker"/> (exclusive, or end of text if null). Null if the
    /// marker is absent or the slice is empty.
    /// </summary>
    public static string? ExtractTextSection(string text, string marker, string? endMarker)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var lines = text.Replace("\r\n", "\n").Split('\n');
        int start = Array.FindIndex(lines, l => l.Trim().StartsWith(marker, StringComparison.OrdinalIgnoreCase));
        if (start < 0) return null;
        int end = lines.Length;
        if (endMarker is not null)
        {
            int e = Array.FindIndex(lines, start + 1, l => l.Trim().StartsWith(endMarker, StringComparison.OrdinalIgnoreCase));
            if (e >= 0) end = e;
        }
        var slice = string.Join("\n", lines[(start + 1)..end]).Trim();
        return slice.Length == 0 ? null : slice;
    }

    // ---- helpers ----

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

    public static List<string> TopWords(IReadOnlyList<string> words, int n) =>
        Freq(words).OrderByDescending(kv => kv.Value).Take(n).Select(kv => kv.Key).ToList();

    private static string GuessPos(string w)
    {
        if (Pronouns.Contains(w)) return "PRONOUN";
        if (Conjunctions.Contains(w)) return "CONJUNCTION";
        if (w.EndsWith("ly", StringComparison.OrdinalIgnoreCase)) return "ADVERB";
        if (w.EndsWith("ing", StringComparison.OrdinalIgnoreCase) || w.EndsWith("ed", StringComparison.OrdinalIgnoreCase)) return "VERB";
        if (w.EndsWith("ous", StringComparison.OrdinalIgnoreCase) || w.EndsWith("ful", StringComparison.OrdinalIgnoreCase) || w.EndsWith("ive", StringComparison.OrdinalIgnoreCase)) return "ADJECTIVE";
        return "NOUN";
    }

    private static string Category(string word)
    {
        // a light category tag for the vocabulary CSV; topic-independent heuristic
        if (word.Length >= 9) return "technical";
        if (word.Length <= 3) return "common";
        return "general";
    }
}
