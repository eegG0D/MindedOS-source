using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Reactive Machine content: the decisions / opportunity /
/// stimulus-response / comparison / multi-input CSVs, the dashboard, and fallbacks for
/// the LM artifacts (eight narratives, research paper, 10-slide deck). Reuses
/// <see cref="NlpContent"/>. Present-moment only — no history.
/// </summary>
public static class ReactiveContent
{
    private static readonly string[] ReactiveConcepts =
    {
        "build", "learn", "solve", "explore", "create", "research", "design", "improve",
    };

    private static double StateScore(IReadOnlyList<(string State, double Value)> states, string name)
    {
        foreach (var (state, value) in states) if (state == name) return value;
        return 50;
    }

    public static string InstantDecisionsCsv(IReadOnlyList<(string State, double Value)> states, IReadOnlyList<string> words)
    {
        (string Decision, double Score)[] rows =
        {
            ("Continue task", StateScore(states, "Focused")),
            ("Investigate topic", StateScore(states, "Curious")),
            ("Explore idea", StateScore(states, "Exploratory")),
            ("Learn concept", StateScore(states, "Learning")),
            ("Solve problem", StateScore(states, "Problem Solving")),
            ("Research subject", StateScore(states, "Analytical")),
        };
        double max = rows.Max(r => r.Score);
        bool flagged = false;
        var sb = new StringBuilder("decision,score,recommended\n");
        foreach (var (decision, score) in rows)
        {
            bool rec = !flagged && score >= max;
            if (rec) flagged = true;
            sb.AppendLine($"{decision},{score:0.0},{(rec ? "yes" : "no")}");
        }
        return sb.ToString();
    }

