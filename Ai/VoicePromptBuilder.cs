using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Voice Recognition program. Self-contained.</summary>
public static class VoicePromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string TopicList(IReadOnlyList<VoiceTopicScore> topics, int n) =>
        string.Join(", ", topics.Take(n).Select(t => $"{t.Topic} {t.Percent:0}%"));

    /// <summary>One reply with three marked sections → voice chat log, learning analysis, communication forecast.</summary>
    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<VoiceTopicScore> topics, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a voice & cognition assistant working from a person's spoken/EEG-decoded concepts. " +
            "Output THREE sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# VOICE CHAT LOG  (a short dialogue alternating 'You:' lines built from the concepts and 'Assistant:' replies that use the concepts as context)\n" +
            "# LEARNING ANALYSIS  (knowledge areas, learning interests, research interests, educational opportunities)\n" +
            "# COMMUNICATION FORECAST  (future communication strengths, leadership development, educational potential, public speaking potential)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Topics: {TopicList(topics, 10)}.\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== SPOKEN / EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the three marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The voice-recognition research report — four level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<VoiceTopicScore> topics, string style, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a communication scientist writing a voice-recognition report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Voice Statistics\n## Correlation Analysis\n## Recommendations. " +
            "Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Dominant style: {style}. Topics: {TopicList(topics, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== SPOKEN / EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 10-slide presentation — exactly 10 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<VoiceTopicScore> topics, string style, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the voice-recognition analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) Recording Overview, " +
            "2) Speech Recognition Results, 3) Voice Features, 4) Topic Analysis, 5) Communication Style, " +
            "6) Sentiment Analysis, 7) EEG Correlation, 8) Speaker Profile, 9) Forecasting, 10) Conclusions.";
        string user =
            $"Dominant style: {style}. Topics: {TopicList(topics, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
