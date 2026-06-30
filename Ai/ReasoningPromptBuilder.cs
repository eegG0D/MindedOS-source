using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Reasoning program.</summary>
public static class ReasoningPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<ReasoningSubjectScore> subjects, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", subjects.Take(9).Select(s => $"{s.Subject} {s.Percent:0}%"));
        string system =
            "You are a reasoning analyst working from a person's EEG-decoded words and detected subjects. " +
            "Output FOUR sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# ARGUMENT  (premises, supporting evidence, conclusions, counterarguments, logical consistency)\n" +
            "# DECISION PATHWAYS  (Markdown: ## Inputs, ## Influencing Factors, ## Alternatives Considered, ## Final Conclusions)\n" +
            "# CRITICAL THINKING  (skepticism, fact evaluation, bias detection, alternative perspectives, risk assessment)\n" +
            "# FUTURE FORECAST  (predicted future strengths in research, engineering, science, leadership, entrepreneurship, innovation)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Detected subjects: {list}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the four marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, int accumulateSeconds, IReadOnlyList<ReasoningSubjectScore> subjects, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", subjects.Take(6).Select(s => $"{s.Subject} {s.Percent:0}%"));
        string system =
            "You are a cognitive scientist writing a Reasoning analysis report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Reasoning Statistics\n## Logic Analysis\n## Decision Analysis\n## Recommendations\n" +
            "## Future Development Areas. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Subjects: {list}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<ReasoningSubjectScore> subjects, double avgAtt, double avgMed, string dominantBand)
    {
        string list = string.Join(", ", subjects.Take(6).Select(s => $"{s.Subject} {s.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the reasoning analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Logic Assessment, 4) Problem Solving, 5) Critical Thinking, " +
            "6) Decision Pathways, 7) Scientific Reasoning, 8) Innovation Analysis, 9) Future Forecast, 10) Conclusions.";
        string user =
            $"Subjects: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
