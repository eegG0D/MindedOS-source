using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>A task extracted from a decoded concept.</summary>
public sealed record TaskItem(string Name, string Description, string Priority, int DurationHours, int Complexity, string Category);

/// <summary>
/// Deterministic, offline-safe Task Automation content: identified tasks, categories, priorities,
/// workflows, project breakdown, schedule, tracking, virtual workforce, automation scripts, the preview
/// scorecard, and fallbacks for the LM artifacts (three narratives, two reports and a 10-slide deck).
/// Self-contained; reuses only <see cref="NlpContent"/>.
/// </summary>
public static class TaskContent
{
    private static readonly string[] Priorities = { "Critical", "High", "Medium", "Low" };
    private static readonly string[] Statuses = { "active", "active", "completed", "delayed", "failed" };

    private static readonly (string Role, string Responsibility)[] Agents =
    {
        ("Planner Agent", "decomposes goals into a plan"),
        ("Research Agent", "gathers facts and sources"),
        ("Writer Agent", "drafts text and documentation"),
        ("Engineer Agent", "designs and builds the solution"),
        ("Tester Agent", "writes and runs tests"),
        ("Reviewer Agent", "reviews quality and correctness"),
        ("Documentation Agent", "maintains docs and guides"),
        ("Deployment Agent", "ships and configures releases"),
        ("Optimization Agent", "improves speed and cost"),
        ("Quality Assurance Agent", "validates the final result"),
    };

