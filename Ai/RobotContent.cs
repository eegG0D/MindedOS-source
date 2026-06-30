using System.Text;
using System.Text.Json;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Robot content: the environment map JSON, memory / simulation /
/// object-recognition CSVs, the dashboard, and fallbacks for the LM artifacts (four narratives,
/// two reports, 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class RobotContent
{
    public static string MimeFor(string path)
    {
        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext switch { ".png" => "image/png", ".gif" => "image/gif", ".webp" => "image/webp", _ => "image/jpeg" };
    }

    public static string EnvironmentMapJson(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 6);
        if (concepts.Count == 0) concepts = new List<string> { "core" };
        var map = new
        {
            rooms = concepts.Take(4).Select((c, i) => new { id = i + 1, name = $"{c} room" }).ToArray(),
            corridors = new[] { new { id = 1, connects = new[] { 1, 2 } }, new { id = 2, connects = new[] { 2, 3 } } },
            obstacles = concepts.Skip(4).Take(2).Select((c, i) => new { id = i + 1, type = c }).ToArray(),
            targets = new[] { new { id = 1, name = concepts.Count > 0 ? concepts[0] : "target", room = 1 } },
            charging_stations = new[] { new { id = 1, room = 1 } },
        };
        return JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
    }

    public static string RobotMemoryCsv(IReadOnlyList<string> words, IReadOnlyList<(string Metric, double Value)> state)
    {
        var concepts = NlpContent.TopWords(words, 6);
        if (concepts.Count == 0) concepts = new List<string> { "the focus" };
        string[] types = { "success", "discovery", "new skill", "repeated task", "failure" };
        var sb = new StringBuilder("experience,type,outcome\n");
        for (int i = 0; i < concepts.Count; i++)
        {
            string type = types[i % types.Length];
            string outcome = type == "failure" ? "retry" : "logged";
            sb.AppendLine($"Explored {concepts[i]},{type},{outcome}");
        }
        return sb.ToString();
    }

    public static string SimulationResultsCsv(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 2);
        string task = concepts.Count > 0 ? $"Investigate {concepts[0]}" : "Idle";
        string goal = concepts.Count > 1 ? $"Reach {concepts[1]}" : "Hold position";
        double battery = dashboard.Count > 2 ? dashboard[2].Value : 75;
        var sb = new StringBuilder("attribute,value\n");
        sb.AppendLine("Position,Room 1 (0,0)");
        sb.AppendLine("Orientation,North");
        sb.AppendLine($"Battery Level,{battery:0}%");
        sb.AppendLine($"Active Task,{task}");
        sb.AppendLine($"Current Goal,{goal}");
        sb.AppendLine("Environment Status,mapped");
        return sb.ToString();
    }

    public static string ObjectRecognitionFallbackCsv(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 6);
        if (concepts.Count == 0) concepts = new List<string> { "object" };
        string[] types = { "tool", "machine", "obstacle", "sign", "person", "target" };
        var sb = new StringBuilder("source,object,type,confidence\n");
        for (int i = 0; i < concepts.Count; i++)
            sb.AppendLine($"concept,{concepts[i]},{types[i % types.Length]},{System.Math.Clamp(80 - i * 6, 30, 95)}");
        return sb.ToString();
    }

    public static string Dashboard(
        IReadOnlyList<(string Score, double Value)> dashboard,
        IReadOnlyList<(string Trait, double Value)> personality,
        IReadOnlyList<(string Skill, double Value)> skills)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Robot Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Metric | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Personality");
        sb.AppendLine("| Trait | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var (trait, value) in personality) sb.AppendLine($"| {trait} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Top Skills");
        sb.AppendLine("| Skill | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (skill, value) in skills) sb.AppendLine($"| {skill} | {value:0} |");
        return sb.ToString();
    }

    // ---- LM fallbacks ----

    public static string DefaultBrainState(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 5);
        string top = concepts.Count > 0 ? concepts[0] : "the mission";
        var sb = new StringBuilder();
        sb.AppendLine("ROBOT BRAIN STATE");
        sb.AppendLine("=================");
        sb.AppendLine($"Objectives: pursue {top} using the decoded concepts {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine("Intentions: navigate, scan and act on the highest-priority commands.");
        sb.AppendLine("Commands: follow the interpreted command stream.");
        sb.AppendLine("Strategies: explore when novelty is high, exploit when a target is clear.");
        sb.AppendLine("Tasks: sequence the autonomous missions below.");
        sb.AppendLine("Action plans: move → scan → act → return → charge.");
        return sb.ToString();
    }

    public static string DefaultAutonomousTasks(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 4);
        string top = concepts.Count > 0 ? concepts[0] : "the area";
        var sb = new StringBuilder();
        sb.AppendLine("# Autonomous Tasks");
        sb.AppendLine();
        sb.AppendLine($"- **Daily mission:** patrol and monitor {top}.");
        sb.AppendLine("- **Exploration mission:** map unvisited rooms and corridors.");
        sb.AppendLine($"- **Research mission:** gather data on {string.Join(", ", concepts.Take(2).DefaultIfEmpty("targets"))}.");
        sb.AppendLine("- **Inspection mission:** check obstacles and charging stations.");
        sb.AppendLine("- **Learning mission:** record outcomes and update skills.");
        return sb.ToString();
    }

    public static string DefaultChatLog(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string top = concepts.Count > 0 ? concepts[0] : "your goal";
        var sb = new StringBuilder();
        sb.AppendLine("ROBOT CHAT LOG");
        sb.AppendLine("==============");
        sb.AppendLine("Human (EEG): " + (concepts.Count > 0 ? string.Join(" ", concepts) : "(no words)"));
        sb.AppendLine($"Robot: I read your intent around {top}. I will navigate there and scan the area.");
        sb.AppendLine("Robot (clarifying): Should I pick up objects or only observe?");
        sb.AppendLine("Robot: Action explained — moving forward, scanning, then returning to base.");
        return sb.ToString();
    }

    public static string DefaultEvolution(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("FUTURE ROBOT EVOLUTION");
        sb.AppendLine("======================");
        sb.AppendLine($"Future capabilities: deeper autonomy around {string.Join(", ", concepts.DefaultIfEmpty("core tasks"))}.");
        sb.AppendLine("New skills: improved navigation and object handling.");
        sb.AppendLine("Autonomous improvements: smarter task planning and recovery.");
        sb.AppendLine("Hardware recommendations: add a depth camera and a second gripper.");
        sb.AppendLine("Sensor upgrades: LiDAR for mapping, IMU for stable orientation.");
        return sb.ToString();
    }

    public static string DefaultAnalysisMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Robot Analysis Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A cognitive robot brain derived from a 3-minute EEG. Leading concepts: {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Brain Analysis");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}. Confidence {dashboard[0].Value:0}, learning {dashboard[3].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Robot Decisions");
        sb.AppendLine("Commands are interpreted from the decoded concepts and ranked by frequency and priority.");
        sb.AppendLine();
        sb.AppendLine("## Navigation Analysis");
        sb.AppendLine($"Exploration {dashboard[4].Value:0}, decision stability {dashboard[6].Value:0} shape the movement plan.");
        sb.AppendLine();
        sb.AppendLine("## Learning Analysis");
        sb.AppendLine("Experiences (successes, discoveries, new skills) accumulate in the robot memory.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine("Reinforce high-confidence commands and expand exploration of unmapped areas.");
        return sb.ToString();
    }

    public static string DefaultEngineeringMarkdown(IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = NlpContent.TopWords(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("# Robot Engineering Report");
        sb.AppendLine();
        sb.AppendLine("## Robot Designs");
        sb.AppendLine($"A mobile platform tuned for {string.Join(", ", concepts.DefaultIfEmpty("general"))} tasks.");
        sb.AppendLine();
        sb.AppendLine("## Mechanical Concepts");
        sb.AppendLine("Differential drive base, articulated arm, modular gripper.");
        sb.AppendLine();
        sb.AppendLine("## Sensor Configurations");
        sb.AppendLine("Camera, LiDAR, IMU, bump and proximity sensors.");
        sb.AppendLine();
        sb.AppendLine("## Electronics Layouts");
        sb.AppendLine("Main controller, motor drivers, battery management, comms module.");
        sb.AppendLine();
        sb.AppendLine("## Actuator Suggestions");
        sb.AppendLine("Brushless wheel motors, servo joints, an electric gripper actuator.");
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
            new("Robot Brain State", new[] { $"Confidence {dashboard[0].Value:0}", $"Goal commitment {dashboard[1].Value:0}" }),
            new("Command Analysis", new[] { "Interpreted from concepts", "Ranked by priority" }),
            new("Navigation System", new[] { $"Exploration {dashboard[4].Value:0}", "Forward / rotate / explore / return" }),
            new("Learning Engine", new[] { $"Learning {dashboard[3].Value:0}", "Successes, discoveries, new skills" }),
            new("Personality System", new[] { "Assistant / Researcher / Explorer", "Engineer / Scientist / Inventor / Teacher" }),
            new("Multi-Robot Coordination", new[] { "Leader, worker, scout", "Research & maintenance robots" }),
            new("Engineering Concepts", new[] { "Drive base & arm", "Sensors & actuators" }),
            new("Conclusions", new[] { "EEG → cognitive robot brain", "Plans, learns, coordinates" }),
        };
    }
}
