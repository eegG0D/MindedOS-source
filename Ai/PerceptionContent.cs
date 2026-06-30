using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Perception content: pattern/artificial/image CSVs,
/// the dashboard, the MIME helper for vision, and fallbacks for the LM artifacts
/// (narratives, research paper, 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class PerceptionContent
{
    public static string PerceptionPatternsCsv(IReadOnlyList<string> words)
    {
        var freq = Freq(words);
        var sb = new StringBuilder("observation,count,type\n");
        foreach (var (word, count) in freq.OrderByDescending(kv => kv.Value))
        {
            string type = count >= 3 ? "dominant" : count == 1 ? "fleeting" : "recurring";
            sb.AppendLine($"{word},{count},{type}");
        }
        if (freq.Count == 0) sb.AppendLine("world,1,fleeting");
        return sb.ToString();
    }

    public static string ArtificialComparisonCsv(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        // human = the user's dashboard scores; computer/AI = fixed synthetic profiles.
        var sb = new StringBuilder("aspect,human,computer,ai\n");
        double[] computer = { 70, 95, 30, 60, 85, 80 };
        double[] ai = { 85, 90, 75, 95, 88, 92 };
        for (int i = 0; i < dashboard.Count; i++)
            sb.AppendLine($"{dashboard[i].Score},{dashboard[i].Value:0},{computer[i % computer.Length]:0},{ai[i % ai.Length]:0}");
        return sb.ToString();
    }

    public static string ImagePlaceholderCsv() =>
        "image,concept_match,visual_interest_alignment,perception_consistency\n(no images),0,0,0\n";

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<PerceptionScore> categories)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Perception Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Score | Value |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Perception Categories");
        sb.AppendLine("| Category | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var c in categories.Take(10)) sb.AppendLine($"| {c.Topic} | {c.Percent:0} |");
        return sb.ToString();
    }

    public static string DefaultImagination(IReadOnlyList<PerceptionScore> topics, IReadOnlyList<string> words)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "General";
        var concepts = NlpContent.TopWords(words, 5);
        var sb = new StringBuilder();
        sb.AppendLine("VISUAL IMAGINATION REPORT");
        sb.AppendLine("=========================");
        sb.AppendLine($"Imagined structures: forms shaped by {top}.");
        sb.AppendLine($"Imagined environments: spaces built around {string.Join(", ", concepts)}.");
        sb.AppendLine($"Imagined technologies: tools extending {top}.");
        sb.AppendLine("Imagined inventions: combinations of the recurring concepts above.");
        sb.AppendLine($"Imagined worlds: a setting where {top} is central.");
        return sb.ToString();
    }

    public static string DefaultMentalModels(IReadOnlyList<PerceptionScore> topics, IReadOnlyList<string> words)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "General";
        var sb = new StringBuilder();
        sb.AppendLine("# Mental Models");
        sb.AppendLine();
        foreach (var (model, gloss) in new[]
        {
            ("Reality", $"understood through a {top} lens"),
            ("Technology", "seen as tools that extend human capability"),
            ("Science", "a method for testing ideas against evidence"),
            ("Society", "a network of people, roles and exchanges"),
            ("Innovation", $"recombining {top} concepts into something new"),
            ("Learning", "iterative practice that compounds over time"),
        })
        {
            sb.AppendLine($"## {model}");
            sb.AppendLine($"- The user appears to model **{model}** as {gloss}.");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string DefaultSituational(IReadOnlyList<PerceptionScore> topics)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "the field";
        var sb = new StringBuilder();
        sb.AppendLine("SITUATIONAL INTERPRETATION");
        sb.AppendLine("==========================");
        sb.AppendLine($"A business problem: framed as an optimization grounded in {top}.");
        sb.AppendLine("A scientific challenge: broken into a testable hypothesis and method.");
        sb.AppendLine("An engineering project: decomposed into components and constraints.");
        sb.AppendLine("A social interaction: read for intent, roles and shared goals.");
        sb.AppendLine("A learning opportunity: turned into deliberate, spaced practice.");
        return sb.ToString();
    }

    public static string DefaultFutureVision(IReadOnlyList<PerceptionScore> topics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FUTURE VISION REPORT");
        sb.AppendLine("====================");
        sb.AppendLine($"Future interests: deepening {(topics.Count > 0 ? topics[0].Topic : "core interests")}.");
        sb.AppendLine($"Desired innovations: advances in {string.Join(", ", topics.Skip(1).Take(2).Select(t => t.Topic))}.");
        sb.AppendLine("Preferred technologies: tools that amplify the dominant interests.");
        sb.AppendLine("Long-term aspirations: mastery and original contribution in the leading field.");
        return sb.ToString();
    }

    public static string DefaultKnowledgeDiscovery(IReadOnlyList<PerceptionScore> topics, IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("KNOWLEDGE DISCOVERY REPORT");
        sb.AppendLine("==========================");
        sb.AppendLine($"Most important concepts: {string.Join(", ", concepts)}.");
        sb.AppendLine($"Hidden interests: {string.Join(", ", topics.Skip(2).Take(2).Select(t => t.Topic))}.");
        sb.AppendLine("Emerging ideas: combinations of the recurring concepts above.");
        sb.AppendLine($"Untapped learning opportunities: under-explored areas adjacent to {(topics.Count > 0 ? topics[0].Topic : "your field")}.");
        return sb.ToString();
    }

    public static string DefaultResearchMarkdown(
        IReadOnlyList<PerceptionScore> topics, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "General";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Perception Analysis Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"The user's perception is oriented toward {top}, with recurring focus on {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Perception Statistics");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}. Top categories: {string.Join(", ", topics.Take(3).Select(t => $"{t.Topic} {t.Percent:0}%"))}.");
        sb.AppendLine();
        sb.AppendLine("## Mental Model Analysis");
        sb.AppendLine($"Reality is modelled through a {top} lens; technology as capability-extending tools.");
        sb.AppendLine();
        sb.AppendLine("## Attention & Awareness");
        sb.AppendLine("Awareness and attention scores indicate how the user observes and sustains focus on the environment.");
        sb.AppendLine();
        sb.AppendLine("## Future Vision");
        sb.AppendLine($"Likely trajectory: deeper engagement with {top} and adjacent fields.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine($"Lean into {top}; broaden observation across new contexts; convert curiosity into deliberate practice.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<PerceptionScore> topics, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Topic(int i) => i < topics.Count ? $"{topics[i].Topic} ({topics[i].Percent:0}%)" : "—";
        var concepts = NlpContent.TopWords(words, 3);
        string top = topics.Count > 0 ? topics[0].Topic : "your field";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Awareness Analysis", new[] { "Situational awareness", "Observation quality", "Context recognition" }),
            new("Attention Analysis", new[] { "Stability & focus duration", "Selective attention", "Distraction susceptibility" }),
            new("Perception Profile", new[] { Topic(0), Topic(1), Topic(2) }),
            new("Mental Models", new[] { $"Reality via {top}", "Technology as tools", "Science as method" }),
            new("Visual Imagination", new[] { $"Imagined {top} structures", "Invented combinations" }),
            new("Future Vision", new[] { $"Deepen {top}", "Desired innovations" }),
            new("Artificial Comparison", new[] { "Human vs computer vs AI perception" }),
            new("Conclusions", new[] { "Perception reveals how you observe & interpret", "Track it to grow awareness" }),
        };
    }

    public static string MimeFor(string path)
    {
        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext switch { ".png" => "image/png", ".gif" => "image/gif", ".webp" => "image/webp", _ => "image/jpeg" };
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
