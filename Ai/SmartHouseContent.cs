using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Smart House content: the room/device/occupancy/mood/IoT CSVs, the
/// dashboard, and fallbacks for the LM artifacts (five narratives, two reports, a deck).
/// Reuses <see cref="NlpContent"/>.
/// </summary>
public static class SmartHouseContent
{
    private static readonly string[] Devices =
        { "Lights", "Smart Plugs", "Thermostats", "Security Cameras", "Door Locks", "Fans", "Air Purifiers", "Speakers", "Displays", "Robotic Devices" };
    private static readonly string[] Protocols = { "Wi-Fi", "Bluetooth", "Zigbee", "Matter", "MQTT" };
    private static readonly string[] Periods = { "morning", "midday", "afternoon", "evening", "night" };

    public static string RoomPreferencesCsv(IReadOnlyList<HouseRoomScore> rooms, IReadOnlyList<(string Score, double Value)> dashboard)
    {
        double comfortBase = dashboard.Count > 0 ? dashboard[0].Value : 50;
        double prodBase = dashboard.Count > 4 ? dashboard[4].Value : 50;
        var sb = new StringBuilder("room,time_preference,comfort,productivity\n");
        for (int i = 0; i < rooms.Count; i++)
        {
            double weight = rooms[i].Percent;
            sb.AppendLine($"{rooms[i].Room},{weight:0.0},{System.Math.Clamp(comfortBase - i * 2, 0, 100):0},{System.Math.Clamp(prodBase - i * 2, 0, 100):0}");
        }
        if (rooms.Count == 0) sb.AppendLine("Living Room,100.0,50,50");
        return sb.ToString();
    }

    public static string DeviceRecommendationsCsv(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<(string Metric, double Value)> moods)
    {
        double automation = dashboard.Count > 2 ? dashboard[2].Value : 50;
        string[] actions = { "auto-adjust", "schedule", "optimize", "monitor", "enable", "lower", "activate", "tune", "dim", "standby" };
        var sb = new StringBuilder("device,recommended_action,priority\n");
        for (int i = 0; i < Devices.Length; i++)
        {
            string priority = i < 3 ? "High" : i < 7 ? "Medium" : "Low";
            sb.AppendLine($"{Devices[i]},{actions[i % actions.Length]},{priority}");
        }
        return sb.ToString();
    }

    public static string OccupancyPredictionsCsv(IReadOnlyList<HouseRoomScore> rooms)
    {
        var sb = new StringBuilder("room,predicted_occupancy,peak_period\n");
        for (int i = 0; i < rooms.Count; i++)
        {
            string occ = rooms[i].Percent >= 20 ? "high" : rooms[i].Percent >= 10 ? "medium" : "low";
            sb.AppendLine($"{rooms[i].Room},{occ},{Periods[i % Periods.Length]}");
        }
        if (rooms.Count == 0) sb.AppendLine("Living Room,medium,evening");
        return sb.ToString();
    }

    public static string MoodAutomationCsv(IReadOnlyList<(string Metric, double Value)> moods)
    {
        var automations = new Dictionary<string, string>
        {
            ["Focused"] = "bright cool light, quiet, cooler temp",
            ["Relaxed"] = "dim warm light, soft music, warmer temp",
            ["Creative"] = "colorful light, ambient music",
            ["Tired"] = "dim light, white noise, warm temp",
            ["Productive"] = "bright light, no distractions",
            ["Learning"] = "neutral light, low music, steady temp",
        };
        var sb = new StringBuilder("mood,score,recommended_automation\n");
        foreach (var (mood, score) in moods)
            sb.AppendLine($"{mood},{score:0.0},\"{(automations.TryGetValue(mood, out var a) ? a : "adjust to preference")}\"");
        return sb.ToString();
    }

    public static string IotDeviceRegistryCsv()
    {
        var sb = new StringBuilder("device,protocol,status\n");
        for (int i = 0; i < Devices.Length; i++)
            sb.AppendLine($"{Devices[i]},{Protocols[i % Protocols.Length]},registered");
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<HouseRoomScore> rooms)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Smart House Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Metric | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Room Preferences");
        sb.AppendLine("| Room | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var r in rooms.Take(8)) sb.AppendLine($"| {r.Room} | {r.Percent:0} |");
        return sb.ToString();
    }

    // ---- LM fallbacks ----

    public static string DefaultEnergyReport(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        double eff = dashboard.Count > 1 ? dashboard[1].Value : 50;
        var sb = new StringBuilder();
        sb.AppendLine("ENERGY OPTIMIZATION REPORT");
        sb.AppendLine("==========================");
        sb.AppendLine($"Energy efficiency score: {eff:0}/100.");
        sb.AppendLine("Energy savings: schedule heavy devices off-peak (est. 10-15%).");
        sb.AppendLine("Cost savings: automate lighting and standby power (est. 8-12%).");
        sb.AppendLine("Efficiency improvements: occupancy-based HVAC and lighting.");
        return sb.ToString();
    }

    public static string DefaultSecurity(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SECURITY RECOMMENDATIONS");
        sb.AppendLine("========================");
        sb.AppendLine("Door monitoring: smart locks with auto-lock at night.");
        sb.AppendLine("Camera placement: entries, driveway and main hallway.");
        sb.AppendLine("Motion detection zones: perimeter and entry points.");
        sb.AppendLine("Automated alerts: notify on unexpected motion when away.");
        sb.AppendLine("Night security profile: arm sensors, dim path lighting.");
        return sb.ToString();
    }

