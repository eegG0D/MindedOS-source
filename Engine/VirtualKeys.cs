namespace MindedOS.Engine;

/// <summary>Maps the key names used in actions.txt to Win32 virtual-key codes.</summary>
public static class VirtualKeys
{
    public static readonly Dictionary<string, byte> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // modifiers
        ["CTRL"] = 0x11, ["CONTROL"] = 0x11, ["ALT"] = 0x12, ["MENU"] = 0x12,
        ["SHIFT"] = 0x10, ["WIN"] = 0x5B, ["LWIN"] = 0x5B,
        // navigation / editing
        ["ENTER"] = 0x0D, ["RETURN"] = 0x0D, ["ESCAPE"] = 0x1B, ["ESC"] = 0x1B,
        ["TAB"] = 0x09, ["SPACE"] = 0x20, ["BACK"] = 0x08, ["BACKSPACE"] = 0x08,
        ["DELETE"] = 0x2E, ["DEL"] = 0x2E, ["INSERT"] = 0x2D,
        ["UP"] = 0x26, ["DOWN"] = 0x28, ["LEFT"] = 0x25, ["RIGHT"] = 0x27,
        ["PRIOR"] = 0x21, ["PAGEUP"] = 0x21, ["PGUP"] = 0x21,
        ["NEXT"] = 0x22, ["PAGEDOWN"] = 0x22, ["PGDN"] = 0x22,
        ["HOME"] = 0x24, ["END"] = 0x23,
        ["CAPITAL"] = 0x14, ["NUMLOCK"] = 0x90, ["SCROLL"] = 0x91,
        ["SNAPSHOT"] = 0x2C, ["PRINTSCREEN"] = 0x2C, ["APPS"] = 0x5D,
        // punctuation used in combos
        ["PLUS"] = 0xBB, ["MINUS"] = 0xBD, ["PERIOD"] = 0xBE, ["COMMA"] = 0xBC,
        ["SLASH"] = 0xBF,
        // media
        ["VOLUME_MUTE"] = 0xAD, ["VOLUME_DOWN"] = 0xAE, ["VOLUME_UP"] = 0xAF,
        ["MEDIA_NEXT_TRACK"] = 0xB0, ["MEDIA_PREV_TRACK"] = 0xB1,
        ["MEDIA_STOP"] = 0xB2, ["MEDIA_PLAY_PAUSE"] = 0xB3,
    };

    public static bool TryResolve(string name, out byte vk)
    {
        name = name.Trim();
        if (Map.TryGetValue(name, out vk)) return true;

        // Single letters and digits map to their ASCII upper value.
        if (name.Length == 1)
        {
            char c = char.ToUpperInvariant(name[0]);
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9') { vk = (byte)c; return true; }
        }
        // Function keys F1..F24 -> 0x70..0x87
        if ((name[0] is 'F' or 'f') && int.TryParse(name.AsSpan(1), out int n) && n is >= 1 and <= 24)
        {
            vk = (byte)(0x70 + n - 1);
            return true;
        }
        vk = 0;
        return false;
    }
}
