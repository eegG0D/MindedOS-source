using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Reinforcement Learning content: the goal/reward-map/episode/
/// agent/simulation CSVs, the dashboard, and fallbacks for the LM artifacts (future strategy,
/// research report, 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class RlContent
{
    private static readonly string[] Challenges =
        { "Programming Challenge", "Robotics Challenge", "AI Design Challenge", "Engineering Challenge", "Research Challenge" };

    public static string GoalAlignmentCsv(IReadOnlyList<RlGoalScore> goals)
    {
        var sb = new StringBuilder("goal,alignment\n");
        foreach (var g in goals) sb.AppendLine($"{g.Goal},{g.Percent:0.0}");
        if (goals.Count == 0) sb.AppendLine("General,100.0");
        return sb.ToString();
    }

    public static string RewardMapCsv(IReadOnlyList<string> words, IReadOnlyList<(string Reward, double Value)> rewards)
    {
        var concepts = NlpContent.TopWords(words, 6);
        if (concepts.Count == 0) concepts = new List<string> { "the focus" };
        double learn = rewards.Count > 0 ? rewards[0].Value : 50;
        double focus = rewards.Count > 4 ? rewards[4].Value : 50;
        double create = rewards.Count > 1 ? rewards[1].Value : 50;
        double innov = rewards.Count > 2 ? rewards[2].Value : 50;
        var sb = new StringBuilder("concept,learning_gain,focus_gain,creativity_gain,innovation_gain\n");
        for (int i = 0; i < concepts.Count; i++)
        {
            double decay = i * 5;
            double Clamp(double v) => System.Math.Clamp(v - decay, 0, 100);
            sb.AppendLine($"{concepts[i]},{Clamp(learn):0},{Clamp(focus):0},{Clamp(create):0},{Clamp(innov):0}");
        }
        return sb.ToString();
    }

    public static string LearningEpisodesCsv(
        IReadOnlyList<string> words,
        IReadOnlyList<(string State, double Value)> states,
        IReadOnlyList<(string Action, double Value)> actions,
        IReadOnlyList<(string Reward, double Value)> rewards)
    {
        var concepts = NlpContent.TopWords(words, 7);
        if (concepts.Count == 0) concepts = new List<string> { "the focus" };
        var sb = new StringBuilder("episode,state,action,reward,outcome\n");
        for (int i = 0; i < concepts.Count; i++)
        {
            var st = states[i % states.Count];
            var ac = actions[i % actions.Count];
            var rw = rewards[i % rewards.Count];
            string outcome = rw.Value >= 60 ? "positive" : rw.Value >= 40 ? "neutral" : "negative";
            sb.AppendLine($"{i + 1},{st.State},{ac.Action},{rw.Value:0},{outcome}");
        }
        return sb.ToString();
    }

    public static string BrainAgentProfileCsv(
        IReadOnlyList<(string Score, double Value)> rlScores, IReadOnlyList<(string Reward, double Value)> rewards)
    {
        double avgReward = rewards.Count > 0 ? rewards.Average(r => r.Value) : 50;
        var sb = new StringBuilder("attribute,value\n");
        foreach (var (score, value) in rlScores) sb.AppendLine($"{score},{value:0.0}");
        sb.AppendLine($"Average Reward,{avgReward:0.0}");
        sb.AppendLine($"Policy Quality,{System.Math.Clamp((avgReward + (rlScores.Count > 0 ? rlScores[0].Value : 50)) / 2, 0, 100):0.0}");
        return sb.ToString();
    }

    public static string SimulationResultsCsv(IReadOnlyList<string> words, IReadOnlyList<(string Reward, double Value)> rewards)
    {
        double baseReward = rewards.Count > 0 ? rewards.Average(r => r.Value) : 50;
        var sb = new StringBuilder("challenge,reward,result\n");
        for (int i = 0; i < Challenges.Length; i++)
        {
            double reward = System.Math.Clamp(baseReward - i * 4 + (i % 2 == 0 ? 6 : -2), 0, 100);
            string result = reward >= 60 ? "passed" : reward >= 40 ? "partial" : "retry";
            sb.AppendLine($"{Challenges[i]},{reward:0},{result}");
        }
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<RlGoalScore> goals)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Reinforcement Learning Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Metric | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Goal Alignment");
        sb.AppendLine("| Goal | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var g in goals.Take(7)) sb.AppendLine($"| {g.Goal} | {g.Percent:0} |");
        return sb.ToString();
    }

    public static string DefaultFutureStrategy(IReadOnlyList<RlGoalScore> goals, IReadOnlyList<string> words)
    {
        string top = goals.Count > 0 ? goals[0].Goal : "your top goal";
        var concepts = NlpContent.TopWords(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("FUTURE LEARNING STRATEGY");
        sb.AppendLine("========================");
        sb.AppendLine($"Learning plan: reinforce the behaviors that earned the highest rewards, anchored in {top}.");
        sb.AppendLine($"Skill development path: build from {string.Join(" → ", concepts.DefaultIfEmpty("the basics"))} toward applied projects.");
        sb.AppendLine("Cognitive improvement: alternate exploration (new concepts) with exploitation (deliberate practice).");
        sb.AppendLine("Productivity: schedule focused sessions and review the reward map weekly to adjust the policy.");
        return sb.ToString();
    }

    public static string DefaultResearchMarkdown(
        IReadOnlyList<RlGoalScore> goals, IReadOnlyList<(string Score, double Value)> dashboard,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = goals.Count > 0 ? goals[0].Goal : "General";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Reinforcement Learning Analysis Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"This report models the user's cognition as a reinforcement-learning agent. The leading goal is {top}; recurring concepts {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Reward Analysis");
        sb.AppendLine($"Reward {dashboard[0].Value:0}/100; the highest-value actions are reinforced toward {top}.");
        sb.AppendLine();
        sb.AppendLine("## State Analysis");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}. Curiosity {dashboard[3].Value:0}, creativity {dashboard[4].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Policy Analysis");
        sb.AppendLine("The learned policy pairs each cognitive state with its highest-reward action, favoring exploration when novelty pays off and exploitation when mastery does.");
        sb.AppendLine();
        sb.AppendLine("## Trend Analysis");
        sb.AppendLine($"Across sessions, learning efficiency {dashboard[1].Value:0} and adaptability {dashboard[2].Value:0} track long-term growth.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine($"Reinforce the behaviors with the highest rewards and practice deliberately toward {top}.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<RlGoalScore> goals, IReadOnlyList<(string Score, double Value)> dashboard,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Goal(int i) => i < goals.Count ? $"{goals[i].Goal} ({goals[i].Percent:0}%)" : "—";
        var concepts = NlpContent.TopWords(words, 3);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Brain States", new[] { $"Reward {dashboard[0].Value:0}", $"Curiosity {dashboard[3].Value:0}", $"Persistence {dashboard[5].Value:0}" }),
            new("Actions Analysis", new[] { "Learning, exploring, analyzing", "Creating, planning, solving" }),
            new("Reward Analysis", new[] { $"Reward {dashboard[0].Value:0}", $"Learning efficiency {dashboard[1].Value:0}" }),
            new("Policy Generation", new[] { "State → best action", "Exploration vs exploitation" }),
            new("Goal Alignment", new[] { Goal(0), Goal(1), Goal(2) }),
            new("Learning Trends", new[] { $"Adaptability {dashboard[2].Value:0}", $"Innovation {dashboard[6].Value:0}" }),
            new("Future Strategies", new[] { "Reinforce high-reward behaviors", "Deliberate practice toward goals" }),
            new("Conclusions", new[] { "Cognition as an RL agent", "Optimize learning & creativity" }),
        };
    }
}
