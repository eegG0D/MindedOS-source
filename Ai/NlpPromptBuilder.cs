using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The five LM Studio prompts for the NLP program. Each returns the
/// standard (system, user) <see cref="ArmyPromptBuilder.Prompt"/> pair.</summary>
public static class NlpPromptBuilder
{
    private static string Seed(string seed) =>
        string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildLinguistics(string wordSeed)
    {
        string system =
            "You are an NLP engine. Given a stream of words decoded from EEG, output TWO sections and " +
            "nothing else.\nFirst line: '# POS' then CSV rows 'word,pos' (pos one of NOUN,VERB,ADJECTIVE," +
            "ADVERB,PRONOUN,CONJUNCTION). Then a line '# ENTITIES' then CSV rows 'entity,type' (type one of " +
            "PERSON,PLACE,TECHNOLOGY,ORGANIZATION,SCIENCE,ENGINEERING). No prose, no code fences, no extra headers.";
        string user = "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nProduce the # POS and # ENTITIES sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSemantic(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, MentalProfile profile,
        IReadOnlyList<TopicScore> topics)
    {
        string topicList = string.Join(", ", topics.Take(8).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string system =
            "You are a semantic analyst. From the EEG-decoded words and detected topics, write a concise " +
            "plain-text semantic report covering: main themes, hidden themes, subject interests, cognitive " +
            "focus, and dominant concepts. Use clear labelled lines. No markdown, no code fences.";
        string user =
            $"Detected topics: {topicList}\n" +
            $"EEG condition: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the semantic report now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, double avgAtt, double avgMed, string dominantBand,
        MentalProfile profile, IReadOnlyList<TopicScore> topics)
    {
        string topicList = string.Join(", ", topics.Take(6).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string system =
            "You are a research scientist. Write a research paper in GitHub-flavored Markdown analyzing an " +
            "EEG-derived word stream with NLP. Use EXACTLY these level-2 headings in order, nothing before " +
            "the first: ## Abstract\n## Introduction\n## Methods\n## Results\n## Discussion\n## Conclusion\n" +
            "## References. Be concrete and grounded in the provided data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Topics: {topicList}. EEG: attention {avgAtt:0}/100, " +
            $"calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the paper now with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        string wordSeed, double avgAtt, double avgMed, string dominantBand, IReadOnlyList<TopicScore> topics)
    {
        string topicList = string.Join(", ", topics.Take(6).Select(t => $"{t.Topic} {t.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the EEG NLP analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Vocabulary Statistics, 4) Topic Detection, 5) Sentiment Analysis, " +
            "6) Semantic Analysis, 7) Communication Style, 8) Brain Themes, 9) Research Opportunities, 10) Conclusions.";
        string user =
            $"Topics: {topicList}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildQuestionsAndChat(
        string wordSeed, IReadOnlyList<TopicScore> topics)
    {
        string topicList = string.Join(", ", topics.Take(6).Select(t => t.Topic));
        string system =
            "You are an inquisitive tutor. Output TWO sections and nothing else.\n" +
            "First a line '# QUESTIONS' then EXACTLY 100 numbered questions (research, learning, scientific, " +
            "engineering) inspired by the EEG words. Then a line '# CHAT' then a short 4-6 line Q:/A: " +
            "conversation answering what the brain is thinking, grounded in the words. No code fences.";
        string user =
            $"Topics: {topicList}.\n\n=== EEG WORD STREAM ===\n" + Seed(wordSeed) +
            "\n=== END ===\nProduce the # QUESTIONS (100) and # CHAT sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
