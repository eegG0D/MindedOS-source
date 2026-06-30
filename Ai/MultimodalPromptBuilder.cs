using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt asking LM Studio to turn a learner's EEG-derived condition,
/// learning scores and subject interests into one Markdown analysis document with
/// a fixed set of `##` sections (so the sections can be sliced into files).
/// </summary>
public static class MultimodalPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile,
        LearningProfile scores, IReadOnlyList<SubjectScore> subjects)
    {
        string focusWord = BandInterpreter.FocusWord((int)avgAttention);
        string calmWord = BandInterpreter.CalmWord((int)avgMeditation);
        string profileDesc = MentalProfileClassifier.Describe(profile);
        string subjectList = string.Join(", ", subjects.Take(10).Select(s => $"{s.Subject} {s.Percent:0}%"));

        string system =
            "You are an expert learning scientist and personal tutor. From a learner's EEG-derived " +
            "condition, computed learning scores and decoded words, write a personalized multimodal " +
            "learning analysis in GitHub-flavored Markdown.\n" +
            "Output ONLY Markdown using EXACTLY these level-2 headings, in this order, and nothing " +
            "before the first heading:\n" +
            "## Learning Strengths\n## Learning Weaknesses\n## Preferred Learning Style\n" +
            "## Knowledge Gaps\n## Suggested Study Methods\n## Personalized Learning Path\n" +
            "## Subject Analysis\n## Curriculum\n## Knowledge Graph\n## AI Mentor\n## Future Prediction\n" +
            "Under '## Curriculum' use the sub-headings '### Beginner', '### Intermediate', '### Advanced', " +
            "each with learning objectives, recommended projects, suggested books and research topics. " +
            "Under '## Knowledge Graph' describe concepts, relationships, dependencies and learning " +
            "pathways as a bullet list. Keep it concrete and actionable. No code fences.";

        string user =
            $"=== EEG CONDITION ({accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Average attention/focus: {avgAttention:0}/100 ({focusWord})\n" +
            $"Average meditation/calm: {avgMeditation:0}/100 ({calmWord})\n" +
            $"Dominant EEG band: {dominantBand}\n" +
            $"Overall state: {profile} — {profileDesc}\n\n" +
            "=== LEARNING SCORES (0-100) ===\n" +
            $"Focus {scores.Focus:0}, Curiosity {scores.Curiosity:0}, Creativity {scores.Creativity:0}, " +
            $"Logic {scores.Logic:0}, Memory {scores.Memory:0}, Problem Solving {scores.ProblemSolving:0}, " +
            $"Flow State {scores.FlowState:0}, Learning Efficiency {scores.LearningEfficiency:0}\n\n" +
            $"=== DETECTED SUBJECT INTERESTS ===\n{subjectList}\n\n" +
            "=== WORDS DECODED FROM THE EEG ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Write the personalized learning analysis now, using exactly the required headings.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
