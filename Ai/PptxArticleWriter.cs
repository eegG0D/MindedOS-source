using System.IO;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

namespace MindedOS.Ai;

/// <summary>One slide: a title and a list of bullet points.</summary>
public sealed record SlideContent(string Title, IReadOnlyList<string> Bullets);

/// <summary>
/// Builds a valid .pptx presentation (Open XML SDK) with a minimal slide
/// master / layout / theme and one title+bullets slide per <see cref="SlideContent"/>,
/// all text in the requested font (Verdana by default). 16:9, 1-inch-ish margins.
/// </summary>
public static class PptxArticleWriter
{
    private const string AMain = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private const string RNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PNs = "http://schemas.openxmlformats.org/presentationml/2006/main";

    public static void Write(IReadOnlyList<SlideContent> slides, string path, string font = "Verdana")
    {
        var list = slides.Count > 0 ? slides : new[] { new SlideContent("Untitled", new[] { "No content" }) };

        using var doc = PresentationDocument.Create(path, PresentationDocumentType.Presentation);
        var pp = doc.AddPresentationPart();
        var masterPart = pp.AddNewPart<SlideMasterPart>();
        var layoutPart = masterPart.AddNewPart<SlideLayoutPart>();
        var themePart = masterPart.AddNewPart<ThemePart>();

        WriteXml(themePart, ThemeXml(font));
        WriteXml(layoutPart, LayoutXml());
        WriteXml(masterPart, MasterXml(masterPart.GetIdOfPart(layoutPart), font));

        var slideParts = new List<SlidePart>();
        foreach (var slide in list)
        {
            var sp = pp.AddNewPart<SlidePart>();
            sp.AddPart(layoutPart);                 // slide -> layout relationship
            WriteXml(sp, SlideXml(slide, font));
            slideParts.Add(sp);
        }

        var masterRid = pp.GetIdOfPart(masterPart);
        var slideRids = slideParts.Select(pp.GetIdOfPart).ToList();
        WriteXml(pp, PresentationXml(masterRid, slideRids));
    }