    public static string OpportunityDetectionCsv(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 5);
        string Concept(int i) => i < concepts.Count ? concepts[i] : "your focus";
        var sb = new StringBuilder("type,opportunity,score\n");
        sb.AppendLine($"Learning,Study {Concept(0)},80");
        sb.AppendLine($"Research,Investigate {Concept(1)},75");
        sb.AppendLine($"Technical,Prototype around {Concept(2)},70");
        sb.AppendLine($"Engineering,Build with {Concept(3)},68");
        sb.AppendLine($"Creative,Reimagine {Concept(4)},72");
        return sb.ToString();
    }

    public static string StimulusResponseCsv(IReadOnlyList<string> words, string dominantState)
    {
        var concepts = NlpContent.TopWords(words, 4);
        var sb = new StringBuilder("input,reaction,action,focus\n");
        if (concepts.Count == 0) concepts = new List<string> { "thought" };
        foreach (var w in concepts)
            sb.AppendLine($"{w} ({dominantState}),engage,explore {w},sustain {w}");
        return sb.ToString();
    }

    public static string HumanVsReactiveCsv(IReadOnlyList<string> words)
    {
        var user = new HashSet<string>(NlpContent.TopWords(words, 12), StringComparer.OrdinalIgnoreCase);
        int overlap = ReactiveConcepts.Count(c => user.Contains(c));
        double conceptOverlap = ReactiveConcepts.Length > 0 ? 100.0 * overlap / ReactiveConcepts.Length : 0;
        var sb = new StringBuilder("aspect,human,reactive\n");
        sb.AppendLine($"Similarity,{conceptOverlap:0},{conceptOverlap:0}");
        sb.AppendLine("Response Speed,60,100");
        sb.AppendLine($"Concept Overlap,{conceptOverlap:0},{conceptOverlap:0}");
        sb.AppendLine($"Innovation Overlap,{conceptOverlap * 0.8:0},{conceptOverlap * 0.8:0}");
        return sb.ToString();
    }

    public static string MultiInputReactionsCsv(int wordCount)
    {
        var sb = new StringBuilder("input,status,reaction\n");
        sb.AppendLine($"EEG,reacted,reacted to {wordCount} decoded words");
        sb.AppendLine("Text,not provided,(no text input this session)");
        sb.AppendLine("CSV,not provided,(no csv input this session)");
        sb.AppendLine("Image,not provided,(no image input this session)");
        sb.AppendLine("Document,not provided,(no document input this session)");
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> scores, IReadOnlyList<(string State, double Value)> states)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Reactive Machine Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Score | Value |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in scores) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Current States");
        sb.AppendLine("| State | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (state, value) in states.OrderByDescending(s => s.Value).Take(5)) sb.AppendLine($"| {state} | {value:0} |");
        return sb.ToString();
    }

    private static string DominantState(IReadOnlyList<(string State, double Value)> states) =>
        states.Count > 0 ? states.OrderByDescending(s => s.Value).First().State : "Focused";

    public static string DefaultReactiveAnalysis(IReadOnlyList<(string State, double Value)> states, IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 5);
        var sb = new StringBuilder();
        sb.AppendLine("REACTIVE ANALYSIS");
        sb.AppendLine("=================");
        sb.AppendLine($"Present state: {DominantState(states)}.");
        sb.AppendLine($"Immediate focus: {string.Join(", ", concepts)}.");
        sb.AppendLine("Reaction: respond to the current input only; no memory of past sessions.");
        return sb.ToString();
    }

    public static string DefaultSituationResponses(IReadOnlyList<(string State, double Value)> states)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SITUATION RESPONSES");
        sb.AppendLine("===================");
        sb.AppendLine("Technical challenges: tackle directly with the current focus.");
        sb.AppendLine("Engineering scenarios: decompose and prototype now.");
        sb.AppendLine("Learning situations: dive into the most active concept.");
        sb.AppendLine("Research questions: form one immediate question to pursue.");
        sb.AppendLine("Innovation opportunities: sketch one idea from the present state.");
        return sb.ToString();
    }

    public static string DefaultProblemSolver(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("PROBLEM SOLVER REPORT");
        sb.AppendLine("=====================");
        sb.AppendLine($"Problems of interest: {string.Join(", ", concepts)}.");
        sb.AppendLine("Areas of concern: unclear scope on the current focus.");
        sb.AppendLine("Topics requiring attention: the most recurring concept above.");
        return sb.ToString();
    }

    public static string DefaultResearchSuggestions(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string top = concepts.Count > 0 ? concepts[0] : "your focus";
        var sb = new StringBuilder();
        sb.AppendLine("RESEARCH SUGGESTIONS");
        sb.AppendLine("====================");
        sb.AppendLine($"Papers to read: foundational work on {top}.");
        sb.AppendLine($"Subjects to study: {string.Join(", ", concepts)}.");
        sb.AppendLine($"Experiments: a quick test of an idea about {top}.");
        sb.AppendLine($"Projects: a small build exploring {top}.");
        return sb.ToString();
    }

    public static string DefaultInnovationIdeas(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string top = concepts.Count > 0 ? concepts[0] : "the current focus";
        var sb = new StringBuilder();
        sb.AppendLine("INNOVATION IDEAS");
        sb.AppendLine("================");
        sb.AppendLine($"Invention: a tool built around {top}.");
        sb.AppendLine($"Software concept: an app applying {top}.");
        sb.AppendLine($"Engineering design: a device using {top}.");
        sb.AppendLine($"Research proposal: a study of {top}.");
        sb.AppendLine($"AI system: an agent reasoning about {top}.");
        return sb.ToString();
    }

    public static string DefaultArchitectureConcepts(IReadOnlyList<string> words)
    {
        string top = NlpContent.TopWords(words, 1).FirstOrDefault() ?? "the focus";
        var sb = new StringBuilder();
        sb.AppendLine("ARCHITECTURE CONCEPTS");
        sb.AppendLine("=====================");
        sb.AppendLine($"Building: a space designed around {top}.");
        sb.AppendLine($"City: a district organized for {top}.");
        sb.AppendLine($"Infrastructure: systems supporting {top} at scale.");
        return sb.ToString();
    }

    public static string DefaultRoboticsConcepts(IReadOnlyList<string> words)
    {
        string top = NlpContent.TopWords(words, 1).FirstOrDefault() ?? "the focus";
        var sb = new StringBuilder();
        sb.AppendLine("ROBOTICS CONCEPTS");
        sb.AppendLine("=================");
        sb.AppendLine($"Robot: a unit that acts on {top}.");
        sb.AppendLine($"Automation: a pipeline handling {top}.");
        sb.AppendLine($"Control system: a loop regulating {top}.");
        sb.AppendLine($"AI agent: an agent reacting to {top} in real time.");
        return sb.ToString();
    }

    public static string DefaultActionRecommendations(IReadOnlyList<(string State, double Value)> states, IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 4);
        string Concept(int i) => i < concepts.Count ? concepts[i] : "your focus";
        var sb = new StringBuilder();
        sb.AppendLine("ACTION RECOMMENDATIONS");
        sb.AppendLine("======================");
        sb.AppendLine($"Learn next: {Concept(0)}.");
        sb.AppendLine($"Build next: something using {Concept(1)}.");
        sb.AppendLine($"Research next: {Concept(2)}.");
        sb.AppendLine($"Improve next: your approach to {Concept(3)}.");
        return sb.ToString();
    }

    public static string DefaultResearchMarkdown(
        IReadOnlyList<(string State, double Value)> states, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string dom = DominantState(states);
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Reactive Machine Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"The present state is {dom}, reacting to {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## State Analysis");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}. Top states: {string.Join(", ", states.OrderByDescending(s => s.Value).Take(3).Select(s => $"{s.State} {s.Value:0}"))}.");
        sb.AppendLine();
        sb.AppendLine("## Opportunity Analysis");
        sb.AppendLine("Immediate learning, research, technical, engineering and creative openings from the current focus.");
        sb.AppendLine();
        sb.AppendLine("## Action Recommendations");
        sb.AppendLine($"Act now on {(concepts.Count > 0 ? concepts[0] : "the current focus")}.");
        sb.AppendLine();
        sb.AppendLine("## Innovation Recommendations");
        sb.AppendLine("Sketch one invention, one software concept and one research proposal from the present state.");
        sb.AppendLine();
        sb.AppendLine("## Conclusions");
        sb.AppendLine("A reactive machine responds to the present moment only — no memory, instant action.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string State, double Value)> states, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string dom = DominantState(states);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Current State", new[] { $"Dominant: {dom}", "Focused / creative / curious mix" }),
            new("Attention Analysis", new[] { "Attention level & stability", "Shifts & response readiness" }),
            new("Opportunity Detection", new[] { "Learning & research", "Technical / engineering / creative" }),
            new("Decision Engine", new[] { "Continue / investigate / explore", "Learn / solve / research" }),
            new("Innovation Engine", new[] { "Inventions & software", "Engineering & AI systems" }),
            new("Research Suggestions", new[] { "Papers & subjects", "Experiments & projects" }),
            new("Action Recommendations", new[] { "Learn / build / research next", "Improve next" }),
            new("Conclusions", new[] { "React to the present moment", "Instant, memory-less response" }),
        };
    }
}
