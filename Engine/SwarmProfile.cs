using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic Swarm Intelligence math: the six dashboard scores, consensus analysis, the
/// human/artificial/hybrid swarm comparison, multi-user stats and the history log. Self-contained.
/// All scores 0–100.
/// </summary>
public static class SwarmProfile
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

    private static int Distinct(IReadOnlyList<string> words) =>
        words.Where(x => x.Trim().Length > 0 && x.Trim() != "—").Distinct(StringComparer.OrdinalIgnoreCase).Count();

    /// <summary>The six headline dashboard scores (0–100). Order: Collective Intelligence, Collaboration, Innovation, Diversity, Consensus, Discovery.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Collective Intelligence", C(avgAtt * 0.4 + div * 40 + beta * 20)),
            ("Collaboration", C(div * 40 + alpha * 30 + avgMed * 0.3)),
            ("Innovation", C(alpha * 40 + gamma * 40 + div * 20)),
            ("Diversity", C(div * 100)),
            ("Consensus", C((1 - div) * 40 + avgMed * 0.3 + alpha * 30)),
            ("Discovery", C(gamma * 40 + div * 40 + theta * 20)),
        };
    }

    public static string ConsensusAnalysisCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var sb = new System.Text.StringBuilder("metric,value\n");
        sb.AppendLine($"Agreement Score,{C((1 - div) * 50 + alpha * 30 + m * 0.2):0.0}");
        sb.AppendLine($"Concept Stability,{C((1 - div) * 60 + m * 0.4):0.0}");
        sb.AppendLine($"Shared Understanding,{C(alpha * 40 + a * 0.3 + (1 - div) * 30):0.0}");
        sb.AppendLine($"Decision Confidence,{C(beta * 50 + a * 0.4 + 10):0.0}");
        return sb.ToString();
    }

    /// <summary>Human (EEG), artificial (synthetic) and hybrid swarm scores: creativity, diversity, innovation, efficiency.</summary>
    public static string SwarmComparisonCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        double hCre = C(alpha * 50 + gamma * 40 + div * 10);
        double hDiv = C(div * 100);
        double hInn = C(alpha * 40 + gamma * 40 + div * 20);
        double hEff = C(a * 0.4 + beta * 40 + 10);
        // artificial swarm: consistent, diverse, efficient by construction
        double aCre = 70, aDiv = 85, aInn = 72, aEff = 90;
        var sb = new System.Text.StringBuilder("swarm,creativity,diversity,innovation,efficiency\n");
        sb.AppendLine($"Human,{hCre:0.0},{hDiv:0.0},{hInn:0.0},{hEff:0.0}");
        sb.AppendLine($"Artificial,{aCre:0.0},{aDiv:0.0},{aInn:0.0},{aEff:0.0}");
        sb.AppendLine($"Hybrid,{(hCre + aCre) / 2:0.0},{(hDiv + aDiv) / 2:0.0},{(hInn + aInn) / 2:0.0},{(hEff + aEff) / 2:0.0}");
        return sb.ToString();
    }

    public static string MultiUserSwarmCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, IReadOnlyList<string> sharedConcepts, int priorSessions)
    {
        int users = priorSessions + 1;
        var dash = Dashboard(a, m, b, w);
        double team = dash[0].Value;
        var sb = new System.Text.StringBuilder("shared_concept,users,team_intelligence,collaboration_opportunity\n");
        if (sharedConcepts.Count == 0)
        {
            sb.AppendLine($"general,{users},{team:0.0},cross-domain synthesis");
            return sb.ToString();
        }
        string[] opps = { "co-research", "joint build", "shared study", "cross-domain synthesis", "mentorship" };
        for (int i = 0; i < sharedConcepts.Count; i++)
            sb.AppendLine($"{sharedConcepts[i]},{users},{C(team - i * 2):0.0},{opps[i % opps.Length]}");
        return sb.ToString();
    }

    // ---- swarm memory database ----

    public static string HistoryHeader() =>
        "date,agents,collective_intelligence,collaboration,innovation,diversity,consensus,discovery,top_role";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int agents, string topRole)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{agents},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{topRole}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, int agents, string topRole)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, agents, topRole));
    }
}