    /// <summary>Parse LM Studio output ("SLIDE n: title" + "- bullet" lines) into slides.</summary>
    public static List<SlideContent> ParseSlides(string text, int max = 6)
    {
        var slides = new List<SlideContent>();
        string? title = null;
        var bullets = new List<string>();

        void Flush()
        {
            if (title is null && bullets.Count == 0) return;
            slides.Add(new SlideContent(title ?? $"Slide {slides.Count + 1}", new List<string>(bullets)));
            title = null; bullets.Clear();
        }

        foreach (var raw in text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'))
        {
            var t = raw.Trim();
            if (t.Length == 0) continue;

            var slideMatch = System.Text.RegularExpressions.Regex.Match(t, @"^(?:#+\s*)?slide\s*\d+\s*[:\-\.]\s*(.+)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (slideMatch.Success) { Flush(); title = Strip(slideMatch.Groups[1].Value); continue; }
            if (t.StartsWith("# ")) { Flush(); title = Strip(t[2..]); continue; }
            if (t.StartsWith("## ")) { Flush(); title = Strip(t[3..]); continue; }

            if (title is null) { title = Strip(t); continue; } // first line as title
            if (t.StartsWith("- ") || t.StartsWith("* ")) bullets.Add(Strip(t[2..]));
            else bullets.Add(Strip(t));
        }
        Flush();

        if (slides.Count > max) slides = slides.GetRange(0, max);
        return slides;
    }

    private static string Strip(string s) => s.Replace("**", "").Replace("*", "").Trim();

    // ---- part IO ----------------------------------------------------------
    private static void WriteXml(OpenXmlPart part, string xml)
    {
        using var stream = part.GetStream(FileMode.Create, FileAccess.Write);
        var bytes = new UTF8Encoding(false).GetBytes(xml);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string Esc(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // ---- XML builders -----------------------------------------------------
    private static string PresentationXml(string masterRid, List<string> slideRids)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append($"<p:presentation xmlns:a=\"{AMain}\" xmlns:r=\"{RNs}\" xmlns:p=\"{PNs}\" saveSubsetFonts=\"1\">");
        sb.Append($"<p:sldMasterIdLst><p:sldMasterId id=\"2147483648\" r:id=\"{masterRid}\"/></p:sldMasterIdLst>");
        sb.Append("<p:sldIdLst>");
        for (int i = 0; i < slideRids.Count; i++)
            sb.Append($"<p:sldId id=\"{256 + i}\" r:id=\"{slideRids[i]}\"/>");
        sb.Append("</p:sldIdLst>");
        sb.Append("<p:sldSz cx=\"12192000\" cy=\"6858000\"/><p:notesSz cx=\"6858000\" cy=\"9144000\"/>");
        sb.Append("</p:presentation>");
        return sb.ToString();
    }

    private static string SlideXml(SlideContent slide, string font)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append($"<p:sld xmlns:a=\"{AMain}\" xmlns:r=\"{RNs}\" xmlns:p=\"{PNs}\"><p:cSld><p:spTree>");
        sb.Append("<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>");
        sb.Append("<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>");

        // Title shape
        sb.Append("<p:sp><p:nvSpPr><p:cNvPr id=\"2\" name=\"Title\"/><p:cNvSpPr><a:spLocks noGrp=\"1\"/></p:cNvSpPr><p:nvPr><p:ph type=\"title\"/></p:nvPr></p:nvSpPr>");
        sb.Append("<p:spPr><a:xfrm><a:off x=\"685800\" y=\"400050\"/><a:ext cx=\"10820400\" cy=\"1238250\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>");
        sb.Append("<p:txBody><a:bodyPr anchor=\"ctr\"/><a:lstStyle/>");
        sb.Append("<a:p><a:pPr algn=\"l\"/><a:r>");
        sb.Append($"<a:rPr lang=\"en-US\" sz=\"3200\" b=\"1\" dirty=\"0\"><a:solidFill><a:srgbClr val=\"1A1F26\"/></a:solidFill><a:latin typeface=\"{Esc(font)}\"/></a:rPr>");
        sb.Append($"<a:t>{Esc(slide.Title)}</a:t></a:r></a:p></p:txBody></p:sp>");

        // Body shape
        sb.Append("<p:sp><p:nvSpPr><p:cNvPr id=\"3\" name=\"Body\"/><p:cNvSpPr><a:spLocks noGrp=\"1\"/></p:cNvSpPr><p:nvPr><p:ph type=\"body\" idx=\"1\"/></p:nvPr></p:nvSpPr>");
        sb.Append("<p:spPr><a:xfrm><a:off x=\"685800\" y=\"1781175\"/><a:ext cx=\"10820400\" cy=\"4525963\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>");
        sb.Append("<p:txBody><a:bodyPr/><a:lstStyle/>");
        if (slide.Bullets.Count == 0)
            sb.Append("<a:p><a:endParaRPr lang=\"en-US\"/></a:p>");
        foreach (var bullet in slide.Bullets)
        {
            sb.Append("<a:p><a:pPr marL=\"285750\" indent=\"-285750\"><a:buFont typeface=\"Arial\"/><a:buChar char=\"•\"/></a:pPr>");
            sb.Append("<a:r>");
            sb.Append($"<a:rPr lang=\"en-US\" sz=\"1800\" dirty=\"0\"><a:solidFill><a:srgbClr val=\"262626\"/></a:solidFill><a:latin typeface=\"{Esc(font)}\"/></a:rPr>");
            sb.Append($"<a:t>{Esc(bullet)}</a:t></a:r></a:p>");
        }
        sb.Append("</p:txBody></p:sp>");

        sb.Append("</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>");
        return sb.ToString();
    }

    private static string LayoutXml()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append($"<p:sldLayout xmlns:a=\"{AMain}\" xmlns:r=\"{RNs}\" xmlns:p=\"{PNs}\" type=\"obj\" preserve=\"1\"><p:cSld name=\"Title and Content\"><p:spTree>");
        sb.Append("<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>");
        sb.Append("<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>");
        sb.Append("</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sldLayout>");
        return sb.ToString();
    }

    private static string MasterXml(string layoutRid, string font)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append($"<p:sldMaster xmlns:a=\"{AMain}\" xmlns:r=\"{RNs}\" xmlns:p=\"{PNs}\"><p:cSld>");
        sb.Append("<p:bg><p:bgRef idx=\"1001\"><a:schemeClr val=\"bg1\"/></p:bgRef></p:bg><p:spTree>");
        sb.Append("<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>");
        sb.Append("<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>");
        sb.Append("</p:spTree></p:cSld>");
        sb.Append("<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>");
        sb.Append($"<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"{layoutRid}\"/></p:sldLayoutIdLst>");
        sb.Append("<p:txStyles>");
        sb.Append($"<p:titleStyle><a:lvl1pPr><a:defRPr sz=\"3200\" b=\"1\"><a:latin typeface=\"{Esc(font)}\"/></a:defRPr></a:lvl1pPr></p:titleStyle>");
        sb.Append($"<p:bodyStyle><a:lvl1pPr marL=\"285750\" indent=\"-285750\"><a:buChar char=\"•\"/><a:defRPr sz=\"1800\"><a:latin typeface=\"{Esc(font)}\"/></a:defRPr></a:lvl1pPr></p:bodyStyle>");
        sb.Append($"<p:otherStyle><a:lvl1pPr><a:defRPr><a:latin typeface=\"{Esc(font)}\"/></a:defRPr></a:lvl1pPr></p:otherStyle>");
        sb.Append("</p:txStyles></p:sldMaster>");
        return sb.ToString();
    }

