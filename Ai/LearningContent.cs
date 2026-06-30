using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe content for the Multimodal Learning program:
/// the report header, the fallback analysis Markdown, the curriculum / knowledge
/// graph sections, the 10-slide deck, and a Markdown section extractor used to
/// slice the LM analysis into curriculum.md / knowledge_graph.md.
/// </summary>
public static class LearningContent
{
    /// <summary>Deterministic report header: title + brain statistics + subject ranking tables.</summary>
    public static string ReportMarkdown(
        LearningProfile p, IReadOnlyList<SubjectScore> subjects,
        double avgAtt, double avgMed, string dominantBand, int seconds)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Multimodal Learning Report");
        sb.AppendLine();
        sb.AppendLine($"Session length: {seconds / 60.0:0.#} min · average attention {avgAtt:0}/100 · " +
                      $"average calm {avgMed:0}/100 · dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Brain Statistics");
        sb.AppendLine();
        sb.AppendLine("| Metric | Score |");
        sb.AppendLine("| --- | --- |");
        sb.AppendLine($"| Focus | {p.Focus:0} |");
        sb.AppendLine($"| Curiosity | {p.Curiosity:0} |");
        sb.AppendLine($"| Creativity | {p.Creativity:0} |");
        sb.AppendLine($"| Logic | {p.Logic:0} |");
        sb.AppendLine($"| Memory | {p.Memory:0} |");
        sb.AppendLine($"| Problem Solving | {p.ProblemSolving:0} |");
        sb.AppendLine($"| Flow State | {p.FlowState:0} |");
        sb.AppendLine($"| Learning Efficiency | {p.LearningEfficiency:0} |");
        sb.AppendLine();
        sb.AppendLine("## Interests");
        sb.AppendLine();
        sb.AppendLine("| Subject | Interest |");
        sb.AppendLine("| --- | --- |");
        foreach (var s in subjects.Take(10))
            sb.AppendLine($"| {s.Subject} | {s.Percent:0}% |");
        return sb.ToString();
    }

    /// <summary>Full fallback analysis Markdown (used when LM Studio is unavailable).</summary>
    public static string DefaultAnalysis(
        LearningProfile p, IReadOnlyList<SubjectScore> subjects,
        double avgAtt, double avgMed, string dominantBand)
    {
        string topSubject = subjects.Count > 0 ? subjects[0].Subject : "General";
        var sb = new StringBuilder();

        sb.AppendLine("## Learning Strengths");
        sb.AppendLine($"- Strongest signal: focus {p.Focus:0}, problem solving {p.ProblemSolving:0}, flow {p.FlowState:0}.");
        sb.AppendLine($"- A {dominantBand}-dominant brain pairs naturally with {topSubject.ToLowerInvariant()} material.");
        sb.AppendLine();
        sb.AppendLine("## Learning Weaknesses");
        sb.AppendLine($"- Lowest scores indicate where to invest: memory {p.Memory:0}, logic {p.Logic:0}, creativity {p.Creativity:0}.");
        sb.AppendLine();
        sb.AppendLine("## Preferred Learning Style");
        sb.AppendLine(avgMed >= avgAtt
            ? "- Reflective and exploratory — calm states favour reading, review and spaced practice."
            : "- Active and focused — high attention favours hands-on projects and timed drills.");
        sb.AppendLine();
        sb.AppendLine("## Knowledge Gaps");
        sb.AppendLine($"- Foundational depth in {topSubject} and adjacent fundamentals.");
        sb.AppendLine();
        sb.AppendLine("## Suggested Study Methods");
        sb.AppendLine("- Spaced repetition, project-based practice, and teach-back summaries.");
        sb.AppendLine();
        sb.AppendLine("## Personalized Learning Path");
        sb.AppendLine($"- Start with {topSubject} fundamentals, then build one applied project, then go deep.");
        sb.AppendLine();
        sb.AppendLine("## Subject Analysis");
        foreach (var s in subjects.Take(10))
            sb.AppendLine($"- {s.Subject}: {s.Percent:0}% interest.");
        sb.AppendLine();
        sb.Append(DefaultCurriculum(p, subjects));
        sb.AppendLine();
        sb.Append(DefaultKnowledgeGraph(subjects));
        sb.AppendLine();
        sb.AppendLine("## AI Mentor");
        sb.AppendLine($"- This week: study {topSubject} basics for 30 min/day and ship one small exercise.");
        sb.AppendLine("- Ask yourself: what did I learn, what confused me, what will I try next?");
        sb.AppendLine();
        sb.AppendLine("## Future Prediction");
        sb.AppendLine($"- Skill trajectory: growing strength in {topSubject}.");
        sb.AppendLine("- Possible paths: applied practitioner, researcher, or builder in that field.");
        return sb.ToString();
    }

    public static string DefaultCurriculum(LearningProfile p, IReadOnlyList<SubjectScore> subjects)
    {
        string s = subjects.Count > 0 ? subjects[0].Subject : "General";
        var sb = new StringBuilder();
        sb.AppendLine("## Curriculum");
        sb.AppendLine("### Beginner");
        sb.AppendLine($"- Objectives: grasp {s} fundamentals and core vocabulary.");
        sb.AppendLine($"- Project: a small hands-on {s.ToLowerInvariant()} exercise.");
        sb.AppendLine("- Books: an introductory text in the field.");
        sb.AppendLine("- Research topics: the field's central questions.");
        sb.AppendLine("### Intermediate");
        sb.AppendLine($"- Objectives: apply {s} concepts to real problems.");
        sb.AppendLine("- Project: a portfolio-quality build.");
        sb.AppendLine("- Books: a standard intermediate reference.");
        sb.AppendLine("- Research topics: current methods and trade-offs.");
        sb.AppendLine("### Advanced");
        sb.AppendLine($"- Objectives: master and extend {s}.");
        sb.AppendLine("- Project: an original contribution or deep-dive.");
        sb.AppendLine("- Books: advanced / primary literature.");
        sb.AppendLine("- Research topics: open problems and frontiers.");
        return sb.ToString();
    }

    public static string DefaultKnowledgeGraph(IReadOnlyList<SubjectScore> subjects)
    {
        var top = subjects.Take(4).Select(s => s.Subject).ToList();
        if (top.Count == 0) top.Add("General");
        var sb = new StringBuilder();
        sb.AppendLine("## Knowledge Graph");
        sb.AppendLine("Concepts, relationships, dependencies and learning pathways:");
        sb.AppendLine();
        for (int i = 0; i < top.Count; i++)
        {
            string next = i + 1 < top.Count ? top[i + 1] : "Mastery";
            sb.AppendLine($"- **{top[i]}** → depends on fundamentals → enables **{next}**");
        }
        sb.AppendLine();
        sb.AppendLine($"Pathway: {string.Join(" → ", top)} → Mastery");
        return sb.ToString();
    }

    /// <summary>Deterministic 10-slide deck (matches the LM deck's titles/order).</summary>
    public static IReadOnlyList<SlideContent> DefaultDeck(
        LearningProfile p, IReadOnlyList<SubjectScore> subjects,
        double avgAtt, double avgMed, string dominantBand)
    {
        string Subj(int i) => i < subjects.Count ? $"{subjects[i].Subject} ({subjects[i].Percent:0}%)" : "—";
        string topStyle = avgMed >= avgAtt ? "Reflective / exploratory" : "Active / focused";

        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Average attention {avgAtt:0}/100", $"Average calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Brain Statistics", new[] { $"Focus {p.Focus:0}", $"Logic {p.Logic:0}", $"Memory {p.Memory:0}", $"Flow {p.FlowState:0}" }),
            new("Learning Style", new[] { topStyle, $"Learning efficiency {p.LearningEfficiency:0}/100" }),
            new("Interests", new[] { Subj(0), Subj(1), Subj(2) }),
            new("Strengths", new[] { $"Focus {p.Focus:0}", $"Problem solving {p.ProblemSolving:0}", $"Creativity {p.Creativity:0}" }),
            new("Weaknesses", new[] { $"Memory {p.Memory:0}", $"Logic {p.Logic:0}", "Invest study time here" }),
            new("Subject Analysis", new[] { Subj(0), Subj(1), Subj(2), Subj(3) }),
            new("Curriculum", new[] { "Beginner: fundamentals", "Intermediate: applied project", "Advanced: original work" }),
            new("Recommendations", new[] { "Spaced repetition", "Project-based practice", "Teach-back summaries" }),
            new("Future Learning Goals", new[] { $"Deepen {(subjects.Count > 0 ? subjects[0].Subject : "your field")}", "Ship one project", "Explore an open problem" }),
        };
    }

    /// <summary>
    /// Return the Markdown from the `## &lt;headingText&gt;` line up to (excluding) the
    /// next `## ` heading, or null if the heading is absent. Sub-headings (###) are kept.
    /// </summary>
    public static string? ExtractSection(string markdown, string headingText)
    {
        if (string.IsNullOrEmpty(markdown)) return null;
        var lines = markdown.Replace("\r\n", "\n").Split('\n');

        bool IsH2(string line) => line.TrimStart().StartsWith("## ") && !line.TrimStart().StartsWith("### ");

        int start = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!IsH2(lines[i])) continue;
            var title = lines[i].TrimStart().TrimStart('#', ' ');
            if (title.StartsWith(headingText, StringComparison.OrdinalIgnoreCase)) { start = i; break; }
        }
        if (start < 0) return null;

        int end = lines.Length;
        for (int i = start + 1; i < lines.Length; i++)
            if (IsH2(lines[i])) { end = i; break; }

        return string.Join("\n", lines[start..end]).Trim();
    }
}
