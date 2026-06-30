using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>One agent in the fixed generic team.</summary>
public sealed record MasAgent(int Index, string Role, string Specialty, string Skew, int Priority);

/// <summary>
/// Deterministic multi-agent team: the fixed 10-agent roster, per-agent performance,
/// team coordination metrics, and the roster/task/collaboration/consensus/communication
/// CSV builders. Mirrors <see cref="ReasoningProfile"/> math. All scores are 0–100.
/// </summary>
public static class MasTeam
{
    private static double C(double v) => Math.Clamp(v, 0, 100);

    private static (double theta, double alpha, double beta, double gamma) Shares(IReadOnlyList<BandReading> bands)
    {
        double total = 0;
        foreach (var b in bands) total += b.Value;
        if (total <= 0) total = 1;
        double Share(string key)
        {
            foreach (var b in bands) if (b.Key == key) return b.Value / total;
            return 0;
        }
        return (Share("theta"), Share("lowAlpha") + Share("highAlpha"),
                Share("lowBeta") + Share("highBeta"), Share("lowGamma") + Share("midGamma"));
    }

    private static double Diversity(IReadOnlyList<string> words)
    {
        int n = words.Count;
        if (n == 0) return 0;
        return (double)words.Distinct(StringComparer.OrdinalIgnoreCase).Count() / n;
    }

    /// <summary>The fixed generic team of 10 cooperating agents (priority order).</summary>
    public static IReadOnlyList<MasAgent> Roster() => new[]
    {
        new MasAgent(1,  "Coordinator", "orchestration; task routing; synthesis",   "decisive, big-picture, keeps the team aligned", 1),
        new MasAgent(2,  "Researcher",  "knowledge gathering; prior art",           "curious, evidence-driven, cites sources",       2),
        new MasAgent(3,  "Analyst",     "data and pattern analysis",                "rigorous, quantitative, skeptical of noise",    3),
        new MasAgent(4,  "Strategist",  "long-term planning; trade-offs",           "forward-looking, prioritizes leverage",         4),
        new MasAgent(5,  "Engineer",    "system and technical design",              "pragmatic, systems-thinking, build-oriented",   5),
        new MasAgent(6,  "Designer",    "UX; structure; communication clarity",     "empathetic, user-first, simplifies",            6),
        new MasAgent(7,  "Critic",      "risk; devils-advocate; edge cases",        "contrarian, stress-tests assumptions",          7),
        new MasAgent(8,  "Implementer", "concrete execution steps",                 "action-biased, sequences the work",             8),
        new MasAgent(9,  "Tester",      "validation; QA; acceptance criteria",      "meticulous, defines done, finds gaps",          9),
        new MasAgent(10, "Documenter",  "recording; reporting; handoff",            "precise, structures knowledge for reuse",       10),
    };

    public static IReadOnlyList<(string Role, double Contribution, double Reliability, double Autonomy, double Overall)> AgentPerformance(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        var roster = Roster();
        var rows = new List<(string, double, double, double, double)>();
        for (int i = 0; i < roster.Count; i++)
        {
            double contribution = C(avgAtt * 0.3 + beta * 40 + div * 30 - i * 1.5);
            double reliability = C(avgMed * 0.3 + alpha * 40 + (1 - div) * 30 - i * 1.0);
            double autonomy = C(gamma * 50 + div * 40 + avgAtt * 0.1 + i * 0.5);
            double overall = (contribution + reliability + autonomy) / 3;
            rows.Add((roster[i].Role, contribution, reliability, autonomy, overall));
        }
        return rows;
    }

