namespace Axit.Core;

/// <summary>The kinds of element the navigation keys move by (docs/axdown/SPEC.md §9).</summary>
public enum ElementKind
{
    Heading,
    Link,
    List,
    ListItem,
    BlockQuote,
    Table,
}

/// <summary>Something a navigation key stops at (docs/axdown/SPEC.md §9): where it starts and what is said on landing there.</summary>
public abstract record Element(int Start)
{
    public abstract string Spoken { get; }
}

/// <summary>A heading: its level (1 to 6), its text without the marks, and the line as written.</summary>
public sealed record Heading(int Start, int Level, string Text, string Line) : Element(Start)
{
    public override string Spoken => $"{Text}, heading level {Level}";
}

/// <summary>A link: where it ends, its text (the address itself for a bare or an angle-bracketed one) and its target.</summary>
public sealed record Link(int Start, int End, string Text, string Target) : Element(Start)
{
    public override string Spoken => $"{Text}, link";
}

/// <summary>A list item: its indentation in columns, its marker ("-", "*", "+", "1.", "1)") and its text.</summary>
public sealed record ListItem(int Start, int Indent, string Marker, string Text) : Element(Start)
{
    public override string Spoken => Text.Length == 0 ? "blank" : Text;
}

/// <summary>A list: the items at its own indentation and marker kind; nested items belong to it but are not counted.</summary>
public sealed record MarkdownList(int Start, IReadOnlyList<ListItem> Items) : Element(Start)
{
    public override string Spoken => $"List with {Items.Count} {(Items.Count == 1 ? "item" : "items")}, {Items[0].Spoken}";
}

/// <summary>A block quote: the first line's text without its marks.</summary>
public sealed record BlockQuote(int Start, string Text) : Element(Start)
{
    public override string Spoken => Text.Length == 0 ? "Block quote" : $"Block quote, {Text}";
}

/// <summary>A table: its rows (the header and the body rows) and its columns.</summary>
public sealed record Table(int Start, int Rows, int Columns) : Element(Start)
{
    public override string Spoken => $"Table with {Rows} {(Rows == 1 ? "row" : "rows")} and {Columns} {(Columns == 1 ? "column" : "columns")}";
}

/// <summary>
/// The structure of a Markdown text for the navigation keys (docs/axdown/SPEC.md §9), outside fenced code blocks (lines opening
/// and closing with <c>```</c> or <c>~~~</c>): headings, links, lists and their items, block quotes and tables, each
/// with a next and a previous from a caret position. The block kinds are found by line: next means on a line after
/// the caret's, previous on a line before it, so a jump from inside one never lands on itself. Links are inline:
/// next starts after the caret, previous ends at or before it. Pure: the text in, positions and spoken texts out;
/// the window moves the caret.
/// </summary>
public static class MarkdownOutline
{
    private sealed record TextLine(int Start, string Text, bool InFence);

    // ---- headings ----

    public static IReadOnlyList<Heading> Headings(string text)
    {
        var headings = new List<Heading>();
        foreach (var line in Lines(text))
        {
            if (line.InFence)
            {
                continue;
            }

            var trimmed = line.Text.TrimStart(' ');
            if (line.Text.Length - trimmed.Length > 3)
            {
                continue;
            }

            var level = HeadingLevel(trimmed);
            if (level > 0)
            {
                headings.Add(new Heading(line.Start, level, HeadingText(trimmed, level), line.Text));
            }
        }

        return headings;
    }

    /// <summary>The first heading on a line after the caret's line, of the level given (1 to 6) or of any level for 0; null when there is none.</summary>
    public static Element? NextHeading(string text, int caret, int level = 0) =>
        After(Headings(text).Where(h => level == 0 || h.Level == level), LineStart(text, caret));

    /// <summary>The last heading on a line before the caret's line, of the level given (1 to 6) or of any level for 0; null when there is none.</summary>
    public static Element? PreviousHeading(string text, int caret, int level = 0) =>
        Before(Headings(text).Where(h => level == 0 || h.Level == level), LineStart(text, caret));

