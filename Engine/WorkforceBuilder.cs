using System.Text;

namespace MindedOS.Engine;

/// <summary>One agent in the augmented workforce.</summary>
public sealed record WorkAgent(int Index, string Id, string Guild, string Role, string Job);

/// <summary>A generated augmented workforce: a diagram, a deploy prompt and the roster.</summary>
public sealed record Workforce(
    string Title,
    string MermaidDiagram,
    string DeployPrompt,
    IReadOnlyList<WorkAgent> Agents);

/// <summary>
/// Deterministically builds an "augmented workforce" of N agents (200 by default)
/// for Claude Code: guilds × roles × focus areas (the focus areas are seeded from
/// the EEG-decoded words), a Mermaid org diagram, and a paste-ready Claude Code
/// deployment prompt. Always yields exactly N agents — no LLM needed.
/// </summary>
public static class WorkforceBuilder
{
    private static readonly (string Guild, string[] Roles)[] Guilds =
    {
        ("Engineering", new[] { "Architect", "Implementer", "Reviewer", "Refactorer", "Debugger", "Optimizer", "Integrator" }),
        ("Research",     new[] { "Researcher", "Analyst", "Prototyper", "Literature Scout", "Experimenter" }),
        ("Data",         new[] { "Data Engineer", "ML Engineer", "Pipeline Builder", "Labeler", "Evaluator" }),
        ("Design",       new[] { "UX Designer", "UI Engineer", "Interaction Designer", "Accessibility Auditor" }),
        ("Product",      new[] { "Product Planner", "Roadmapper", "Spec Writer", "Prioritizer" }),
        ("QA",           new[] { "Test Engineer", "Fuzzer", "Regression Hunter", "Coverage Analyst" }),
        ("Security",     new[] { "Security Auditor", "Threat Modeler", "Pentester", "Secrets Scanner" }),
        ("DevOps",       new[] { "Release Manager", "CI Engineer", "Infra Provisioner", "Observability Engineer" }),
        ("Docs",         new[] { "Technical Writer", "API Documenter", "Tutorial Author", "Changelog Keeper" }),
        ("Growth",       new[] { "Growth Engineer", "SEO Analyst", "Telemetry Analyst", "Experiment Runner" }),
        ("Finance",      new[] { "Cost Optimizer", "Budget Analyst", "Usage Auditor" }),
        ("Operations",   new[] { "Scheduler", "Coordinator", "Incident Responder", "Knowledge Keeper" }),
    };

    private static readonly Dictionary<string, string> RoleJob = new()
    {
        ["Architect"] = "designs the architecture and module boundaries for {f}",
        ["Implementer"] = "writes and ships the implementation of {f}",
        ["Reviewer"] = "reviews changes and enforces standards across {f}",
        ["Refactorer"] = "untangles and simplifies the code in {f}",
        ["Debugger"] = "reproduces and fixes defects in {f}",
        ["Optimizer"] = "profiles and speeds up {f}",
        ["Integrator"] = "wires {f} into the rest of the system",
        ["Researcher"] = "investigates approaches and trade-offs for {f}",
        ["Analyst"] = "analyzes requirements and metrics for {f}",
        ["Prototyper"] = "builds throwaway prototypes to de-risk {f}",
        ["Test Engineer"] = "writes and maintains the tests for {f}",
        ["Fuzzer"] = "fuzzes inputs to harden {f}",
        ["Security Auditor"] = "audits {f} for OWASP-class vulnerabilities",
        ["Threat Modeler"] = "threat-models the attack surface of {f}",
        ["Technical Writer"] = "documents {f} for developers",
        ["Release Manager"] = "cuts and ships releases that include {f}",
        ["CI Engineer"] = "keeps CI green for {f}",
        ["Data Engineer"] = "builds the data pipeline behind {f}",
        ["ML Engineer"] = "trains and evaluates the model powering {f}",
        ["Cost Optimizer"] = "reduces the running cost of {f}",
        ["Scheduler"] = "schedules and sequences work on {f}",
        ["Coordinator"] = "coordinates the agents working on {f}",
    };

