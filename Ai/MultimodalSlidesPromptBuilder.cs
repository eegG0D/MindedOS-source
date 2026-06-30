using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Builds the prompt asking LM Studio for a 10-slide multimodal-learning deck in the
/// simple "SLIDE n: title / - bullet" format that
/// <see cref="PptxArticleWriter.ParseSlides"/> turns into a .pptx.
/// </summary>
public static class MultimodalSlidesPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(
        string wordSeed, int accumulateSeconds,
        double avgAttention, double avgMeditation, string dominantBand, MentalProfile profile,
        LearningProfile scores, IReadOnlyList<SubjectScore> subjects)
    {
        string subjectList = string.Join(", ", subjects.Take(6).Select(s => $"{s.Subject} {s.Percent:0}%"));

        string system =
            "You are a learning-experience designer. From the learner's EEG condition, learning scores " +
            "and subject interests, create EXACTLY 10 slides.\n" +
            "Output EXACTLY 10 slides, each in this format and nothing else:\n" +
            "SLIDE <n>: <slide title>\n- <bullet>\n- <bullet>\n(3 to 5 concise bullets per slide). " +
            "No preamble, no closing remarks, no markdown headings — only the SLIDE blocks.\n" +
            "Use these titles in this order: 1) EEG Overview, 2) Brain Statistics, 3) Learning Style, " +
            "4) Interests, 5) Strengths, 6) Weaknesses, 7) Subject Analysis, 8) Curriculum, " +
            "9) Recommendations, 10) Future Learning Goals.";

        string user =
            $"=== EEG CONDITION ({accumulateSeconds / 60.0:0.#} min) ===\n" +
            $"Average attention {avgAttention:0}/100, average calm {avgMeditation:0}/100, " +
            $"dominant band {dominantBand}, state {profile}.\n\n" +
            "=== LEARNING SCORES (0-100) ===\n" +
            $"Focus {scores.Focus:0}, Curiosity {scores.Curiosity:0}, Creativity {scores.Creativity:0}, " +
            $"Logic {scores.Logic:0}, Memory {scores.Memory:0}, Problem Solving {scores.ProblemSolving:0}, " +
            $"Flow State {scores.FlowState:0}, Learning Efficiency {scores.LearningEfficiency:0}\n\n" +
            $"=== DETECTED SUBJECT INTERESTS ===\n{subjectList}\n\n" +
            "=== WORDS DECODED FROM THE EEG ===\n" +
            (string.IsNullOrWhiteSpace(wordSeed) ? "(no words captured)" : wordSeed) +
            "\n=== END ===\n\n" +
            "Write the 10 slides now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