    /// <summary>1 to 6 for <c># </c> to <c>###### </c> (or hashes alone); 0 for anything else, including <c>#tag</c>.</summary>
    private static int HeadingLevel(ReadOnlySpan<char> line)
    {
        var level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        if (level is 0 or > 6)
        {
            return 0;
        }

        return level == line.Length || line[level] is ' ' or '\t' ? level : 0;
    }

    /// <summary>The heading without its opening marks, trailing closing hashes and surrounding spaces.</summary>
    private static string HeadingText(ReadOnlySpan<char> line, int level)
    {
        var text = line[level..].Trim();
        var closing = text.Length;
        while (closing > 0 && text[closing - 1] == '#')
        {
            closing--;
        }

        if (closing < text.Length && (closing == 0 || text[closing - 1] is ' ' or '\t'))
        {
            text = text[..closing].TrimEnd();
        }

        return text.ToString();
    }

    // ---- links ----

    /// <summary>
    /// Inline links <c>[text](target)</c>, reference links <c>[text][ref]</c>, angle-bracketed addresses
    /// <c>&lt;https://…&gt;</c> and bare addresses starting with http:// or https://, in order; not images
    /// (<c>![alt](src)</c>) and nothing inside a code span.
    /// </summary>
    public static IReadOnlyList<Link> Links(string text)
    {
        var links = new List<Link>();
        foreach (var line in Lines(text))
        {
            if (line.InFence)
            {
                continue;
            }

            var s = line.Text;
            var inCode = false;
            var i = 0;
            while (i < s.Length)
            {
                var c = s[i];
                if (c == '`')
                {
                    inCode = !inCode;
                    i++;
                    continue;
                }

                if (inCode)
                {
                    i++;
                    continue;
                }

                if (c == '[' && (i == 0 || s[i - 1] != '!'))
                {
                    var close = Matching(s, i, '[', ']');
                    if (close > 0 && close + 1 < s.Length && s[close + 1] is '(' or '[')
                    {
                        var open = s[close + 1];
                        var end = Matching(s, close + 1, open, open == '(' ? ')' : ']');
                        if (end > 0)
                        {
                            links.Add(new Link(line.Start + i, line.Start + end + 1, s[(i + 1)..close].Trim(), s[(close + 2)..end].Trim()));
                            i = end + 1;
                            continue;
                        }
                    }
                }
                else if (c == '<')
                {
                    var end = s.IndexOf('>', i + 1);
                    if (end > 0 && IsAddress(s.AsSpan(i + 1, end - i - 1)))
                    {
                        var address = s[(i + 1)..end];
                        links.Add(new Link(line.Start + i, line.Start + end + 1, address, address));
                        i = end + 1;
                        continue;
                    }
                }
                else if ((i == 0 || char.IsWhiteSpace(s[i - 1]) || s[i - 1] is '(' or '"' or '\'') && StartsAddress(s.AsSpan(i)))
                {
                    var end = i;
                    while (end < s.Length && !char.IsWhiteSpace(s[end]) && s[end] is not ('<' or '>'))
                    {
                        end++;
                    }

                    while (end > i && s[end - 1] is '.' or ',' or ';' or ':' or '!' or '?' or ')' or '"' or '\'')
                    {
                        end--;
                    }

                    var address = s[i..end];
                    links.Add(new Link(line.Start + i, line.Start + end, address, address));
                    i = end;
                    continue;
                }

                i++;
            }
        }

        return links;
    }

    /// <summary>The first link starting after the caret; null when there is none.</summary>
    public static Element? NextLink(string text, int caret) => After(Links(text), Math.Clamp(caret, 0, text.Length));

    /// <summary>The last link ending at or before the caret, so that from inside a link it is the one before; null when there is none.</summary>
    public static Element? PreviousLink(string text, int caret)
    {
        var position = Math.Clamp(caret, 0, text.Length);
        Link? found = null;
        foreach (var link in Links(text))
        {
            if (link.End <= position)
            {
                found = link;
            }
        }

        return found;
    }