    public static string DefaultAssistantLog(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string top = concepts.Count > 0 ? concepts[0] : "your routine";
        var sb = new StringBuilder();
        sb.AppendLine("HOUSE ASSISTANT LOG");
        sb.AppendLine("===================");
        sb.AppendLine($"You: optimize the home for {top}.");
        sb.AppendLine("Assistant: I will tune lighting, temperature and devices to match.");
        sb.AppendLine("Assistant: energy usage looks efficient; I suggest off-peak scheduling.");
        sb.AppendLine("Assistant: I can expand automation when you add more devices.");
        return sb.ToString();
    }

    public static string DefaultHomeDesign(IReadOnlyList<HouseRoomScore> rooms)
    {
        string top = rooms.Count > 0 ? rooms[0].Room : "Living Room";
        var sb = new StringBuilder();
        sb.AppendLine("# Home Design Recommendations");
        sb.AppendLine();
        sb.AppendLine($"- **Furniture layout:** optimize the {top} for its primary use.");
        sb.AppendLine("- **Workspace:** a quiet desk with good light near a window.");
        sb.AppendLine("- **Learning space:** low-distraction nook with storage.");
        sb.AppendLine("- **Entertainment area:** central speakers and display.");
        sb.AppendLine("- **Robotics station:** a charging/work bench with power.");
        sb.AppendLine("- **Smart device placement:** sensors per room, hub central.");
        return sb.ToString();
    }

    public static string DefaultSimulation(IReadOnlyList<(string Score, double Value)> dashboard)
    {
        var sb = new StringBuilder();
        sb.AppendLine("HOUSE SIMULATION REPORT");
        sb.AppendLine("=======================");
        sb.AppendLine("Daily usage: lighting and HVAC follow occupancy.");
        sb.AppendLine($"Energy consumption: moderate, efficiency {(dashboard.Count > 1 ? dashboard[1].Value : 50):0}/100.");
        sb.AppendLine("Automation workflows: morning wake-up, work focus, evening wind-down.");
        sb.AppendLine("Occupancy patterns: peaks in the evening and overnight in the bedroom.");
        return sb.ToString();
    }

    public static string DefaultFuturePlanMarkdown(IReadOnlyList<HouseRoomScore> rooms, IReadOnlyList<string> words)
    {
        string top = rooms.Count > 0 ? rooms[0].Room : "the home";
        var sb = new StringBuilder();
        sb.AppendLine("# Future Smart Home Plan");
        sb.AppendLine();
        sb.AppendLine("## Future Upgrades");
        sb.AppendLine($"Add adaptive lighting and HVAC zoning, prioritizing the {top}.");
        sb.AppendLine();
        sb.AppendLine("## Automation Expansion");
        sb.AppendLine("Expand routines to cover more rooms and times of day.");
        sb.AppendLine();
        sb.AppendLine("## New Devices");
        sb.AppendLine("Add occupancy sensors, smart blinds and energy monitors.");
        sb.AppendLine();
        sb.AppendLine("## AI Assistants");
        sb.AppendLine("Introduce a voice assistant tied to the automation engine.");
        sb.AppendLine();
        sb.AppendLine("## Robotics Integration");
        sb.AppendLine("Add a cleaning robot and a delivery/handoff station.");
        return sb.ToString();
    }

    public static string DefaultAnalysisMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<HouseRoomScore> rooms,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = rooms.Count > 0 ? rooms[0].Room : "Living Room";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Smart House Analysis Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"An adaptive smart-home profile from a 3-minute EEG. Preferred room: {top}; recurring concepts {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Automation Recommendations");
        sb.AppendLine($"Automation readiness {dashboard[2].Value:0}; automate lighting, temperature and devices per mood and routine.");
        sb.AppendLine();
        sb.AppendLine("## Energy Analysis");
        sb.AppendLine($"Energy efficiency {dashboard[1].Value:0}; off-peak scheduling and occupancy-based control reduce usage.");
        sb.AppendLine();
        sb.AppendLine("## Security Analysis");
        sb.AppendLine($"Security {dashboard[3].Value:0}; smart locks, cameras and night profiles recommended.");
        sb.AppendLine();
        sb.AppendLine("## Future Planning");
        sb.AppendLine("Expand automation, add sensors, and integrate an AI assistant and robotics over time.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<HouseRoomScore> rooms,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Room(int i) => i < rooms.Count ? $"{rooms[i].Room} ({rooms[i].Percent:0}%)" : "—";
        var concepts = NlpContent.TopWords(words, 3);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Home Preferences", new[] { Room(0), Room(1), Room(2) }),
            new("Environmental Controls", new[] { "Temperature & lighting", "Sound & ambiance" }),
            new("Device Recommendations", new[] { "Lights, plugs, thermostats", "Cameras, locks, speakers" }),
            new("Energy Optimization", new[] { $"Efficiency {dashboard[1].Value:0}", "Off-peak scheduling" }),
            new("Security Analysis", new[] { $"Security {dashboard[3].Value:0}", "Locks, cameras, alerts" }),
            new("Automation Workflows", new[] { $"Automation {dashboard[2].Value:0}", "Mood & routine based" }),
            new("Future Smart Home Plan", new[] { "New devices & sensors", "AI assistant & robotics" }),
            new("Conclusions", new[] { "EEG → adaptive home", "Comfort, energy & security" }),
        };
    }
}
