using System.Text.Json.Nodes;
using Axit.Core;

namespace AxPass.Core;

/// <summary>
/// An entry's fields (docs/axpass/SPEC.md PW-6.1) as the item apps' document: <c>show</c>'s lines read into KeePass's
/// standard fields with their types, then any other attribute, read only. The password itself is never read: its
/// field holds an empty value and reads "hidden" (PW-6.2). A change of the document becomes the arguments of
/// <c>edit</c>.
/// </summary>
public static class EntryFields
{
    public const string Title = "Title";
    public const string UserName = "UserName";
    public const string Password = "Password";
    public const string Url = "URL";
    public const string Notes = "Notes";

    /// <summary>The attributes <c>show</c> prints that are not fields of the entry.</summary>
    private static readonly string[] Bookkeeping = ["Uuid", "Tags"];

    private static readonly (string Title, string Type)[] Standard =
    [
        (Title, FieldTypes.Text),
        (UserName, FieldTypes.UserName),
        (Password, FieldTypes.Password),
        (Url, FieldTypes.Text),
        (Notes, FieldTypes.LongText),
    ];

    /// <summary>
    /// <c>show</c>'s output (without <c>-s</c>) as a document: "Name: value" lines, a value of several lines going on
    /// until the next line that starts with a known name and a colon (a note's own lines can hold colons, so only
    /// the names <c>show</c> prints, KeePass's standard ones and its bookkeeping, start a new field).
    /// </summary>
    public static ItemDocument Parse(string show)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var order = new List<string>();
        string? current = null;
        var known = Standard.Select(field => field.Title).Concat(Bookkeeping).ToHashSet(StringComparer.Ordinal);
        foreach (var line in show.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n'))
        {
            var colon = line.IndexOf(": ", StringComparison.Ordinal);
            var name = colon > 0 ? line[..colon] : line.EndsWith(':') ? line[..^1] : null;
            if (name is not null && (known.Contains(name) || current is null || current == "Tags"))
            {
                current = name;
                order.Add(name);
                values[name] = colon > 0 ? line[(colon + 2)..] : string.Empty;
            }
            else if (current is not null)
            {
                values[current] += "\n" + line;
            }
        }

        var fields = new List<ItemField>();
        foreach (var (title, type) in Standard)
        {
            var value = title == Password ? string.Empty : values.GetValueOrDefault(title, string.Empty);
            fields.Add(new ItemField(title, type, JsonValue.Create(value), []));
        }

        foreach (var name in order.Where(name => !known.Contains(name)))
        {
            fields.Add(new ItemField(name, FieldTypes.Text, JsonValue.Create(values[name]), []));
        }

        return ItemDocument.NewItem(values.GetValueOrDefault(Title, string.Empty), fields.Skip(1).ToList());
    }

    /// <summary>Whether a field is KeePass's own, which the tool's <c>edit</c> changes; the others are read only (PW-D3).</summary>
    public static bool IsStandard(string title) => Standard.Any(field => field.Title == title);

    /// <summary>
    /// The options of <c>edit</c> that make <paramref name="before"/> into <paramref name="after"/>: <c>-t</c>,
    /// <c>-u</c>, <c>--url</c>, <c>--notes</c> for what changed; empty when nothing the tool changes did. The password
    /// is changed apart, with <c>-p</c> and standard input.
    /// </summary>
    public static IReadOnlyList<string> EditOptions(ItemDocument before, ItemDocument after)
    {
        var options = new List<string>();
        foreach (var (title, option) in new[] { (Title, "-t"), (UserName, "-u"), (Url, "--url"), (Notes, "--notes") })
        {
            var was = ValueOf(before, title);
            var now = ValueOf(after, title);
            if (was != now)
            {
                options.Add(option);
                options.Add(now);
            }
        }

        return options;
    }

    /// <summary>A field's text; empty when the entry has no such field.</summary>
    public static string ValueOf(ItemDocument document, string title) =>
        document.IndexOf(title) is var i && i >= 0 ? FieldTypes.TextOf(document.Fields[i].Value) : string.Empty;
}
