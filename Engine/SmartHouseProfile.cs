using MindedOS.Core;

namespace MindedOS.Engine;

/// <summary>
/// Deterministic smart-house score-sets from EEG averages, band shares and word diversity.
/// Mirrors <see cref="RlProfile"/> math. All scores 0–100.
/// </summary>
public static class SmartHouseProfile
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

    /// <summary>The six dashboard scores (0–100). Order: Comfort, Energy Efficiency, Automation, Security, Productivity, Occupancy Forecast.</summary>
    public static IReadOnlyList<(string Score, double Value)> Dashboard(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Comfort", C(avgMed * 0.5 + alpha * 30 + 10)),
            ("Energy Efficiency", C(avgMed * 0.3 + (1 - div) * 40 + beta * 10)),
            ("Automation", C(avgAtt * 0.4 + beta * 30 + div * 20)),
            ("Security", C(avgAtt * 0.4 + beta * 30 + avgMed * 0.1)),
            ("Productivity", C(avgAtt * 0.6 + beta * 20)),
            ("Occupancy Forecast", C(avgAtt * 0.3 + div * 40 + alpha * 20)),
        };
    }

    public static IReadOnlyList<(string Metric, double Value)> MoodScores(
        double avgAtt, double avgMed, IReadOnlyList<BandReading> bands, IReadOnlyList<string> words)
    {
        var (theta, alpha, beta, gamma) = Shares(bands);
        double div = Diversity(words);
        return new (string, double)[]
        {
            ("Focused", C(avgAtt * 0.7 + beta * 20)),
            ("Relaxed", C(avgMed * 0.6 + alpha * 30)),
            ("Creative", C(alpha * 50 + gamma * 40 + div * 10)),
            ("Tired", C((100 - avgAtt) * 0.5 + theta * 30)),
            ("Productive", C(avgAtt * 0.5 + beta * 30)),
            ("Learning", C(avgAtt * 0.4 + div * 30 + beta * 20)),
        };
    }

    public static string SmartHomeProfileCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        double div = Diversity(w);
        var rows = new (string, double)[]
        {
            ("Comfort Preference", C(m * 0.5 + alpha * 30 + 10)),
            ("Environmental Sensitivity", C(alpha * 40 + a * 0.3 + 10)),
            ("Activity Level", C(a * 0.6 + beta * 20)),
            ("Energy Tendency", C(m * 0.3 + (1 - div) * 40)),
            ("Automation Readiness", C(a * 0.4 + beta * 30 + div * 20)),
        };
        var sb = new System.Text.StringBuilder("metric,score\n");
        foreach (var (name, value) in rows) sb.AppendLine($"{name},{value:0.0}");
        return sb.ToString();
    }

    public static string EnvironmentPreferencesCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var (theta, alpha, beta, gamma) = Shares(b);
        bool warm = m >= 50;
        bool bright = a >= 50;
        var sb = new System.Text.StringBuilder("aspect,preference,value\n");
        sb.AppendLine($"Temperature Range,{(warm ? "warm" : "cool")},{(warm ? "21-24C" : "18-21C")}");
        sb.AppendLine($"Heating,{(warm ? "higher" : "moderate")},{C(m * 0.5 + 30):0}");
        sb.AppendLine($"Cooling,{(warm ? "moderate" : "higher")},{C((100 - m) * 0.5 + 30):0}");
        sb.AppendLine($"Brightness,{(bright ? "bright" : "dim")},{C(a * 0.7 + 20):0}");
        sb.AppendLine($"Color Temperature,{(bright ? "cool white" : "warm white")},{C(a * 0.5 + 30):0}");
        sb.AppendLine($"Daytime Lighting,active,{C(a * 0.6 + 30):0}");
        sb.AppendLine($"Nighttime Lighting,dim warm,{C((100 - a) * 0.4 + 20):0}");
        sb.AppendLine($"Quiet Environment,{(m >= 50 ? "preferred" : "neutral")},{C(m * 0.6 + 20):0}");
        sb.AppendLine($"Music Environment,{(alpha > 0.2 ? "preferred" : "neutral")},{C(alpha * 100 + 30):0}");
        sb.AppendLine($"Ambient Sound,soft,{C(m * 0.4 + 30):0}");
        return sb.ToString();
    }

    public static string DailyRoutinesCsv(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w)
    {
        var d = Dashboard(a, m, b, w);
        var routines = new (string Routine, string Time)[]
        {
            ("Wake-up", "07:00"),
            ("Work Session", "09:00"),
            ("Learning Session", "14:00"),
            ("Relaxation", "18:00"),
            ("Sleep Preparation", "22:00"),
        };
        var sb = new System.Text.StringBuilder("routine,detected,suggested_time\n");
        foreach (var (routine, time) in routines)
        {
            string detected = d[4].Value >= 50 || d[0].Value >= 50 ? "yes" : "likely";
            sb.AppendLine($"{routine},{detected},{time}");
        }
        return sb.ToString();
    }

    // ---- history ----

    public static string HistoryHeader() =>
        "date,comfort,energy_efficiency,automation,security,productivity,occupancy_forecast,top_room";

    public static string HistoryRow(double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topRoom)
    {
        var d = Dashboard(a, m, b, w);
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"{date},{d[0].Value:0},{d[1].Value:0},{d[2].Value:0},{d[3].Value:0},{d[4].Value:0},{d[5].Value:0},{topRoom}";
    }

    public static void AppendHistory(string path, double a, double m, IReadOnlyList<BandReading> b, IReadOnlyList<string> w, string topRoom)
    {
        bool isNew = !System.IO.File.Exists(path);
        using var writer = new System.IO.StreamWriter(path, append: true);
        if (isNew) writer.WriteLine(HistoryHeader());
        writer.WriteLine(HistoryRow(a, m, b, w, topRoom));
    }
}
