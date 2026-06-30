using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Processor program.</summary>
public static class ProcessorPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<CoreScore> cores, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", cores.Take(6).Select(c => $"{c.Core} {c.Percent:0}%"));
        string system =
            "You model a person's brain as an information processor, working from their EEG-decoded words and " +
            "active brain cores. Output THREE sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# TASK PROCESSING  (how this processor would handle engineering, scientific, mathematical, creative and research tasks)\n" +
            "# BOTTLENECKS  (processing bottlenecks, cognitive overload areas, repeated interruptions, inefficient transitions)\n" +
            "# OPTIMIZATION  (recommendations to improve focus, reasoning, throughput, memory utilization and task execution)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Active cores: {list}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the three marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, IReadOnlyList<CoreScore> cores, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", cores.Take(6).Select(c => $"{c.Core} {c.Percent:0}%"));
        string system =
            "You are a cognitive engineer writing a Processor analysis report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Processing Statistics\n## Throughput & Speed\n## Logic & Memory\n## Recommendations\n" +
            "## Conclusions. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Cores: {list}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<CoreScore> cores, double avgAtt, double avgMed, string dominantBand)
    {
        string list = string.Join(", ", cores.Take(6).Select(c => $"{c.Core} {c.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the processor analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Input Processing, 4) Throughput Analysis, 5) Logic Processing, " +
            "6) Memory Processing, 7) Scheduler Analysis, 8) Bottleneck Detection, 9) Optimization, 10) Conclusions.";
        string user =
            $"Cores: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
