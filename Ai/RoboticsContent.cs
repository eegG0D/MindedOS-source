using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Robotics content: the class / control-log / vision CSVs, the
/// dashboard, and fallbacks for every LM artifact (nine narratives, two reports, a PDF, a deck).
/// Reuses <see cref="NlpContent"/>.
/// </summary>
public static class RoboticsContent
{
    private static readonly string[] Interfaces = { "USB", "Serial", "Bluetooth", "WiFi", "MQTT" };
    private static readonly string[] DefaultCommands = { "Move Forward", "Turn Left", "Pick Object", "Stop", "Wave Hand" };
    private static readonly string[] VisionCategories = { "object", "person", "obstacle", "navigation path" };

    public static string MimeFor(string path)
    {
        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext switch { ".png" => "image/png", ".gif" => "image/gif", ".webp" => "image/webp", _ => "image/jpeg" };
    }

    public static string ClassScoresCsv(IReadOnlyList<RoboticsClassScore> classes)
    {
        var sb = new StringBuilder("class,score\n");
        foreach (var c in classes) sb.AppendLine($"{c.Class},{c.Percent:0.0}");
        if (classes.Count == 0) sb.AppendLine("General,100.0");
        return sb.ToString();
    }

    public static string ControlLogCsv(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder("step,interface,command,status\n");
        for (int i = 0; i < 8; i++)
        {
            string iface = Interfaces[i % Interfaces.Length];
            string command = DefaultCommands[i % DefaultCommands.Length];
            string status = i % 3 == 0 ? "ack" : "sent";
            sb.AppendLine($"{i + 1},{iface},{command},{status}");
        }
        return sb.ToString();
    }

    public static string VisionFallbackCsv(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 6);
        if (concepts.Count == 0) concepts = new List<string> { "object" };
        var sb = new StringBuilder("source,detection,category,confidence\n");
        for (int i = 0; i < concepts.Count; i++)
            sb.AppendLine($"concept,{concepts[i]},{VisionCategories[i % VisionCategories.Length]},{System.Math.Clamp(80 - i * 6, 30, 95)}");
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<RoboticsClassScore> classes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Robotics Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Metric | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Robot Classification");
        sb.AppendLine("| Class | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var c in classes.Take(10)) sb.AppendLine($"| {c.Class} | {c.Percent:0} |");
        return sb.ToString();
    }

    // ---- LM fallbacks: nine narratives ----

    public static string DefaultRobotProfile(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 5);
        string top = concepts.Count > 0 ? concepts[0] : "general tasks";
        var sb = new StringBuilder();
        sb.AppendLine("ROBOT PROFILE");
        sb.AppendLine("=============");
        sb.AppendLine($"Objectives: serve {top}.");
        sb.AppendLine($"Behaviors: navigate, sense and act on {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine("Functions: perception, planning, manipulation.");
        sb.AppendLine("Missions: complete assigned tasks autonomously.");
        sb.AppendLine("Roles: assistant and worker.");
        sb.AppendLine("Capabilities: mobility, sensing, communication.");
        return sb.ToString();
    }

    public static string DefaultBrainArchitecture(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Robot Brain Architecture");
        sb.AppendLine();
        foreach (var module in new[] { "Vision System", "Hearing System", "Speech System", "Navigation System", "Learning System", "Decision System", "Memory System", "Motion System" })
            sb.AppendLine($"- **{module}:** handles {module.Replace(" System", "").ToLowerInvariant()} processing.");
        return sb.ToString();
    }

    public static string DefaultAutonomousBehavior(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("AUTONOMOUS BEHAVIOR");
        sb.AppendLine("===================");
        sb.AppendLine($"Autonomous tasks: monitor and act on {string.Join(", ", concepts.DefaultIfEmpty("the environment"))}.");
        sb.AppendLine("Autonomous objectives: maximize task completion safely.");
        sb.AppendLine("Autonomous workflows: sense → plan → act → verify.");
        sb.AppendLine("Autonomous behaviors: explore, avoid obstacles, recharge.");
        return sb.ToString();
    }

