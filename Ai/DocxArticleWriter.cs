using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace MindedOS.Ai;

/// <summary>
/// Renders a Markdown article into a cleanly formatted .docx using the Open XML
/// SDK: a modern sans-serif body font (Verdana by default) applied via document
/// defaults and per-run, with a centered title, bold headings, justified body,
/// bullet lists and 1-inch margins.
/// </summary>
public static class DocxArticleWriter
{
    public static void Write(string markdown, string path, string font = "Verdana")
    {
        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        AddStyleDefaults(main, font);
        RenderMarkdown(body, markdown, font);
        AddSectionProperties(body);

        main.Document.Save();
    }

    // ---- document-wide default font ---------------------------------------
    private static void AddStyleDefaults(MainDocumentPart main, string font)
    {
        var part = main.AddNewPart<StyleDefinitionsPart>();
        part.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunPropertiesBaseStyle(
                        new RunFonts { Ascii = font, HighAnsi = font, ComplexScript = font },
                        new FontSize { Val = "22" }))));      // 11pt
        part.Styles.Save();
    }

    private static void AddSectionProperties(Body body) =>
        body.AppendChild(new SectionProperties(
            new PageMargin { Top = 1440, Bottom = 1440, Left = 1440, Right = 1440, Header = 720, Footer = 720, Gutter = 0 }));

    // ---- markdown -> paragraphs -------------------------------------------
    private static void RenderMarkdown(Body body, string markdown, string font)
    {
        foreach (var (kind, text) in MarkdownBlocks.Parse(markdown))
            body.AppendChild(BuildParagraph(kind, text, font));
    }

    private static Paragraph BuildParagraph(BlockKind kind, string text, string font)
    {
        var (size, bold) = kind switch
        {
            BlockKind.Title => ("56", true),   // 28pt
            BlockKind.H1 => ("32", true),      // 16pt
            BlockKind.H2 => ("26", true),      // 13pt
            _ => ("22", false),                // 11pt
        };

        var pPr = new ParagraphProperties();
        switch (kind)
        {
            case BlockKind.Title:
                pPr.Append(new Justification { Val = JustificationValues.Center });
                pPr.Append(new SpacingBetweenLines { After = "240", Before = "120" });
                break;
            case BlockKind.H1:
                pPr.Append(new SpacingBetweenLines { Before = "240", After = "120" });
                break;
            case BlockKind.H2:
                pPr.Append(new SpacingBetweenLines { Before = "160", After = "80" });
                break;
            case BlockKind.Bullet:
                pPr.Append(new SpacingBetweenLines { After = "80", Line = "276", LineRule = LineSpacingRuleValues.Auto });
                pPr.Append(new Indentation { Left = "720", Hanging = "360" });
                break;
            default: // Body
                pPr.Append(new Justification { Val = JustificationValues.Both });
                pPr.Append(new SpacingBetweenLines { After = "160", Line = "276", LineRule = LineSpacingRuleValues.Auto });
                break;
        }

        var para = new Paragraph(pPr);
        if (kind == BlockKind.Bullet)
            para.Append(MakeRun("•  ", font, size, bold, false));
        foreach (var (seg, b, i) in MarkdownBlocks.Inline(text))
            para.Append(MakeRun(seg, font, size, bold || b, i));
        return para;
    }

    private static Run MakeRun(string text, string font, string size, bool bold, bool italic)
    {
        var rPr = new RunProperties(
            new RunFonts { Ascii = font, HighAnsi = font, ComplexScript = font },
            new FontSize { Val = size });
        if (bold) rPr.Append(new Bold());
        if (italic) rPr.Append(new Italic());
        return new Run(rPr, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }
}
