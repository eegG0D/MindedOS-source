using System.Text;
using System.Text.Json;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Semi-Supervised content: the labeled/unlabeled/classification/
/// expanded/prediction CSVs, the brain model JSON, the knowledge graph, the dashboard, and
/// fallbacks for the LM artifacts (two discovery narratives, report, 10-slide deck). Reuses <see cref="NlpContent"/>.
/// </summary>
public static class SemiSupContent
{
    private static Dictionary<string, int> Freq(IReadOnlyList<string> words)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in words)
        {
            var word = w.Trim();
            if (word.Length == 0 || word == "—") continue;
            freq[word] = freq.TryGetValue(word, out var c) ? c + 1 : 1;
        }
        return freq;
    }

    public static string LabeledKnowledgeCsv(IReadOnlyList<string> words, IReadOnlyList<SemiSupCategoryScore> categories)
    {
        string topCat = categories.Count > 0 ? categories[0].Category : "General";
        var sb = new StringBuilder("word,count,category\n");
        foreach (var (word, count) in Freq(words).Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Value).Take(20))
            sb.AppendLine($"{word},{count},{topCat}");
        if (sb.ToString().Trim().Split('\n').Length <= 1) sb.AppendLine($"(none),0,{topCat}");
        return sb.ToString();
    }

    public static string UnlabeledDiscoveriesCsv(IReadOnlyList<string> words)
    {
        var sb = new StringBuilder("pattern,first_seen,note\n");
        var singletons = Freq(words).Where(kv => kv.Value == 1).Select(kv => kv.Key).Take(20).ToList();
        foreach (var w in singletons) sb.AppendLine($"{w},this session,novel pattern");
        if (singletons.Count == 0) sb.AppendLine("(none),-,no novel patterns");
        return sb.ToString();
    }

    public static string ClassificationCsv(IReadOnlyList<SemiSupCategoryScore> categories)
    {
        var sb = new StringBuilder("category,score,classification\n");
        for (int i = 0; i < categories.Count; i++)
        {
            string cls = i == 0 ? "primary" : categories[i].Percent >= 10 ? "secondary" : "minor";
            sb.AppendLine($"{categories[i].Category},{categories[i].Percent:0.0},{cls}");
        }
        if (categories.Count == 0) sb.AppendLine("General,100.0,primary");
        return sb.ToString();
    }

    public static string ExpandedKnowledgeBaseCsv(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 8);
        if (concepts.Count == 0) concepts = new List<string> { "the focus" };
        var sb = new StringBuilder("concept,related_concepts,topic_association\n");
        for (int i = 0; i < concepts.Count; i++)
        {
            string related = string.Join(" ", concepts.Where((_, j) => j != i).Take(2));
            if (related.Length == 0) related = "(none)";
            sb.AppendLine($"{concepts[i]},{related},theme {i % 3 + 1}");
        }
        return sb.ToString();
    }

    public static string TopicPredictionsCsv(IReadOnlyList<SemiSupCategoryScore> categories)
    {
        var sb = new StringBuilder("predicted_interest,probability,trend\n");
        for (int i = 0; i < categories.Count; i++)
        {
            double prob = System.Math.Clamp(categories[i].Percent + (i == 0 ? 5 : -i), 0, 100);
            string trend = i < 3 ? "rising" : "steady";
            sb.AppendLine($"{categories[i].Category},{prob:0.0},{trend}");
        }
        if (categories.Count == 0) sb.AppendLine("General,100.0,steady");
        return sb.ToString();
    }

    public static string BrainModelJson(
        IReadOnlyList<string> words, IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<SemiSupCategoryScore> categories)
    {
        var freq = Freq(words);
        var known = freq.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Value).Select(kv => kv.Key).Take(8).ToArray();
        var unknown = freq.Where(kv => kv.Value == 1).Select(kv => kv.Key).Take(8).ToArray();
        var model = new
        {
            known,
            unknown,
            predicted = categories.Take(5).Select(c => new { category = c.Category, percent = System.Math.Round(c.Percent, 1) }).ToArray(),
            emerging = categories.Skip(5).Take(3).Select(c => c.Category).ToArray(),
            metrics = dashboard.Select(d => new { name = d.Score, value = System.Math.Round(d.Value, 1) }).ToArray(),
        };
        return JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
    }

    public static string KnowledgeGraphMd(IReadOnlyList<string> words, IReadOnlyList<SemiSupCategoryScore> categories)
    {
        var concepts = NlpContent.TopWords(words, 5);
        if (concepts.Count == 0) concepts = new List<string> { "core" };
        var cats = categories.Take(4).Select(c => c.Category).ToList();
        if (cats.Count == 0) cats.Add("General");
        var sb = new StringBuilder();
        sb.AppendLine("# Semi-Supervised Knowledge Graph");
        sb.AppendLine();
        sb.AppendLine("Relationships across known, predicted, discovered and emerging concepts:");
        sb.AppendLine();
        for (int i = 0; i < cats.Count; i++)
        {
            string next = i + 1 < cats.Count ? cats[i + 1] : "Synthesis";
            sb.AppendLine($"- **{cats[i]}** → relates to → **{next}**");
        }
        sb.AppendLine();
        sb.AppendLine($"Core concepts: {string.Join(" · ", concepts)}");
        sb.AppendLine();
        sb.AppendLine($"Pathway: {string.Join(" → ", cats)} → Synthesis");
        return sb.ToString();
    }

    public static string Dashboard(IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<SemiSupCategoryScore> categories)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Semi-Supervised Learning Dashboard");
        sb.AppendLine();
        sb.AppendLine("| Metric | Score |");
        sb.AppendLine("| --- | --- |");
        foreach (var (name, value) in dashboard) sb.AppendLine($"| {name} | {value:0} |");
        sb.AppendLine();
        sb.AppendLine("## Classification");
        sb.AppendLine("| Category | % |");
        sb.AppendLine("| --- | --- |");
        foreach (var c in categories.Take(10)) sb.AppendLine($"| {c.Category} | {c.Percent:0} |");
        return sb.ToString();
    }

    // ---- LM fallbacks ----

    public static string DefaultConceptDiscovery(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 5);
        var sb = new StringBuilder();
        sb.AppendLine("CONCEPT DISCOVERY");
        sb.AppendLine("=================");
        sb.AppendLine($"Emerging interests: {string.Join(", ", concepts.Take(3).DefaultIfEmpty("the recurring focus"))}.");
        sb.AppendLine("Hidden topics: themes that connect the recurring concepts.");
        sb.AppendLine("Unrecognized concepts: rare patterns worth tracking.");
        sb.AppendLine("Novel combinations: pairing distant concepts may reveal new directions.");
        return sb.ToString();
    }

    public static string DefaultAiDiscoveries(IReadOnlyList<string> words)
    {
        var concepts = NlpContent.TopWords(words, 4);
        string top = concepts.Count > 0 ? concepts[0] : "the focus";
        var sb = new StringBuilder();
        sb.AppendLine("AI-ASSISTED DISCOVERIES");
        sb.AppendLine("=======================");
        sb.AppendLine($"Unknown pattern: a rare concept near {top}.");
        sb.AppendLine("Possible meaning: an association with the dominant theme.");
        sb.AppendLine("Reasoning: co-occurrence with the recurring concepts.");
        sb.AppendLine("Estimated probability: moderate.");
        sb.AppendLine("Validation method: collect more sessions and confirm recurrence.");
        return sb.ToString();
    }

    public static string DefaultReportMarkdown(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<SemiSupCategoryScore> categories,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string top = categories.Count > 0 ? categories[0].Category : "General";
        var concepts = NlpContent.TopWords(words, 6);
        var sb = new StringBuilder();
        sb.AppendLine("# Semi-Supervised Learning Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"Combining labeled and unlabeled EEG patterns, the leading category is {top}; recurring concepts {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine();
        sb.AppendLine("## Learning Statistics");
        sb.AppendLine($"Knowledge coverage {dashboard[0].Value:0}, discovery rate {dashboard[1].Value:0}, pattern diversity {dashboard[5].Value:0}. Attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Classification Analysis");
        sb.AppendLine($"Unknown patterns are classified toward {top} and adjacent categories with accuracy {dashboard[2].Value:0}.");
        sb.AppendLine();
        sb.AppendLine("## Discovery Analysis");
        sb.AppendLine("Rare, single-occurrence concepts are treated as novel discoveries pending confirmation across sessions.");
        sb.AppendLine();
        sb.AppendLine("## Future Recommendations");
        sb.AppendLine("Record more sessions to validate discoveries and grow the knowledge base.");
        return sb.ToString();
    }

    public static IReadOnlyList<SlideContent> DefaultDeck(
        IReadOnlyList<(string Score, double Value)> dashboard, IReadOnlyList<SemiSupCategoryScore> categories,
        IReadOnlyList<string> words, double avgAtt, double avgMed, string dominantBand)
    {
        string Cat(int i) => i < categories.Count ? $"{categories[i].Category} ({categories[i].Percent:0}%)" : "—";
        var concepts = NlpContent.TopWords(words, 3);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Translation Results", new[] { $"{words.Count} words decoded", concepts.Count > 0 ? $"Recurring: {string.Join(", ", concepts)}" : "—" }),
            new("Labeled Data Analysis", new[] { "Known/frequent concepts", $"Coverage {dashboard[0].Value:0}" }),
            new("Unlabeled Data Analysis", new[] { "Rare/novel patterns", $"Discovery rate {dashboard[1].Value:0}" }),
            new("Brain Clusters", new[] { "Analytical & creative", "Problem solving & research" }),
            new("Concept Discovery", new[] { "Emerging interests", "Hidden topics" }),
            new("Knowledge Expansion", new[] { "Related concepts", "Topic associations" }),
            new("Predictions", new[] { Cat(0), Cat(1), Cat(2) }),
            new("Future Learning", new[] { $"Knowledge growth {dashboard[3].Value:0}", "Record more sessions" }),
            new("Conclusions", new[] { "Labeled + unlabeled learning", "An evolving cognitive model" }),
        };
    }
}
