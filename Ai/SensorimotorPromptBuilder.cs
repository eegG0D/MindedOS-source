using MindedOS.Core;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Sensorimotor Learning program.</summary>
public static class SensorimotorPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string DashLine(IReadOnlyList<(string Score, double Value)> dashboard) =>
        string.Join(", ", dashboard.Select(d => $"{d.Score} {d.Value:0}"));

    public static ArmyPromptBuilder.Prompt BuildTraining(
        string wordSeed, IReadOnlyList<(string Score, double Value)> dashboard, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a sensorimotor learning coach working from a person's EEG-decoded words and dashboard. " +
            "Write TRAINING RECOMMENDATIONS in plain text with these labeled parts: Skill improvement suggestions; " +
            "Motor training exercises; Reaction improvement plans; Coordination exercises; Focus enhancement techniques. " +
            "Be concrete and actionable. No markdown headings, no code fences.";
        string user =
            $"Dashboard: {DashLine(dashboard)}\nEEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the training recommendations now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<(string Score, double Value)> dashboard, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a sensorimotor researcher writing a learning report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Sensor Analysis\n## Motor Analysis\n## Coordination Assessment\n## Learning Assessment\n" +
            "## Recommendations. Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Dashboard: {DashLine(dashboard)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<(string Score, double Value)> dashboard, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the sensorimotor learning analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Translation Results, 3) Sensory Processing, 4) Motor Planning, 5) Coordination Profile, " +
            "6) Learning Assessment, 7) BMI Control, 8) Adaptation Analysis, 9) Recommendations, 10) Conclusions.";
        string user =
            $"Dashboard: {DashLine(dashboard)}. EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
