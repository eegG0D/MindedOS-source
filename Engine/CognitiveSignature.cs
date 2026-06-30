using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>A brain's 8-axis cognitive signature (each 0–100).</summary>
public sealed record CognitiveSignature(
    double Logic, double Creativity, double Focus, double Curiosity,
    double Innovation, double Exploration, double Consistency, double Adaptability)
{
    private static double C(double v) => Math.Clamp(v, 0, 100);

    /// <summary>The eight axes in canonical order (name + value).</summary>
    public IReadOnlyList<(string Name, double Value)> Axes() => new (string, double)[]
    {
        ("Logic", Logic), ("Creativity", Creativity), ("Focus", Focus), ("Curiosity", Curiosity),
        ("Innovation", Innovation), ("Exploration", Exploration), ("Consistency", Consistency), ("Adaptability", Adaptability),
    };

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
        return (Share("theta"),
                Share("lowAlpha") + Share("highAlpha"),
                Share("lowBeta") + Share("highBeta"),
                Share("lowGamma") + Share("midGamma"));
    }

    public static CognitiveSignature Compute(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        int n = words.Count;
        int distinct = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        double diversity = n > 0 ? (double)distinct / n : 0;

        return new CognitiveSignature(
            Logic: C(beta * 90 + avgAtt * 0.4),
            Creativity: C(alpha * 70 + gamma * 50 + diversity * 30),
            Focus: C(avgAtt),
            Curiosity: C(diversity * 60 + theta * 40),
            Innovation: C(gamma * 80 + diversity * 30),
            Exploration: C(diversity * 80 + theta * 20),
            Consistency: C((1 - diversity) * 70 + avgMed * 0.3),
            Adaptability: C(alpha * 50 + beta * 30 + diversity * 40));
    }

    /// <summary>Cosine similarity between two signatures' axis vectors, scaled to 0–100.</summary>
    public static double Similarity(CognitiveSignature a, CognitiveSignature b)
    {
        var va = a.Axes(); var vb = b.Axes();
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < va.Count; i++)
        {
            dot += va[i].Value * vb[i].Value;
            na += va[i].Value * va[i].Value;
            nb += vb[i].Value * vb[i].Value;
        }
        if (na <= 0 || nb <= 0) return 0;
        return C(dot / (Math.Sqrt(na) * Math.Sqrt(nb)) * 100);
    }

    /// <summary>Nine brain-state scores (0–100) derived from the signature + att/calm.</summary>
    public static IReadOnlyList<(string State, double Score)> BrainStates(
        CognitiveSignature s, double avgAtt, double avgMed) => new (string, double)[]
    {
        ("Highly Focused", C(s.Focus)),
        ("Creative", C(s.Creativity)),
        ("Analytical", C(s.Logic)),
        ("Relaxed", C(avgMed)),
        ("Curious", C(s.Curiosity)),
        ("Exploratory", C(s.Exploration)),
        ("Learning", C((s.Curiosity + s.Focus) / 2)),
        ("Problem Solving", C((s.Logic + s.Focus) / 2)),
        ("Innovative", C(s.Innovation)),
    };

    public static (string State, double Score) DominantState(CognitiveSignature s, double avgAtt, double avgMed) =>
        BrainStates(s, avgAtt, avgMed).OrderByDescending(x => x.Score).First();

    /// <summary>Fixed archetype signatures for clustering and synthetic comparison.</summary>
    public static IReadOnlyList<(string Name, CognitiveSignature Sig)> Archetypes() => new (string, CognitiveSignature)[]
    {
        ("Scientist",    new CognitiveSignature(90, 60, 80, 85, 70, 75, 70, 60)),
        ("Engineer",     new CognitiveSignature(85, 55, 80, 60, 65, 50, 80, 65)),
        ("Inventor",     new CognitiveSignature(70, 90, 65, 85, 95, 85, 50, 80)),
        ("Entrepreneur", new CognitiveSignature(65, 75, 70, 80, 85, 80, 55, 90)),
        ("Designer",     new CognitiveSignature(55, 90, 65, 75, 80, 80, 55, 75)),
        ("Leader",       new CognitiveSignature(70, 65, 80, 70, 70, 65, 75, 85)),
        ("Researcher",   new CognitiveSignature(85, 70, 85, 90, 75, 85, 70, 65)),
        ("Educator",     new CognitiveSignature(75, 70, 75, 75, 60, 60, 80, 80)),
    };

    /// <summary>The archetype most similar to the given signature.</summary>
    public static string NearestArchetype(CognitiveSignature s) =>
        Archetypes().OrderByDescending(a => Similarity(s, a.Sig)).First().Name;

    public static string SignatureCsv(CognitiveSignature s)
    {
        var sb = new System.Text.StringBuilder("axis,score\n");
        foreach (var (name, value) in s.Axes()) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string BrainStatesCsv(CognitiveSignature s, double avgAtt, double avgMed)
    {
        var states = BrainStates(s, avgAtt, avgMed);
        double max = states.Max(x => x.Score);
        var sb = new System.Text.StringBuilder("state,score,dominant\n");
        foreach (var (state, score) in states)
            sb.AppendLine($"{state},{score:0.0},{(score >= max ? "yes" : "no")}");
        return sb.ToString();
    }

    public static string SyntheticComparisonCsv(CognitiveSignature s)
    {
        var sb = new System.Text.StringBuilder("archetype,similarity,closest_axis\n");
        foreach (var (name, sig) in Archetypes())
        {
            double sim = Similarity(s, sig);
            var ua = s.Axes(); var aa = sig.Axes();
            string closest = ua[0].Name; double best = double.MaxValue;
            for (int i = 0; i < ua.Count; i++)
            {
                double d = Math.Abs(ua[i].Value - aa[i].Value);
                if (d < best) { best = d; closest = ua[i].Name; }
            }
            sb.AppendLine($"{name},{sim:0.0},{closest}");
        }
        return sb.ToString();
    }
}