    private static List<string> Concepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "plan", "build", "research", "write", "review", "ship" };
    }

    // ---- task extraction ----

    public static IReadOnlyList<TaskItem> BuildTasks(IReadOnlyList<string> words, TaskCategories categories, int max = 10)
    {
        var concepts = Concepts(words, max);
        var tasks = new List<TaskItem>();
        for (int i = 0; i < concepts.Count; i++)
        {
            string concept = concepts[i];
            string priority = Priorities[Math.Min(i / 3, Priorities.Length - 1)];
            int duration = 2 + (i % 5) * 2;
            int complexity = Math.Clamp(90 - i * 7, 20, 90);
            string category = categories.CategoryOf(concept);
            string name = $"Advance {concept}";
            string desc = $"Turn the recurring concept '{concept}' into a concrete {category.ToLowerInvariant()} deliverable.";
            tasks.Add(new TaskItem(name, desc, priority, duration, complexity, category));
        }
        return tasks;
    }

    public static string IdentifiedTasksCsv(IReadOnlyList<TaskItem> tasks)
    {
        var sb = new StringBuilder("task_name,description,priority,estimated_duration_hours,complexity_score,category\n");
        foreach (var t in tasks)
            sb.AppendLine($"{t.Name},{t.Description.Replace(",", ";")},{t.Priority},{t.DurationHours},{t.Complexity},{t.Category}");
        if (tasks.Count == 0) sb.AppendLine("Advance plan,Define the first deliverable,High,4,60,Personal Development");
        return sb.ToString();
    }

    public static string TaskCategoriesCsv(IReadOnlyList<TaskCategoryScore> categories)
    {
        var sb = new StringBuilder("category,count,percent\n");
        foreach (var c in categories) sb.AppendLine($"{c.Category},{c.Count},{c.Percent:0.0}");
        if (categories.Count == 0) sb.AppendLine("Personal Development,0,100.0");
        return sb.ToString();
    }

    public static string TaskPrioritiesCsv(IReadOnlyList<TaskItem> tasks)
    {
        var sb = new StringBuilder("task,urgency,importance,impact,complexity,risk,priority\n");
        for (int i = 0; i < tasks.Count; i++)
        {
            var t = tasks[i];
            int urgency = Math.Clamp(95 - i * 8, 10, 95);
            int importance = Math.Clamp(90 - i * 6, 15, 95);
            int impact = Math.Clamp(88 - i * 7, 15, 95);
            int risk = Math.Clamp(30 + (t.Complexity / 3), 10, 90);
            sb.AppendLine($"{t.Name},{urgency},{importance},{impact},{t.Complexity},{risk},{t.Priority}");
        }
        return sb.ToString();
    }

    // ---- workflows ----

    public static string WorkflowDefinitionsMd(IReadOnlyList<TaskItem> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Workflow Definitions");
        sb.AppendLine();
        for (int i = 0; i < tasks.Count; i++)
        {
            var t = tasks[i];
            string dep = i == 0 ? "none" : tasks[i - 1].Name;
            sb.AppendLine($"## {t.Name}");
            sb.AppendLine($"- **Inputs:** the concept '{t.Name.Replace("Advance ", "")}', prior context");
            sb.AppendLine($"- **Outputs:** a {t.Category.ToLowerInvariant()} deliverable");
            sb.AppendLine($"- **Dependencies:** {dep}");
            sb.AppendLine($"- **Required Resources:** {t.DurationHours}h, focus, tools");
            sb.AppendLine("- **Execution Steps:** scope → draft → build → review → ship");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // ---- project breakdown ----

    public static string ProjectBreakdownCsv(IReadOnlyList<TaskItem> tasks)
    {
        var sb = new StringBuilder("milestone,phase,subtask,deliverable\n");
        for (int i = 0; i < tasks.Count; i++)
        {
            var t = tasks[i];
            string phase = i < 2 ? "Discovery" : i < 5 ? "Build" : "Delivery";
            sb.AppendLine($"M{i + 1},{phase},{t.Name},{t.Category} artifact");
        }
        return sb.ToString();
    }

    // ---- intelligent scheduling ----

    public static string TaskScheduleCsv(IReadOnlyList<TaskItem> tasks)
    {
        var sb = new StringBuilder("task,start_date,end_date,milestone_date,completion_target\n");
        var cursor = DateTime.Now.Date;
        foreach (var t in tasks)
        {
            int days = Math.Max(1, t.DurationHours / 4);
            var start = cursor;
            var end = start.AddDays(days);
            var mid = start.AddDays(days / 2);
            sb.AppendLine($"{t.Name},{start:yyyy-MM-dd},{end:yyyy-MM-dd},{mid:yyyy-MM-dd},{end:yyyy-MM-dd}");
            cursor = end;
        }
        return sb.ToString();
    }

    // ---- continuous monitoring ----

    public static string StatusOf(int i) => Statuses[i % Statuses.Length];

    public static (int active, int completed, int delayed, int failed) StatusCounts(int count)
    {
        int active = 0, completed = 0, delayed = 0, failed = 0;
        for (int i = 0; i < count; i++)
            switch (StatusOf(i))
            {
                case "active": active++; break;
                case "completed": completed++; break;
                case "delayed": delayed++; break;
                case "failed": failed++; break;
            }
        return (active, completed, delayed, failed);
    }

    public static string TaskTrackingCsv(IReadOnlyList<TaskItem> tasks)
    {
        var sb = new StringBuilder("task,status\n");
        for (int i = 0; i < tasks.Count; i++) sb.AppendLine($"{tasks[i].Name},{StatusOf(i)}");
        return sb.ToString();
    }

    // ---- team simulation ----

    public static string VirtualWorkforceCsv(IReadOnlyList<TaskItem> tasks)
    {
        string[] roleTypes = { "Worker", "Specialist", "Manager", "Research" };
        var sb = new StringBuilder("role_type,role,assigned_task\n");
        for (int i = 0; i < tasks.Count; i++)
        {
            string roleType = roleTypes[i % roleTypes.Length];
            string role = $"{tasks[i].Category} {roleType}";
            sb.AppendLine($"{roleType},{role},{tasks[i].Name}");
        }
        return sb.ToString();
    }

    // ---- automation scripts ----

    public static IReadOnlyDictionary<string, string> AutomationScripts(IReadOnlyList<TaskItem> tasks)
    {
        string top = tasks.Count > 0 ? tasks[0].Name : "the task";
        var list = string.Join("\n", tasks.Take(8).Select(t => $"# - {t.Name} ({t.Category}, {t.DurationHours}h)"));
        var map = new Dictionary<string, string>
        {
            ["python_automation.py"] =
                "#!/usr/bin/env python3\n\"\"\"Auto-generated task runner (deterministic template).\"\"\"\n" +
                "TASKS = [\n" + string.Join("\n", tasks.Select(t => $"    {{'name': '{t.Name}', 'category': '{t.Category}', 'hours': {t.DurationHours}}},")) +
                "\n]\n\ndef run():\n    for t in TASKS:\n        print(f\"Running {t['name']} ({t['category']})\")\n\nif __name__ == '__main__':\n    run()\n",
            ["batch_script.bat"] =
                "@echo off\nREM Auto-generated task batch (template)\n" +
                string.Join("\n", tasks.Take(8).Select(t => $"echo Running {t.Name}")) + "\n",
            ["workflow_template.md"] =
                "# Workflow Template\n\n" + list + "\n\nSteps: scope -> draft -> build -> review -> ship.\n",
            ["claude_code_prompt.txt"] =
                $"You are Claude Code. Implement the following tasks in order, smallest first:\n{list}\n\n" +
                "For each: write a failing test, implement minimally, run tests, commit.\n",
            ["lmstudio_prompt.txt"] =
                $"Act as a project manager. Given these tasks, produce a day-by-day plan:\n{list}\n",
        };
        return map;
    }

    // ---- preview scorecard ----

    public static string Scorecard(IReadOnlyList<(string Score, double Value)> scores, int total, int active, int completed)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TASK AUTOMATION DASHBOARD");
        sb.AppendLine("=========================");
        sb.AppendLine($"Total tasks: {total}   ·   Active: {active}   ·   Completed: {completed}");
        foreach (var (name, value) in scores)
        {
            int filled = (int)Math.Round(value / 5.0);
            string bar = new string('█', Math.Clamp(filled, 0, 20)) + new string('░', Math.Clamp(20 - filled, 0, 20));
            sb.AppendLine($"{name,-20} {bar} {value:0}");
        }
        return sb.ToString();
    }

    // ---- LM fallbacks: narratives ----

    public static string DefaultRecommendations(IReadOnlyList<TaskItem> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TASK RECOMMENDATIONS");
        sb.AppendLine("====================");
        string start = tasks.Count > 0 ? tasks[0].Name : "the top task";
        string postpone = tasks.Count > 1 ? tasks[^1].Name : "low-priority work";
        sb.AppendLine($"Start now: {start} (highest priority).");
        sb.AppendLine($"Postpone: {postpone} (lowest urgency).");
        sb.AppendLine("Automate: repetitive build/test steps via the generated scripts.");
        sb.AppendLine("Delegate: documentation and review to the agent team.");
        sb.AppendLine("Eliminate: tasks with low impact and high complexity.");
        return sb.ToString();
    }

    public static string DefaultAgentTeam(IReadOnlyList<TaskItem> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Multi-Agent Task Team");
        sb.AppendLine();
        for (int i = 0; i < Agents.Length; i++)
        {
            var (role, resp) = Agents[i];
            string input = i == 0 ? "the goal" : Agents[i - 1].Role;
            string output = i + 1 < Agents.Length ? $"feeds {Agents[i + 1].Role}" : "final result";
            sb.AppendLine($"## {role}");
            sb.AppendLine($"- **Role:** {role}");
            sb.AppendLine($"- **Responsibilities:** {resp}");
            sb.AppendLine($"- **Inputs:** {input}");
            sb.AppendLine($"- **Outputs:** {output}");
            sb.AppendLine($"- **Dependencies:** {(i == 0 ? "none" : Agents[i - 1].Role)}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string DefaultAutomationPlans(IReadOnlyList<TaskItem> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Automation Plans");
        sb.AppendLine();
        sb.AppendLine("## Programming Tasks");
        sb.AppendLine("- Claude Code prompts, Python scripts, project specifications.");
        sb.AppendLine();
        sb.AppendLine("## Research Tasks");
        sb.AppendLine("- Research plans, data collection strategies, literature review outlines.");
        sb.AppendLine();
        sb.AppendLine("## Writing Tasks");
        sb.AppendLine("- Article outlines, report structures, documentation plans.");
        sb.AppendLine();
        sb.AppendLine("## Engineering Tasks");
        sb.AppendLine("- Design requirements, development phases, validation plans.");
        return sb.ToString();
    }

    // ---- LM fallback: project manager report (.docx) ----

    public static string DefaultProjectManagerMarkdown(IReadOnlyList<TaskItem> tasks, IReadOnlyList<(string Score, double Value)> scores)
    {
        string start = tasks.Count > 0 ? tasks[0].Name : "the top task";
        var sb = new StringBuilder();
        sb.AppendLine("# AI Project Manager Report");
        sb.AppendLine();
        sb.AppendLine("## Daily Objectives");
        sb.AppendLine($"Begin {start}; complete one execution step.");
        sb.AppendLine();
        sb.AppendLine("## Weekly Objectives");
        sb.AppendLine("Finish the Discovery phase and start Build.");
        sb.AppendLine();
        sb.AppendLine("## Monthly Objectives");
        sb.AppendLine("Deliver the top three milestones end to end.");
        sb.AppendLine();
        sb.AppendLine("## Risk Assessment");
        sb.AppendLine("Highest risk in the most complex tasks; mitigate with smaller steps and tests.");
        sb.AppendLine();
        sb.AppendLine("## Progress Summary");
        sb.AppendLine($"Productivity {scores[0].Value:0}, automation {scores[1].Value:0}, project progress {scores[2].Value:0}.");
        return sb.ToString();
    }

    // ---- LM fallback: automation report (.docx) ----

    public static string DefaultReportMarkdown(
        IReadOnlyList<TaskItem> tasks, IReadOnlyList<TaskCategoryScore> categories,
        IReadOnlyList<(string Score, double Value)> scores, double avgAtt, double avgMed, string dominantBand)
    {
        string top = categories.Count > 0 ? categories[0].Category : "Personal Development";
        var sb = new StringBuilder();
        sb.AppendLine("# Task Automation Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"{tasks.Count} tasks extracted from a 3-minute EEG; leading category {top}; productivity {scores[0].Value:0}, automation {scores[1].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Workflow Diagrams");
        sb.AppendLine("Each task flows scope → draft → build → review → ship, with dependencies chained in order.");
        sb.AppendLine();
        sb.AppendLine("## Productivity Statistics");
        sb.AppendLine($"Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}; project progress {scores[2].Value:0}, agent activity {scores[3].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Automation Opportunities");
        sb.AppendLine("Repetitive build/test/documentation steps are automated via the generated scripts and the agent team.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine("Start the highest-priority task, automate the repetitive steps, and re-run to track progress.");
        return sb.ToString();
    }

    // ---- LM fallback: 10-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<TaskItem> tasks, IReadOnlyList<TaskCategoryScore> categories,
        IReadOnlyList<(string Score, double Value)> scores, double avgAtt, double avgMed, string dominantBand)
    {
        string Cat(int i) => i < categories.Count ? $"{categories[i].Category} ({categories[i].Percent:0}%)" : "—";
        string topTask = tasks.Count > 0 ? tasks[0].Name : "—";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Task Extraction", new[] { $"{tasks.Count} tasks identified", $"Top: {topTask}" }),
            new("Task Categories", new[] { Cat(0), Cat(1), Cat(2) }),
            new("Priority Analysis", new[] { "Urgency, importance, impact", "Critical / High / Medium / Low" }),
            new("Workflow Design", new[] { "Inputs, outputs, dependencies", "scope → draft → build → review → ship" }),
            new("Agent Team Structure", new[] { "Planner, Research, Writer, Engineer", "Tester, Reviewer, Deployment, QA" }),
            new("Scheduling", new[] { "Start/end & milestone dates", "Sequenced by priority & dependencies" }),
            new("Productivity Metrics", new[] { $"Productivity {scores[0].Value:0}", $"Automation {scores[1].Value:0}" }),
            new("Automation Opportunities", new[] { "Scripts, batch, Claude Code prompts", "Delegate repetitive steps to agents" }),
            new("Conclusions", new[] { "EEG → actionable workflows", "Improves with each session" }),
        };
    }
}
