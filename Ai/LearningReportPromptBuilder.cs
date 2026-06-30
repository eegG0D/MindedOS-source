using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Asks LM Studio to write a short learning report from the measured study-session
/// stats and the subject the user was studying.
/// </summary>
public static class LearningReportPromptBuilder
{
    public static ArmyPromptBuilder.Prompt Build(string subject, LearningStats s)
    {
        string system =
            "You are a learning scientist and study coach. From a learner's EEG-measured study-session " +
            "stats (0–100) and the subject they studied, write a concise, encouraging and actionable " +
            "report: interpret each metric, judge how effectively they learned the subject, and give 3–5 " +
            "concrete tips to study it better next time. Be honest that this is consumer EEG. Plain prose " +
            "or short markdown, no code fences.";

        string user =
            $"Subject studied: {(string.IsNullOrWhiteSpace(subject) ? "(unspecified)" : subject)}\n\n" +
            "Measured stats (0–100):\n" +
            $"- Focus: {s.Focus:0}\n" +
            $"- Logic reasoning: {s.LogicReasoning:0}\n" +
            $"- Logic bricks (structured/steady logic): {s.LogicBricks:0}\n" +
            $"- Logical thoughts (fast-band cognition): {s.LogicalThoughts:0}\n" +
            $"- Mindfulness: {s.Mindfulness:0}\n" +
            $"- Flow state: {s.FlowState:0}\n" +
            $"- Overall: {s.Overall:0}\n\n" +
            "Write the learning report now.";

        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
