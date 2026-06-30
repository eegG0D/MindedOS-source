using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Sensorimotor content: the driving / human-vs-AI / pattern CSVs, the
/// dashboard, and fallbacks for the LM artifacts (training narrative, report, 10-slide deck).
/// Reuses <see cref="NlpContent"/>.
/// </summary>
public static class SensorimotorContent
{
    public static string DrivingPerformanceCsv(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        // dashboard: [1]=Motor Planning, [2]=Coordination, [4]=Learning
        double accuracy = dashboard.Count > 2 ? dashboard[2].Value : 50;
        double reaction = dashboard.Count > 1 ? dashboard[1].Value : 50;
        double navigation = dashboard.Count > 4 ? dashboard[4].Value : 50;
        var sb = new StringBuilder("metric,value\n");
        sb.AppendLine($"Driving Accuracy,{accuracy:0.0}");
        sb.AppendLine($"Reaction Performance,{reaction:0.0}");
        sb.AppendLine($"Navigation Efficiency,{navigation:0.0}");
        return sb.ToString();
    }

    public static string HumanVsAiControlCsv(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        double[] ai = { 90, 92, 85, 95 }; // fixed artificial-agent profile
        string[] metrics = { "Accuracy", "Efficiency", "Adaptability", "Consistency" };
        double[] human =
        {
            dashboard.Count > 2 ? dashboard[2].Value : 50, // Accuracy ~ Coordination
            dashboard.Count > 1 ? dashboard[1].Value : 50, // Efficiency ~ Motor Planning
            dashboard.Count > 3 ? dashboard[3].Value : 50, // Adaptability ~ Adaptation
            dashboard.Count > 0 ? dashboard[0].Value : 50, // Consistency ~ Sensory Awareness
        };
        var sb = new StringBuilder("metric,human,ai\n");
        for (int i = 0; i < metrics.Length; i++) sb.AppendLine($"{metrics[i]},{human[i]:0},{ai[i]:0}");
        return sb.ToString();
    }

    public static string SensorimotorPatternsCsv(IReadOnlyList<string> words)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }
        var sb = new StringBuilder("pattern,occurrences,type\n");
        foreach (var (word, count) in freq.OrderByDescending(kv => kv.Value).Take(10))
            sb.AppendLine($"{word},{count},{(count >= 3 ? "habitual action" : count == 2 ? "repeated intention" : "occasional")}");
        if (freq.Count == 0) sb.AppendLine("(none),0,none");
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<(string State, double Value)> states)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Sensorimotor Learning Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Metric | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Sensorimotor States");
        sb.AppendLine("| State | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (state, value) in states) sb.AppendLine($"| {state} | {value:0} |");
        return sb.ToString();
    }

    // ---- LM fallbacks ----

    public static string DefaultTrainingRecommendations(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string top = concepts.Count > 0 ? concepts[0] : "your focus";
        var sb = new StringBuilder();
        sb.AppendLine("TRAINING RECOMMENDATIONS");
        sb.AppendLine("========================");
        sb.AppendLine($"Skill improvement: practice tasks related to {top} with gradual difficulty.");
        sb.AppendLine("Motor training exercises: short, repeated movement drills with feedback.");
        sb.AppendLine("Reaction improvement: timed go/no-go drills to sharpen response speed.");
        sb.AppendLine("Coordination exercises: dual-task and hand-eye tracking practice.");
        sb.AppendLine("Focus enhancement: brief mindfulness before each practice block.");
        return sb.ToString();
    }

    public static string DefaultReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Sensorimotor Learning Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A model of the perception→motor loop from a 3-minute EEG. Recurring concepts: {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Sensor Analysis");
        sb.AppendLine($"Sensory awareness {dashboard[0].Value:0}; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Motor Analysis");
        sb.AppendLine($"Motor planning {dashboard[1].Value:0}; BMI readiness {dashboard[5].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Coordination Assessment");
        sb.AppendLine($"Coordination {dashboard[2].Value:0} across hand-eye, spatial and multi-step tasks.");
        sb.AppendLine();
        sb.AppendLine("## Learning Assessment");
        sb.AppendLine($"Learning {dashboard[4].Value:0} and adaptation {dashboard[3].Value:0} drive skill acquisition.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine("Practice deliberately, add feedback, and progressively increase task difficulty.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = NlpContent.TopWords(words, 3);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Sensory Processing", new[] { $"Sensory awareness {dashboard[0].Value:0}", "Visual, spatial, situational" }),
            new("Motor Planning", new[] { $"Motor planning {dashboard[1].Value:0}", "Action preparation & intentions" }),
            new("Coordination Profile", new[] { $"Coordination {dashboard[2].Value:0}", "Hand-eye, spatial, precision" }),
            new("Learning Assessment", new[] { $"Learning {dashboard[4].Value:0}", $"Adaptation {dashboard[3].Value:0}" }),
            new("BMI Control", new[] { $"BMI readiness {dashboard[5].Value:0}", "Up/Down/Left/Right/Forward/Back/Stop/Action" }),
            new("Adaptation Analysis", new[] { "Learning curve & recovery", "Strategy evolution" }),
            new("Recommendations", new[] { "Deliberate practice", "Reaction & coordination drills" }),
            new("Conclusions", new[] { "EEG → sensorimotor model", "Track skill growth over time" }),
        };
    }
}
