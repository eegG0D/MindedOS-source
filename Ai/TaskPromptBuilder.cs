using MindedOS.Core;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>The LM Studio prompts for the Task Automation program. Self-contained.</summary>
public static class TaskPromptBuilder
{
    private static string Seed(string seed) => string.IsNullOrWhiteSpace(seed) ? "(no words captured)" : seed;

    private static string CategoryList(IReadOnlyList<TaskCategoryScore> categories, int n) =>
        string.Join(", ", categories.Take(n).Select(c => $"{c.Category} {c.Percent:0}%"));

    /// <summary>One reply with three marked sections → recommendations, agent team, automation plans.</summary>
    public static ArmyPromptBuilder.Prompt BuildNarratives(
        string wordSeed, IReadOnlyList<TaskCategoryScore> categories, int taskCount, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are a task-automation engine working from a person's EEG-decoded concepts and extracted tasks. " +
            "Output THREE sections and nothing else, each starting with its exact marker line on its own line:\n" +
            "# TASK RECOMMENDATIONS  (tasks to start now, to postpone, to automate, to delegate, to eliminate)\n" +
            "# AGENT TEAM  (Markdown for 10 agents: Planner, Research, Writer, Engineer, Tester, Reviewer, Documentation, Deployment, Optimization, Quality Assurance — each with Role, Responsibilities, Inputs, Outputs, Dependencies)\n" +
            "# AUTOMATION PLANS  (Markdown with ## Programming Tasks, ## Research Tasks, ## Writing Tasks, ## Engineering Tasks)\n" +
            "No preamble before the first marker. No code fences.";
        string user =
            $"Tasks: {taskCount}. Categories: {CategoryList(categories, 10)}.\n" +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the three marked sections now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The AI project manager report — five level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildProjectManager(
        string wordSeed, IReadOnlyList<TaskCategoryScore> categories, int taskCount, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are an AI project manager writing a report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Daily Objectives\n" +
            "## Weekly Objectives\n## Monthly Objectives\n## Risk Assessment\n## Progress Summary. " +
            "Be concrete and actionable. No code fences.";
        string user =
            $"Tasks: {taskCount}. Categories: {CategoryList(categories, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The task-automation research report — five level-2 headings, Markdown → Verdana .docx.</summary>
    public static ArmyPromptBuilder.Prompt BuildReport(
        string wordSeed, int accumulateSeconds, IReadOnlyList<TaskCategoryScore> categories, int taskCount, double avgAtt, double avgMed, string dominantBand, MentalProfile profile)
    {
        string system =
            "You are an automation analyst writing a task-automation report in GitHub-flavored Markdown. " +
            "Use EXACTLY these level-2 headings in order, nothing before the first: ## Executive Summary\n" +
            "## Workflow Diagrams\n## Productivity Statistics\n## Automation Opportunities\n## Recommendations. " +
            "Be concrete and grounded in the data. No code fences.";
        string user =
            $"Session: {accumulateSeconds / 60.0:0.#} min. Tasks: {taskCount}. Categories: {CategoryList(categories, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}, state {profile}.\n\n" +
            "=== EEG WORD STREAM ===\n" + Seed(wordSeed) + "\n=== END ===\nWrite the report with the exact headings.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }

    /// <summary>The 10-slide presentation — exactly 10 SLIDE blocks with the spec titles.</summary>
    public static ArmyPromptBuilder.Prompt BuildSlides(
        IReadOnlyList<TaskCategoryScore> categories, int taskCount, double avgAtt, double avgMed, string dominantBand)
    {
        string system =
            "You are a presentation designer. Create EXACTLY 10 slides from the task-automation analysis.\n" +
            "Output EXACTLY 10 slides, each: 'SLIDE <n>: <title>' then '- bullet' lines (3-5 bullets). " +
            "No preamble, no markdown headings — only SLIDE blocks.\nTitles in order: 1) EEG Overview, " +
            "2) Task Extraction, 3) Task Categories, 4) Priority Analysis, 5) Workflow Design, " +
            "6) Agent Team Structure, 7) Scheduling, 8) Productivity Metrics, 9) Automation Opportunities, 10) Conclusions.";
        string user =
            $"Tasks: {taskCount}. Categories: {CategoryList(categories, 5)}. " +
            $"EEG: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.\n\nWrite the 10 slides now.";
        return new ArmyPromptBuilder.Prompt(system, user);
    }
}
