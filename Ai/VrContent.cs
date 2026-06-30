using System.Text;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Virtual Reality content: virtual characters, AI companions, EEG→VR
/// control mappings, the preview scorecard, and fallbacks for the LM artifacts (the world / creative /
/// innovation narrative bundles, a report and a 10-slide deck). Self-contained; reuses only
/// <see cref="NlpContent"/>.
/// </summary>
public static class VrContent
{
    private static readonly string[] CharacterTypes =
        { "Scientist", "Engineer", "Inventor", "Explorer", "Researcher", "AI Entity", "Robot", "Citizen" };

    private static readonly string[] EnvironmentTypes =
    {
        "Futuristic City", "Space Colony", "Research Facility", "Underwater Civilization", "Ancient Civilization",
        "Fantasy World", "Smart Megacity", "Floating City", "Robotics Laboratory", "Artificial Intelligence World",
    };

    private static readonly string[] VrActions =
        { "Move Forward", "Move Backward", "Turn Left", "Turn Right", "Select Object", "Open Menu", "Interact", "Teleport" };

    private static List<string> Concepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "city", "robot", "energy", "explore", "design", "future" };
    }

    public static string Theme(IReadOnlyList<string> words)
    {
        var c = Concepts(words, 1);
        return EnvironmentTypes[Math.Abs(c[0].GetHashCode()) % EnvironmentTypes.Length];
    }

    // ---- characters ----

    public static string VirtualCharactersCsv(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 8);
        var sb = new StringBuilder("name,type,biography,personality,skills,goals,role\n");
        for (int i = 0; i < CharacterTypes.Length; i++)
        {
            string type = CharacterTypes[i];
            string concept = concepts[i % concepts.Count];
            string name = $"{type.Split(' ')[0]}-{(i + 1):00}";
            string bio = $"A {type.ToLowerInvariant()} shaped by '{concept}'".Replace(",", ";");
            string personality = new[] { "curious", "bold", "methodical", "visionary", "calm", "driven" }[i % 6];
            string skills = $"{concept}; problem-solving";
            string goals = $"advance {concept} in the world";
            string role = $"{type} of the {concept} district";
            sb.AppendLine($"{name},{type},{bio},{personality},{skills},{goals},{role}");
        }
        return sb.ToString();
    }

    // ---- AI companions ----

    public static string AiCompanionsCsv(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 6);
        string[] names = { "ARIA", "NOVA", "ORION", "ECHO", "SAGE" };
        var sb = new StringBuilder("name,personality,knowledge_domain,skills,behaviors,dialogue_example\n");
        for (int i = 0; i < names.Length; i++)
        {
            string concept = concepts[i % concepts.Count];
            string personality = new[] { "warm", "analytical", "playful", "stoic", "wise" }[i % 5];
            string domain = concept;
            string skills = $"guidance; {concept} expertise";
            string behaviors = "assists; explains; adapts";
            string dialogue = $"\"\"Let me show you the {concept} sector.\"\"";
            sb.AppendLine($"{names[i]},{personality},{domain},{skills},{behaviors},{dialogue}");
        }
        return sb.ToString();
    }

    // ---- EEG-controlled VR mappings ----

    public static string VrControlsCsv(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 8);
        var sb = new StringBuilder("eeg_pattern,concept,vr_action\n");
        for (int i = 0; i < VrActions.Length; i++)
        {
            string concept = concepts[i % concepts.Count];
            sb.AppendLine($"EEG_{(i + 1):00},{concept},{VrActions[i]}");
        }
        return sb.ToString();
    }

    // ---- preview scorecard ----

    public static string Scorecard(IReadOnlyList<(string Score, double Value)> dashboard, string theme)
    {
        var sb = new StringBuilder();
        sb.AppendLine("VIRTUAL REALITY DASHBOARD");
        sb.AppendLine("=========================");
        sb.AppendLine($"Generated world theme: {theme}");
        foreach (var (name, value) in dashboard)
        {
            int filled = (int)Math.Round(value / 5.0);
            string bar = new string('█', Math.Clamp(filled, 0, 20)) + new string('░', Math.Clamp(20 - filled, 0, 20));
            sb.AppendLine($"{name,-18} {bar} {value:0}");
        }
        return sb.ToString();
    }

    // ---- LM fallbacks: world bundle ----

    public static string DefaultVirtualWorlds(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("VIRTUAL WORLDS");
        sb.AppendLine("==============");
        for (int i = 0; i < EnvironmentTypes.Length; i++)
            sb.AppendLine($"- {EnvironmentTypes[i]}: themed around {concepts[i % concepts.Count]}.");
        return sb.ToString();
    }

    public static string DefaultWorldBlueprints(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 3);
        string focus = concepts[0];
        var sb = new StringBuilder();
        sb.AppendLine("# World Blueprints");
        sb.AppendLine();
        foreach (var section in new[] { "Geography", "Buildings", "Transportation", "Technology", "Economy", "Society", "Infrastructure", "Energy Systems", "Communication Systems" })
        {
            sb.AppendLine($"## {section}");
            sb.AppendLine($"Designed around '{focus}', the {section.ToLowerInvariant()} blends futuristic and functional design.");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string DefaultVrExperiences(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("VR EXPERIENCES");
        sb.AppendLine("==============");
        foreach (var e in new[] { "Scientific Discovery Missions", "Space Exploration", "Engineering Challenges", "AI Research Simulations", "Educational Experiences", "Historical Reconstructions", "Innovation Workshops" })
            sb.AppendLine($"- {e}: built from '{concepts[0]}', interactive and immersive.");
        return sb.ToString();
    }

    // ---- LM fallbacks: creative bundle ----

    public static string DefaultArchitecturePrompts(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 3);
        string focus = concepts[0];
        var sb = new StringBuilder();
        sb.AppendLine("ARCHITECTURE PROMPTS");
        sb.AppendLine("====================");
        foreach (var engine in new[] { "Blender", "Unreal Engine", "Unity", "Godot" })
            sb.AppendLine($"- {engine}: model a {focus}-themed city with research labs and a transportation network; PBR materials, modular kit, day/night lighting.");
        return sb.ToString();
    }

    public static string DefaultVrStory(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("# VR Story");
        sb.AppendLine();
        sb.AppendLine($"## Main Storyline");
        sb.AppendLine($"The player explores a world built on '{concepts[0]}' and must restore its core system.");
        sb.AppendLine();
        sb.AppendLine("## Side Quests");
        foreach (var c in concepts) sb.AppendLine($"- Investigate the {c} district.");
        sb.AppendLine();
        sb.AppendLine("## Missions");
        sb.AppendLine("- Repair, discover, and unite the districts.");
        sb.AppendLine();
        sb.AppendLine("## Challenges");
        sb.AppendLine("- Puzzles and engineering tasks gate progress.");
        sb.AppendLine();
        sb.AppendLine("## Exploration Objectives");
        sb.AppendLine("- Map every district and unlock the AI core.");
        return sb.ToString();
    }

    public static string DefaultTrainingWorlds(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 2);
        var sb = new StringBuilder();
        sb.AppendLine("TRAINING WORLDS");
        sb.AppendLine("===============");
        foreach (var subject in new[] { "Artificial Intelligence", "Robotics", "Programming", "Engineering", "Neuroscience", "Architecture", "Mathematics" })
            sb.AppendLine($"- {subject}: a hands-on simulation themed around '{concepts[0]}'.");
        return sb.ToString();
    }

    public static string DefaultMetaquestPrompts(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 3);
        string focus = concepts[0];
        var sb = new StringBuilder();
        sb.AppendLine("META QUEST VR PROMPTS");
        sb.AppendLine("=====================");
        foreach (var target in new[] { "Meta Quest", "Unreal Engine VR", "Unity XR", "Godot XR" })
            sb.AppendLine($"- {target}: build an immersive {focus} world with hand-tracking, teleport locomotion, 72Hz+ performance budget, and interactive {concepts[Math.Min(1, concepts.Count - 1)]} objects.");
        return sb.ToString();
    }

    // ---- LM fallbacks: innovation bundle ----

    public static string DefaultInnovationSimulations(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("INNOVATION SIMULATIONS");
        sb.AppendLine("======================");
        foreach (var s in new[] { "New Technologies", "Scientific Breakthroughs", "Engineering Concepts", "Smart Cities", "Future Transportation" })
            sb.AppendLine($"- {s}: a future simulation seeded by '{concepts[0]}'.");
        return sb.ToString();
    }

    public static string DefaultEmotionalWorlds(IReadOnlyList<string> words, double avgAtt, double avgMed)
    {
        var concepts = Concepts(words, 2);
        var sb = new StringBuilder();
        sb.AppendLine("EMOTIONAL WORLDS");
        sb.AppendLine("================");
        sb.AppendLine($"(attention {avgAtt:0}/100, calm {avgMed:0}/100)");
        foreach (var e in new[] { "Calm", "Productive", "Creative", "Exploration", "Scientific" })
            sb.AppendLine($"- {e} Environment: tuned to your state, themed around '{concepts[0]}'.");
        return sb.ToString();
    }

    public static string DefaultSharedWorlds(IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("SHARED VIRTUAL WORLDS");
        sb.AppendLine("=====================");
        sb.AppendLine($"Shared interests: {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine("Common themes: exploration and innovation recur across sessions.");
        sb.AppendLine("Collaborative opportunities: co-build the shared districts with other users.");
        return sb.ToString();
    }

    // ---- LM fallback: research report (.docx) ----

    public static string DefaultReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<string> words,
        double avgAtt, double avgMed, string dominantBand, string theme)
    {
        var concepts = Concepts(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Virtual Reality Analysis");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A VR world generated from a 3-minute EEG. Theme: {theme}; world complexity {dashboard[0].Value:0}, immersion {dashboard[5].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## World Analysis");
        sb.AppendLine($"The world is built around {string.Join(", ", concepts.Take(3))}; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Character Analysis");
        sb.AppendLine("Eight character archetypes (scientist, engineer, inventor, explorer, researcher, AI entity, robot, citizen) populate the world.");
        sb.AppendLine();
        sb.AppendLine("## Experience Analysis");
        sb.AppendLine($"VR experiences span discovery, exploration and innovation; creativity {dashboard[1].Value:0}, exploration {dashboard[2].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Future Applications");
        sb.AppendLine($"Export the prompts to Unreal, Unity, Godot and Meta Quest; educational value {dashboard[4].Value:0}.");
        return sb.ToString();
    }

    // ---- LM fallback: 10-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<string> words,
        double avgAtt, double avgMed, string dominantBand, string theme)
    {
        var concepts = Concepts(words, 3);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", $"Recurring: {string.Join(", ", concepts)}" }),
            new("World Generation", new[] { $"Theme: {theme}", $"Complexity {dashboard[0].Value:0}" }),
            new("Character Generation", new[] { "Scientist, Engineer, Inventor, Explorer", "Researcher, AI Entity, Robot, Citizen" }),
            new("VR Experiences", new[] { "Discovery, exploration, challenges", $"Exploration {dashboard[2].Value:0}" }),
            new("Educational Worlds", new[] { "AI, robotics, programming", $"Educational value {dashboard[4].Value:0}" }),
            new("AI Companions", new[] { "ARIA, NOVA, ORION, ECHO, SAGE", "Personalities & dialogue" }),
            new("Innovation Simulations", new[] { $"Innovation {dashboard[3].Value:0}", "Smart cities, future transport" }),
            new("Future Applications", new[] { "Unreal, Unity, Godot", "Meta Quest VR prompts" }),
            new("Conclusions", new[] { "EEG → immersive VR worlds", $"Immersion {dashboard[5].Value:0}" }),
        };
    }
}
