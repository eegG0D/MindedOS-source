using System.IO;
using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Scans recorded EEG word CSVs (prior recorded_eeg_*.csv + a csv_files/ folder)
/// into a multi-user planning network. Reuses <see cref="PatternScan.LoadWords"/>.
/// </summary>
public static class PlanningScan
{
    public sealed record PlanningProfileRow(string Id, double StrategicThinking, double GoalClarity, double InnovationPlanning, double Feasibility);

    public static IReadOnlyList<PlanningProfileRow> Scan(string outputDir, string dataDir)
    {
        var rows = new List<PlanningProfileRow>();
        void Add(string file)
        {
            try
            {
                var words = PatternScan.LoadWords(file);
                if (words.Count == 0) return;
                var scores = PlanningProfile.PlanningScores(50, 50, Array.Empty<BandReading>(), words);
                var forecasts = PlanningProfile.GoalForecasts(50, 50, Array.Empty<BandReading>(), words);
                rows.Add(new PlanningProfileRow(Path.GetFileNameWithoutExtension(file),
                    scores[0].Value /* Strategic Thinking */, scores[2].Value /* Goal Clarity */,
                    scores[4].Value /* Innovation Planning */, forecasts[0].Value /* Success ~ Feasibility */));
            }
            catch { /* skip unreadable */ }
        }

        if (Directory.Exists(outputDir))
            foreach (var f in Directory.EnumerateFiles(outputDir, "recorded_eeg_*.csv")) Add(f);
        var csvFiles = Path.Combine(outputDir, "csv_files");
        if (Directory.Exists(csvFiles))
            foreach (var f in Directory.EnumerateFiles(csvFiles, "*.csv")) Add(f);

        return rows;
    }

    public static string NetworkCsv(IReadOnlyList<PlanningProfileRow> rows)
    {
        var sb = new System.Text.StringBuilder("id,strategic_thinking,goal_clarity,innovation_planning,feasibility\n");
        foreach (var r in rows)
            sb.AppendLine($"{r.Id},{r.StrategicThinking:0.0},{r.GoalClarity:0.0},{r.InnovationPlanning:0.0},{r.Feasibility:0.0}");
        if (rows.Count == 0) sb.AppendLine("(no profiles),0,0,0,0");
        return sb.ToString();
    }
}
