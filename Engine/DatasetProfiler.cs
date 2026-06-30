using System.Globalization;
using System.IO;
using System.Text;

namespace MindedOS.Engine;

/// <summary>Profile of one CSV column.</summary>
public sealed record ColumnProfile(string Name, string Type, int NonEmpty, double? Min, double? Max, double? Mean, string Sample);

/// <summary>Profile of a CSV dataset.</summary>
public sealed record DatasetProfile(string File, long Rows, int Columns, IReadOnlyList<ColumnProfile> ColumnProfiles);

/// <summary>
/// Streams a CSV to build a compact profile without loading it all into memory, so
/// it works on huge datasets: it counts every row but only samples the first N rows
/// for per-column type inference and numeric statistics.
/// </summary>
public static class DatasetProfiler
{
    public static DatasetProfile Profile(string path, int sampleRows = 10000)
    {
        using var reader = new StreamReader(path);
        var headerLine = reader.ReadLine();
        if (headerLine is null) return new DatasetProfile(Path.GetFileName(path), 0, 0, Array.Empty<ColumnProfile>());

        var names = SplitCsv(headerLine);
        int cols = names.Count;
        var nonEmpty = new int[cols];
        var numeric = new int[cols];
        var sum = new double[cols];
        var min = new double[cols];
        var max = new double[cols];
        var sample = new string[cols];
        for (int i = 0; i < cols; i++) { min[i] = double.MaxValue; max[i] = double.MinValue; sample[i] = ""; }

        long rows = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            rows++;
            if (rows > sampleRows) continue; // count all rows, sample the first N for stats

            var cells = SplitCsv(line);
            for (int i = 0; i < cols && i < cells.Count; i++)
            {
                var v = cells[i].Trim();
                if (v.Length == 0) continue;
                nonEmpty[i]++;
                if (sample[i].Length == 0) sample[i] = v;
                if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                {
                    numeric[i]++;
                    sum[i] += d;
                    if (d < min[i]) min[i] = d;
                    if (d > max[i]) max[i] = d;
                }
            }
        }

        var profiles = new List<ColumnProfile>(cols);
        for (int i = 0; i < cols; i++)
        {
            bool isNum = nonEmpty[i] > 0 && numeric[i] >= 0.8 * nonEmpty[i];
            profiles.Add(new ColumnProfile(
                Name: names[i],
                Type: nonEmpty[i] == 0 ? "empty" : isNum ? "numeric" : "text",
                NonEmpty: nonEmpty[i],
                Min: isNum ? min[i] : null,
                Max: isNum ? max[i] : null,
                Mean: isNum && numeric[i] > 0 ? sum[i] / numeric[i] : null,
                Sample: sample[i]));
        }
        return new DatasetProfile(Path.GetFileName(path), rows, cols, profiles);
    }

    /// <summary>Compact textual profile to hand to LM Studio.</summary>
    public static string ToText(DatasetProfile d)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"File: {d.File}");
        sb.AppendLine($"Rows: {d.Rows:N0}");
        sb.AppendLine($"Columns: {d.Columns}");
        sb.AppendLine("Column profiles:");
        foreach (var c in d.ColumnProfiles)
        {
            sb.Append($"- {c.Name} ({c.Type}");
            if (c.Type == "numeric")
                sb.Append(CultureInfo.InvariantCulture, $", range {c.Min:0.##}..{c.Max:0.##}, mean {c.Mean:0.##}");
            else if (c.Sample.Length > 0)
                sb.Append($", e.g. \"{Trunc(c.Sample, 30)}\"");
            sb.AppendLine(")");
        }
        return sb.ToString();
    }

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n] + "…";

    /// <summary>Minimal quote-aware CSV field splitter.</summary>
    private static List<string> SplitCsv(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes) { fields.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(ch);
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
