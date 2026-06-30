using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Strong AI program.</summary>
public static class StrongAiPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    public static ArmyPromptBuilder.Prompt BuildCognition(
        string wordSeed, IReadOnlyList<StrongAiDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", domains.Take(7).Select(d => $"{d.Domain} {d.Percent:0}%"));
        string system =
            "You are a Strong-AI-inspired cognitive engine (NOT claiming human-level intelligence) working from a person's " +
            "EEG-decoded words and domains. Output FOUR sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# REASONING  (deductive conclusions, inductive patterns, abductive hypotheses)\n" +
            "# SELF REFLECTION  (thinking patterns, decision patterns, learning habits, strengths, weaknesses)\n" +
            "# CREATIVITY  (inventions, product ideas, scientific theories, engineering/architecture concepts, research directions)\n" +
            "# DECISION  (decision alternatives, risk assessments, expected outcomes)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Domains: {list}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the four marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildWorld(
        string wordSeed, IReadOnlyList<StrongAiDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", domains.Take(7).Select(d => $"{d.Domain} {d.Percent:0}%"));
        string system =
            "You are a Strong-AI-inspired cognitive engine. Output THREE sections and nothing else, each starting with its " +
            "exact marker line on its own line:\n" +
            "# AGENT REPORTS  (Markdown bullets for 10 agents: Scientist, Engineer, Architect, Programmer, Research, Strategist, Inventor, Analyst, Planner, Educator — each contributes and collaborates)\n" +
            "# WORLD MODEL  (Markdown: technology, science, society, economics, education, engineering — relationships, dependencies, trends, opportunities)\n" +
            "# FUTURE PREDICTIONS  (learning growth, research directions, technology interests, project opportunities)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Domains: {list}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the three marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildProblemSolving(
        string wordSeed, IReadOnlyList<StrongAiDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a problem-solving engine writing a report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Science\n" +
            "## Engineering\n## Programming\n## Architecture\n## Robotics\n## Business\n## Research. " +
            "Under each, give a challenge and a concrete solution/strategy. No code fences.";
        string user =
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildResearch(
        string wordSeed, IReadOnlyList<StrongAiDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", domains.Take(5).Select(d => $"{d.Domain} {d.Percent:0}%"));
        string system =
            "You are an autonomous research system writing research opportunities in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Research Opportunities\n" +
            "## Hypotheses\n## Proposed Experiments\n## Suggested Publications. Be concrete. No code fences.";
        string user =
            $"Domains: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildAnalysis(
        string wordSeed, int accumulateSeconds, IReadOnlyList<StrongAiDomainScore> domains, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string list = string.Join(", ", domains.Take(5).Select(d => $"{d.Domain} {d.Percent:0}%"));
        string system =
            "You are a cognitive scientist writing a Strong AI analysis report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Cognitive Profile\n## Reasoning Analysis\n## Memory Analysis\n## Creativity Analysis\n" +
            "## Future Opportunities. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Domains: {list}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<StrongAiDomainScore> domains, double avgAtt, double avgMed, string dominantBand)
    {
        string list = string.Join(", ", domains.Take(5).Select(d => $"{d.Domain} {d.Percent:0}%"));
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the Strong AI analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Knowledge Base, 4) Reasoning Engine, 5) Memory System, " +
            "6) Creativity Analysis, 7) Multi-Agent System, 8) World Model, 9) Future Predictions, 10) Conclusions.";
        string user =
            $"Domains: {list}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
