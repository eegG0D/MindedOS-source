using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Scans recorded EEG word CSVs (this run's history + an optional csv_files/ folder),
/// computes a cognitive signature per session, and builds the cross-session pattern
/// CSVs. Everything degrades gracefully to a single session.
/// </summary>
public static class PatternScan
{
    public sealed record ScannedSession(string Id, DateTime Time, CognitiveSignature Signature, string TopTopic, int Words);

    /// <summary>Read the "word" column (header "word", else the 2nd column) from a recorded CSV.</summary>
    public static List<string> LoadWords(string path)
    {
        var words = new List<string>();
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0) return words;
        var header = lines[0].Split(',');
        int col = Array.FindIndex(header, h => h.Trim().Equals("word", StringComparison.OrdinalIgnoreCase));
        if (col < 0) col = header.Length > 1 ? 1 : 0;
        for (int i = 1; i < lines.Length; i++)
        {
            var c = lines[i].Split(',');
            if (c.Length > col)
            {
                var w = c[col].Trim();
                if (w.Length > 0) words.Add(w);
            }
        }
        return words;
    }

    /// <summary>Scan prior recorded_eeg_*.csv in outputDir plus outputDir\csv_files\*.csv.</summary>
    public static IReadOnlyList<ScannedSession> Scan(string outputDir, string dataDir = "")
    {
        var sessions = new List<ScannedSession>();
        void Add(string file)
        {
            try
            {
                var words = LoadWords(file);
                if (words.Count == 0) return;
                var sig = CognitiveSignature.Compute(50, 50, Array.Empty<BandReading>(), words);
                var topics = string.IsNullOrEmpty(dataDir)
                    ? (IReadOnlyList<PatternTopicScore>)Array.Empty<PatternTopicScore>()
                    : PatternTopics.DetectFromFile(dataDir, words);
                string top = topics.Count > 0 ? topics[0].Topic : "General";
                sessions.Add(new ScannedSession(Path.GetFileNameWithoutExtension(file),
                    File.GetLastWriteTime(file), sig, top, words.Count));
            }
            catch { /* skip unreadable file */ }
        }

        if (Directory.Exists(outputDir))
            foreach (var f in Directory.EnumerateFiles(outputDir, "recorded_eeg_*.csv")) Add(f);
        var csvFiles = Path.Combine(outputDir, "csv_files");
        if (Directory.Exists(csvFiles))
            foreach (var f in Directory.EnumerateFiles(csvFiles, "*.csv")) Add(f);

        return sessions.OrderBy(s => s.Time).ToList();
    }

    public static string SessionComparisonCsv(IReadOnlyList<ScannedSession> sessions, ScannedSession current)
    {
        var sb = new System.Text.StringBuilder("session,time,dominant_state,top_topic,focus,creativity,similarity_to_current\n");
        foreach (var s in sessions)
        {
            var dom = CognitiveSignature.DominantState(s.Signature, 50, 50).State;
            double sim = CognitiveSignature.Similarity(s.Signature, current.Signature);
            sb.AppendLine($"{s.Id},{s.Time:yyyy-MM-dd HH:mm:ss},{dom},{s.TopTopic},{s.Signature.Focus:0},{s.Signature.Creativity:0},{sim:0.0}");
        }
        return sb.ToString();
    }

    public static string BrainClustersCsv(IReadOnlyList<ScannedSession> sessions)
    {
        var sb = new System.Text.StringBuilder("session,cluster\n");
        foreach (var s in sessions)
            sb.AppendLine($"{s.Id},{CognitiveSignature.NearestArchetype(s.Signature)}");
        return sb.ToString();
    }

    public static string SimilarityMatrixCsv(IReadOnlyList<ScannedSession> sessions)
    {
        var sb = new System.Text.StringBuilder("session," + string.Join(",", sessions.Select(s => s.Id)) + "\n");
        foreach (var a in sessions)
        {
            sb.Append(a.Id);
            foreach (var b in sessions)
                sb.Append(',').Append(CognitiveSignature.Similarity(a.Signature, b.Signature).ToString("0"));
            sb.Append('\n');
        }
        return sb.ToString();
    }

    public static string NetworkRankingsCsv(IReadOnlyList<ScannedSession> sessions)
    {
        (string Metric, Func<CognitiveSignature, double> Sel)[] metrics =
        {
            ("Most Creative", s => s.Creativity),
            ("Most Logical", s => s.Logic),
            ("Most Curious", s => s.Curiosity),
            ("Most Innovative", s => s.Innovation),
            ("Most Consistent", s => s.Consistency),
            ("Most Analytical", s => s.Logic),
        };
        var sb = new System.Text.StringBuilder("ranking,top_session,score\n");
        foreach (var (metric, sel) in metrics)
        {
            var best = sessions.OrderByDescending(s => sel(s.Signature)).First();
            sb.AppendLine($"{metric},{best.Id},{sel(best.Signature):0.0}");
        }
        return sb.ToString();
    }

    public static string TrendAnalysisCsv(IReadOnlyList<ScannedSession> sessions)
    {
        var ordered = sessions.OrderBy(s => s.Time).ToList();
        var sb = new System.Text.StringBuilder("axis,earliest,latest,trend\n");
        var first = ordered.First().Signature.Axes();
        var last = ordered.Last().Signature.Axes();
        bool single = ordered.Count < 2;
        for (int i = 0; i < first.Count; i++)
        {
            double e = first[i].Value, l = last[i].Value;
            string trend = single ? "insufficient data"
                         : l > e + 2 ? "growth" : l < e - 2 ? "declining" : "stable";
            sb.AppendLine($"{first[i].Name},{e:0.0},{l:0.0},{trend}");
        }
        return sb.ToString();
    }

    public static string HistoryHeader() =>
        "date,logic,creativity,focus,curiosity,innovation,exploration,consistency,adaptability,top_topic";

    public static string HistoryRow(CognitiveSignature s, string topTopic)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{s.Logic:0},{s.Creativity:0},{s.Focus:0},{s.Curiosity:0},{s.Innovation:0}," +
               $"{s.Exploration:0},{s.Consistency:0},{s.Adaptability:0},{topTopic}";
    }

    public static void AppendHistory(string path, CognitiveSignature s, string topTopic)
    {
        bool isNew = !File.Exists(path);
        using var w = new StreamWriter(path, append: true);
        if (isNew) w.WriteLine(HistoryHeader());
        w.WriteLine(HistoryRow(s, topTopic));
    }
}
