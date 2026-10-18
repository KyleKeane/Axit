using System.Text;

namespace AxJSON.Core;

/// <summary>
/// Format (docs/axjson/SPEC.md AJ-2.3): the document written out again with two spaces of indentation, one member or
/// item per line, keys in double quotes and the file's line endings, after the repairs the parser makes (a trailing
/// comma, a missing comma, a single-quoted string, an unquoted key, a comment). A document with a problem the parser
/// cannot repair is left as it is, and the message names the line and what was expected. Pure code, tested.
/// </summary>
public static class JsonFormat
{
    /// <summary>The formatted text and the one announcement: "Formatted", "Formatted, repaired: 2 trailing commas", "Already formatted", or "Not valid JSON: line 12, …".</summary>
    public static (string Text, string Message) Apply(string text)
    {
        var tree = JsonParser.Parse(text);
        if (tree.Problem is { } problem)
        {
            return (text, $"Not valid JSON: line {JsonOutline.Line(text, problem.Start)}, {problem.Message}");
        }

        if (tree.Root is null)
        {
            return (text, "Nothing to format");
        }

        var newLine = text.Contains("\r\n", StringComparison.Ordinal) || !text.Contains('\n') ? "\r\n" : "\n";
        var builder = new StringBuilder(text.Length + 64);
        Write(tree.Root, builder, 0, newLine);
        builder.Append(newLine);
        var formatted = builder.ToString();
        if (formatted == text)
        {
            return (text, "Already formatted");
        }

        return (formatted, tree.Repairs.Count == 0 ? "Formatted" : "Formatted, repaired: " + Repairs(tree.Repairs));
    }

    private static void Write(JsonNode node, StringBuilder builder, int depth, string newLine)
    {
        switch (node.Kind)
        {
            case TokenKind.OpenObject:
                WriteContainer(node, builder, depth, newLine, '{', '}', member => JsonScanner.Quote(member.Key ?? string.Empty) + ": ");
                break;
            case TokenKind.OpenArray:
                WriteContainer(node, builder, depth, newLine, '[', ']', _ => string.Empty);
                break;
            case TokenKind.String:
                builder.Append(JsonScanner.Quote(node.Value));
                break;
            default:
                builder.Append(node.Text);
                break;
        }
    }

    private static void WriteContainer(JsonNode node, StringBuilder builder, int depth, string newLine, char open, char close, Func<JsonNode, string> prefix)
    {
        builder.Append(open);
        if (node.Children.Count == 0)
        {
            builder.Append(close);
            return;
        }

        var indent = new string(' ', (depth + 1) * 2);
        for (var i = 0; i < node.Children.Count; i++)
        {
            builder.Append(newLine).Append(indent).Append(prefix(node.Children[i]));
            Write(node.Children[i], builder, depth + 1, newLine);
            if (i < node.Children.Count - 1)
            {
                builder.Append(',');
            }
        }

        builder.Append(newLine).Append(' ', depth * 2).Append(close);
    }

    /// <summary>"2 trailing commas, 1 unquoted key", in the order the kinds are declared.</summary>
    private static string Repairs(IReadOnlyList<RepairKind> repairs) =>
        string.Join(", ", Enum.GetValues<RepairKind>()
            .Select(kind => (Kind: kind, Count: repairs.Count(r => r == kind)))
            .Where(pair => pair.Count > 0)
            .Select(pair => $"{pair.Count} {Name(pair.Kind)}{(pair.Count == 1 ? string.Empty : "s")}"));

    private static string Name(RepairKind kind) => kind switch
    {
        RepairKind.Comment => "comment",
        RepairKind.SingleQuotedString => "single-quoted string",
        RepairKind.UnquotedKey => "unquoted key",
        RepairKind.TrailingComma => "trailing comma",
        _ => "missing comma",
    };
}
