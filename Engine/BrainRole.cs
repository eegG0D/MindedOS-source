using System.IO;

namespace MindedOS.Engine;

/// <summary>One node in the noosphere: a user CSV, its scores and assigned role.</summary>
public sealed record RoleAssignment(string User, string Role, BrainScores Scores);

/// <summary>The result of building the noosphere from a folder of EEG CSVs.</summary>
public sealed record NoosphereResult(
    IReadOnlyList<RoleAssignment> Roles,
    string Leader,
    double Cohesion,                                  // 0–100: how interconnected/aligned the network is
    IReadOnlyDictionary<string, int> RoleCounts);

/// <summary>
/// Connects many EEG CSVs into a "noosphere" matrix, determines the most advanced
/// brain (the leader) and assigns company-style roles (Inventor, Engineer,
/// Economist, Worker) to every other node from its EEG profile.
/// </summary>
public static class Noosphere
{
    public const string Leader = "Leader";
    public const string Inventor = "Inventor";
    public const string Engineer = "Engineer";
    public const string Economist = "Economist";
    public const string Worker = "Worker";

    private static readonly string[] Order = { Leader, Inventor, Engineer, Economist, Worker };

    public static NoosphereResult Build(string folder)
    {
        var nodes = new List<(string user, BrainFeatureVector vec, BrainScores score)>();
        if (Directory.Exists(folder))
        {
            foreach (var path in Directory.EnumerateFiles(folder, "*.csv"))
            {
                try
                {
                    var vec = BrainFeatureVector.FromCsv(path);
                    var user = Path.GetFileNameWithoutExtension(path);
                    nodes.Add((user, vec, BrainScorer.Score(user, vec)));
                }
                catch { /* skip foreign csv */ }
            }
        }
        if (nodes.Count == 0)
            return new NoosphereResult(Array.Empty<RoleAssignment>(), "—", 0, new Dictionary<string, int>());

        // Most advanced brain = highest overall score → Leader.
        int leaderIdx = 0;
        for (int i = 1; i < nodes.Count; i++)
            if (nodes[i].score.Overall > nodes[leaderIdx].score.Overall) leaderIdx = i;

        var roles = new List<RoleAssignment>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            string role = i == leaderIdx ? Leader : RoleOf(nodes[i].vec);
            roles.Add(new RoleAssignment(nodes[i].user, role, nodes[i].score));
        }

        // Network cohesion: average pairwise similarity across the matrix (capped for cost).
        double cohesion = Cohesion(nodes.Select(n => n.vec).ToList());

        var counts = roles.GroupBy(r => r.Role).ToDictionary(g => g.Key, g => g.Count());
        var sorted = roles
            .OrderBy(r => System.Array.IndexOf(Order, r.Role))
            .ThenByDescending(r => r.Scores.Overall)
            .ToList();

        return new NoosphereResult(sorted, nodes[leaderIdx].user, cohesion, counts);
    }

    /// <summary>Pick the company role whose trait this brain expresses most strongly.</summary>
    private static string RoleOf(BrainFeatureVector v)
    {
        var s = v.ShareVector(); // [delta, theta, alpha, beta, gamma, att, med, blink]
        double theta = s[1], alpha = s[2], beta = s[3], gamma = s[4], att = s[5], med = s[6];

        double inventor = gamma * 2.0 + alpha * 0.8;   // creative / high-frequency synthesis
        double engineer = beta * 2.0 + att * 0.6;      // analytical, focused construction
        double economist = med * 1.2 + (alpha + theta) * 0.6; // calm, reflective, strategic
        double worker = att * 0.9 + 0.12;              // diligent baseline

        var ranked = new[]
        {
            (Inventor, inventor), (Engineer, engineer), (Economist, economist), (Worker, worker),
        };
        return ranked.OrderByDescending(p => p.Item2).First().Item1;
    }

    private static double Cohesion(IReadOnlyList<BrainFeatureVector> vecs)
    {
        int n = Math.Min(vecs.Count, 400); // cap O(n^2) for very large networks
        if (n < 2) return 100;
        double sum = 0; long pairs = 0;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                sum += ArtificialityComparer.PercentArtificial(vecs[i], vecs[j]);
                pairs++;
            }
        return pairs > 0 ? sum / pairs : 100;
    }
}
