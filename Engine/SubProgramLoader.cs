using System.IO;
using System.Text.Json;

namespace MindedOS.Engine;

/// <summary>Loads sub-program JSON files and merges any referenced AHK template.</summary>
public sealed class SubProgramLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string? _templatesDir;

    /// <param name="templatesDir">Optional dir holding ProgramN.ahk templates.</param>
    public SubProgramLoader(string? templatesDir = null) => _templatesDir = templatesDir;

    public SubProgram LoadFile(string path)
    {
        var program = JsonSerializer.Deserialize<SubProgram>(File.ReadAllText(path), Options)
                      ?? throw new InvalidDataException($"Empty or invalid program: {path}");
        program.SourcePath = path;

        // If a template is named, seed its controls first, then append JSON controls.
        if (!string.IsNullOrWhiteSpace(program.Template) && _templatesDir is not null)
        {
            var templ = AhkTemplate.TryLoad(_templatesDir, program.Template!);
            if (templ is { Count: > 0 })
            {
                var merged = new List<ControlSpec>(templ);
                merged.AddRange(program.Controls);
                program.Controls = merged;
            }
        }
        return program;
    }

    public IReadOnlyList<SubProgram> LoadDirectory(string dir)
    {
        var list = new List<SubProgram>();
        if (!Directory.Exists(dir)) return list;
        foreach (var path in Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories)
                                      .OrderBy(p => p))
        {
            try { list.Add(LoadFile(path)); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Skipping {path}: {ex.Message}");
            }
        }
        return list;
    }
}