    public static Workforce Build(string wordSeed, int target = 200)
    {
        var focuses = FocusAreas(wordSeed);
        var pairs = new List<(string Guild, string Role)>();
        foreach (var (guild, roles) in Guilds)
            foreach (var role in roles)
                pairs.Add((guild, role));

        var agents = new List<WorkAgent>(target);
        for (int i = 0; i < target; i++)
        {
            var (guild, role) = pairs[i % pairs.Count];
            var focus = focuses[i % focuses.Count];
            string job = (RoleJob.TryGetValue(role, out var tmpl) ? tmpl : "handles " + role.ToLowerInvariant() + " duties for {f}")
                .Replace("{f}", focus);
            agents.Add(new WorkAgent(i + 1, $"AGT-{i + 1:D3}", guild, role, job));
        }

        var title = $"Augmented Workforce — {target} Agents for Claude Code";
        return new Workforce(title, Diagram(agents), DeployPrompt(agents.Count), agents);
    }

    private static List<string> FocusAreas(string wordSeed)
    {
        var focuses = new List<string>
        {
            "the core engine", "the API layer", "the data pipeline", "authentication", "the UI",
            "performance", "the test suite", "documentation", "CI/CD", "telemetry", "error handling",
            "the database", "caching", "the build system", "accessibility",
        };
        var words = (wordSeed ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 4).Distinct().Take(10).ToList();
        for (int i = words.Count - 1; i >= 0; i--) focuses.Insert(0, $"the '{words[i]}' module");
        return focuses;
    }

    private static string Diagram(IReadOnlyList<WorkAgent> agents)
    {
        var byGuild = agents.GroupBy(a => a.Guild).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("graph TD");
        sb.AppendLine("  ORCH[\"Orchestrator (you in Claude Code)\"]");
        foreach (var g in byGuild)
        {
            string id = Slug(g.Key);
            sb.AppendLine($"  ORCH --> {id}[\"{g.Key} Guild · {g.Count()} agents\"]");
            foreach (var role in g.Select(a => a.Role).Distinct())
                sb.AppendLine($"  {id} --> {id}_{Slug(role)}[\"{role}\"]");
        }
        return sb.ToString().TrimEnd();
    }

    private static string DeployPrompt(int count)
    {
        return
            $"You are the Orchestrator of an augmented workforce of {count} agents (full roster below).\n" +
            "Deploy them with the Task/subagent tool, in dependency order, not all at once:\n" +
            "1. Spin up the Architects first; have them define module boundaries and a shared plan.\n" +
            "2. Dispatch Implementers in parallel batches (5–10 at a time), one subagent per agent, each\n" +
            "   given ONLY its role + job from the roster and the relevant files.\n" +
            "3. Gate every Implementer's output behind a Reviewer and a Test Engineer subagent.\n" +
            "4. Run Security Auditors and Optimizers on the merged result; Technical Writers document it.\n" +
            "5. The Operations guild (Scheduler/Coordinator) tracks progress and reports back to you.\n" +
            "Process the roster top-to-bottom by guild priority (Engineering → QA → Security → Docs → rest).\n" +
            "Keep each subagent scoped to its single job; collect results and summarize. Begin with the\n" +
            "Architects now.";
    }

    public static string DefaultElaboration(Workforce wf) =>
        $"An augmented workforce turns one operator into an organization. Instead of writing every line " +
        $"yourself, you orchestrate {wf.Agents.Count} specialized agents — grouped into guilds like " +
        "Engineering, Research, Security and Operations — each owning a single, well-scoped job. " +
        "Architects set the boundaries, Implementers build in parallel, Reviewers and Test Engineers " +
        "gate quality, and the Operations guild keeps the whole swarm coordinated. Deployed inside " +
        "Claude Code as subagents and driven by a local LM Studio model, the workforce scales your " +
        "intent across a codebase the way a company scales a founder's vision.";

    public static string ToMarkdown(Workforce wf, string elaboration)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {wf.Title}").AppendLine();
        sb.AppendLine(elaboration.Trim()).AppendLine();

        sb.AppendLine("## Workforce diagram").AppendLine();
        sb.AppendLine("```mermaid").AppendLine(wf.MermaidDiagram).AppendLine("```").AppendLine();

        sb.AppendLine("## Deployment prompt (paste into Claude Code)").AppendLine();
        sb.AppendLine("```text").AppendLine(wf.DeployPrompt).AppendLine("```").AppendLine();

        sb.AppendLine($"## Agent roster ({wf.Agents.Count})").AppendLine();
        sb.AppendLine("| # | Agent | Guild | Role | Job |");
        sb.AppendLine("|---|-------|-------|------|-----|");
        foreach (var a in wf.Agents)
            sb.AppendLine($"| {a.Index} | {a.Id} | {a.Guild} | {a.Role} | {a.Job} |");
        return sb.ToString();
    }

    private static string Slug(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s) if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.Length == 0 ? "N" : sb.ToString();
    }
}
