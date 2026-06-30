using System.Text;
using System.Text.Json;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Self-Awareness content: recurring thoughts, curiosity map, the 100
/// self-questions, the knowledge graph, the long-term self model JSON, the dashboard, and
/// fallbacks for the LM artifacts (six narratives, report, 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class SelfAwarenessContent
{
    public static string RecurringThoughtsCsv(IReadOnlyList<string> words)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }
        var sb = new StringBuilder("thought,count,theme\n");
        foreach (var (word, count) in freq.OrderByDescending(kv => kv.Value).Take(15))
            sb.AppendLine($"{word},{count},{(count >= 3 ? "recurring" : "occasional")}");
        if (freq.Count == 0) sb.AppendLine("(none),0,none");
        return sb.ToString();
    }

    public static string CuriosityMapCsv(IReadOnlyList<CuriosityDomainScore> domains)
    {
        var sb = new StringBuilder("domain,percent\n");
        foreach (var d in domains) sb.AppendLine($"{d.Domain},{d.Percent:0.0}");
        if (domains.Count == 0) sb.AppendLine("General,100.0");
        return sb.ToString();
    }

    public static string SelfQuestionsText(IReadOnlyList<string> words, IReadOnlyList<CuriosityDomainScore> domains)
    {
        var concepts = NlpContent.TopWords(words, 12);
        if (concepts.Count == 0) concepts = new List<string> { "your focus", "learning", "growth" };
        var doms = domains.Select(d => d.Domain).ToList();
        if (doms.Count == 0) doms.Add("Research");
        string[] templates =
        {
            "What should I learn next about {0}?",
            "What skills related to {0} appear underdeveloped?",
            "Why does {0} recur in my thinking?",
            "What project could I build around {0}?",
            "How does {0} connect to {1}?",
            "What would deepen my understanding of {0} in {2}?",
            "What is my next step after {0}?",
            "How could I apply {0} to {2}?",
            "What am I avoiding about {0}?",
            "What question about {0} have I not asked yet?",
        };
        var sb = new StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            string a = concepts[i % concepts.Count];
            string b = concepts[(i + 1) % concepts.Count];
            string d = doms[i % doms.Count];
            sb.AppendLine($"{i + 1}. {string.Format(templates[i % templates.Length], a, b, d)}");
        }
        return sb.ToString();
    }

    public static string PersonalKnowledgeGraphMd(IReadOnlyList<string> words, IReadOnlyList<CuriosityDomainScore> domains)
    {
        var concepts = NlpContent.TopWords(words, 5);
        if (concepts.Count == 0) concepts = new List<string> { "core" };
        var doms = domains.Take(4).Select(d => d.Domain).ToList();
        if (doms.Count == 0) doms.Add("Research");
        var sb = new StringBuilder();
        sb.AppendLine("# Personal Knowledge Graph");
        sb.AppendLine();
        sb.AppendLine("Connections between interests, goals, topics, concepts and skills:");
        sb.AppendLine();
        for (int i = 0; i < doms.Count; i++)
        {
            string next = i + 1 < doms.Count ? doms[i + 1] : "Synthesis";
            sb.AppendLine($"- **{doms[i]}** → relates to → **{next}**");
        }
        sb.AppendLine();
        sb.AppendLine($"Core concepts: {string.Join(" · ", concepts)}");
        sb.AppendLine();
        sb.AppendLine($"Pathway: {string.Join(" → ", doms)} → Synthesis");
        return sb.ToString();
    }

    public static string LongTermSelfModelJson(
        IReadOnlyList<string> words, IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<CuriosityDomainScore> domains)
    {
        var concepts = NlpContent.TopWords(words, 8);
        var model = new
        {
            interests = domains.Take(5).Select(d => new { domain = d.Domain, percent = System.Math.Round(d.Percent, 1) }).ToArray(),
            skills = dashboard.Select(s => new { name = s.Score, score = System.Math.Round(s.Value, 1) }).ToArray(),
            learning_progression = concepts.Take(5).ToArray(),
            research_interests = domains.Skip(5).Take(3).Select(d => d.Domain).ToArray(),
            project_history = concepts.Take(3).Select(c => $"Explore {c}").ToArray(),
        };
        return JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<CuriosityDomainScore> domains)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Self-Awareness Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Metric | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Curiosity Map");
        sb.AppendLine("| Domain | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var d in domains.Take(10)) sb.AppendLine($"| {d.Domain} | {d.Percent:0} |");
        return sb.ToString();
    }

    // ---- LM fallbacks ----

    public static string DefaultSelfProfile(IReadOnlyList<string> words, IReadOnlyList<CuriosityDomainScore> domains)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var concepts = NlpContent.TopWords(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("# Self Profile");
        sb.AppendLine();
        sb.AppendLine($"## Current Interests\nRecurring concepts: {string.Join(", ", concepts.DefaultIfEmpty("learning"))}; leading domain {top}.");
        sb.AppendLine($"## Cognitive Style\nReflective and analytical, drawn to {top}.");
        sb.AppendLine("## Learning Style\nLearns by building and connecting ideas.");
        sb.AppendLine($"## Preferred Subjects\n{top} and adjacent domains.");
        sb.AppendLine("## Problem-Solving Approach\nExplore broadly, then focus and iterate.");
        return sb.ToString();
    }

    public static string DefaultIdentity(IReadOnlyList<string> words, IReadOnlyList<CuriosityDomainScore> domains)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("# Identity Profile");
        sb.AppendLine();
        sb.AppendLine($"- **Interests:** {string.Join(", ", domains.Take(3).Select(d => d.Domain))}.");
        sb.AppendLine("- **Motivations:** mastery, curiosity and building.");
        sb.AppendLine($"- **Values (inferred):** learning and progress in {top}.");
        sb.AppendLine($"- **Preferred domains:** {top}.");
        sb.AppendLine("- **Cognitive tendencies:** reflective, exploratory, systematic.");
        return sb.ToString();
    }

    public static string DefaultInternalDialogue(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 4);
        string top = concepts.Count > 0 ? concepts[0] : "your focus";
        var sb = new StringBuilder();
        sb.AppendLine("INTERNAL DIALOGUE");
        sb.AppendLine("=================");
        sb.AppendLine($"Question to explore: what is the next layer of {top}?");
        sb.AppendLine($"Topic worth researching: how {string.Join(" and ", concepts.Take(2).DefaultIfEmpty("ideas"))} connect.");
        sb.AppendLine($"New idea: combine {top} with an unrelated field.");
        sb.AppendLine("Alternative viewpoint: consider the opposite assumption.");
        return sb.ToString();
    }

    public static string DefaultProjects(IReadOnlyList<string> words, IReadOnlyList<CuriosityDomainScore> domains)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("# Project Recommendations");
        sb.AppendLine();
        sb.AppendLine($"- **Technical:** build a small tool in {top}.");
        sb.AppendLine($"- **Research:** investigate an open question in {top}.");
        sb.AppendLine("- **Creative:** make something expressive from the recurring concepts.");
        sb.AppendLine("- **Educational:** teach what you just learned.");
        sb.AppendLine("- **Engineering:** prototype a device or system.");
        return sb.ToString();
    }

    public static string DefaultReflectionJournal(IReadOnlyList<string> words, IReadOnlyList<CuriosityDomainScore> domains)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var concepts = NlpContent.TopWords(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("REFLECTION JOURNAL");
        sb.AppendLine("==================");
        sb.AppendLine($"Current themes: {string.Join(", ", concepts.DefaultIfEmpty("learning"))}.");
        sb.AppendLine($"Recent interests: {top}.");
        sb.AppendLine($"Potential future directions: go deeper into {top}.");
        sb.AppendLine("Areas of growth: consistency and finishing projects.");
        return sb.ToString();
    }

    public static string DefaultMentor(IReadOnlyList<string> words, IReadOnlyList<CuriosityDomainScore> domains)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("AI MENTOR");
        sb.AppendLine("=========");
        sb.AppendLine($"Suggested learning path: foundations → applied projects in {top}.");
        sb.AppendLine($"Suggested project: a focused build in {top}.");
        sb.AppendLine("Development tracking: revisit this reflection weekly.");
        sb.AppendLine("Next reflection: note what changed since today.");
        return sb.ToString();
    }

    public static string DefaultReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<CuriosityDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = domains.Count > 0 ? domains[0].Domain : "General";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Personal Development Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"An AI-assisted reflection on a 3-minute EEG. Leading curiosity domain: {top}; recurring concepts {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Strength Analysis");
        sb.AppendLine($"Focus {dashboard[0].Value:0}, creativity {dashboard[3].Value:0}, learning {dashboard[5].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Growth Analysis");
        sb.AppendLine($"Consistency {dashboard[4].Value:0}; finishing projects and steady practice are the main opportunities.");
        sb.AppendLine();
        sb.AppendLine("## Goal Analysis");
        sb.AppendLine($"Goals span technical, educational and research directions anchored in {top}.");
        sb.AppendLine();
        sb.AppendLine("## Historical Trends");
        sb.AppendLine("Across sessions, recurring interests stabilize while new ones emerge.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine($"Pick one project in {top}, schedule consistent practice, and reflect weekly.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<CuriosityDomainScore> domains,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Domain(int i) => i < domains.Count ? $"{domains[i].Domain} ({domains[i].Percent:0}%)" : "—";
        var concepts = NlpContent.TopWords(words, 3);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Cognitive Metrics", new[] { $"Focus {dashboard[0].Value:0}", $"Reflection {dashboard[1].Value:0}", $"Curiosity {dashboard[2].Value:0}" }),
            new("Interests", new[] { Domain(0), Domain(1), Domain(2) }),
            new("Strengths", new[] { $"Creativity {dashboard[3].Value:0}", $"Learning {dashboard[5].Value:0}", $"Innovation {dashboard[6].Value:0}" }),
            new("Growth Opportunities", new[] { $"Consistency {dashboard[4].Value:0}", "Finish projects", "Steady practice" }),
            new("Goals", new[] { "Technical & educational", "Research & creative" }),
            new("Historical Trends", new[] { "Stable vs new interests", "Emerging opportunities" }),
            new("Future Directions", new[] { $"Go deeper into {(domains.Count > 0 ? domains[0].Domain : "your field")}", "Build & reflect" }),
            new("Conclusions", new[] { "Self-reflection from EEG", "Track growth over time" }),
        };
    }
}
