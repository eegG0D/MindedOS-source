using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace MindedOS.Ai;

/// <summary>
/// Renders a Markdown article into a cleanly formatted PDF using MigraDoc/PDFsharp
/// (GDI build, so installed fonts like Verdana resolve automatically): centered
/// title, bold headings, justified body, bullet lists and 1-inch margins.
/// </summary>
public static class PdfArticleWriter
{
    public static void Write(string markdown, string path, string font = "Verdana")
    {
        var doc = new Document();
        BuildStyles(doc, font);

        var section = doc.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromInch(1);
        section.PageSetup.BottomMargin = Unit.FromInch(1);
        section.PageSetup.LeftMargin = Unit.FromInch(1);
        section.PageSetup.RightMargin = Unit.FromInch(1);

        foreach (var (kind, text) in MarkdownBlocks.Parse(markdown))
            AddBlock(section, kind, text);

        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(path);
    }

    private static void BuildStyles(Document doc, string font)
    {
        var normal = doc.Styles["Normal"]!;
        normal.Font.Name = font;
        normal.Font.Size = 11;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(8);
        normal.ParagraphFormat.LineSpacingRule = LineSpacingRule.Multiple;
        normal.ParagraphFormat.LineSpacing = 1.3;

        var title = doc.Styles.AddStyle("ArticleTitle", "Normal");
        title.Font.Name = font; title.Font.Size = 22; title.Font.Bold = true;
        title.ParagraphFormat.Alignment = ParagraphAlignment.Center;
        title.ParagraphFormat.SpaceBefore = Unit.FromPoint(6);
        title.ParagraphFormat.SpaceAfter = Unit.FromPoint(14);

        var h1 = doc.Styles.AddStyle("ArticleH1", "Normal");
        h1.Font.Name = font; h1.Font.Size = 16; h1.Font.Bold = true;
        h1.ParagraphFormat.SpaceBefore = Unit.FromPoint(12);
        h1.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);

        var h2 = doc.Styles.AddStyle("ArticleH2", "Normal");
        h2.Font.Name = font; h2.Font.Size = 13; h2.Font.Bold = true;
        h2.ParagraphFormat.SpaceBefore = Unit.FromPoint(8);
        h2.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);

        var body = doc.Styles.AddStyle("ArticleBody", "Normal");
        body.ParagraphFormat.Alignment = ParagraphAlignment.Justify;

        var bullet = doc.Styles.AddStyle("ArticleBullet", "Normal");
        bullet.ParagraphFormat.LeftIndent = Unit.FromInch(0.4);
        bullet.ParagraphFormat.FirstLineIndent = Unit.FromInch(-0.2);
        bullet.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);
    }

    private static void AddBlock(Section section, BlockKind kind, string text)
    {
        var (style, bullet) = kind switch
        {
            BlockKind.Title => ("ArticleTitle", false),
            BlockKind.H1 => ("ArticleH1", false),
            BlockKind.H2 => ("ArticleH2", false),
            BlockKind.Bullet => ("ArticleBullet", true),
            _ => ("ArticleBody", false),
        };

        var p = section.AddParagraph();
        p.Style = style;
        if (bullet) p.AddText("•  ");
        foreach (var (seg, b, i) in MarkdownBlocks.Inline(text))
        {
            var ft = p.AddFormattedText(seg);
            if (b) ft.Bold = true;
            if (i) ft.Italic = true;
        }
    }
}
