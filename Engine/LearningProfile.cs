using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>The deterministic Brain Learning Profile: eight 0–100 scores.</summary>
public sealed record LearningProfile(
    double Focus, double Curiosity, double Creativity, double Logic,
    double Memory, double ProblemSolving, double FlowState, double LearningEfficiency)
{
    /// <summary>Compute the eight scores from EEG averages, band shares and word diversity.</summary>
    public static LearningProfile Compute(
        double avgAttention, double avgMeditation,
        IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        double total = 0;
        foreach (var b in bands) total += b.Value;
        if (total <= 0) total = 1;

        double Share(string key)
        {
            foreach (var b in bands) if (b.Key == key) return b.Value / total;
            return 0;
        }

        double theta = Share("theta");
        double alpha = Share("lowAlpha") + Share("highAlpha");
        double beta = Share("lowBeta") + Share("highBeta");
        double gamma = Share("lowGamma") + Share("midGamma");

        int n = words.Count;
        int distinct = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        double diversity = n > 0 ? (double)distinct / n : 0; // 0..1

        static double C(double v) => Math.Clamp(v, 0, 100);

        double focus = C(avgAttention);
        double curiosity = C(diversity * 60 + (theta + alpha) * 80);
        double creativity = C(alpha * 70 + gamma * 60 + diversity * 40);
        double logic = C(beta * 90 + avgAttention * 0.4);
        double memory = C(theta * 80 + avgMeditation * 0.5);
        double problemSolving = C(beta * 70 + avgAttention * 0.3 + diversity * 30);
        double flow = C(100 - Math.Abs(avgAttention - avgMeditation));
        double efficiency = C((focus + curiosity + creativity + logic + memory + problemSolving + flow) / 7.0);

        return new LearningProfile(focus, curiosity, creativity, logic, memory, problemSolving, flow, efficiency);
    }

    public static string CsvHeader() =>
        "date,focus,curiosity,creativity,logic,memory,problem_solving,flow_state,learning_efficiency,top_subjects,dominant_band";

    public string CsvRow(IReadOnlyList<SubjectScore> subjects, string dominantBand)
    {
        string top = string.Join(" | ", subjects.Take(3).Select(s => $"{s.Subject} {s.Percent:0}%"));
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{Focus:0},{Curiosity:0},{Creativity:0},{Logic:0},{Memory:0}," +
               $"{ProblemSolving:0},{FlowState:0},{LearningEfficiency:0},\"{top}\",{dominantBand}";
    }

    /// <summary>Static helper so tests/callers can build a row without a separate instance call.</summary>
    public static string CsvRow(LearningProfile p, IReadOnlyList<SubjectScore> subjects, string dominantBand) =>
        p.CsvRow(subjects, dominantBand);

    /// <summary>Write the current session snapshot (header + one row).</summary>
    public void WriteProfileCsv(string path, IReadOnlyList<SubjectScore> subjects, string dominantBand) =>
        File.WriteAllText(path, CsvHeader() + "\n" + CsvRow(subjects, dominantBand) + "\n");

    /// <summary>Append one row to the long-term history (writing the header if new).</summary>
    public void AppendHistory(string path, IReadOnlyList<SubjectScore> subjects, string dominantBand)
    {
        bool isNew = !File.Exists(path);
        using var w = new StreamWriter(path, append: true);
        if (isNew) w.WriteLine(CsvHeader());
        w.WriteLine(CsvRow(subjects, dominantBand));
    }
}
