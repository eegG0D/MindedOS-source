using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Unsupervised Learning content. This app ships no ML library, so the
/// clustering, PCA/t-SNE/UMAP projections, topic modeling, similarity, anomaly, association mining and
/// community detection are deterministic SIMULATIONS computed from a stable per-concept pseudo-embedding
/// and word frequencies. Also holds fallbacks for the LM artifacts (two narratives, a report and a
/// 10-slide deck). Self-contained; reuses only <see cref="NlpContent"/>.
/// </summary>
public static class UnsupContent
{
    private static readonly string[] States =
        { "Highly Focused", "Creative", "Analytical", "Relaxed", "Exploratory", "Innovative" };

    private static readonly string[] Roles =
        { "Explorer", "Innovator", "Analyst", "Connector", "Visionary" };

    private const int Dim = 5;

    private static List<string> TopConcepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "concept", "idea", "signal", "pattern", "topic", "state" };
    }

    /// <summary>A stable 5-dim pseudo-embedding from the concept's characters (deterministic, 0..1 per dim).</summary>
    private static double[] Embed(string concept)
    {
        var v = new double[Dim];
        var w = concept.Trim().ToLowerInvariant();
        for (int i = 0; i < w.Length; i++) v[i % Dim] += w[i];
        double max = v.Max();
        if (max <= 0) max = 1;
        for (int k = 0; k < Dim; k++) v[k] = v[k] / max;
        return v;
    }

    private static int StableHash(string concept)
    {
        int h = 0;
        foreach (var ch in concept.Trim().ToLowerInvariant()) h = (h * 31 + ch) & 0x7fffffff;
        return h;
    }

    private static string ClusterOf(string concept) => States[StableHash(concept) % States.Length];

    private static double Cosine(double[] a, double[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int k = 0; k < Dim; k++) { dot += a[k] * b[k]; na += a[k] * a[k]; nb += b[k] * b[k]; }
        if (na <= 0 || nb <= 0) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    private static Dictionary<string, int> Freq(IReadOnlyList<string> words)
    {
        var f = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var x = w.Trim().ToLowerInvariant();
            if (x.Length == 0 || x == "—") continue;
            f[x] = f.TryGetValue(x, out var c) ? c + 1 : 1;
        }
        return f;
    }

    // ---- feature extraction ----

    public static string TextFeaturesCsv(IReadOnlyList<string> words)
    {
        var freq = Freq(words);
        var sb = new StringBuilder("word,frequency,weight\n");
        foreach (var (word, count) in freq.OrderByDescending(kv => kv.Value).Take(15))
            sb.AppendLine($"{word},{count},{(double)count / Math.Max(words.Count, 1):0.000}");
        if (freq.Count == 0) sb.AppendLine("concept,1,1.000");
        return sb.ToString();
    }

    public static string EmbeddingVectorsCsv(IReadOnlyList<string> words)
    {
        var concepts = TopConcepts(words, 12);
        var sb = new StringBuilder("concept,d1,d2,d3,d4,d5\n");
        foreach (var c in concepts)
        {
            var v = Embed(c);
            sb.AppendLine($"{c},{v[0]:0.000},{v[1]:0.000},{v[2]:0.000},{v[3]:0.000},{v[4]:0.000}");
        }
        return sb.ToString();
    }

    // ---- clustering ----

    public static string ClusterAssignmentsCsv(IReadOnlyList<string> words)
    {
        var concepts = TopConcepts(words, 12);
        var sb = new StringBuilder("concept,cluster,distance\n");
        foreach (var c in concepts)
        {
            var v = Embed(c);
            double distance = Math.Round(1 - v.Max(), 3); // distance to its cluster centroid (proxy)
            sb.AppendLine($"{c},{ClusterOf(c)},{distance:0.000}");
        }
        return sb.ToString();
    }

    public static string ClusterStatisticsCsv(IReadOnlyList<string> words, UnsupTopics topics)
    {
        var concepts = TopConcepts(words, 12);
        var sb = new StringBuilder("cluster,size,share_percent,dominant_topic\n");
        foreach (var state in States)
        {
            var members = concepts.Where(c => ClusterOf(c) == state).ToList();
            string dom = members.Count > 0
                ? members.Select(topics.TopicOf).GroupBy(t => t).OrderByDescending(g => g.Count()).First().Key
                : "—";
            double share = concepts.Count > 0 ? 100.0 * members.Count / concepts.Count : 0;
            sb.AppendLine($"{state},{members.Count},{share:0.0},{dom}");
        }
        return sb.ToString();
    }

    // ---- dimensionality reduction (deterministic 2D projections) ----

    public static string ProjectionCsv(IReadOnlyList<string> words, string method)
    {
        var concepts = TopConcepts(words, 12);
        var sb = new StringBuilder("concept,x,y,cluster\n");
        foreach (var c in concepts)
        {
            var v = Embed(c);
            // each method uses a different deterministic 2D transform of the embedding
            double x, y;
            switch (method)
            {
                case "tsne": x = v[1] - v[3]; y = v[2] - v[4]; break;
                case "umap": x = (v[0] + v[2]) / 2 - v[4]; y = (v[1] + v[3]) / 2 - v[0]; break;
                default: x = v[0] - v[2]; y = v[1] - v[3]; break; // pca
            }
            sb.AppendLine($"{c},{x:0.000},{y:0.000},{ClusterOf(c)}");
        }
        return sb.ToString();
    }

    // ---- topic discovery ----

    public static string LatentTopicsCsv(IReadOnlyList<UnsupTopicScore> topics)
    {
        var sb = new StringBuilder("topic,weight,percent\n");
        foreach (var t in topics)
        {
            string weight = t.Percent >= 20 ? "High" : t.Percent >= 8 ? "Medium" : "Low";
            sb.AppendLine($"{t.Topic},{weight},{t.Percent:0.0}");
        }
        return sb.ToString();
    }

    public static string TopicKeywordsCsv(IReadOnlyList<UnsupTopicScore> topics, IReadOnlyList<string> words, UnsupTopics topicObj)
    {
        var concepts = TopConcepts(words, 16);
        var sb = new StringBuilder("topic,keywords\n");
        foreach (var t in topics.Take(6))
        {
            var hits = concepts.Where(c => topicObj.TopicOf(c) == t.Topic).Distinct().Take(5).ToList();
            if (hits.Count == 0) hits = topicObj.KeywordsOf(t.Topic).Take(5).ToList();
            sb.AppendLine($"{t.Topic},{string.Join(" ", hits)}");
        }
        return sb.ToString();
    }

    public static string TopicDistributionsCsv(IReadOnlyList<string> words, UnsupTopics topicObj)
    {
        var concepts = TopConcepts(words, 12);
        var sb = new StringBuilder("concept,topic,weight\n");
        foreach (var c in concepts)
        {
            var v = Embed(c);
            sb.AppendLine($"{c},{topicObj.TopicOf(c)},{v.Max():0.000}");
        }
        return sb.ToString();
    }

    // ---- similarity analysis ----

    public static string SimilarityMatrixCsv(IReadOnlyList<string> words)
    {
        var concepts = TopConcepts(words, 8);
        var emb = concepts.ToDictionary(c => c, Embed);
        var sb = new StringBuilder("concept," + string.Join(",", concepts) + "\n");
        foreach (var a in concepts)
        {
            var row = concepts.Select(b => Cosine(emb[a], emb[b]).ToString("0.000"));
            sb.AppendLine($"{a}," + string.Join(",", row));
        }
        return sb.ToString();
    }

    public static string NearestNeighborsCsv(IReadOnlyList<string> words)
    {
        var concepts = TopConcepts(words, 10);
        var emb = concepts.ToDictionary(c => c, Embed);
        var sb = new StringBuilder("concept,neighbor_1,neighbor_2,neighbor_3\n");
        foreach (var a in concepts)
        {
            var nn = concepts.Where(b => b != a).OrderByDescending(b => Cosine(emb[a], emb[b])).Take(3).ToList();
            while (nn.Count < 3) nn.Add("—");
            sb.AppendLine($"{a},{nn[0]},{nn[1]},{nn[2]}");
        }
        return sb.ToString();
    }

    // ---- anomaly detection ----

    public static string AnomalyScoresCsv(IReadOnlyList<string> words)
    {
        var freq = Freq(words);
        int max = freq.Count > 0 ? freq.Values.Max() : 1;
        var sb = new StringBuilder("concept,frequency,anomaly_score,flag\n");
        foreach (var (word, count) in freq.OrderBy(kv => kv.Value).Take(15))
        {
            double score = Math.Round(1.0 - (double)count / max, 3); // rarer → higher
            string flag = score >= 0.8 ? "anomaly" : "normal";
            sb.AppendLine($"{word},{count},{score:0.000},{flag}");
        }
        if (freq.Count == 0) sb.AppendLine("concept,1,0.000,normal");
        return sb.ToString();
    }

    // ---- association mining ----

    public static string AssociationRulesCsv(IReadOnlyList<string> words)
    {
        var clean = words.Select(w => w.Trim().ToLowerInvariant()).Where(w => w.Length > 0 && w != "—").ToList();
        var pairs = new Dictionary<(string, string), int>();
        for (int i = 0; i + 1 < clean.Count; i++)
        {
            var key = (clean[i], clean[i + 1]);
            pairs[key] = pairs.TryGetValue(key, out var c) ? c + 1 : 1;
        }
        var freq = Freq(words);
        int n = Math.Max(clean.Count, 1);
        var sb = new StringBuilder("antecedent,consequent,support,confidence\n");
        foreach (var ((aWord, bWord), count) in pairs.OrderByDescending(p => p.Value).Take(12))
        {
            double support = (double)count / n;
            double confidence = freq.TryGetValue(aWord, out var af) && af > 0 ? (double)count / af : 0;
            sb.AppendLine($"{aWord},{bWord},{support:0.000},{confidence:0.000}");
        }
        if (pairs.Count == 0) sb.AppendLine("concept,concept,0.000,0.000");
        return sb.ToString();
    }

    public static string ConceptNetworkCsv(IReadOnlyList<string> words)
    {
        var concepts = TopConcepts(words, 10);
        var emb = concepts.ToDictionary(c => c, Embed);
        var sb = new StringBuilder("source,target,weight\n");
        for (int i = 0; i < concepts.Count; i++)
            for (int j = i + 1; j < concepts.Count; j++)
            {
                double w = Cosine(emb[concepts[i]], emb[concepts[j]]);
                if (w >= 0.85) sb.AppendLine($"{concepts[i]},{concepts[j]},{w:0.000}");
            }
        if (concepts.Count >= 2) sb.AppendLine($"{concepts[0]},{concepts[1]},{Cosine(emb[concepts[0]], emb[concepts[1]]):0.000}");
        return sb.ToString();
    }

    // ---- multi-user network (synthetic from session count) ----

    public static string UserClustersCsv(int priorSessions)
    {
        int users = priorSessions + 1;
        var sb = new StringBuilder("user,cluster\n");
        for (int i = 0; i < users; i++) sb.AppendLine($"session_{i + 1},{States[i % States.Length]}");
        return sb.ToString();
    }

    public static string CommunityNetworkCsv(int priorSessions)
    {
        int users = priorSessions + 1;
        var sb = new StringBuilder("source,target,weight\n");
        if (users < 2) { sb.AppendLine("session_1,session_1,1.000"); return sb.ToString(); }
        for (int i = 0; i + 1 < users; i++)
            sb.AppendLine($"session_{i + 1},session_{i + 2},{0.9 - 0.05 * i:0.000}");
        return sb.ToString();
    }

    public static string EmergentRolesCsv(int priorSessions)
    {
        int users = priorSessions + 1;
        var sb = new StringBuilder("user,role\n");
        for (int i = 0; i < users; i++) sb.AppendLine($"session_{i + 1},{Roles[i % Roles.Length]}");
        return sb.ToString();
    }

    // ---- preview scorecard ----

    public static string Scorecard(IReadOnlyList<(string Score, double Value)> dashboard, int clusters, string dominantTopic)
    {
        var sb = new StringBuilder();
        sb.AppendLine("UNSUPERVISED LEARNING DASHBOARD");
        sb.AppendLine("===============================");
        sb.AppendLine($"Clusters discovered: {clusters}   ·   Dominant topic: {dominantTopic}");
        foreach (var (name, value) in dashboard)
        {
            int filled = (int)Math.Round(value / 5.0);
            string bar = new string('█', Math.Clamp(filled, 0, 20)) + new string('░', Math.Clamp(20 - filled, 0, 20));
            sb.AppendLine($"{name,-22} {bar} {value:0}");
        }
        return sb.ToString();
    }

    // ---- LM fallbacks: narratives ----

    public static string DefaultEmergentBehaviors(IReadOnlyList<UnsupTopicScore> topics, IReadOnlyList<string> words)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "the data";
        var concepts = TopConcepts(words, 3);
        var sb = new StringBuilder();
        sb.AppendLine("EMERGENT BEHAVIORS");
        sb.AppendLine("==================");
        sb.AppendLine($"Inventive thinking: novel combinations emerge around {concepts[0]}.");
        sb.AppendLine($"Systems thinking: the clusters interconnect through {top}.");
        sb.AppendLine("Strategic reasoning: high-frequency concepts anchor decisions.");
        sb.AppendLine("Deep curiosity: rare concepts signal exploration.");
        sb.AppendLine("Interdisciplinary thinking: topics overlap across the discovered clusters.");
        return sb.ToString();
    }

    public static string DefaultRarePatterns(IReadOnlyList<string> words)
    {
        var freq = Freq(words);
        var rare = freq.OrderBy(kv => kv.Value).Take(6).Select(kv => kv.Key).ToList();
        if (rare.Count == 0) rare.Add("(no rare patterns)");
        var sb = new StringBuilder();
        sb.AppendLine("RARE PATTERNS");
        sb.AppendLine("=============");
        sb.AppendLine($"Highly unusual cognitive states: {string.Join(", ", rare.Take(3))}.");
        sb.AppendLine($"Rare thought patterns: {string.Join(", ", rare)}.");
        sb.AppendLine("Potential breakthrough moments: where rare concepts co-occur with high-frequency anchors.");
        sb.AppendLine("Signal anomalies: low-frequency words flagged by the anomaly scorer.");
        return sb.ToString();
    }

    // ---- LM fallback: research report (.docx) ----

    public static string DefaultReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<UnsupTopicScore> topics,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = topics.Count > 0 ? topics[0].Topic : "General";
        var concepts = TopConcepts(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Unsupervised Learning Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"An unsupervised analysis of a 3-minute EEG (no predefined labels). Dominant latent topic: {top}; cognitive diversity {dashboard[0].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Clustering Results");
        sb.AppendLine($"Six cognitive-state clusters were discovered; cluster separation {dashboard[1].Value:0}. Recurring concepts: {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Topic Discovery Results");
        sb.AppendLine($"Latent topics span {string.Join(", ", topics.Take(3).Select(t => t.Topic))}; topic diversity {dashboard[2].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Similarity Analysis");
        sb.AppendLine($"Concept similarity forms a dense network; similarity density {dashboard[5].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Anomaly Findings");
        sb.AppendLine($"Rare concepts were flagged as anomalies; anomaly rate {dashboard[3].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Emergent Behavior Insights");
        sb.AppendLine($"Emergence {dashboard[4].Value:0}; inventive, systems and interdisciplinary thinking arise from the clusters.");
        sb.AppendLine();
        sb.AppendLine($"EEG context: attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        return sb.ToString();
    }

    // ---- LM fallback: 10-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<UnsupTopicScore> topics,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        var concepts = TopConcepts(words, 3);
        string Topic(int i) => i < topics.Count ? $"{topics[i].Topic} ({topics[i].Percent:0}%)" : "—";
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", $"Recurring: {string.Join(", ", concepts)}" }),
            new("Feature Extraction", new[] { "Signal variance, bands, entropy", "Word vectors & embeddings" }),
            new("Clustering Results", new[] { "6 cognitive-state clusters", $"Separation {dashboard[1].Value:0}" }),
            new("Topic Discovery", new[] { Topic(0), Topic(1), Topic(2) }),
            new("Similarity Network", new[] { $"Similarity density {dashboard[5].Value:0}", "Cosine neighbors & matrix" }),
            new("Anomaly Detection", new[] { $"Anomaly rate {dashboard[3].Value:0}", "Rare patterns flagged" }),
            new("Emergent Behaviors", new[] { $"Emergence {dashboard[4].Value:0}", "Inventive & systems thinking" }),
            new("Multi-User Communities", new[] { "Cognitive groups & roles", "Explorer, Innovator, Analyst…" }),
            new("Conclusions", new[] { "Hidden structure, no labels", "Tracks evolving cognition" }),
        };
    }
}
