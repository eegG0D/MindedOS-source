using System.IO;
using System.Text.Json;

namespace MindedOS.Engine;

/// <summary>Loads EEG sequence JSON files from the sequences/ folder.</summary>
public sealed class SequenceLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _sequencesDir;

    public SequenceLoader(string sequencesDir) => _sequencesDir = sequencesDir;

    public EegSequence LoadFile(string path)
    {
        var seq = JsonSerializer.Deserialize<EegSequence>(File.ReadAllText(path), Options)
                  ?? throw new InvalidDataException($"Empty or invalid sequence: {path}");
        seq.SourcePath = path;
        return seq;
    }

    /// <summary>Resolve a sequence by file name (with or without .json) under the dir.</summary>
    public EegSequence? Resolve(string name)
    {
        var file = name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? name : name + ".json";
        var path = Path.IsPathRooted(file) ? file : Path.Combine(_sequencesDir, file);
        if (!File.Exists(path)) return null;
        return LoadFile(path);
    }
}
