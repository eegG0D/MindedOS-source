using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Semi-Supervised Learning program.</summary>
public static class SemiSupPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildDiscoveries(
        string wordSeed, IReadOnlyList<SemiSupCategoryScore> categories, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", categories.Take(10).Select(c => $"{c.Category} {c.Percent:0}%"));
        string system =
            "You are a semi-supervised learning research assistant working from a person's EEG-decoded words and " +
            "detected categories. Output TWO sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# CONCEPT DISCOVERY  (emerging interests, hidden topics, unrecognized concepts, novel combinations)\n" +
            "# AI DISCOVERIES  (for the unknown patterns: possible meanings, reasoning, estimated probability, validation methods)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Detected categories: {list}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the two marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<SemiSupCategoryScore> categories, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", categories.Take(6).Select(c => $"{c.Category} {c.Percent:0}%"));
        string system =
            "You are a machine-learning researcher writing a Semi-Supervised Learning report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Learning Statistics\n## Classification Analysis\n## Discovery Analysis\n" +
            "## Future Recommendations. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Categories: {list}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<SemiSupCategoryScore> categories, double avgAtt, double avgMed, string dominantBand)
    {
        string list = string.Join(", ", categories.Take(6).Select(c => $"{c.Category} {c.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the semi-supervised learning analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Labeled Data Analysis, 4) Unlabeled Data Analysis, 5) Brain Clusters, " +
            "6) Concept Discovery, 7) Knowledge Expansion, 8) Predictions, 9) Future Learning, 10) Conclusions.";
        string user =
            $"Categories: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
