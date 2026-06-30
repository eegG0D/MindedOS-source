using System.IO;

namespace MindedOS.Engine;

/// <summary>One computer action parsed from actions.txt.</summary>
public sealed record ComputerAction(string Id, string Category, string Name, string Kind, string Payload);

/// <summary>Loads actions.txt (id|category|name|kind|payload) into a lookup.</summary>
public sealed class ActionRegistry
{
    private readonly Dictionary<string, ComputerAction> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ComputerAction> _all = new();

    public IReadOnlyList<ComputerAction> All => _all;
    public int Count => _all.Count;

    public void Load(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var parts = line.Split('|');
            if (parts.Length < 5) continue;

            var action = new ComputerAction(
                parts[0].Trim(), parts[1].Trim(), parts[2].Trim(),
                parts[3].Trim(), parts[4]); // payload kept verbatim (may contain spaces)
            _byId[action.Id] = action;
            _all.Add(action);
        }
    }

    public ComputerAction? Find(string id) =>
        _byId.TryGetValue(id, out var a) ? a : null;

    public IEnumerable<IGrouping<string, ComputerAction>> ByCategory() =>
        _all.GroupBy(a => a.Category);
}
