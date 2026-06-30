using System.IO;
using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Voice Recognition content. No microphone/STT exists in this environment,
/// so the "transcript" is the EEG-decoded word stream. Produces the transcript, keywords, topic table,
/// knowledge graph, a silent placeholder .wav, the preview scorecard, and fallbacks for the LM artifacts
/// (the chat-log / learning / forecast narratives, a report and a 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class VoiceContent
{
    private static List<string> Concepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "idea", "concept", "topic", "system", "design", "learn" };
    }

    /// <summary>Writes a valid silent mono 16-bit PCM WAV placeholder (the app cannot capture audio).</summary>
    public static void WriteSilentWav(string path, int seconds = 1, int sampleRate = 8000)
    {
        int dataBytes = seconds * sampleRate * 2;
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataBytes);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);             // PCM fmt chunk size
        bw.Write((short)1);       // audio format = PCM
        bw.Write((short)1);       // mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2); // byte rate
        bw.Write((short)2);       // block align
        bw.Write((short)16);      // bits per sample
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(dataBytes);
        bw.Write(new byte[dataBytes]);
        bw.Flush();
        File.WriteAllBytes(path, ms.ToArray());
    }

    public static string TranscribedSpeech(IReadOnlyList<string> words, int seconds)
    {
        int wordCount = words.Count;
        int sentences = Math.Max(1, wordCount / 8);
        double rate = seconds > 0 ? wordCount / (seconds / 60.0) : 0;
        var sb = new StringBuilder();
        sb.AppendLine("# Transcribed Speech");
        sb.AppendLine("# (No microphone/STT in this environment — the EEG-decoded word stream is used as the transcript.)");
        sb.AppendLine($"# word_count: {wordCount}");
        sb.AppendLine($"# sentence_count: {sentences}");
        sb.AppendLine($"# speaking_duration_seconds: {seconds}");
        sb.AppendLine($"# speaking_rate_wpm: {rate:0.0}");
        sb.AppendLine();
        foreach (var s in NlpContent.Sentences(words)) sb.AppendLine(s + ".");
        return sb.ToString();
    }

    public static string KeywordsCsv(IReadOnlyList<string> words, VoiceTopics topics)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var x = w.Trim().ToLowerInvariant();
            if (x.Length == 0 || x == "—") continue;
            freq[x] = freq.TryGetValue(x, out var c) ? c + 1 : 1;
        }
        int max = freq.Count > 0 ? freq.Values.Max() : 1;
        var sb = new StringBuilder("keyword,frequency,importance,type\n");
        foreach (var (word, count) in freq.OrderByDescending(kv => kv.Value).Take(15))
        {
            double importance = Math.Round(100.0 * count / max, 1);
            string type = topics.IsTechnicalTerm(word) ? "technical" : "concept";
            sb.AppendLine($"{word},{count},{importance:0.0},{type}");
        }
        if (freq.Count == 0) sb.AppendLine("idea,1,100.0,concept");
        return sb.ToString();
    }

    public static string VoiceTopicsCsv(IReadOnlyList<VoiceTopicScore> topics)
    {
        var sb = new StringBuilder("topic,percent\n");
        foreach (var t in topics) sb.AppendLine($"{t.Topic},{t.Percent:0.0}");
        if (topics.Count == 0) sb.AppendLine("Research,100.0");
        return sb.ToString();
    }

    public static string KnowledgeGraphMd(IReadOnlyList<string> words, IReadOnlyList<VoiceTopicScore> topics)
    {
        var concepts = Concepts(words, 6);
        var tops = topics.Take(4).Select(t => t.Topic).ToList();
        if (tops.Count == 0) tops.Add("Research");
        var sb = new StringBuilder();
        sb.AppendLine("# Voice Knowledge Graph");
        sb.AppendLine();
        sb.AppendLine("## Concepts");
        foreach (var c in concepts) sb.AppendLine($"- {c}");
        sb.AppendLine();
        sb.AppendLine("## Keywords → Topics");
        for (int i = 0; i < concepts.Count; i++)
            sb.AppendLine($"- **{concepts[i]}** → {tops[i % tops.Count]}");
        sb.AppendLine();
        sb.AppendLine("## Ideas");
        sb.AppendLine($"- {string.Join(" + ", concepts.Take(3))} → a connected line of thought");
        return sb.ToString();
    }

    // ---- preview scorecard ----

    public static string Scorecard(IReadOnlyList<(string Score, double Value)> dashboard, string topTopic, string style)
    {
        var sb = new StringBuilder();
        sb.AppendLine("VOICE RECOGNITION DASHBOARD");
        sb.AppendLine("===========================");
        sb.AppendLine($"Top topic: {topTopic}   ·   Dominant style: {style}");
        foreach (var (name, value) in dashboard)
        {
            int filled = (int)Math.Round(value / 5.0);
            string bar = new string('█', Math.Clamp(filled, 0, 20)) + new string('░', Math.Clamp(20 - filled, 0, 20));
            sb.AppendLine($"{name,-22} {bar} {value:0}");
        }
        return sb.ToString();
    }

    // ---- LM fallbacks: narratives ----

    public static string DefaultChatLog(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 5);
        var sb = new StringBuilder();
        sb.AppendLine("VOICE CHAT LOG");
        sb.AppendLine("==============");
        for (int i = 0; i < concepts.Count; i++)
        {
            sb.AppendLine($"You: Tell me about {concepts[i]}.");
            sb.AppendLine($"Assistant: {concepts[i]} connects to your other interests; here is a concise explanation and a next step.");
        }
        return sb.ToString();
    }

    public static string DefaultLearningAnalysis(IReadOnlyList<VoiceTopicScore> topics, IReadOnlyList<string> words)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "your field";
        var concepts = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("LEARNING ANALYSIS");
        sb.AppendLine("=================");
        sb.AppendLine($"Knowledge areas: {string.Join(", ", topics.Take(3).Select(t => t.Topic))}.");
        sb.AppendLine($"Learning interests: deeper study of {concepts[0]} within {top}.");
        sb.AppendLine("Research interests: open questions around the recurring concepts.");
        sb.AppendLine("Educational opportunities: structured courses and hands-on projects.");
        return sb.ToString();
    }

    public static string DefaultForecast(IReadOnlyList<VoiceTopicScore> topics)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("COMMUNICATION FORECAST");
        sb.AppendLine("======================");
        sb.AppendLine($"Future communication strengths: clearer, more structured delivery in {top}.");
        sb.AppendLine("Leadership development: growing ability to direct and align a team.");
        sb.AppendLine("Educational potential: strong capacity to teach the recurring concepts.");
        sb.AppendLine("Public speaking potential: improves steadily with practice and feedback.");
        return sb.ToString();
    }

    // ---- LM fallback: research report (.docx) ----

    public static string DefaultReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<VoiceTopicScore> topics,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand, string style)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "General";
        var concepts = Concepts(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Voice Recognition Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A voice & cognition analysis from a 3-minute session. Top topic: {top}; dominant style: {style}; speech rate {dashboard[0].Value:0} wpm.");
        sb.AppendLine();
        sb.AppendLine("## Voice Statistics");
        sb.AppendLine($"Vocabulary diversity {dashboard[2].Value:0}, confidence {dashboard[1].Value:0}; recurring concepts {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Correlation Analysis");
        sb.AppendLine($"Spoken language and EEG-translated concepts align; EEG correlation {dashboard[5].Value:0}. Context: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine("Lean into the dominant style, broaden vocabulary in adjacent topics, and rehearse to raise confidence and engagement.");
        return sb.ToString();
    }

    // ---- LM fallback: 10-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<VoiceTopicScore> topics,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand, string style)
    {
        var concepts = Concepts(words, 3);
        string Topic(int i) => i < topics.Count ? $"{topics[i].Topic} ({topics[i].Percent:0}%)" : "—";
        return new List<SlideContent>
        {
            new("Recording Overview", new[] { $"{words.Count} words", $"Attention {avgAtt:0}/100", $"Dominant band {dominantBand}" }),
            new("Speech Recognition Results", new[] { $"Speech rate {dashboard[0].Value:0} wpm", $"Recurring: {string.Join(", ", concepts)}" }),
            new("Voice Features", new[] { "Pitch, volume, rhythm", "Consistency & dynamics" }),
            new("Topic Analysis", new[] { Topic(0), Topic(1), Topic(2) }),
            new("Communication Style", new[] { $"Dominant: {style}", $"Balance {dashboard[4].Value:0}" }),
            new("Sentiment Analysis", new[] { $"Confidence {dashboard[1].Value:0}", "Enthusiasm, curiosity, motivation" }),
            new("EEG Correlation", new[] { $"Correlation {dashboard[5].Value:0}", "Speech ↔ EEG concept alignment" }),
            new("Speaker Profile", new[] { "Strengths & knowledge focus", "Teaching & leadership indicators" }),
            new("Forecasting", new[] { "Future communication strengths", "Leadership & public speaking" }),
            new("Conclusions", new[] { "Voice + EEG analyzed together", "Tracks growth over sessions" }),
        };
    }
}
