using System.IO;
using System.Text.RegularExpressions;

namespace MindedOS.Engine;

/// <summary>
/// Parses an AutoHotkey GUI template (e.g. Program100.ahk) into control specs.
/// Lines look like: Gui,Add,Button,x10 y235 w461 h23,&OK
/// The control type, x/y/w/h and trailing label are lifted into a <see cref="ControlSpec"/>.
/// </summary>
public static partial class AhkTemplate
{
    [GeneratedRegex(@"Gui\s*,\s*Add\s*,\s*(?<type>\w+)\s*,\s*(?<opts>[^,]*)(?:,(?<label>.*))?$",
        RegexOptions.IgnoreCase)]
    private static partial Regex GuiAddLine();

    [GeneratedRegex(@"\b(?<k>[xywh])(?<v>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex DimToken();

    public static List<ControlSpec> Parse(string ahkText)
    {
        var controls = new List<ControlSpec>();
        foreach (var rawLine in ahkText.Split('\n'))
        {
            var line = rawLine.Trim();
            var m = GuiAddLine().Match(line);
            if (!m.Success) continue;

            var type = m.Groups["type"].Value;
            if (type.Equals("Show", StringComparison.OrdinalIgnoreCase)) continue;

            var spec = new ControlSpec { Type = MapType(type) };

            foreach (Match d in DimToken().Matches(m.Groups["opts"].Value))
            {
                double v = double.Parse(d.Groups["v"].Value);
                switch (char.ToLowerInvariant(d.Groups["k"].Value[0]))
                {
                    case 'x': spec.X = v; break;
                    case 'y': spec.Y = v; break;
                    case 'w': spec.W = v; break;
                    case 'h': spec.H = v; break;
                }
            }

            var label = m.Groups["label"].Value.Trim().Replace("&", "").TrimEnd('|');
            spec.Label = string.IsNullOrWhiteSpace(label) ? type : label;
            controls.Add(spec);
        }
        return controls;
    }

    public static List<ControlSpec>? TryLoad(string templatesDir, string templateName)
    {
        if (string.IsNullOrWhiteSpace(templatesDir)) return null;
        var name = templateName.EndsWith(".ahk", StringComparison.OrdinalIgnoreCase)
            ? templateName : templateName + ".ahk";
        var path = Path.Combine(templatesDir, name);
        if (!File.Exists(path)) return null;
        return Parse(MindedOS.Core.FileCrypto.ReadTextMaybeEncrypted(path));
    }

    /// <summary>Map AHK control names to the WPF control kinds the renderer knows.</summary>
    public static string MapType(string ahk) => ahk.ToLowerInvariant() switch
    {
        "button" => "Button",
        "checkbox" => "CheckBox",
        "radio" => "RadioButton",
        "slider" => "Slider",
        "combobox" => "ComboBox",
        "dropdownlist" => "ComboBox",
        "ddl" => "ComboBox",
        "listbox" => "ListBox",
        "listview" => "ListView",
        "edit" => "TextBox",
        "progress" => "ProgressBar",
        "text" => "Text",
        "link" => "Text",
        "datetime" => "TextBox",
        "updown" => "Slider",
        "hotkey" => "TextBox",
        "picture" => "Text",
        _ => "Text",
    };
}
