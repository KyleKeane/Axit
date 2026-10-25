using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace AxPreview.Core;

/// <summary>
/// The text of a Word document, a PowerPoint presentation or an OpenDocument text (SPEC.md AP-7.2, 2): each is a zip
/// of XML, read with the framework's own zip and XML classes. A paragraph is a line; a heading the document marks is
/// a Markdown heading ("## Install"), a slide is "## Slide 3", a list item starts with "- ", and a table row is its
/// cells separated by tabs, so that the viewer's Markdown keys move through the structure. Tested in
/// <c>tests/AxPreview.Tests/RenderersTests</c>.
/// </summary>
public static class OfficeText
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace OfficeNs = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
    private static readonly XNamespace TextNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
    private static readonly XNamespace TableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";

    public static Rendered Render(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        var counts = new Counts();
        var (lines, kind) = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".docx" => (Word(zip, counts), "Word document"),
            ".pptx" => (PowerPoint(zip, counts), "PowerPoint presentation"),
            _ => (OpenDocument(zip, counts), "OpenDocument text"),
        };
        var contents = new List<string> { $"Format: {kind}" };
        if (counts.Slides > 0)
        {
            contents.Add($"Slides: {counts.Slides:N0}");
        }

        contents.Add($"Paragraphs: {counts.Paragraphs:N0}");
        contents.Add($"Headings: {counts.Headings:N0}");
        contents.Add($"Tables: {counts.Tables:N0}");
        if (lines.Count == 0)
        {
            lines.Add($"{Path.GetFileName(path)} has no text.");
        }

        return Rendered.Of(lines, kind, contents, WayOut.Windows);
    }

    private sealed class Counts
    {
        public int Paragraphs;
        public int Headings;
        public int Tables;
        public int Slides;

        /// <summary>The bytes the document's parts have unpacked to so far.</summary>
        public long Unpacked;
    }

    /// <summary>
    /// The most all the parts of one document may unpack to, counted on the bytes as they are read rather than taken
    /// from the zip's own record of their sizes: a file made to unpack to gigabytes stops here, and the readable text is
    /// shown with the reason (security review S-47, 2026-10-25; until then each part might unpack to 256 MB and be held
    /// whole, a few gigabytes for a file of a few hundred kilobytes). The editors' limit, as much text as a window holds.
    /// </summary>
    private const long MaxUnpacked = TextSniff.MaxBytes;

    /// <summary>XML read without a DTD and without resolving anything outside the part (S-48): a part that declares one is refused, and the file is shown as its readable text.</summary>
    private static readonly XmlReaderSettings Xml = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };

    /// <summary>A part by its name, a backslash taken as a slash: zips written by Windows PowerShell 5.1 name them so (found by the window's script, 2026-10-24).</summary>
    private static XDocument? Part(ZipArchive zip, string name, Counts counts) =>
        zip.Entries.FirstOrDefault(entry => EntryName(entry) == name) is { } entry ? Load(entry, counts) : null;

    private static string EntryName(ZipArchiveEntry entry) => entry.FullName.Replace('\\', '/');

    /// <summary>One XML part, its unpacked bytes counted against the document's budget.</summary>
    private static XDocument Load(ZipArchiveEntry entry, Counts counts)
    {
        using var stream = new Budget(entry.Open(), counts);
        using var reader = XmlReader.Create(stream, Xml);
        return XDocument.Load(reader);
    }

    /// <summary>A part's stream that counts what it gives against <see cref="MaxUnpacked"/> for the whole document.</summary>
    private sealed class Budget(Stream inner, Counts counts) : Stream
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            counts.Unpacked += read;
            if (counts.Unpacked > MaxUnpacked)
            {
                throw new InvalidDataException($"it unpacks to more than {MaxUnpacked / (1024 * 1024)} MB");
            }

            return read;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    // ---- Word: word/document.xml, the body's paragraphs and tables in order ----

    private static List<string> Word(ZipArchive zip, Counts counts)
    {
        var lines = new List<string>();
        if (Part(zip, "word/document.xml", counts)?.Root?.Element(W + "body") is not { } body)
        {
            return lines;
        }

        foreach (var block in body.Elements())
        {
            if (block.Name == W + "p")
            {
                AddWordParagraph(block, lines, counts);
            }
            else if (block.Name == W + "tbl")
            {
                counts.Tables++;
                foreach (var row in block.Elements(W + "tr"))
                {
                    lines.Add(string.Join("\t", row.Elements(W + "tc").Select(cell => string.Join(" ", cell.Elements(W + "p").Select(WordRuns)))));
                }

                lines.Add(string.Empty);
            }
        }

        return lines;
    }

    private static void AddWordParagraph(XElement paragraph, List<string> lines, Counts counts)
    {
        var text = WordRuns(paragraph);
        if (text.Length == 0)
        {
            return;
        }

        counts.Paragraphs++;
        var properties = paragraph.Element(W + "pPr");
        var style = properties?.Element(W + "pStyle")?.Attribute(W + "val")?.Value ?? string.Empty;
        if (HeadingLevel(style) is { } level)
        {
            counts.Headings++;
            lines.Add(new string('#', level) + " " + text);
        }
        else if (properties?.Element(W + "numPr") is not null)
        {
            lines.Add("- " + text);
        }
        else
        {
            lines.Add(text);
        }
    }

    /// <summary>"Heading2" and "heading 2" are level 2, "Title" level 1; Word's own style ids, and the names of documents made elsewhere.</summary>
    private static int? HeadingLevel(string style)
    {
        if (style.Equals("Title", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        var match = Regex.Match(style, @"^heading\s*([1-6])$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value[0] - '0' : null;
    }

    /// <summary>A paragraph's text: its runs' text, a tab as a tab, a break as a space.</summary>
    private static string WordRuns(XElement paragraph)
    {
        var text = new StringBuilder();
        foreach (var element in paragraph.Descendants())
        {
            if (element.Name == W + "t")
            {
                text.Append(element.Value);
            }
            else if (element.Name == W + "tab")
            {
                text.Append('\t');
            }
            else if (element.Name == W + "br" || element.Name == W + "cr")
            {
                text.Append(' ');
            }
        }

        return text.ToString().Trim();
    }

    // ---- PowerPoint: ppt/slides/slideN.xml in the order of their numbers ----

    private static List<string> PowerPoint(ZipArchive zip, Counts counts)
    {
        var lines = new List<string>();
        var slides = zip.Entries
            .Select(entry => (entry, match: Regex.Match(EntryName(entry), @"^ppt/slides/slide(\d+)\.xml$")))
            .Where(slide => slide.match.Success)
            .OrderBy(slide => int.Parse(slide.match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .Select(slide => slide.entry);
        foreach (var slide in slides)
        {
            counts.Slides++;
            counts.Headings++;
            lines.Add($"## Slide {counts.Slides}");
            var document = Load(slide, counts);
            foreach (var paragraph in document.Descendants(A + "p"))
            {
                var text = string.Concat(paragraph.Descendants(A + "t").Select(t => t.Value)).Trim();
                if (text.Length > 0)
                {
                    counts.Paragraphs++;
                    lines.Add(text);
                }
            }

            lines.Add(string.Empty);
        }

        return lines;
    }

    // ---- OpenDocument text: content.xml, the office:text body walked in order ----

    private static List<string> OpenDocument(ZipArchive zip, Counts counts)
    {
        var lines = new List<string>();
        if (Part(zip, "content.xml", counts)?.Root?.Element(OfficeNs + "body")?.Element(OfficeNs + "text") is { } body)
        {
            AddOpenDocumentBlocks(body, lines, counts, inList: false);
        }

        return lines;
    }

    private static void AddOpenDocumentBlocks(XElement parent, List<string> lines, Counts counts, bool inList)
    {
        foreach (var block in parent.Elements())
        {
            if (block.Name == TextNs + "h")
            {
                var text = OpenDocumentText(block);
                if (text.Length > 0)
                {
                    counts.Paragraphs++;
                    counts.Headings++;
                    var level = int.TryParse(block.Attribute(TextNs + "outline-level")?.Value, out var value) ? Math.Clamp(value, 1, 6) : 1;
                    lines.Add(new string('#', level) + " " + text);
                }
            }
            else if (block.Name == TextNs + "p")
            {
                var text = OpenDocumentText(block);
                if (text.Length > 0)
                {
                    counts.Paragraphs++;
                    lines.Add(inList ? "- " + text : text);
                }
            }
            else if (block.Name == TextNs + "list" || block.Name == TextNs + "list-item" || block.Name == TextNs + "section")
            {
                AddOpenDocumentBlocks(block, lines, counts, inList || block.Name != TextNs + "section");
            }
            else if (block.Name == TableNs + "table")
            {
                counts.Tables++;
                foreach (var row in block.Descendants(TableNs + "table-row"))
                {
                    lines.Add(string.Join("\t", row.Elements(TableNs + "table-cell").Select(cell => string.Join(" ", cell.Elements(TextNs + "p").Select(OpenDocumentText)))));
                }

                lines.Add(string.Empty);
            }
        }
    }

    /// <summary>A paragraph's text: its text nodes, text:s as its count of spaces, text:tab as a tab, text:line-break as a space.</summary>
    private static string OpenDocumentText(XElement paragraph)
    {
        var text = new StringBuilder();
        AppendOpenDocument(paragraph, text);
        return text.ToString().Trim();
    }

    private static void AppendOpenDocument(XElement element, StringBuilder text)
    {
        foreach (var node in element.Nodes())
        {
            if (node is XText plain)
            {
                text.Append(plain.Value);
            }
            else if (node is XElement child)
            {
                if (child.Name == TextNs + "s")
                {
                    text.Append(' ', int.TryParse(child.Attribute(TextNs + "c")?.Value, out var count) ? Math.Clamp(count, 1, 100) : 1);
                }
                else if (child.Name == TextNs + "tab")
                {
                    text.Append('\t');
                }
                else if (child.Name == TextNs + "line-break")
                {
                    text.Append(' ');
                }
                else if (child.Name != TextNs + "note")
                {
                    AppendOpenDocument(child, text);
                }
            }
        }
    }
}
