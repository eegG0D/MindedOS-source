using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The four LM Studio prompts for the Pattern Recognition program.</summary>
public static class PatternPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string Sig(CognitiveSignature s) =>
        string.Join(", ", s.Axes().Select(a => $"{a.Name} {a.Value:0}"));

    public static ArmyPromptBuilder.Prompt BuildHidden(
        string wordSeed, CognitiveSignature sig, IReadOnlyList<PatternTopicScore> topics)
    {
        string topicList = string.Join(", ", topics.Take(8).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string system =
            "You are a pattern-mining analyst. From an EEG-decoded word stream, detected topics and a " +
            "cognitive signature, surface HIDDEN patterns: hidden relationships, unexpected connections, " +
            "rare combinations and novel concepts. Write concise plain text with labelled lines. " +
            "No markdown, no code fences.";
        string user =
            $"Signature: {Sig(sig)}\nTopics: {topicList}\n\n=== EEG WORD STREAM ===\n" + Seed(wordSeed) +
            "\n=== END ===\nList the hidden patterns now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildFuture(
        CognitiveSignature sig, IReadOnlyList<PatternTopicScore> topics)
    {
        string topicList = string.Join(", ", topics.Take(8).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string system =
            "You are a forecasting analyst. From a cognitive signature and topic ranking, predict FUTURE " +
            "patterns: future interests, emerging topics, learning direction, research potential and " +
            "innovation potential. Concise plain text with labelled lines. No markdown, no code fences.";
        string user = $"Signature: {Sig(sig)}\nTopics: {topicList}\n\nWrite the future patterns now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, CognitiveSignature sig, IReadOnlyList<PatternTopicScore> topics,
        double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string topicList = string.Join(", ", topics.Take(6).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string system =
            "You are a research scientist writing a Pattern Recognition report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Brain Statistics\n## Pattern Analysis\n## Trend Analysis\n## Future Predictions\n" +
            "## Recommendations. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Signature: {Sig(sig)}. Topics: {topicList}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        CognitiveSignature sig, IReadOnlyList<PatternTopicScore> topics, double avgAtt, double avgMed, string dominantBand)
    {
        string topicList = string.Join(", ", topics.Take(6).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the pattern analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Word Patterns, 4) Topic Patterns, 5) Cognitive Signature, " +
            "6) Hidden Patterns, 7) Brain States, 8) Trend Analysis, 9) Forecasting, 10) Conclusions.";
        string user =
            $"Signature: {Sig(sig)}. Topics: {topicList}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\n" +
            "Write the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
