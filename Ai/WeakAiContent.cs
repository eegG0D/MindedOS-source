using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Weak AI content: detected domains, task recommendations, knowledge
/// extraction, the preview scorecard, and fallbacks for the LM artifacts (the assistant bundle, the
/// chat/decision/future/expert bundle, a report and a 10-slide deck). A narrow, task-oriented AI —
/// NOT general intelligence. Self-contained; reuses only <see cref="NlpContent"/>.
/// </summary>
public static class WeakAiContent
{
    private static readonly string[] RecommendationCategories =
        { "Project", "Learning Activity", "Research Topic", "Design Idea", "Programming Task" };

    private static List<string> Concepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "concept", "idea", "system", "design", "study", "model" };
    }

    // ---- domain detection ----

    public static string DetectedDomainsCsv(IReadOnlyList<WeakAiDomainScore> domains)
    {
        var sb = new StringBuilder("domain,count,percent\n");
        foreach (var d in domains) sb.AppendLine($"{d.Domain},{d.Count},{d.Percent:0.0}");
        if (domains.Count == 0) sb.AppendLine("Research,0,100.0");
        return sb.ToString();
    }

    // ---- task recommendations ----

    public static string TaskRecommendationsCsv(IReadOnlyList<string> words, IReadOnlyList<WeakAiDomainScore> domains)
    {
        var concepts = Concepts(words, 10);
        var sb = new StringBuilder("recommendation,category,relevance\n");
        for (int i = 0; i < concepts.Count; i++)
        {
            string category = RecommendationCategories[i % RecommendationCategories.Length];
            string domain = i < domains.Count ? domains[i].Domain : "General";
            double relevance = Math.Clamp(95 - i * 6, 30, 95);
            sb.AppendLine($"{category}: explore '{concepts[i]}' in {domain},{category},{relevance:0.0}");
        }
        return sb.ToString();
    }

    public static int TaskCount(IReadOnlyList<string> words) => Concepts(words, 10).Count;

    // ---- knowledge extraction ----

    public static string KnowledgeExtractionCsv(IReadOnlyList<string> words, WeakAiDomains domainObj)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var x = w.Trim().ToLowerInvariant();
            if (x.Length == 0 || x == "—") continue;
            freq[x] = freq.TryGetValue(x, out var c) ? c + 1 : 1;
        }
        var sb = new StringBuilder("concept,frequency,theme,expertise\n");
        foreach (var (word, count) in freq.OrderByDescending(kv => kv.Value).Take(12))
        {
            string theme = domainObj.DomainOf(word);
            string expertise = count >= 3 ? "strong" : count == 2 ? "moderate" : "emerging";
            sb.AppendLine($"{word},{count},{theme},{expertise}");
        }
        if (freq.Count == 0) sb.AppendLine("concept,1,General,emerging");
        return sb.ToString();
    }

    // ---- preview scorecard ----

    public static string Scorecard(IReadOnlyList<(string Score, double Value)> dashboard, string topDomain, string cognitive, int recCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WEAK AI DASHBOARD (narrow, task-oriented)");
        sb.AppendLine("=========================================");
        sb.AppendLine($"Top domain: {topDomain}   ·   Dominant cognition: {cognitive}   ·   Recommendations: {recCount}");
        foreach (var (name, value) in dashboard)
        {
            int filled = (int)Math.Round(value / 5.0);
            string bar = new string('█', Math.Clamp(filled, 0, 20)) + new string('░', Math.Clamp(20 - filled, 0, 20));
            sb.AppendLine($"{name,-22} {bar} {value:0}");
        }
        return sb.ToString();
    }

    // ---- LM fallbacks: specialized assistant bundle ----

    public static string DefaultResearchAssistant(IReadOnlyList<WeakAiDomainScore> domains, IReadOnlyList<string> words)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var c = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("RESEARCH ASSISTANT REPORT");
        sb.AppendLine("=========================");
        sb.AppendLine($"Research ideas: study {c[0]} within {top}.");
        sb.AppendLine("Study plans: a focused 4-week plan with weekly milestones.");
        sb.AppendLine("Literature review suggestions: survey recent work on the recurring concepts.");
        sb.AppendLine("Future investigations: extend the strongest idea into a small experiment.");
        return sb.ToString();
    }

    public static string DefaultEngineeringAssistant(IReadOnlyList<WeakAiDomainScore> domains, IReadOnlyList<string> words)
    {
        var c = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("ENGINEERING ASSISTANT REPORT");
        sb.AppendLine("============================");
        sb.AppendLine($"Engineering concepts: a system built around {c[0]}.");
        sb.AppendLine("System designs: modular components with clear interfaces.");
        sb.AppendLine("Project suggestions: prototype the core, then iterate.");
        sb.AppendLine("Technical recommendations: measure, test, and optimize the bottleneck.");
        return sb.ToString();
    }

    public static string DefaultProgrammingAssistant(IReadOnlyList<string> words)
    {
        var c = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("PROGRAMMING ASSISTANT REPORT");
        sb.AppendLine("============================");
        sb.AppendLine($"Software ideas: a tool that automates work around {c[0]}.");
        sb.AppendLine("Algorithms: a clear, well-tested approach for the core task.");
        sb.AppendLine("Application concepts: a focused app for the recurring concepts.");
        sb.AppendLine("Automation opportunities: script the repetitive steps.");
        return sb.ToString();
    }

    public static string DefaultLearningAssistant(IReadOnlyList<WeakAiDomainScore> domains, IReadOnlyList<string> words)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("LEARNING ASSISTANT REPORT");
        sb.AppendLine("=========================");
        sb.AppendLine($"Learning plans: a structured path through {top} fundamentals.");
        sb.AppendLine("Skill recommendations: build the skills your concepts point to.");
        sb.AppendLine("Educational pathways: courses, projects, then teaching others.");
        sb.AppendLine("Knowledge gaps: fill the adjacent topics you touched only lightly.");
        return sb.ToString();
    }

    // ---- LM fallbacks: chat / decision / future / experts bundle ----

    public static string DefaultChatLog(IReadOnlyList<string> words)
    {
        var c = Concepts(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("WEAK AI CHAT LOG");
        sb.AppendLine("================");
        foreach (var concept in c)
        {
            sb.AppendLine($"You: What should I do about {concept}?");
            sb.AppendLine($"Weak AI: For {concept}, here is one concrete, task-focused next step.");
        }
        return sb.ToString();
    }

    public static string DefaultDecisionSupport(IReadOnlyList<string> words)
    {
        var c = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("DECISION SUPPORT REPORT");
        sb.AppendLine("=======================");
        sb.AppendLine($"Decision: focus on {c[0]} now vs. explore {(c.Count > 1 ? c[1] : "alternatives")}.");
        sb.AppendLine("Pros: focusing accelerates mastery and delivers a result sooner.");
        sb.AppendLine("Cons: it narrows exposure to adjacent ideas.");
        sb.AppendLine("Priority ranking: 1) focus, 2) ship, 3) explore.");
        sb.AppendLine("Suggested decision: focus now, schedule exploration next.");
        return sb.ToString();
    }

    public static string DefaultFuturePredictions(IReadOnlyList<WeakAiDomainScore> domains)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("FUTURE TASK PREDICTIONS");
        sb.AppendLine("=======================");
        sb.AppendLine($"Future interests: deeper engagement with {top}.");
        sb.AppendLine("Likely projects: a concrete build in the leading domain.");
        sb.AppendLine("Potential learning goals: master the recurring concepts.");
        sb.AppendLine("Emerging expertise areas: adjacent domains begin to grow.");
        return sb.ToString();
    }

    public static string DefaultExpertProfiles(IReadOnlyList<WeakAiDomainScore> domains, IReadOnlyList<string> words)
    {
        var c = Concepts(words, 2);
        string top = domains.Count > 0 ? domains[0].Domain : "the focus";
        var sb = new StringBuilder();
        sb.AppendLine("# Artificial Expert Profiles");
        sb.AppendLine();
        foreach (var expert in new[] { "Engineer", "Scientist", "Researcher", "Architect", "Programmer", "Educator" })
            sb.AppendLine($"- **{expert}:** uses your EEG context ({c[0]}) to advise on {top} from a {expert.ToLowerInvariant()} viewpoint.");
        return sb.ToString();
    }

    // ---- LM fallback: domain knowledge report (.docx) ----

    public static string DefaultReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<WeakAiDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand, string cognitive)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "General";
        var c = Concepts(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Domain Knowledge Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A narrow, task-oriented Weak AI analysis from a 3-minute EEG. Leading domain: {top}; dominant cognition: {cognitive}; domain specialization {dashboard[0].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Domain Knowledge");
        sb.AppendLine($"Concepts cluster in {string.Join(", ", domains.Take(3).Select(d => d.Domain))}; recurring ideas {string.Join(", ", c.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Cognitive Profile");
        sb.AppendLine($"Cognitive strength {dashboard[1].Value:0}; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine($"Prioritize the highest-relevance tasks in {top}; productivity {dashboard[2].Value:0}, task focus {dashboard[4].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Conclusions");
        sb.AppendLine("As a specialized assistant, the system delivers focused recommendations rather than general reasoning.");
        return sb.ToString();
    }

    // ---- LM fallback: 10-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<WeakAiDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand, string cognitive)
    {
        var c = Concepts(words, 3);
        string Dom(int i) => i < domains.Count ? $"{domains[i].Domain} ({domains[i].Percent:0}%)" : "—";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", $"Recurring: {string.Join(", ", c)}" }),
            new("Domain Detection", new[] { Dom(0), Dom(1), Dom(2) }),
            new("Cognitive Classification", new[] { $"Dominant: {cognitive}", $"Cognitive strength {dashboard[1].Value:0}" }),
            new("Knowledge Extraction", new[] { "Important concepts & themes", $"Knowledge density {dashboard[3].Value:0}" }),
            new("Task Recommendations", new[] { "Projects, learning, research", "Design & programming tasks" }),
            new("Decision Support", new[] { "Pros & cons, priorities", "Suggested decision" }),
            new("Productivity Analysis", new[] { $"Productivity {dashboard[2].Value:0}", $"Task focus {dashboard[4].Value:0}" }),
            new("Specialized AI Outputs", new[] { "Research, engineering", "Programming, learning assistants" }),
            new("Conclusions", new[] { "Narrow, task-oriented AI", $"Benchmark {dashboard[5].Value:0}" }),
        };
    }
}
