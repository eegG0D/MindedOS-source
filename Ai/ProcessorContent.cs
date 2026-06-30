using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Processor content: the multi-core/CPU-EEG/comparison
/// CSVs, the dashboard, and fallbacks for the LM artifacts (three narratives,
/// research paper, 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class ProcessorContent
{
    private static readonly string[] Cpu =
    {
        "compute", "process", "data", "execute", "cycle", "cache", "memory", "load",
        "store", "fetch", "decode", "register", "thread", "queue", "buffer", "clock",
    };

    /// <summary>The deterministic CPU word stream used for comparison.</summary>
    public static IReadOnlyList<string> CpuWords()
    {
        var list = new List<string>();
        for (int i = 0; i < 64; i++) list.Add(Cpu[i % Cpu.Length]);
        return list;
    }

    public static string CpuProcessorEegCsv()
    {
        var words = CpuWords();
        var sb = new StringBuilder("index,word\n");
        for (int i = 0; i < words.Count; i++) sb.AppendLine($"{i},{words[i]}");
        return sb.ToString();
    }

    public static string MulticoreBrainCsv(IReadOnlyList<CoreScore> cores, IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 12);
        var sb = new StringBuilder("core,load_percent,assigned_concepts\n");
        for (int i = 0; i < cores.Count; i++)
        {
            // round-robin the top concepts across the cores so each core gets some
            var assigned = new List<string>();
            for (int j = i; j < concepts.Count; j += cores.Count) assigned.Add(concepts[j]);
            string list = assigned.Count > 0 ? string.Join(" ", assigned) : "(none)";
            sb.AppendLine($"{cores[i].Core},{cores[i].Percent:0.0},\"{list}\"");
        }
        if (cores.Count == 0) sb.AppendLine("Logic,100.0,\"(none)\"");
        return sb.ToString();
    }

    public static string ProcessorComparisonCsv(
        IReadOnlyList<(string Score, double Value)> human, IReadOnlyList<(string Score, double Value)> cpu)
    {
        // human = user's dashboard; cpu = dashboard computed from the CPU word stream; ai = fixed profile.
        double[] ai = { 92, 95, 90, 88, 90, 85, 88 };
        var sb = new StringBuilder("aspect,human,cpu,ai\n");
        for (int i = 0; i < human.Count; i++)
        {
            double cpuVal = i < cpu.Count ? cpu[i].Value : 0;
            sb.AppendLine($"{human[i].Score},{human[i].Value:0},{cpuVal:0},{ai[i % ai.Length]:0}");
        }
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<CoreScore> cores)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Processor Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Score | Value |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Brain Cores");
        sb.AppendLine("| Core | Load % |");
        sb.AppendLine("| --- | --- |");
        foreach (var c in cores.Take(6)) sb.AppendLine($"| {c.Core} | {c.Percent:0} |");
        return sb.ToString();
    }

    public static string DefaultTaskProcessing(IReadOnlyList<CoreScore> cores, IReadOnlyList<string> words)
    {
        string top = cores.Count > 0 ? cores[0].Core : "the brain";
        var concepts = NlpContent.TopWords(words, 5);
        var sb = new StringBuilder();
        sb.AppendLine("TASK PROCESSING REPORT");
        sb.AppendLine("======================");
        sb.AppendLine($"Engineering problems: processed structurally, leaning on the {top} core.");
        sb.AppendLine("Scientific problems: hypothesis-first, evidence-driven.");
        sb.AppendLine("Mathematical problems: sequential, rule-based steps.");
        sb.AppendLine($"Creative projects: exploratory, recombining {string.Join(", ", concepts)}.");
        sb.AppendLine("Research challenges: question-led, iterative investigation.");
        return sb.ToString();
    }

    public static string DefaultBottleneck(IReadOnlyList<CoreScore> cores, IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("BOTTLENECK REPORT");
        sb.AppendLine("=================");
        sb.AppendLine("Processing bottlenecks: context switching between unrelated concepts.");
        sb.AppendLine("Cognitive overload areas: high concept diversity without consolidation.");
        sb.AppendLine($"Repeated interruptions: the recurring concepts {string.Join(", ", concepts)} compete for focus.");
        sb.AppendLine("Inefficient transitions: jumps between distant topics slow throughput.");
        return sb.ToString();
    }

    public static string DefaultOptimization(IReadOnlyList<CoreScore> cores)
    {
        string top = cores.Count > 0 ? cores[0].Core : "your strongest core";
        var sb = new StringBuilder();
        sb.AppendLine("PROCESSOR OPTIMIZATION");
        sb.AppendLine("======================");
        sb.AppendLine("Improving focus: single-task in short, timed blocks.");
        sb.AppendLine("Improving reasoning: practice structured decomposition.");
        sb.AppendLine("Improving throughput: reduce context switching; batch similar work.");
        sb.AppendLine("Improving memory utilization: spaced review and consolidation.");
        sb.AppendLine($"Improving task execution: lean on the {top} core and offload the rest.");
        return sb.ToString();
    }

    public static string DefaultResearchMarkdown(
        IReadOnlyList<CoreScore> cores, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = cores.Count > 0 ? cores[0].Core : "Logic";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Processor Analysis Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"The brain processes like a {top}-dominant core, with recurring inputs {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Processing Statistics");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}. Leading cores: {string.Join(", ", cores.Take(3).Select(c => $"{c.Core} {c.Percent:0}%"))}.");
        sb.AppendLine();
        sb.AppendLine("## Throughput & Speed");
        sb.AppendLine("Throughput reflects concepts processed per minute; speed reflects fast-band activity.");
        sb.AppendLine();
        sb.AppendLine("## Logic & Memory");
        sb.AppendLine("Logic is driven by fast bands; memory by slow (theta) activity and calm.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine("Reduce context switching; batch similar tasks; consolidate with spaced review.");
        sb.AppendLine();
        sb.AppendLine("## Conclusions");
        sb.AppendLine($"As an information processor, this brain favors the {top} core.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<CoreScore> cores, IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = NlpContent.TopWords(words, 3);
        string top = cores.Count > 0 ? cores[0].Core : "Logic";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Input Processing", new[] { "Input complexity & density", "Concept diversity", "Processing load" }),
            new("Throughput Analysis", new[] { "Concepts & thoughts per minute", "Processing efficiency" }),
            new("Logic Processing", new[] { "Logical & sequential reasoning", "Analytical depth" }),
            new("Memory Processing", new[] { "Working memory", "Concept retention & recall" }),
            new("Scheduler Analysis", new[] { "Priority assignment", "Task switching & allocation" }),
            new("Bottleneck Detection", new[] { "Context-switching cost", "Overload areas" }),
            new("Optimization", new[] { "Reduce switching", "Consolidate memory", $"Lean on {top} core" }),
            new("Conclusions", new[] { "Brain as a processor", "Where to optimize next" }),
        };
    }
}
