using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Perception program.</summary>
public static class PerceptionPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<PerceptionScore> topics, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string topicList = string.Join(", ", topics.Take(8).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string system =
            "You analyze how a person perceives the world from their EEG-decoded words. Output FIVE sections " +
            "and nothing else, each starting with its exact marker line on its own line:\n" +
            "# IMAGINATION  (imagined structures, environments, technologies, inventions, worlds)\n" +
            "# MENTAL MODELS  (Markdown with ## Reality, ## Technology, ## Science, ## Society, ## Innovation, ## Learning)\n" +
            "# SITUATIONAL  (how they'd interpret a business problem, scientific challenge, engineering project, social interaction, learning opportunity)\n" +
            "# FUTURE VISION  (future interests, desired innovations, preferred technologies, long-term aspirations)\n" +
            "# KNOWLEDGE  (most important concepts, hidden interests, emerging ideas, untapped learning opportunities)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Detected interests: {topicList}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the five marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, IReadOnlyList<PerceptionScore> topics, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string topicList = string.Join(", ", topics.Take(6).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string system =
            "You are a cognitive scientist writing a Perception analysis report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Perception Statistics\n## Mental Model Analysis\n## Attention & Awareness\n## Future Vision\n" +
            "## Recommendations. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Interests: {topicList}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<PerceptionScore> topics, double avgAtt, double avgMed, string dominantBand)
    {
        string topicList = string.Join(", ", topics.Take(6).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the perception analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Awareness Analysis, 4) Attention Analysis, 5) Perception Profile, " +
            "6) Mental Models, 7) Visual Imagination, 8) Future Vision, 9) Artificial Comparison, 10) Conclusions.";
        string user =
            $"Interests: {topicList}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildImage(string wordSeed)
    {
        string system =
            "You compare an image to a person's EEG-decoded concepts. Look at the image, then output ONE line " +
            "of CSV and nothing else: three integers 0-100 separated by commas — concept_match,visual_interest_alignment,perception_consistency. " +
            "No header, no prose, no code fences.";
        string user = "EEG concepts: " + Seed(wordSeed) + "\nScore the image now as `match,alignment,consistency`.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
