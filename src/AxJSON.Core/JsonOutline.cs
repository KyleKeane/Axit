using Axit.Core;

namespace AxJSON.Core;

/// <summary>A member of an object: the caret goes to its key; spoken as "model: opus", or "permissions, object with 2 keys" for a container.</summary>
public sealed record JsonKey(int Start, string Name, string Value, bool Container) : Element(Start)
{
    public override string Spoken => Container ? $"{Name}, {Value}" : $"{Name}: {Value}";
}

/// <summary>An object, at its opening brace; spoken with its key when it is a member.</summary>
public sealed record JsonObject(int Start, string? Key, int Count) : Element(Start)
{
    public override string Spoken => Key is null ? JsonOutline.Describe(TokenKind.OpenObject, Count) : $"{Key}, {JsonOutline.Describe(TokenKind.OpenObject, Count)}";
}

/// <summary>An array, at its opening bracket; spoken with its key when it is a member.</summary>
public sealed record JsonArray(int Start, string? Key, int Count) : Element(Start)
{
    public override string Spoken => Key is null ? JsonOutline.Describe(TokenKind.OpenArray, Count) : $"{Key}, {JsonOutline.Describe(TokenKind.OpenArray, Count)}";
}

/// <summary>An item of an array, at its first character; "Item 2 of 5, Read".</summary>
public sealed record JsonItem(int Start, int Index, int Count, string Value) : Element(Start)
{
    public override string Spoken => $"Item {Index} of {Count}, {Value}";
}

/// <summary>Where the document stops being JSON; "Error, line 12: expected a comma or a closing brace after model, found a string".</summary>
public sealed record JsonError(int Start, int Line, string Message) : Element(Start)
{
    public override string Spoken => $"Error, line {Line}: {Message}";
}

/// <summary>
/// The elements of a JSON text for the navigation keys (docs/axjson/SPEC.md AJ-2.2): keys, objects, arrays, items and
/// the first error, each in document order with next and previous from a caret position, in the shape
/// <c>Axit.Core.MarkdownOutline</c> has, so that the editor shell's <c>TextJump</c> takes them. Pure code, tested.
/// </summary>
public static class JsonOutline
{
    public static IReadOnlyList<Element> Keys(string text) => Collect(text, (node, _, _, list) =>
    {
        if (node.Key is not null)
        {
            list.Add(new JsonKey(node.KeyStart, node.Key, Preview(node), node.IsContainer));
        }
    });

    public static IReadOnlyList<Element> Objects(string text) => Collect(text, (node, _, _, list) =>
    {
        if (node.Kind == TokenKind.OpenObject)
        {
            list.Add(new JsonObject(node.Start, node.Key, node.Children.Count));
        }
    });

    public static IReadOnlyList<Element> Arrays(string text) => Collect(text, (node, _, _, list) =>
    {
        if (node.Kind == TokenKind.OpenArray)
        {
            list.Add(new JsonArray(node.Start, node.Key, node.Children.Count));
        }
    });

    public static IReadOnlyList<Element> Items(string text) => Collect(text, (node, index, count, list) =>
    {
        if (index > 0)
        {
            list.Add(new JsonItem(node.Start, index, count, Preview(node)));
        }
    });

    /// <summary>The first problem of the document as one element, or none when it is JSON (with the repairs the formatter would make).</summary>
    public static IReadOnlyList<Element> Errors(string text)
    {
        var tree = JsonParser.Parse(text);
        return tree.Problem is { } problem ? [new JsonError(problem.Start, Line(text, problem.Start), problem.Message)] : [];
    }

    public static Element? NextKey(string text, int caret) => After(Keys(text), caret);

    public static Element? PreviousKey(string text, int caret) => Before(Keys(text), caret);

    public static Element? NextObject(string text, int caret) => After(Objects(text), caret);

    public static Element? PreviousObject(string text, int caret) => Before(Objects(text), caret);

    public static Element? NextArray(string text, int caret) => After(Arrays(text), caret);

    public static Element? PreviousArray(string text, int caret) => Before(Arrays(text), caret);

    public static Element? NextItem(string text, int caret) => After(Items(text), caret);

    public static Element? PreviousItem(string text, int caret) => Before(Items(text), caret);

    public static Element? NextError(string text, int caret) => After(Errors(text), caret);

    public static Element? PreviousError(string text, int caret) => Before(Errors(text), caret);

    /// <summary>"object with 3 keys", "empty object", "array with 1 item".</summary>
    public static string Describe(TokenKind kind, int count)
    {
        var what = kind == TokenKind.OpenObject ? "object" : "array";
        if (count == 0)
        {
            return $"empty {what}";
        }

        var unit = kind == TokenKind.OpenObject ? "key" : "item";
        return $"{what} with {count} {unit}{(count == 1 ? string.Empty : "s")}";
    }

    /// <summary>The line of an offset, counting from 1.</summary>
    public static int Line(string text, int offset)
    {
        var line = 1;
        for (var i = 0; i < offset && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    /// <summary>What a value is read as: a scalar's characters (a string without its quotes, cut at 60), or a container's size.</summary>
    private static string Preview(JsonNode node)
    {
        if (node.IsContainer)
        {
            return Describe(node.Kind, node.Children.Count);
        }

        var value = node.Kind == TokenKind.String && node.Value.Length == 0 ? "empty string" : node.Value;
        return value.Length > 60 ? value[..60] + "…" : value;
    }

    private static Element? After(IReadOnlyList<Element> elements, int caret) => elements.FirstOrDefault(e => e.Start > caret);

    private static Element? Before(IReadOnlyList<Element> elements, int caret) => elements.LastOrDefault(e => e.Start < caret);

    /// <summary>Walks the tree in document order; the visitor gets each node with its item index (1 and up inside an array, 0 otherwise) and the array's size.</summary>
    private static IReadOnlyList<Element> Collect(string text, Action<JsonNode, int, int, List<Element>> visit)
    {
        var list = new List<Element>();
        if (JsonParser.Parse(text).Root is { } root)
        {
            Walk(root, 0, 0, visit, list);
        }

        return list;
    }

    private static void Walk(JsonNode node, int index, int count, Action<JsonNode, int, int, List<Element>> visit, List<Element> list)
    {
        visit(node, index, count, list);
        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            var inArray = node.Kind == TokenKind.OpenArray;
            Walk(child, inArray ? i + 1 : 0, inArray ? node.Children.Count : 0, visit, list);
        }
    }
}
