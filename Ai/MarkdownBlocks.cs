namespace MindedOS.Ai;

public enum BlockKind { Title, H1, H2, Body, Bullet }

/// <summary>
/// Minimal Markdown reader shared by the .docx and .pdf article writers. Splits a
/// document into block elements and resolves inline **bold** / *italic* spans.
/// </summary>
public static class MarkdownBlocks
{
    public static IEnumerable<(BlockKind kind, string text)> Parse(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var bodyBuf = new List<string>();

        IEnumerable<(BlockKind, string)> FlushBody()
        {
            if (bodyBuf.Count == 0) yield break;
            yield return (BlockKind.Body, string.Join(" ", bodyBuf));
            bodyBuf.Clear();
        }

        foreach (var raw in lines)
        {
            var t = raw.Trim();
            if (t.Length == 0) { foreach (var b in FlushBody()) yield return b; continue; }

            if (t.StartsWith("### ")) { foreach (var b in FlushBody()) yield return b; yield return (BlockKind.H2, t[4..]); }
            else if (t.StartsWith("## ")) { foreach (var b in FlushBody()) yield return b; yield return (BlockKind.H1, t[3..]); }
            else if (t.StartsWith("# ")) { foreach (var b in FlushBody()) yield return b; yield return (BlockKind.Title, t[2..]); }
            else if (t.StartsWith("- ") || t.StartsWith("* ")) { foreach (var b in FlushBody()) yield return b; yield return (BlockKind.Bullet, t[2..]); }
            else bodyBuf.Add(t);
        }
        foreach (var b in FlushBody()) yield return b;
    }

    /// <summary>Split a line into runs honoring **bold** and *italic* markers.</summary>
    public static IEnumerable<(string text, bool bold, bool italic)> Inline(string s)
    {
        var segs = new List<(string, bool, bool)>();
        var buf = new System.Text.StringBuilder();
        bool bold = false, italic = false;
        void Flush() { if (buf.Length > 0) { segs.Add((buf.ToString(), bold, italic)); buf.Clear(); } }

        for (int i = 0; i < s.Length;)
        {
            if (i + 1 < s.Length && s[i] == '*' && s[i + 1] == '*') { Flush(); bold = !bold; i += 2; }
            else if (s[i] == '*') { Flush(); italic = !italic; i += 1; }
            else { buf.Append(s[i]); i += 1; }
        }
        Flush();
        if (segs.Count == 0) segs.Add((s, false, false));
        return segs;
    }
}