    /// <summary>Six team-level coordination metrics (0–100). Order: Cohesion, Coverage, Consensus, Throughput, Resilience, Autonomy.</summary>
    public static IReadOnlyList<(string Metric, double Value)> CoordinationMetrics(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Cohesion", C(avgMed * 0.3 + alpha * 50 + (1 - div) * 20)),
            ("Coverage", C(div * 60 + beta * 40)),
            ("Consensus", C(avgMed * 0.4 + alpha * 40 + (1 - div) * 20)),
            ("Throughput", C(avgAtt * 0.5 + beta * 40)),
            ("Resilience", C(avgMed * 0.3 + theta * 30 + alpha * 30)),
            ("Autonomy", C(gamma * 50 + div * 40 + avgAtt * 0.1)),
        };
    }

    // ---- CSV builders ----

    public static string RosterCsv(IReadOnlyList<MasAgent> agents)
    {
        var sb = new System.Text.StringBuilder("agent,role,specialty,skew,priority\n");
        foreach (var a in agents)
            sb.AppendLine($"{a.Index},{a.Role},\"{a.Specialty}\",\"{a.Skew}\",{a.Priority}");
        return sb.ToString();
    }

    public static string AgentPerformanceCsv(double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var sb = new System.Text.StringBuilder("agent,contribution,reliability,autonomy,overall\n");
        foreach (var (role, c, r, a, o) in AgentPerformance(avgAtt, avgMed, bands, words))
            sb.AppendLine($"{role},{c:0.0},{r:0.0},{a:0.0},{o:0.0}");
        return sb.ToString();
    }

    public static string CoordinationMetricsCsv(double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var sb = new System.Text.StringBuilder("metric,score\n");
        foreach (var (metric, value) in CoordinationMetrics(avgAtt, avgMed, bands, words))
            sb.AppendLine($"{metric},{value:0.0}");
        return sb.ToString();
    }

    public static string TaskAssignmentsCsv(IReadOnlyList<MasAgent> agents)
    {
        // A fixed cooperative workflow: each task is owned by the best-fit agent and depends on the prior task.
        var tasks = new (string Task, string Owner, string DependsOn)[]
        {
            ("Frame the mission", "Coordinator", "-"),
            ("Gather knowledge", "Researcher", "Frame the mission"),
            ("Analyze findings", "Analyst", "Gather knowledge"),
            ("Set strategy", "Strategist", "Analyze findings"),
            ("Design the system", "Engineer", "Set strategy"),
            ("Shape the experience", "Designer", "Design the system"),
            ("Stress-test risks", "Critic", "Shape the experience"),
            ("Implement the plan", "Implementer", "Stress-test risks"),
            ("Validate the result", "Tester", "Implement the plan"),
            ("Document and hand off", "Documenter", "Validate the result"),
        };
        var sb = new System.Text.StringBuilder("task,assigned_agent,depends_on,status\n");
        for (int i = 0; i < tasks.Length; i++)
        {
            string status = i == 0 ? "in_progress" : "pending";
            sb.AppendLine($"{tasks[i].Task},{tasks[i].Owner},{tasks[i].DependsOn},{status}");
        }
        return sb.ToString();
    }

    public static string CollaborationMatrixCsv(IReadOnlyList<MasAgent> agents, double avgAtt, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double cohesionBoost = alpha * 20;
        var sb = new System.Text.StringBuilder("agent");
        foreach (var a in agents) sb.Append(',').Append(a.Role);
        sb.Append('\n');
        for (int i = 0; i < agents.Count; i++)
        {
            sb.Append(agents[i].Role);
            for (int j = 0; j < agents.Count; j++)
            {
                double v = i == j ? 100 : C(80 - Math.Abs(i - j) * 6 + cohesionBoost);
                sb.Append(',').Append(v.ToString("0"));
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }

    public static string ConsensusAnalysisCsv(double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var m = CoordinationMetrics(avgAtt, avgMed, bands, words);
        double consensus = m[2].Value; // Consensus
        var decisions = new (string Decision, double Support)[]
        {
            ("Adopt the mission plan", consensus),
            ("Prioritize the top task", C(consensus - 5)),
            ("Allocate team resources", C(consensus - 10)),
            ("Accept the quality gate", C(consensus - 15)),
        };
        var sb = new System.Text.StringBuilder("decision,support,dissent,outcome\n");
        foreach (var (decision, support) in decisions)
        {
            double s = C(support);
            string outcome = s >= 60 ? "approved" : "revisit";
            sb.AppendLine($"{decision},{s:0},{C(100 - s):0},{outcome}");
        }
        return sb.ToString();
    }

    public static string CommunicationLogCsv(IReadOnlyList<MasAgent> agents)
    {
        var flow = new (string From, string To, string Type)[]
        {
            ("Coordinator", "All", "briefing"),
            ("Researcher", "Analyst", "findings"),
            ("Analyst", "Strategist", "analysis"),
            ("Strategist", "Engineer", "plan"),
            ("Engineer", "Implementer", "design"),
            ("Implementer", "Tester", "build"),
            ("Tester", "Critic", "results"),
            ("Critic", "Coordinator", "risks"),
            ("Documenter", "All", "summary"),
            ("Coordinator", "All", "decision"),
        };
        var sb = new System.Text.StringBuilder("step,from,to,message_type\n");
        for (int i = 0; i < flow.Length; i++)
            sb.AppendLine($"{i + 1},{flow[i].From},{flow[i].To},{flow[i].Type}");
        return sb.ToString();
    }

    // ---- history ----

    public static string HistoryHeader() =>
        "date,cohesion,coverage,consensus,throughput,resilience,autonomy,top_domain";

    public static string HistoryRow(double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words, string topDomain)
    {
        var m = CoordinationMetrics(avgAtt, avgMed, bands, words);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{m[0].Value:0},{m[1].Value:0},{m[2].Value:0},{m[3].Value:0},{m[4].Value:0},{m[5].Value:0},{topDomain}";
    }

    public static void AppendHistory(string path, double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words, string topDomain)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(avgAtt, avgMed, bands, words, topDomain));
    }
}
