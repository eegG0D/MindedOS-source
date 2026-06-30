using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Turing Test content: the preview scorecard and fallbacks for the LM
/// artifacts (the artificial-thoughts and human-vs-AI-chat narratives, a report and a 10-slide deck).
/// Self-contained; reuses only <see cref="NlpContent"/>.
/// </summary>
public static class TuringContent
{
    private static List<string> Concepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "idea", "thought", "concept", "reason", "question", "answer" };
    }

    // ---- preview scorecard ----

    public static string Scorecard(IReadOnlyList<(string Score, double Value)> dashboard, string verdict)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TURING TEST DASHBOARD");
        sb.AppendLine("=====================");
        sb.AppendLine($"Verdict: {verdict}");
        foreach (var (name, value) in dashboard)
        {
            int filled = (int)System.Math.Round(value / 5.0);
            string bar = new string('█', System.Math.Clamp(filled, 0, 20)) + new string('░', System.Math.Clamp(20 - filled, 0, 20));
            sb.AppendLine($"{name,-18} {bar} {value:0}");
        }
        return sb.ToString();
    }

    public static string Verdict(double humanLikeness, double machineLikeness)
    {
        double diff = humanLikeness - machineLikeness;
        if (diff > 12) return "Appears HUMAN-like";
        if (diff < -12) return "Appears AI-like";
        return "MIXED / uniquely individual";
    }

    // ---- LM fallbacks: narratives ----

    public static string DefaultArtificialThoughts(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("ARTIFICIAL THOUGHTS");
        sb.AppendLine("===================");
        sb.AppendLine($"AI thought stream: a structured chain of reasoning about {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine("AI reasoning sample: premise → inference → conclusion, stated explicitly.");
        sb.AppendLine("AI explanation: each step is justified with general principles.");
        sb.AppendLine("AI conversation: balanced, polite, on-topic, low emotional variance.");
        sb.AppendLine("AI problem-solving example: decompose, evaluate options, recommend the optimal one.");
        return sb.ToString();
    }

    public static string DefaultHumanVsAiChat(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("HUMAN vs AI CHAT");
        sb.AppendLine("================");
        for (int i = 0; i < concepts.Count && i < 5; i++)
        {
            sb.AppendLine($"Human: {concepts[i]}… it makes me wonder about the bigger picture.");
            sb.AppendLine($"AI: Regarding {concepts[i]}, here is a structured analysis with three points.");
        }
        sb.AppendLine();
        sb.AppendLine("The human turns are associative and curious; the AI turns are structured and consistent.");
        return sb.ToString();
    }

    // ---- LM fallback: research report (.docx) ----

    public static string DefaultReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<string> words,
        double avgAtt, double avgMed, string dominantBand, string verdict)
    {
        var concepts = Concepts(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Turing Test Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A Turing-test comparison of EEG-derived thought against AI output. Verdict: {verdict}. Human-likeness {dashboard[0].Value:0}, machine-likeness {dashboard[1].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Statistical Analysis");
        sb.AppendLine($"Creativity {dashboard[2].Value:0}, logic {dashboard[3].Value:0}, curiosity {dashboard[4].Value:0}, authenticity {dashboard[5].Value:0}; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Comparison Charts");
        sb.AppendLine($"Across cognition, reasoning, creativity and knowledge, the human profile is more diverse and emotional while the AI profile is more structured and predictable. Recurring concepts: {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Human vs AI Findings");
        sb.AppendLine("The blind judge distinguishes the samples primarily by emotional variance and concept diversity.");
        sb.AppendLine();
        sb.AppendLine("## Conclusions");
        sb.AppendLine($"The EEG-derived thoughts read as {verdict.ToLowerInvariant()}; track this over sessions to see how human-likeness changes.");
        return sb.ToString();
    }

    // ---- LM fallback: 10-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<string> words,
        double avgAtt, double avgMed, string dominantBand, string verdict)
    {
        var concepts = Concepts(words, 3);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", $"Recurring: {string.Join(", ", concepts)}" }),
            new("Human Thought Profile", new[] { "Vocabulary & concept diversity", "Creativity, curiosity, abstraction" }),
            new("AI Thought Profile", new[] { "Structured & predictable", "Low emotional variance" }),
            new("Human-Likeness Scores", new[] { $"Human-likeness {dashboard[0].Value:0}", "Naturalness, authenticity, originality" }),
            new("Machine-Likeness Scores", new[] { $"Machine-likeness {dashboard[1].Value:0}", "Predictability, repetition, structure" }),
            new("Blind Judge Results", new[] { "Judge guesses without labels", "Distinguished by diversity & emotion" }),
            new("Creativity Comparison", new[] { $"Creativity {dashboard[2].Value:0}", "Originality, novelty, imagination" }),
            new("Reasoning Comparison", new[] { $"Logic {dashboard[3].Value:0}", "Problem solving, abstract, strategic" }),
            new("Conclusions", new[] { $"Verdict: {verdict}", "Track human-likeness over time" }),
        };
    }
}