    public static string DefaultElectronicsDesign(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Robot Electronics Design");
        sb.AppendLine();
        sb.AppendLine("## Sensors");
        sb.AppendLine("- Camera, LiDAR, IMU, proximity, encoders.");
        sb.AppendLine("## Motor Controllers");
        sb.AppendLine("- Brushless ESCs and servo drivers.");
        sb.AppendLine("## Microcontroller");
        sb.AppendLine("- A real-time MCU plus an SBC for high-level AI.");
        sb.AppendLine("## Power System");
        sb.AppendLine("- Li-ion pack, BMS, regulated rails.");
        sb.AppendLine("## Wiring");
        sb.AppendLine("- Bus-based wiring with clearly labeled connectors for KiCad/PCB design.");
        return sb.ToString();
    }

    public static string DefaultBlenderPrompt(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("BLENDER / CAD ROBOT PROMPT");
        sb.AppendLine("==========================");
        sb.AppendLine($"Model a robot themed around {string.Join(", ", concepts.DefaultIfEmpty("a versatile platform"))}.");
        sb.AppendLine("Mechanical design: a mobile base with an articulated arm and modular gripper.");
        sb.AppendLine("External appearance: clean panels, status LEDs, a sensor head.");
        sb.AppendLine("Internal structure: chassis, battery bay, electronics tray.");
        sb.AppendLine("Sensors: camera dome, LiDAR ring, proximity strips.");
        sb.AppendLine("Actuators: wheel motors, servo joints, gripper actuator.");
        return sb.ToString();
    }

    public static string DefaultSimulationScenarios(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ROBOT SIMULATION SCENARIOS");
        sb.AppendLine("==========================");
        foreach (var s in new[] { "Warehouse", "City Streets", "Homes", "Hospitals", "Factories", "Laboratories", "Space Stations" })
            sb.AppendLine($"- {s}: navigate, sense and perform representative tasks.");
        return sb.ToString();
    }

    public static string DefaultSwarmDesign(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Robot Swarm Design");
        sb.AppendLine();
        sb.AppendLine("## 10-Robot Swarm");
        sb.AppendLine("- Roles: Leader, Navigator, Worker, Builder, Scout, Inspector, Coordinator.");
        sb.AppendLine("## 50-Robot Swarm");
        sb.AppendLine("- Multiple sub-teams, each with a coordinator reporting to a leader.");
        sb.AppendLine("## 100-Robot Swarm");
        sb.AppendLine("- Hierarchical coordination with redundant leaders and scouts.");
        return sb.ToString();
    }

    public static string DefaultLearningPlan(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ROBOT LEARNING PLAN");
        sb.AppendLine("===================");
        foreach (var skill in new[] { "Navigation", "Manipulation", "Communication", "Object Recognition", "Task Planning" })
            sb.AppendLine($"- {skill}: train in simulation, then transfer to hardware.");
        return sb.ToString();
    }

    public static string DefaultFutureRobotics(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FUTURE ROBOTICS");
        sb.AppendLine("===============");
        sb.AppendLine("New sensors: event cameras, tactile skin.");
        sb.AppendLine("Better actuators: compliant, energy-dense drives.");
        sb.AppendLine("Advanced AI systems: on-board foundation models.");
        sb.AppendLine("Brain-machine interfaces: direct EEG control.");
        sb.AppendLine("Swarm capabilities: self-organizing fleets.");
        sb.AppendLine("Autonomous learning systems: lifelong on-device learning.");
        return sb.ToString();
    }

    // ---- LM fallbacks: documents ----

    public static string DefaultDesignReportMarkdown(
        IReadOnlyList<RoboticsClassScore> classes, IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<string> words)
    {
        string topClass = classes.Count > 0 ? classes[0].Class : "Service";
        var concepts = NlpContent.TopWords(words, 3);
        string name = concepts.Count > 0 ? $"{char.ToUpper(concepts[0][0])}{concepts[0][1..]}-Bot" : "Mind-Bot";
        var sb = new StringBuilder();
        sb.AppendLine("# Robot Design Report");
        sb.AppendLine();
        sb.AppendLine("## Robot Name");
        sb.AppendLine(name);
        sb.AppendLine();
        sb.AppendLine("## Purpose");
        sb.AppendLine($"A {topClass.ToLowerInvariant()} robot derived from the user's EEG concepts.");
        sb.AppendLine();
        sb.AppendLine("## Specifications");
        sb.AppendLine($"Complexity {dashboard[0].Value:0}/100, autonomy {dashboard[1].Value:0}/100, intelligence {dashboard[2].Value:0}/100.");
        sb.AppendLine();
        sb.AppendLine("## Dimensions");
        sb.AppendLine("Approx. 0.6 m x 0.4 m x 1.2 m; ~25 kg.");
        sb.AppendLine();
        sb.AppendLine("## Sensors");
        sb.AppendLine("Camera, LiDAR, IMU, proximity, microphone.");
        sb.AppendLine();
        sb.AppendLine("## Actuators");
        sb.AppendLine("Wheel motors, arm servos, gripper actuator.");
        sb.AppendLine();
        sb.AppendLine("## Power Systems");
        sb.AppendLine("Li-ion battery with BMS and fast charging.");
        sb.AppendLine();
        sb.AppendLine("## Communication Systems");
        sb.AppendLine("WiFi, Bluetooth and MQTT telemetry.");
        return sb.ToString();
    }

