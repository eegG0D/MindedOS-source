using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Reasoning content: the hypotheses / artificial-comparison /
/// subject-scores CSVs, the dashboard, and fallbacks for the LM artifacts (four narratives,
/// research paper, 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class ReasoningContent
{
    public static string HypothesesCsv(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 5);
        if (concepts.Count == 0) concepts = new List<string> { "the focus" };
        var sb = new StringBuilder("hypothesis,supporting_concepts,confidence,applications\n");
        for (int i = 0; i < concepts.Count; i++)
        {
            string c = concepts[i];
            string support = string.Join(" ", concepts.Where((_, j) => j != i).Take(2));
            int confidence = System.Math.Clamp(70 - i * 8, 30, 90);
            sb.AppendLine($"\"{c} drives the current reasoning\",\"{support}\",{confidence},\"research on {c}\"");
        }
        return sb.ToString();
    }

    public static string ArtificialComparisonCsv(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        // human = the user's dashboard scores; ai = fixed AI profile; hybrid = blend.
        double[] ai = { 92, 85, 88, 90, 90, 88 };
        var sb = new StringBuilder("aspect,human,ai,hybrid\n");
        for (int i = 0; i < dashboard.Count; i++)
        {
            double h = dashboard[i].Value;
            double a = ai[i % ai.Length];
            sb.AppendLine($"{dashboard[i].Score},{h:0},{a:0},{(h + a) / 2:0}");
        }
        return sb.ToString();
    }

    public static string SubjectScoresCsv(IReadOnlyList<ReasoningSubjectScore> subjects)
    {
        var sb = new StringBuilder("subject,score\n");
        foreach (var s in subjects) sb.AppendLine($"{s.Subject},{s.Percent:0.0}");
        if (subjects.Count == 0) sb.AppendLine("General,100.0");
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> scores, IReadOnlyList<ReasoningSubjectScore> subjects)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Reasoning Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Score | Value |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in scores) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Subject Reasoning");
        sb.AppendLine("| Subject | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var s in subjects.Take(9)) sb.AppendLine($"| {s.Subject} | {s.Percent:0} |");
        return sb.ToString();
    }

    public static string DefaultArgumentAnalysis(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 5);
        var sb = new StringBuilder();
        sb.AppendLine("ARGUMENT ANALYSIS");
        sb.AppendLine("=================");
        sb.AppendLine($"Premises: grounded in {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine("Supporting evidence: the recurring decoded concepts.");
        sb.AppendLine("Conclusions: follow from the dominant theme.");
        sb.AppendLine("Counterarguments: alternative readings of the same concepts.");
        sb.AppendLine("Logical consistency: coherent within the present focus.");
        return sb.ToString();
    }

    public static string DefaultDecisionPathways(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 4);
        string top = concepts.Count > 0 ? concepts[0] : "the focus";
        var sb = new StringBuilder();
        sb.AppendLine("# Decision Pathways");
        sb.AppendLine();
        sb.AppendLine("## Inputs");
        sb.AppendLine($"- The decoded concepts: {string.Join(", ", concepts)}.");
        sb.AppendLine("## Influencing Factors");
        sb.AppendLine("- Attention, calm and the dominant band.");
        sb.AppendLine("## Alternatives Considered");
        sb.AppendLine($"- Different framings of {top}.");
        sb.AppendLine("## Final Conclusions");
        sb.AppendLine($"- Commit to the path most aligned with {top}.");
        return sb.ToString();
    }

    public static string DefaultCriticalThinking(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CRITICAL THINKING REPORT");
        sb.AppendLine("========================");
        sb.AppendLine("Skepticism: question the first interpretation.");
        sb.AppendLine("Fact evaluation: weigh the recurring concepts against evidence.");
        sb.AppendLine("Bias detection: watch for over-focusing on the dominant theme.");
        sb.AppendLine("Alternative perspectives: consider adjacent subjects.");
        sb.AppendLine("Risk assessment: note where conclusions are weakly supported.");
        return sb.ToString();
    }

    public static string DefaultFutureForecast(IReadOnlyList<ReasoningSubjectScore> subjects)
    {
        string top = subjects.Count > 0 ? subjects[0].Subject : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("FUTURE REASONING FORECAST");
        sb.AppendLine("=========================");
        sb.AppendLine($"Research: strong potential anchored in {top}.");
        sb.AppendLine("Engineering: growing system and design thinking.");
        sb.AppendLine("Science: hypothesis-driven reasoning developing.");
        sb.AppendLine("Leadership: strategic decision quality improving.");
        sb.AppendLine("Entrepreneurship: opportunity recognition emerging.");
        sb.AppendLine("Innovation: novel associations across subjects.");
        return sb.ToString();
    }

    public static string DefaultResearchMarkdown(
        IReadOnlyList<ReasoningSubjectScore> subjects, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = subjects.Count > 0 ? subjects[0].Subject : "General";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Reasoning Analysis Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"The reasoning centers on {top}, with recurring concepts {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Reasoning Statistics");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}. Top subjects: {string.Join(", ", subjects.Take(3).Select(s => $"{s.Subject} {s.Percent:0}%"))}.");
        sb.AppendLine();
        sb.AppendLine("## Logic Analysis");
        sb.AppendLine("Deductive, inductive and analytical reasoning are driven by fast-band activity.");
        sb.AppendLine();
        sb.AppendLine("## Decision Analysis");
        sb.AppendLine("Decision quality balances attention, calm and analytical depth.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine($"Practice structured argument and hypothesis testing in {top}.");
        sb.AppendLine();
        sb.AppendLine("## Future Development Areas");
        sb.AppendLine("Strengthen critical thinking and multi-step reasoning chains over time.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<ReasoningSubjectScore> subjects, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Subject(int i) => i < subjects.Count ? $"{subjects[i].Subject} ({subjects[i].Percent:0}%)" : "—";
        var concepts = NlpContent.TopWords(words, 3);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Logic Assessment", new[] { "Deductive & inductive", "Analytical & sequential" }),
            new("Problem Solving", new[] { "Solution generation", "Strategic & optimization" }),
            new("Critical Thinking", new[] { "Skepticism & bias detection", "Evidence evaluation" }),
            new("Decision Pathways", new[] { "Inputs & factors", "Alternatives & conclusions" }),
            new("Scientific Reasoning", new[] { "Hypothesis formation", "Evidence evaluation" }),
            new("Innovation Analysis", new[] { "Novel ideas", "Inventive concepts" }),
            new("Future Forecast", new[] { Subject(0), Subject(1), Subject(2) }),
            new("Conclusions", new[] { "How you reason & decide", "Where to grow next" }),
        };
    }
}
