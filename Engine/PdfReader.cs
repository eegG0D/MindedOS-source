using System.Text;
using UglyToad.PdfPig;

namespace MindedOS.Engine;

/// <summary>Extracts text from PDF documents so LM Studio can read them.</summary>
public static class PdfReader
{
    /// <summary>Extract up to <paramref name="maxChars"/> characters of text from a PDF.</summary>
    public static string ExtractText(string path, int maxChars = 4000)
    {
        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(path);
        foreach (var page in doc.GetPages())
        {
            sb.Append(page.Text).Append('\n');
            if (sb.Length >= maxChars) break;
        }
        var text = sb.ToString().Trim();
        return text.Length > maxChars ? text[..maxChars] : text;
    }

    public static int PageCount(string path)
    {
        using var doc = PdfDocument.Open(path);
        return doc.NumberOfPages;
    }
}
