using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Unsupervised Learning program. Self-contained.</summary>
public static class UnsupPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string TopicList(IReadOnlyList<UnsupTopicScore> topics, int n) =>
        string.Join(", ", topics.Take(n).Select(t => $"{t.Topic} {t.Percent:0}%"));

    /// <summary>One reply with two marked sections → emergent behaviors and rare patterns.</summary>
    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<UnsupTopicScore> topics, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are an unsupervised-learning engine that discovers hidden structure in a person's EEG-decoded " +
            "concepts WITHOUT predefined labels. Output TWO sections and nothing else, each starting with its " +
            "exact marker line on its own line:\n" +
            "# EMERGENT BEHAVIORS  (higher-level behaviors emerging from the clusters and topics: inventive thinking, systems thinking, strategic reasoning, deep curiosity, interdisciplinary thinking)\n" +
            "# RARE PATTERNS  (highly unusual cognitive states, rare thought patterns, potential breakthrough moments, signal anomalies)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Discovered latent topics: {TopicList(topics, 10)}.\n" +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the two marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The unsupervised-learning research report — six level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<UnsupTopicScore> topics, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a data scientist writing an unsupervised-learning report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Clustering Results\n## Topic Discovery Results\n## Similarity Analysis\n## Anomaly Findings\n" +
            "## Emergent Behavior Insights. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Topics: {TopicList(topics, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 10-slide presentation — exactly 10 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<UnsupTopicScore> topics, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the unsupervised-learning analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Feature Extraction, 4) Clustering Results, 5) Topic Discovery, " +
            "6) Similarity Network, 7) Anomaly Detection, 8) Emergent Behaviors, 9) Multi-User Communities, 10) Conclusions.";
        string user =
            $"Topics: {TopicList(topics, 5)}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