    public static string DefaultResearchPaperMarkdown(
        IReadOnlyList<RoboticsClassScore> classes, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string topClass = classes.Count > 0 ? classes[0].Class : "Service";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Robotics Research Paper");
        sb.AppendLine();
        sb.AppendLine("## Abstract");
        sb.AppendLine($"We derive a {topClass.ToLowerInvariant()} robot concept from a 3-minute EEG-decoded word stream.");
        sb.AppendLine();
        sb.AppendLine("## Introduction");
        sb.AppendLine($"Brain-derived concepts ({string.Join(", ", concepts.Take(3))}) seed a robotics design.");
        sb.AppendLine();
        sb.AppendLine("## Methods");
        sb.AppendLine($"EEG decoded via eeg_map; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Design");
        sb.AppendLine("A mobile platform with an articulated arm, multimodal sensing and an AI controller.");
        sb.AppendLine();
        sb.AppendLine("## Results");
        sb.AppendLine($"The leading class is {topClass}; the design scores well on autonomy and engineering.");
        sb.AppendLine();
        sb.AppendLine("## Discussion");
        sb.AppendLine("EEG-derived concepts can usefully constrain early robot design exploration.");
        sb.AppendLine();
        sb.AppendLine("## Future Work");
        sb.AppendLine("Close the loop with live EEG teleoperation and on-device learning.");
        return sb.ToString();
    }

    public static string DefaultConceptsMarkdown(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string theme = concepts.Count > 0 ? concepts[0] : "general";
        var sb = new StringBuilder();
        sb.AppendLine("# Robotics Engineering Concepts");
        sb.AppendLine();
        sb.AppendLine("## Service Robot");
        sb.AppendLine($"Assists people with {theme}-related tasks.");
        sb.AppendLine();
        sb.AppendLine("## Factory Automation");
        sb.AppendLine("A robotic arm cell for repetitive precision work.");
        sb.AppendLine();
        sb.AppendLine("## Smart Home");
        sb.AppendLine("A domestic robot for monitoring and chores.");
        sb.AppendLine();
        sb.AppendLine("## Research Robot");
        sb.AppendLine("A sensor-rich platform for data collection.");
        sb.AppendLine();
        sb.AppendLine("## Educational Robot");
        sb.AppendLine("A friendly robot that teaches and demonstrates.");
        sb.AppendLine();
        sb.AppendLine("## Space Exploration");
        sb.AppendLine("A rugged rover for remote terrain.");
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
            new("Robot Concept", new[] { $"Complexity {dashboard[0].Value:0}", $"Intelligence {dashboard[2].Value:0}" }),
            new("Robot Architecture", new[] { "Vision, hearing, speech", "Navigation, learning, decision, memory, motion" }),
            new("Control System", new[] { "USB / Serial / Bluetooth / WiFi / MQTT", "EEG-derived commands" }),
            new("Autonomous Behaviors", new[] { $"Autonomy {dashboard[1].Value:0}", "Sense → plan → act → verify" }),
            new("Electronics Design", new[] { "Sensors & motor controllers", "MCU + SBC, power system" }),
            new("Simulation Environment", new[] { "Warehouse, home, hospital", "Factory, lab, space station" }),
            new("Future Upgrades", new[] { "New sensors & actuators", "BMIs & swarm capabilities" }),
            new("Conclusions", new[] { "EEG → robot design", "Bridges brain concepts & robotics" }),
        };
    }
}