    private static string ThemeXml(string font)
    {
        // Minimal but complete Office theme (clr/font/fmt schemes) with the chosen font.
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append($"<a:theme xmlns:a=\"{AMain}\" name=\"mindedOS\"><a:themeElements>");
        sb.Append("<a:clrScheme name=\"mindedOS\">");
        sb.Append("<a:dk1><a:sysClr val=\"windowText\" lastClr=\"000000\"/></a:dk1><a:lt1><a:sysClr val=\"window\" lastClr=\"FFFFFF\"/></a:lt1>");
        sb.Append("<a:dk2><a:srgbClr val=\"1A1F26\"/></a:dk2><a:lt2><a:srgbClr val=\"F2F4F7\"/></a:lt2>");
        sb.Append("<a:accent1><a:srgbClr val=\"0F9D58\"/></a:accent1><a:accent2><a:srgbClr val=\"2196F3\"/></a:accent2><a:accent3><a:srgbClr val=\"C2185B\"/></a:accent3>");
        sb.Append("<a:accent4><a:srgbClr val=\"FF9800\"/></a:accent4><a:accent5><a:srgbClr val=\"7B1FA2\"/></a:accent5><a:accent6><a:srgbClr val=\"607D8B\"/></a:accent6>");
        sb.Append("<a:hlink><a:srgbClr val=\"0F9D58\"/></a:hlink><a:folHlink><a:srgbClr val=\"7B1FA2\"/></a:folHlink></a:clrScheme>");
        sb.Append($"<a:fontScheme name=\"mindedOS\"><a:majorFont><a:latin typeface=\"{Esc(font)}\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:majorFont>");
        sb.Append($"<a:minorFont><a:latin typeface=\"{Esc(font)}\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:minorFont></a:fontScheme>");
        sb.Append("<a:fmtScheme name=\"mindedOS\">");
        sb.Append("<a:fillStyleLst>");
        sb.Append("<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>");
        sb.Append("<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>");
        sb.Append("<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>");
        sb.Append("</a:fillStyleLst>");
        sb.Append("<a:lnStyleLst>");
        sb.Append("<a:ln w=\"6350\" cap=\"flat\" cmpd=\"sng\" algn=\"ctr\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill><a:prstDash val=\"solid\"/></a:ln>");
        sb.Append("<a:ln w=\"12700\" cap=\"flat\" cmpd=\"sng\" algn=\"ctr\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill><a:prstDash val=\"solid\"/></a:ln>");
        sb.Append("<a:ln w=\"19050\" cap=\"flat\" cmpd=\"sng\" algn=\"ctr\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill><a:prstDash val=\"solid\"/></a:ln>");
        sb.Append("</a:lnStyleLst>");
        sb.Append("<a:effectStyleLst>");
        sb.Append("<a:effectStyle><a:effectLst/></a:effectStyle>");
        sb.Append("<a:effectStyle><a:effectLst/></a:effectStyle>");
        sb.Append("<a:effectStyle><a:effectLst/></a:effectStyle>");
        sb.Append("</a:effectStyleLst>");
        sb.Append("<a:bgFillStyleLst>");
        sb.Append("<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>");
        sb.Append("<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>");
        sb.Append("<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>");
        sb.Append("</a:bgFillStyleLst>");
        sb.Append("</a:fmtScheme></a:themeElements></a:theme>");
        return sb.ToString();
    }
}
