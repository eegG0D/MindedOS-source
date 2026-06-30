using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Theory of Mind content: the intent model, goal predictions, belief
/// structure, knowledge graph, the preview scorecard, and fallbacks for the LM artifacts (two
/// narratives, a report and a 10-slide deck). Self-contained; reuses only <see cref="NlpContent"/>.
/// Every inferred mental-state model is a probabilistic hypothesis, NOT a verified fact.
/// </summary>
public static class TomContent
{
    public const string Disclaimer =
        "NOTE: All inferred goals, beliefs, intentions and perspectives below are probabilistic " +
        "hypotheses and simulations derived from EEG-decoded concepts — not verified facts about " +
        "anyone's actual thoughts.";

    private static readonly string[] Viewpoints =
        { "Scientist", "Engineer", "Entrepreneur", "Researcher", "Educator", "Inventor", "Designer", "AI System" };

    private static List<string> Concepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "idea", "goal", "concept", "plan", "design", "learn" };
    }

    // ---- intent model + goal predictions ----

    public static string IntentModelCsv(IReadOnlyList<TomIntentScore> intents)
    {
        var sb = new StringBuilder("intent,score,percent\n");
        foreach (var i in intents)
        {
            string score = i.Percent >= 20 ? "High" : i.Percent >= 8 ? "Medium" : "Low";
            sb.AppendLine($"{i.Intent},{score},{i.Percent:0.0}");
        }
        if (intents.Count == 0) sb.AppendLine("Learning,Medium,100.0");
        return sb.ToString();
    }

    public static string GoalPredictionsCsv(IReadOnlyList<TomIntentScore> intents, IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 8);
        string[] horizons = { "short-term", "short-term", "medium-term", "medium-term", "long-term" };
        var sb = new StringBuilder("predicted_goal,likelihood,horizon\n");
        for (int i = 0; i < concepts.Count; i++)
        {
            string intent = i < intents.Count ? intents[i % Math.Max(intents.Count, 1)].Intent : "Learning";
            double likelihood = Math.Clamp(90 - i * 7, 20, 90);
            string horizon = horizons[i % horizons.Length];
            sb.AppendLine($"{intent}: {concepts[i]},{likelihood:0.0},{horizon}");
        }
        return sb.ToString();
    }

    // ---- belief structure + knowledge graph ----

    public static string BeliefStructureMd(IReadOnlyList<string> words, IReadOnlyList<TomIntentScore> intents)
    {
        var concepts = Concepts(words, 6);
        string top = intents.Count > 0 ? intents[0].Intent : "learning";
        var sb = new StringBuilder();
        sb.AppendLine("# Belief Structure");
        sb.AppendLine();
        sb.AppendLine($"_{Disclaimer}_");
        sb.AppendLine();
        sb.AppendLine("## Core Assumptions");
        sb.AppendLine($"- Progress is driven by {top.ToLowerInvariant()} and the recurring concepts.");
        sb.AppendLine();
        sb.AppendLine("## Repeated Themes");
        foreach (var c in concepts.Take(4)) sb.AppendLine($"- {c}");
        sb.AppendLine();
        sb.AppendLine("## Consistent Viewpoints");
        sb.AppendLine($"- A consistent orientation toward {top.ToLowerInvariant()}.");
        sb.AppendLine();
        sb.AppendLine("## Intellectual Priorities");
        sb.AppendLine($"- {string.Join(", ", concepts.Take(3))}");
        return sb.ToString();
    }

    public static string TheoryOfMindGraphMd(IReadOnlyList<string> words, IReadOnlyList<TomIntentScore> intents)
    {
        var concepts = Concepts(words, 5);
        var topIntents = intents.Take(4).Select(i => i.Intent).ToList();
        if (topIntents.Count == 0) topIntents.Add("Learning");
        var sb = new StringBuilder();
        sb.AppendLine("# Theory of Mind Knowledge Graph");
        sb.AppendLine();
        sb.AppendLine($"_{Disclaimer}_");
        sb.AppendLine();
        sb.AppendLine("Connecting goals, beliefs, interests, intentions, motivations and decisions:");
        sb.AppendLine();
        for (int i = 0; i < topIntents.Count; i++)
        {
            string concept = concepts[i % concepts.Count];
            sb.AppendLine($"- **Intention: {topIntents[i]}** → motivates → **Goal: advance {concept}** → shapes → **Decision**");
        }
        sb.AppendLine();
        sb.AppendLine($"Beliefs anchor on: {string.Join(", ", concepts)}");
        return sb.ToString();
    }

    // ---- preview scorecard ----

    public static string Scorecard(IReadOnlyList<(string Score, double Value)> dashboard, string topIntent)
    {
        var sb = new StringBuilder();
        sb.AppendLine("THEORY OF MIND DASHBOARD");
        sb.AppendLine("========================");
        sb.AppendLine($"Top inferred intent (hypothesis): {topIntent}");
        foreach (var (name, value) in dashboard)
        {
            int filled = (int)Math.Round(value / 5.0);
            string bar = new string('█', Math.Clamp(filled, 0, 20)) + new string('░', Math.Clamp(20 - filled, 0, 20));
            sb.AppendLine($"{name,-20} {bar} {value:0}");
        }
        sb.AppendLine();
        sb.AppendLine(Disclaimer);
        return sb.ToString();
    }

    // ---- LM fallbacks: narratives ----

    public static string DefaultPerspectiveSimulations(IReadOnlyList<string> words, IReadOnlyList<TomIntentScore> intents)
    {
        var concepts = Concepts(words, 3);
        string topic = concepts[0];
        var sb = new StringBuilder();
        sb.AppendLine("PERSPECTIVE SIMULATIONS");
        sb.AppendLine("=======================");
        sb.AppendLine(Disclaimer);
        sb.AppendLine();
        sb.AppendLine($"Topic from your EEG: '{topic}'. Hypothetical viewpoints:");
        foreach (var vp in Viewpoints)
            sb.AppendLine($"- **{vp}:** would likely approach {topic} from a {vp.ToLowerInvariant()} angle, emphasizing its priorities.");
        return sb.ToString();
    }

    public static string DefaultCognitiveScenarios(IReadOnlyList<TomIntentScore> intents, IReadOnlyList<string> words)
    {
        string top = intents.Count > 0 ? intents[0].Intent : "learning";
        var sb = new StringBuilder();
        sb.AppendLine("COGNITIVE SCENARIOS");
        sb.AppendLine("===================");
        sb.AppendLine(Disclaimer);
        sb.AppendLine();
        foreach (var s in new[] { "Solving a scientific problem", "Leading a project", "Designing a robot", "Creating a business", "Learning a new skill", "Conducting research" })
            sb.AppendLine($"- **{s}:** a probable reasoning path would start from {top.ToLowerInvariant()}, then iterate toward a concrete result.");
        return sb.ToString();
    }

    // ---- LM fallback: research report (.docx) ----

    public static string DefaultReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<TomIntentScore> intents,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = intents.Count > 0 ? intents[0].Intent : "Learning";
        var concepts = Concepts(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Theory of Mind Report");
        sb.AppendLine();
        sb.AppendLine($"_{Disclaimer}_");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A hypothetical mental-state model from a 3-minute EEG. Top inferred intent: {top}; goal {dashboard[0].Value:0}, curiosity {dashboard[1].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Perspective Analysis");
        sb.AppendLine($"Personal, scientific, engineering and social perspectives are estimated; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Goal Analysis");
        sb.AppendLine($"Likely goals cluster around {string.Join(", ", concepts.Take(3))}, aligned with {top.ToLowerInvariant()}.");
        sb.AppendLine();
        sb.AppendLine("## Belief Structure Analysis");
        sb.AppendLine($"Repeated themes ({string.Join(", ", concepts.Take(3))}) suggest consistent intellectual priorities.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine("Treat these as hypotheses; validate with the person directly, record more sessions, and watch for evolving goals.");
        return sb.ToString();
    }

    // ---- LM fallback: 10-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<TomIntentScore> intents,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = Concepts(words, 3);
        string Intent(int i) => i < intents.Count ? $"{intents[i].Intent} ({intents[i].Percent:0}%)" : "—";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", $"Recurring: {string.Join(", ", concepts)}" }),
            new("Goals and Intentions", new[] { Intent(0), Intent(1), "Hypotheses, not facts" }),
            new("Perspective Analysis", new[] { "Personal, scientific", "Engineering, social" }),
            new("Belief Structures", new[] { "Core assumptions & repeated themes", "Consistent viewpoints" }),
            new("Decision Styles", new[] { "Analytical, intuitive, exploratory", "Strategic; risk-taking vs avoidance" }),
            new("Social Cognition", new[] { $"Collaboration {dashboard[3].Value:0}", $"Leadership {dashboard[2].Value:0}" }),
            new("Scenario Simulations", new[] { "Solving, leading, designing", "Creating, learning, researching" }),
            new("Human vs AI Comparison", new[] { "Shared vs unique concepts", "Similarities & differences" }),
            new("Conclusions", new[] { "EEG → hypothetical mind model", "Probabilistic, not verified" }),
        };
    }
}
