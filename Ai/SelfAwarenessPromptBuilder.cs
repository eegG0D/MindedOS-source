using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Self-Awareness AI program.</summary>
public static class SelfAwarenessPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<CuriosityDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", domains.Take(10).Select(d => $"{d.Domain} {d.Percent:0}%"));
        string system =
            "You are an AI-assisted self-reflection guide (NOT claiming consciousness) working from a person's " +
            "EEG-decoded words and curiosity domains. Output SIX sections and nothing else, each starting with its " +
            "exact marker line on its own line:\n" +
            "# SELF PROFILE  (Markdown: ## Current Interests, ## Cognitive Style, ## Learning Style, ## Preferred Subjects, ## Problem-Solving Approach)\n" +
            "# IDENTITY  (Markdown bullets: interests, motivations, values, preferred domains, cognitive tendencies)\n" +
            "# INTERNAL DIALOGUE  (questions to explore, topics to research, new ideas, alternative viewpoints)\n" +
            "# PROJECTS  (Markdown bullets: technical, research, creative, educational, engineering projects)\n" +
            "# REFLECTION JOURNAL  (a short journal entry: current themes, recent interests, future directions, growth)\n" +
            "# MENTOR  (a mentor note: learning paths, projects, development tracking, next reflection)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Curiosity domains: {list}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the six marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<CuriosityDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", domains.Take(6).Select(d => $"{d.Domain} {d.Percent:0}%"));
        string system =
            "You are a personal-development analyst writing a Self-Awareness report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Strength Analysis\n## Growth Analysis\n## Goal Analysis\n## Historical Trends\n" +
            "## Recommendations. Be supportive, concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Curiosity domains: {list}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<CuriosityDomainScore> domains, double avgAtt, double avgMed, string dominantBand)
    {
        string list = string.Join(", ", domains.Take(6).Select(d => $"{d.Domain} {d.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the self-awareness analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Cognitive Metrics, 4) Interests, 5) Strengths, " +
            "6) Growth Opportunities, 7) Goals, 8) Historical Trends, 9) Future Directions, 10) Conclusions.";
        string user =
            $"Curiosity domains: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
