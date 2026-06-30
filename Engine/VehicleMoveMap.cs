using System.Globalization;
using System.IO;

namespace MindedOS.Engine;

/// <summary>One mapping rule: when signal `op` value, drive `move` (priority orders rules).</summary>
public sealed record VehicleRule(int Priority, string Signal, string Op, double Value, string Move);

/// <summary>
/// Parses eeg_map_vehicle.csv and resolves the current vehicle move from live EEG
/// signals: the highest-priority rule whose condition holds wins; a "default" rule
/// always matches as the fallback.
/// </summary>
public sealed class VehicleMoveMap
{
    private readonly List<VehicleRule> _rules;

    public VehicleMoveMap(IEnumerable<VehicleRule> rules) =>
        _rules = rules.OrderByDescending(r => r.Priority).ToList();

    public IReadOnlyList<VehicleRule> Rules => _rules;

    public static VehicleMoveMap Parse(string text)
    {
        var rules = new List<VehicleRule>();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("priority", StringComparison.OrdinalIgnoreCase)) continue; // header
            var c = line.Split(',');
            if (c.Length < 5) continue;
            int.TryParse(c[0].Trim(), out int prio);
            double.TryParse(c[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
            var move = c[4].Trim();    // case preserved (games compare case-insensitively)
            if (move.Length == 0) continue;
            rules.Add(new VehicleRule(prio, c[1].Trim(), c[2].Trim(), val, move));
        }
        if (rules.Count == 0) rules.Add(new VehicleRule(0, "default", "", 0, "go"));
        return new VehicleMoveMap(rules);
    }

    public static VehicleMoveMap Load(string path) => Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));

    /// <summary>Resolve the move for the current signal values.</summary>
    public string Resolve(Func<string, double> signal)
    {
        foreach (var r in _rules)
        {
            if (string.Equals(r.Signal, "default", StringComparison.OrdinalIgnoreCase) || r.Signal.Length == 0)
                return r.Move;
            if (Holds(signal(r.Signal), r.Op, r.Value)) return r.Move;
        }
        return "go";
    }

    private static bool Holds(double actual, string op, double value) => op switch
    {
        ">" => actual > value,
        "<" => actual < value,
        ">=" => actual >= value,
        "<=" => actual <= value,
        "==" => Math.Abs(actual - value) < 1e-6,
        "!=" => Math.Abs(actual - value) >= 1e-6,
        _ => false,
    };
}
