using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Supervised Learning program. Self-contained.</summary>
public static class SupervisedPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string CareerList(IReadOnlyList<CareerScore> careers, int n) =>
        string.Join(", ", careers.Take(n).Select(c => $"{c.Career} {c.Percent:0}%"));

    /// <summary>One reply with two marked sections → knowledge discovery and AI explanations.</summary>
    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<CareerScore> careers, string predicted, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a supervised-learning analyst working from a person's EEG-decoded words, extracted features and a predicted label. " +
            "Output TWO sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# KNOWLEDGE DISCOVERY  (most predictive EEG patterns, most important features, strongest cognitive indicators, hidden relationships)\n" +
            "# AI EXPLANATIONS  (why the prediction was made, important EEG features, learning patterns, model confidence)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Predicted label: {predicted}. Career strengths: {CareerList(careers, 8)}.\n" +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the two marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The supervised-learning research report — six level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<CareerScore> careers, string predicted, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a machine-learning scientist writing a supervised-learning report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Dataset Description\n## Feature Analysis\n## Model Results\n## Prediction Statistics\n" +
            "## Recommendations. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Predicted label: {predicted}. Career strengths: {CareerList(careers, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 10-slide presentation — exactly 10 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<CareerScore> careers, string predicted, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the supervised-learning analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Dataset Statistics, 3) Feature Extraction, 4) Label Distribution, 5) Model Architecture, " +
            "6) Prediction Results, 7) Evaluation Metrics, 8) Skill Predictions, 9) Knowledge Discovery, 10) Conclusions.";
        string user =
            $"Predicted label: {predicted}. Career strengths: {CareerList(careers, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
