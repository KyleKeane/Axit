using System.Text.RegularExpressions;

namespace AxList.Core;

/// <summary>What a line of a list file is (SPEC.md §5).</summary>
public enum LineKind
{
    Blank,
    Heading,
    Text,
    Item,
}

/// <summary>
/// One line of a list file as read (SPEC.md §5): its kind, its indentation as a width in spaces, its marker (a bullet
/// "-", "*" or "+"; for a numbered item the separator "." or ")" with its number; for a heading its "#" signs), whether it
/// is a task and done, and its text. <see cref="Raw"/> is the line as it was in the file and is what is written back
/// for a line nothing changed; a change makes a new line whose raw text is <see cref="Render"/>.
/// </summary>
public sealed partial record ListLine(LineKind Kind, int Width, string Marker, int Number, bool IsTask, bool Done, string Text, string Raw)
{
    public bool IsNumbered => Kind == LineKind.Item && Marker is "." or ")";

    public static ListLine Parse(string raw)
    {
        if (raw.Trim().Length == 0)
        {
            return new ListLine(LineKind.Blank, 0, string.Empty, 0, false, false, string.Empty, raw);
        }

        var width = WidthOf(raw);
        if (HeadingRegex().Match(raw) is { Success: true } heading)
        {
            return new ListLine(LineKind.Heading, width, heading.Groups["hashes"].Value, 0, false, false, heading.Groups["text"].Value.Trim(), raw);
        }

        if (ItemRegex().Match(raw) is { Success: true } item)
        {
            var numbered = item.Groups["number"].Success;
            var check = item.Groups["check"];
            return new ListLine(LineKind.Item, width,
                numbered ? item.Groups["separator"].Value : item.Groups["bullet"].Value,
                numbered && int.TryParse(item.Groups["number"].Value, out var number) ? number : 0,
                check.Success, check.Success && check.Value is "x" or "X",
                item.Groups["text"].Value.TrimEnd(), raw);
        }

        return new ListLine(LineKind.Text, width, string.Empty, 0, false, false, raw.Trim(), raw);
    }

    /// <summary>The line as axList writes it: two spaces a level of width, the marker, the box, the text.</summary>
    public string Render()
    {
        var indent = new string(' ', Width);
        return Kind switch
        {
            LineKind.Item => $"{indent}{(IsNumbered ? Number + Marker : Marker)} {(IsTask ? (Done ? "[x] " : "[ ] ") : string.Empty)}{Text}",
            LineKind.Heading => $"{indent}{Marker} {Text}",
            LineKind.Text => indent + Text,
            _ => Raw,
        };
    }

    /// <summary>This line with what changed, its raw text rendered again.</summary>
    public ListLine With(int? width = null, bool? isTask = null, bool? done = null, string? text = null, int? number = null)
    {
        var line = this with
        {
            Width = width ?? Width,
            IsTask = isTask ?? IsTask,
            Done = done ?? Done,
            Text = text ?? Text,
            Number = number ?? Number,
        };
        return line with { Raw = line.Render() };
    }

    /// <summary>A new item of this line's kind (a task not done when this is a task, the next number when numbered) with another text, at another width.</summary>
    public ListLine Sibling(string text, int width)
    {
        var line = new ListLine(LineKind.Item, width, Marker, IsNumbered ? Number + 1 : 0, IsTask, false, text, string.Empty);
        return line with { Raw = line.Render() };
    }

    /// <summary>A new task, "- [ ] text", at a width: the item for a list with none to follow.</summary>
    public static ListLine NewTask(string text, int width)
    {
        var line = new ListLine(LineKind.Item, width, "-", 0, true, false, text, string.Empty);
        return line with { Raw = line.Render() };
    }

    /// <summary>The indentation as spaces, a tab counting two (SPEC.md §5: "two spaces (or a tab)").</summary>
    private static int WidthOf(string raw)
    {
        var width = 0;
        foreach (var c in raw)
        {
            if (c == ' ')
            {
                width++;
            }
            else if (c == '\t')
            {
                width += 2;
            }
            else
            {
                break;
            }
        }

        return width;
    }

    [GeneratedRegex(@"^[ \t]*(?<hashes>#{1,6})(?:[ \t]+(?<text>.*))?$")]
    private static partial Regex HeadingRegex();

    // "- text", "* [ ] text", "1. [x] text", "2) text": a marker, a space, an optional box with a space, the text.
    [GeneratedRegex(@"^[ \t]*(?:(?<bullet>[-*+])|(?<number>\d{1,9})(?<separator>[.)]))(?:[ \t]+(?:\[(?<check>[ xX])\](?:[ \t]+|$))?(?<text>.*)|$)$")]
    private static partial Regex ItemRegex();
}