    private static bool StartsAddress(ReadOnlySpan<char> s) =>
        s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static bool IsAddress(ReadOnlySpan<char> s) =>
        (StartsAddress(s) || s.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) && s.IndexOf(' ') < 0;

    /// <summary>The index of the bracket that closes the one at <paramref name="openIndex"/>, nesting counted; -1 when there is none.</summary>
    private static int Matching(string s, int openIndex, char open, char close)
    {
        var depth = 0;
        for (var i = openIndex; i < s.Length; i++)
        {
            if (s[i] == open)
            {
                depth++;
            }
            else if (s[i] == close && --depth == 0)
            {
                return i;
            }
        }

        return -1;
    }

    // ---- lists ----

    /// <summary>Every list item line, at any indentation: a bullet (<c>-</c>, <c>*</c>, <c>+</c>) or a number with <c>.</c> or <c>)</c>, then a space or the end of the line.</summary>
    public static IReadOnlyList<ListItem> ListItems(string text)
    {
        var items = new List<ListItem>();
        foreach (var line in Lines(text))
        {
            if (!line.InFence && ParseItem(line) is { } item)
            {
                items.Add(item);
            }
        }

        return items;
    }

    /// <summary>
    /// The lists: a list starts at an item and goes on through items at its indentation with the same kind of marker,
    /// anything indented deeper (nested items, continuation lines) and blank lines that are followed by more of it.
    /// An item indented less, or with another kind of marker at the same indentation, starts the next list.
    /// </summary>
    public static IReadOnlyList<MarkdownList> Lists(string text)
    {
        var lists = new List<MarkdownList>();
        var lines = Lines(text);
        var index = 0;
        while (index < lines.Count)
        {
            var first = lines[index].InFence ? null : ParseItem(lines[index]);
            if (first is null)
            {
                index++;
                continue;
            }

            var items = new List<ListItem> { first };
            var next = index + 1;
            while (next < lines.Count && !lines[next].InFence)
            {
                var line = lines[next];
                if (ParseItem(line) is { } item)
                {
                    if (item.Indent < first.Indent || (item.Indent == first.Indent && !SameKind(first, item)))
                    {
                        break;
                    }

                    if (item.Indent == first.Indent)
                    {
                        items.Add(item);
                    }

                    next++;
                }
                else if (line.Text.Trim().Length == 0)
                {
                    var after = next + 1;
                    while (after < lines.Count && lines[after].Text.Trim().Length == 0)
                    {
                        after++;
                    }

                    if (after < lines.Count && !lines[after].InFence && Continues(lines[after], first))
                    {
                        next = after;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (Indentation(line.Text) > first.Indent)
                {
                    next++;
                }
                else
                {
                    break;
                }
            }

            lists.Add(new MarkdownList(first.Start, items));
            index = next;
        }

        return lists;
    }

    /// <summary>The first list starting on a line after the caret's line; null when there is none.</summary>
    public static Element? NextList(string text, int caret) => After(Lists(text), LineStart(text, caret));

    /// <summary>The last list starting on a line before the caret's line; null when there is none.</summary>
    public static Element? PreviousList(string text, int caret) => Before(Lists(text), LineStart(text, caret));

    /// <summary>The first list item on a line after the caret's line; null when there is none.</summary>
    public static Element? NextListItem(string text, int caret) => After(ListItems(text), LineStart(text, caret));

    /// <summary>The last list item on a line before the caret's line; null when there is none.</summary>
    public static Element? PreviousListItem(string text, int caret) => Before(ListItems(text), LineStart(text, caret));

    private static ListItem? ParseItem(TextLine line)
    {
        var s = line.Text;
        var indent = 0;
        var i = 0;
        while (i < s.Length && s[i] is ' ' or '\t')
        {
            indent += s[i] == '\t' ? 4 : 1;
            i++;
        }

        if (i >= s.Length)
        {
            return null;
        }

        int markerEnd;
        if (s[i] is '-' or '*' or '+')
        {
            markerEnd = i + 1;
        }
        else
        {
            var digits = 0;
            while (i + digits < s.Length && char.IsAsciiDigit(s[i + digits]))
            {
                digits++;
            }

            if (digits is 0 or > 9 || i + digits >= s.Length || s[i + digits] is not ('.' or ')'))
            {
                return null;
            }

            markerEnd = i + digits + 1;
        }

        if (markerEnd < s.Length && s[markerEnd] is not (' ' or '\t'))
        {
            return null;
        }

        return new ListItem(line.Start, indent, s[i..markerEnd], s[markerEnd..].Trim());
    }

    /// <summary>Two bullets with the same character, or two numbers with the same delimiter.</summary>
    private static bool SameKind(ListItem a, ListItem b) =>
        char.IsAsciiDigit(a.Marker[0]) ? char.IsAsciiDigit(b.Marker[0]) && a.Marker[^1] == b.Marker[^1] : a.Marker == b.Marker;

    private static bool Continues(TextLine line, ListItem first) =>
        ParseItem(line) is { } item ? item.Indent > first.Indent || (item.Indent == first.Indent && SameKind(first, item)) : Indentation(line.Text) > first.Indent;

    private static int Indentation(string s)
    {
        var indent = 0;
        foreach (var c in s)
        {
            if (c == ' ')
            {
                indent++;
            }
            else if (c == '\t')
            {
                indent += 4;
            }
            else
            {
                break;
            }
        }

        return indent;
    }

    // ---- block quotes ----

    /// <summary>The block quotes: each run of lines starting with <c>&gt;</c> after at most three spaces.</summary>
    public static IReadOnlyList<BlockQuote> BlockQuotes(string text)
    {
        var quotes = new List<BlockQuote>();
        var lines = Lines(text);
        var index = 0;
        while (index < lines.Count)
        {
            if (QuoteText(lines[index]) is { } firstText)
            {
                quotes.Add(new BlockQuote(lines[index].Start, firstText));
                while (index < lines.Count && QuoteText(lines[index]) is not null)
                {
                    index++;
                }
            }
            else
            {
                index++;
            }
        }

        return quotes;
    }

    /// <summary>The first block quote starting on a line after the caret's line; null when there is none.</summary>
    public static Element? NextBlockQuote(string text, int caret) => After(BlockQuotes(text), LineStart(text, caret));

    /// <summary>The last block quote starting on a line before the caret's line; null when there is none.</summary>
    public static Element? PreviousBlockQuote(string text, int caret) => Before(BlockQuotes(text), LineStart(text, caret));

    /// <summary>The line's text without its quote marks when it is a quote line; null otherwise.</summary>
    private static string? QuoteText(TextLine line)
    {
        if (line.InFence)
        {
            return null;
        }

        var trimmed = line.Text.TrimStart(' ');
        if (line.Text.Length - trimmed.Length > 3 || !trimmed.StartsWith('>'))
        {
            return null;
        }

        var content = trimmed.AsSpan(1);
        while (content.Length > 0 && content[0] is '>' or ' ')
        {
            content = content[1..];
        }

        return content.Trim().ToString();
    }

    // ---- tables ----

    /// <summary>The tables: a header line with cells between pipes, a delimiter row of dashes under it, then the body rows that have a pipe.</summary>
    public static IReadOnlyList<Table> Tables(string text)
    {
        var tables = new List<Table>();
        var lines = Lines(text);
        var index = 0;
        while (index + 1 < lines.Count)
        {
            var header = lines[index];
            var delimiter = lines[index + 1];
            var columns = header.InFence ? 0 : Cells(header.Text).Count;
            if (columns > 0 && !delimiter.InFence && IsDelimiterRow(delimiter.Text))
            {
                var rows = 1;
                var next = index + 2;
                while (next < lines.Count && !lines[next].InFence && lines[next].Text.Contains('|'))
                {
                    rows++;
                    next++;
                }

                tables.Add(new Table(header.Start, rows, columns));
                index = next;
            }
            else
            {
                index++;
            }
        }

        return tables;
    }

    /// <summary>The first table starting on a line after the caret's line; null when there is none.</summary>
    public static Element? NextTable(string text, int caret) => After(Tables(text), LineStart(text, caret));

    /// <summary>The last table starting on a line before the caret's line; null when there is none.</summary>
    public static Element? PreviousTable(string text, int caret) => Before(Tables(text), LineStart(text, caret));

    /// <summary>The cells of a table row, trimmed, an outer pipe on either side allowed; empty when the line has no pipe.</summary>
    private static List<string> Cells(string s)
    {
        if (!s.Contains('|'))
        {
            return [];
        }

        var row = s.Trim();
        if (row.StartsWith('|'))
        {
            row = row[1..];
        }

        if (row.EndsWith('|') && !row.EndsWith("\\|"))
        {
            row = row[..^1];
        }

        return row.Split('|').Select(c => c.Trim()).ToList();
    }

    private static bool IsDelimiterRow(string s)
    {
        var cells = Cells(s);
        return cells.Count > 0 && cells.All(c => c.Trim(':').Length > 0 && c.Trim(':').All(ch => ch == '-'));
    }

    // ---- by kind ----

    /// <summary>The next element of a kind from the caret, as the functions above; the level narrows headings, 0 for any.</summary>
    public static Element? Next(ElementKind kind, string text, int caret, int level = 0) => kind switch
    {
        ElementKind.Heading => NextHeading(text, caret, level),
        ElementKind.Link => NextLink(text, caret),
        ElementKind.List => NextList(text, caret),
        ElementKind.ListItem => NextListItem(text, caret),
        ElementKind.BlockQuote => NextBlockQuote(text, caret),
        _ => NextTable(text, caret),
    };

    /// <summary>The previous element of a kind from the caret, as the functions above.</summary>
    public static Element? Previous(ElementKind kind, string text, int caret, int level = 0) => kind switch
    {
        ElementKind.Heading => PreviousHeading(text, caret, level),
        ElementKind.Link => PreviousLink(text, caret),
        ElementKind.List => PreviousList(text, caret),
        ElementKind.ListItem => PreviousListItem(text, caret),
        ElementKind.BlockQuote => PreviousBlockQuote(text, caret),
        _ => PreviousTable(text, caret),
    };

    /// <summary>The kind as it is spoken when there is none: "No next heading level 2", "No previous list item".</summary>
    public static string Name(ElementKind kind, int level = 0) => kind switch
    {
        ElementKind.Heading => level > 0 ? $"heading level {level}" : "heading",
        ElementKind.Link => "link",
        ElementKind.List => "list",
        ElementKind.ListItem => "list item",
        ElementKind.BlockQuote => "block quote",
        _ => "table",
    };

    // ---- shared ----

    /// <summary>The lines with their start offsets, <c>\r</c> dropped; a fence line and every line inside a fence are marked.</summary>
    private static List<TextLine> Lines(string text)
    {
        var lines = new List<TextLine>();
        var inFence = false;
        var start = 0;
        while (start <= text.Length)
        {
            var end = text.IndexOf('\n', start);
            var lineEnd = end < 0 ? text.Length : end;
            var line = text.AsSpan(start, lineEnd - start).TrimEnd('\r').ToString();
            var trimmed = line.TrimStart(' ');
            var fenceLine = line.Length - trimmed.Length <= 3 && (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"));
            if (fenceLine)
            {
                inFence = !inFence;
            }

            lines.Add(new TextLine(start, line, inFence || fenceLine));
            if (end < 0)
            {
                break;
            }

            start = end + 1;
        }

        return lines;
    }

    private static Element? After(IEnumerable<Element> elements, int position)
    {
        foreach (var element in elements)
        {
            if (element.Start > position)
            {
                return element;
            }
        }

        return null;
    }

    private static Element? Before(IEnumerable<Element> elements, int position)
    {
        Element? found = null;
        foreach (var element in elements)
        {
            if (element.Start < position)
            {
                found = element;
            }
        }

        return found;
    }

    private static int LineStart(string text, int caret)
    {
        caret = Math.Clamp(caret, 0, text.Length);
        var newline = caret > 0 ? text.LastIndexOf('\n', caret - 1) : -1;
        return newline + 1;
    }
}
