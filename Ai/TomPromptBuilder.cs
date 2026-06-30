using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Theory of Mind program. Self-contained. All outputs are framed as hypotheses.</summary>
public static class TomPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string IntentList(IReadOnlyList<TomIntentScore> intents, int n) =>
        string.Join(", ", intents.Take(n).Select(i => $"{i.Intent} {i.Percent:0}%"));

    private const string Frame =
        "Treat every inference as a PROBABILISTIC HYPOTHESIS and SIMULATION, never as a verified fact about anyone's thoughts. ";

    /// <summary>One reply with two marked sections → perspective simulations and cognitive scenarios.</summary>
    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<TomIntentScore> intents, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a Theory of Mind engine working from a person's EEG-decoded concepts. " + Frame +
            "Output TWO sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# PERSPECTIVE SIMULATIONS  (for a topic from the EEG, give the hypothetical viewpoint of each: Scientist, Engineer, Entrepreneur, Researcher, Educator, Inventor, Designer, AI System, then compare)\n" +
            "# COGNITIVE SCENARIOS  (probable reasoning paths for: solving a scientific problem, leading a project, designing a robot, creating a business, learning a new skill, conducting research)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Inferred intents (hypotheses): {IntentList(intents, 9)}.\n" +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the two marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The Theory of Mind research report — five level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<TomIntentScore> intents, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a cognitive scientist writing a Theory of Mind report in GitHub-flavored Markdown. " + Frame +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Perspective Analysis\n## Goal Analysis\n## Belief Structure Analysis\n## Recommendations. " +
            "State clearly that all models are hypotheses. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Inferred intents: {IntentList(intents, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 10-slide presentation — exactly 10 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<TomIntentScore> intents, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the Theory of Mind analysis. " + Frame +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Goals and Intentions, 4) Perspective Analysis, 5) Belief Structures, " +
            "6) Decision Styles, 7) Social Cognition, 8) Scenario Simulations, 9) Human vs AI Comparison, 10) Conclusions.";
        string user =
            $"Inferred intents: {IntentList(intents, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
