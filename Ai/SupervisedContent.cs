using System.Text;
using MindedOS.Engine;

namespace MindedOS.Ai;

/// <summary>
/// Deterministic, offline-safe Supervised Learning content: the labeled training dataset, label
/// database, career classification, the simulated trained-model descriptors, the preview scorecard,
/// and fallbacks for the LM artifacts (two narratives, a report and a 10-slide deck). Self-contained;
/// reuses only <see cref="NlpContent"/>.
/// </summary>
public static class SupervisedContent
{
    private static readonly (string Label, string Category)[] LabelCategories =
    {
        ("Focused", "Attention"), ("Creative", "Creativity"), ("Analytical", "Reasoning"),
        ("Learning", "Learning"), ("Problem Solving", "Reasoning"), ("Innovative", "Creativity"),
        ("Curious", "Exploration"), ("Distracted", "Attention"),
    };

    private static List<string> Concepts(IReadOnlyList<string> words, int n)
    {
        var c = NlpContent.TopWords(words, n);
        return c.Count > 0 ? new List<string>(c) : new List<string> { "signal", "focus", "idea", "pattern" };
    }

    // ---- label management ----

    public static string LabelDatabaseCsv()
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd");
        var sb = new StringBuilder("label_id,label,category,created\n");
        for (int i = 0; i < LabelCategories.Length; i++)
            sb.AppendLine($"{i + 1},{LabelCategories[i].Label},{LabelCategories[i].Category},{date}");
        return sb.ToString();
    }

    // ---- training dataset ----

    public static string TrainingDatasetCsv(
        double a, double m, IReadOnlyList<MindedOS.Core.BandReading> bands, IReadOnlyList<string> words, string dominantBand)
    {
        var feats = SupervisedProfile.Features(a, m, bands, dominantBand);
        string featSig = string.Join("|", feats.Take(4).Select(f => $"{f.Name.Split(' ')[0]}={f.Value}"));
        string predicted = SupervisedProfile.PredictLabel(a, m, bands, words);
        var concepts = Concepts(words, 24);
        var sb = new StringBuilder("eeg_features,translated_concepts,user_label,category,timestamp\n");
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        int rows = Math.Clamp(concepts.Count, 4, 12);
        for (int i = 0; i < rows; i++)
        {
            // first row uses the predicted label; the rest rotate through the label set deterministically
            var (label, category) = i == 0
                ? (predicted, CategoryOf(predicted))
                : (LabelCategories[i % LabelCategories.Length].Label, LabelCategories[i % LabelCategories.Length].Category);
            string conceptWindow = string.Join(" ", concepts.Skip(i * 2).Take(2).DefaultIfEmpty(concepts[i % concepts.Count]));
            sb.AppendLine($"{featSig},{conceptWindow},{label},{category},{date}");
        }
        return sb.ToString();
    }

    private static string CategoryOf(string label)
    {
        foreach (var (l, c) in LabelCategories) if (l == label) return c;
        return "General";
    }

    // ---- career classification ----

    public static string CareerClassificationCsv(IReadOnlyList<CareerScore> careers)
    {
        var sb = new StringBuilder("career,strength,percent\n");
        foreach (var c in careers)
        {
            string strength = c.Percent >= 20 ? "Strong" : c.Percent >= 10 ? "Moderate" : "Emerging";
            sb.AppendLine($"{c.Career},{strength},{c.Percent:0.0}");
        }
        if (careers.Count == 0) sb.AppendLine("Research,Moderate,100.0");
        return sb.ToString();
    }

    // ---- simulated trained models (trained_models/ descriptors) ----

    public static IReadOnlyDictionary<string, string> TrainedModelDescriptors(
        string evaluationCsv, string targetLabel, IReadOnlyList<string> words)
    {
        var feats = string.Join(", ", new[] { "Mean Signal", "Beta Activity", "Alpha Activity", "Theta Activity", "Gamma Activity" });
        var lines = evaluationCsv.Replace("\r\n", "\n").Split('\n').Skip(1).Where(l => l.Contains(',')).ToList();
        var map = new Dictionary<string, string>();
        foreach (var model in SupervisedProfile.Models)
        {
            string file = model.ToLowerInvariant().Replace(' ', '_') + ".txt";
            var row = lines.FirstOrDefault(l => l.StartsWith(model + ","));
            string acc = row is not null ? row.Split(',')[1] : "—";
            var sb = new StringBuilder();
            sb.AppendLine($"# {model} (simulated descriptor)");
            sb.AppendLine("# This mindedOS program ships no ML library; this descriptor records a deterministic,");
            sb.AppendLine("# offline simulation of a supervised model trained on the EEG features.");
            sb.AppendLine();
            sb.AppendLine($"model_type: {model}");
            sb.AppendLine($"target: {targetLabel}");
            sb.AppendLine($"features: {feats}");
            sb.AppendLine($"simulated_accuracy: {acc}");
            sb.AppendLine($"classes: {string.Join(", ", SupervisedProfile.Labels)}");
            map[file] = sb.ToString();
        }
        return map;
    }

    // ---- preview scorecard ----

    public static string Scorecard(string evaluationCsv, double focus, double creativity, double productivity, string predicted, string topCareer)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SUPERVISED LEARNING DASHBOARD");
        sb.AppendLine("=============================");
        sb.AppendLine($"Predicted brain state : {predicted}");
        sb.AppendLine($"Top career strength   : {topCareer}");
        sb.AppendLine(Bar("Focus", focus));
        sb.AppendLine(Bar("Creativity", creativity));
        sb.AppendLine(Bar("Productivity", productivity));
        var best = evaluationCsv.Replace("\r\n", "\n").Split('\n').Skip(1)
            .Where(l => l.Contains(',')).Select(l => l.Split(',')).ToList();
        if (best.Count > 0)
        {
            double avgAcc = best.Average(c => double.TryParse(c[1], out var v) ? v : 0);
            sb.AppendLine(Bar("Avg model accuracy", avgAcc));
        }
        return sb.ToString();
    }

    private static string Bar(string name, double value)
    {
        int filled = (int)Math.Round(value / 5.0);
        string bar = new string('█', Math.Clamp(filled, 0, 20)) + new string('░', Math.Clamp(20 - filled, 0, 20));
        return $"{name,-22} {bar} {value:0}";
    }

    // ---- LM fallbacks: narratives ----

    public static string DefaultKnowledgeDiscovery(IReadOnlyList<string> words, IReadOnlyList<CareerScore> careers)
    {
        var concepts = Concepts(words, 5);
        string top = careers.Count > 0 ? careers[0].Career : "your field";
        var sb = new StringBuilder();
        sb.AppendLine("KNOWLEDGE DISCOVERY REPORT");
        sb.AppendLine("==========================");
        sb.AppendLine($"Most predictive EEG patterns: recurring concepts {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine("Most important features: Beta Activity and Mean Signal dominate the prediction.");
        sb.AppendLine($"Strongest cognitive indicators: sustained attention aligned with {top}.");
        sb.AppendLine("Hidden relationships: high beta with high concept diversity predicts analytical focus.");
        return sb.ToString();
    }

    public static string DefaultAiExplanations(string predicted, IReadOnlyList<string> words)
    {
        var concepts = Concepts(words, 4);
        var sb = new StringBuilder();
        sb.AppendLine("AI EXPLANATIONS");
        sb.AppendLine("===============");
        sb.AppendLine($"Why this prediction: the model labeled this session '{predicted}' because the dominant band and");
        sb.AppendLine($"attention level most resemble that class, reinforced by concepts {string.Join(", ", concepts.Take(3))}.");
        sb.AppendLine("Important EEG features: beta (focus), alpha (relaxed creativity), theta (learning).");
        sb.AppendLine("Learning patterns: stable signals raise confidence; diverse concepts widen the class boundary.");
        sb.AppendLine("Model confidence: derived from feature separation between the top two candidate labels.");
        return sb.ToString();
    }

    // ---- LM fallback: report (.docx) ----

    public static string DefaultReportMarkdown(
        string evaluationCsv, IReadOnlyList<CareerScore> careers, IReadOnlyList<string> words,
        double avgAtt, double avgMed, string dominantBand, string predicted, int datasetRows)
    {
        string top = careers.Count > 0 ? careers[0].Career : "General";
        var concepts = Concepts(words, 6);
        double avgAcc = evaluationCsv.Replace("\r\n", "\n").Split('\n').Skip(1)
            .Where(l => l.Contains(',')).Select(l => l.Split(',')).DefaultIfEmpty(new[] { "", "0" })
            .Average(c => double.TryParse(c.Length > 1 ? c[1] : "0", out var v) ? v : 0);
        var sb = new StringBuilder();
        sb.AppendLine("# Supervised Learning Report");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine($"A supervised-learning analysis from a 3-minute EEG. Predicted brain state: {predicted}; leading career strength: {top}; mean simulated model accuracy {avgAcc:0}%.");
        sb.AppendLine();
        sb.AppendLine("## Dataset Description");
        sb.AppendLine($"{datasetRows} labeled examples built from the decoded concepts ({string.Join(", ", concepts.Take(3))}) across {SupervisedProfile.Labels.Length} labels.");
        sb.AppendLine();
        sb.AppendLine("## Feature Analysis");
        sb.AppendLine($"Eight EEG features extracted; attention {avgAtt:0}/100, calm {avgMed:0}/100, dominant band {dominantBand}.");
        sb.AppendLine();
        sb.AppendLine("## Model Results");
        sb.AppendLine($"Five model types simulated (Decision Tree, Random Forest, Logistic Regression, Neural Network, SVM); mean accuracy {avgAcc:0}%.");
        sb.AppendLine();
        sb.AppendLine("## Prediction Statistics");
        sb.AppendLine($"Predicted brain state {predicted}; focus, creativity and productivity scored from the live features.");
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        sb.AppendLine("Record more labeled sessions to improve accuracy, give feedback to relabel mistakes, and retrain incrementally.");
        return sb.ToString();
    }

    // ---- LM fallback: 10-slide deck (.pptx) ----

    public static IReadOnlyList<SlideContent> DefaultDeck(
        string evaluationCsv, IReadOnlyList<CareerScore> careers, IReadOnlyList<string> words,
        double avgAtt, double avgMed, string dominantBand, string predicted, int datasetRows)
    {
        var concepts = Concepts(words, 3);
        string top = careers.Count > 0 ? $"{careers[0].Career} ({careers[0].Percent:0}%)" : "—";
        double avgAcc = evaluationCsv.Replace("\r\n", "\n").Split('\n').Skip(1)
            .Where(l => l.Contains(',')).Select(l => l.Split(',')).DefaultIfEmpty(new[] { "", "0" })
            .Average(c => double.TryParse(c.Length > 1 ? c[1] : "0", out var v) ? v : 0);
        return new List<SlideContent>
        {
            new("EEG Overview", new[] { $"Attention {avgAtt:0}/100", $"Calm {avgMed:0}/100", $"Dominant band {dominantBand}" }),
            new("Dataset Statistics", new[] { $"{datasetRows} labeled examples", $"{SupervisedProfile.Labels.Length} labels" }),
            new("Feature Extraction", new[] { "Mean, variance, peak frequency", "Alpha, beta, theta, gamma activity" }),
            new("Label Distribution", new[] { "Focused, Creative, Analytical, Learning", "Problem Solving, Innovative, Curious, Distracted" }),
            new("Model Architecture", new[] { "Decision Tree, Random Forest", "Logistic Regression, Neural Network, SVM" }),
            new("Prediction Results", new[] { $"Brain state: {predicted}", $"Concepts: {string.Join(", ", concepts)}" }),
            new("Evaluation Metrics", new[] { $"Mean accuracy {avgAcc:0}%", "Precision, recall, F1, confidence" }),
            new("Skill Predictions", new[] { "Problem solving, research, innovation", "Technical aptitude, learning capacity" }),
            new("Knowledge Discovery", new[] { "Most predictive patterns & features", $"Top career strength: {top}" }),
            new("Conclusions", new[] { "EEG → labeled dataset → predictions", "Improves with feedback & retraining" }),
        };
    }
}
