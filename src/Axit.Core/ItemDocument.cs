using System.Text.Json;
using System.Text.Json.Nodes;

namespace Axit.Core;

/// <summary>One field as the window shows it (docs/axit/item-apps.md IA-4.1): its title, its type, its value as in the file, and a choice's options.</summary>
public sealed record ItemField(string Title, string Type, JsonNode? Value, IReadOnlyList<string> Options);

/// <summary>
/// An item file or a list's <c>list.json</c> (IA-4.1, IA-4.3): a JSON object with a <c>format</c> and a list of
/// <c>fields</c>, each a title, a type and a value, and for a list a <c>new item</c> template. The object is kept
/// whole, so that keys the app does not know are written back as they were read (IA-4.1). Every change returns a new
/// document; the one read stays as it was, for the check against the file on disk and for undo.
/// </summary>
public sealed class ItemDocument
{
    public const string ItemFormat = "axit-item 1";
    public const string ListFormat = "axit-list 1";
    public const string ListFile = "list.json";
    public const string TitleField = "Title";

    private const string FieldsKey = "fields";
    private const string TemplateKey = "new item";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly JsonObject _root;

    private ItemDocument(JsonObject root)
    {
        _root = root;
    }

    /// <summary>The fields in the file's order.</summary>
    public IReadOnlyList<ItemField> Fields => Read(_root[FieldsKey] as JsonArray);

    /// <summary>A list's template (IA-4.3): the fields a new item gets after its Title; empty for an item.</summary>
    public IReadOnlyList<ItemField> Template => Read(_root[TemplateKey] as JsonArray);

    /// <summary>Whether the list's file names a template at all, so that the app's default applies when it does not (TK-4.1).</summary>
    public bool HasTemplate => _root[TemplateKey] is JsonArray;

    /// <summary>The Title field's text; empty when there is none.</summary>
    public string Title => IndexOf(TitleField) is var i && i >= 0 && Fields[i].Value is JsonValue value && value.TryGetValue<string>(out var text) ? text : string.Empty;

    /// <summary>The document in a file's text; an <see cref="InvalidDataException"/> when it is not JSON or not an object with a list of fields.</summary>
    public static ItemDocument Parse(string text)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Not JSON: " + ex.Message, ex);
        }

        if (node is not JsonObject root)
        {
            throw new InvalidDataException("Not an item: the file is not a JSON object");
        }

        if (root[FieldsKey] is not JsonArray fields || fields.Any(field => field is not JsonObject))
        {
            throw new InvalidDataException("Not an item: no list of fields");
        }

        return new ItemDocument(root);
    }

    /// <summary>A new item (IA-4.3): its Title, then the template's fields with their values.</summary>
    public static ItemDocument NewItem(string title, IReadOnlyList<ItemField> template)
    {
        var fields = new JsonArray { FieldObject(TitleField, FieldTypes.Text, JsonValue.Create(title), []) };
        foreach (var field in template.Where(field => !field.Title.Equals(TitleField, StringComparison.OrdinalIgnoreCase)))
        {
            fields.Add(FieldObject(field.Title, field.Type, field.Value?.DeepClone(), field.Options));
        }

        return new ItemDocument(new JsonObject { ["format"] = ItemFormat, [FieldsKey] = fields });
    }

    /// <summary>A list's details with no fields of its own and the template given (IA-4.4: written the first time a detail changes).</summary>
    public static ItemDocument NewList(IReadOnlyList<ItemField> template)
    {
        var root = new JsonObject { ["format"] = ListFormat, [FieldsKey] = new JsonArray { FieldObject("Notes", FieldTypes.LongText, JsonValue.Create(string.Empty), []) } };
        root[TemplateKey] = TemplateArray(template);
        return new ItemDocument(root);
    }

    /// <summary>The text written to the file: indented JSON with Windows line ends.</summary>
    public string ToText() => _root.ToJsonString(Indented).ReplaceLineEndings("\r\n") + "\r\n";

    /// <summary>The index of the first field with this title (case ignored), -1 for none.</summary>
    public int IndexOf(string title)
    {
        var fields = Fields;
        for (var i = 0; i < fields.Count; i++)
        {
            if (fields[i].Title.Equals(title, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Whether field <paramref name="index"/> is still the one shown, by title and type (IA-5.5).</summary>
    public bool Holds(int index, ItemField shown) =>
        index >= 0 && index < Fields.Count && Fields[index].Title == shown.Title && Fields[index].Type == shown.Type;

    public ItemDocument WithValue(int index, JsonNode? value) => Change(fields => ((JsonObject)fields[index]!)["value"] = value?.DeepClone());

    public ItemDocument WithTitle(string title)
    {
        var index = IndexOf(TitleField);
        return index >= 0
            ? WithValue(index, JsonValue.Create(title))
            : Change(fields => fields.Insert(0, FieldObject(TitleField, FieldTypes.Text, JsonValue.Create(title), [])));
    }

    /// <summary>A new field after <paramref name="after"/> (-1 for the end), empty (IA-4.2).</summary>
    public ItemDocument WithField(int after, string title, string type, IReadOnlyList<string> options) =>
        Change(fields =>
        {
            var field = FieldObject(title, type, FieldTypes.Empty(type, options), options);
            if (after < 0 || after >= fields.Count - 1)
            {
                fields.Add(field);
            }
            else
            {
                fields.Insert(after + 1, field);
            }
        });

    public ItemDocument WithoutField(int index) => Change(fields => fields.RemoveAt(index));

    public ItemDocument WithFieldTitle(int index, string title) => Change(fields => ((JsonObject)fields[index]!)["title"] = title);

    /// <summary>The field moved one place up or down; null at the end it would leave.</summary>
    public ItemDocument? WithFieldMoved(int index, int direction)
    {
        var to = index + direction;
        if (to < 0 || to >= Fields.Count)
        {
            return null;
        }

        return Change(fields =>
        {
            var field = fields[index];
            fields.RemoveAt(index);
            fields.Insert(to, field);
        });
    }

    /// <summary>A list's template replaced (IA-4.3).</summary>
    public ItemDocument WithTemplate(IReadOnlyList<ItemField> template)
    {
        var root = (JsonObject)_root.DeepClone();
        root[TemplateKey] = TemplateArray(template);
        return new ItemDocument(root);
    }

    private ItemDocument Change(Action<JsonArray> change)
    {
        var root = (JsonObject)_root.DeepClone();
        change((JsonArray)root[FieldsKey]!);
        return new ItemDocument(root);
    }

    private static JsonArray TemplateArray(IReadOnlyList<ItemField> template)
    {
        var array = new JsonArray();
        foreach (var field in template.Where(field => !field.Title.Equals(TitleField, StringComparison.OrdinalIgnoreCase)))
        {
            array.Add(FieldObject(field.Title, field.Type, FieldTypes.Empty(field.Type, field.Options), field.Options));
        }

        return array;
    }

    private static JsonObject FieldObject(string title, string type, JsonNode? value, IReadOnlyList<string> options)
    {
        var field = new JsonObject { ["title"] = title, ["type"] = type, ["value"] = value };
        if (type == FieldTypes.Choice)
        {
            field["options"] = new JsonArray([.. options.Select(option => (JsonNode?)JsonValue.Create(option))]);
        }

        return field;
    }

    private static IReadOnlyList<ItemField> Read(JsonArray? fields)
    {
        if (fields is null)
        {
            return [];
        }

        var read = new List<ItemField>();
        foreach (var node in fields)
        {
            if (node is not JsonObject field)
            {
                continue;
            }

            var options = field["options"] is JsonArray array
                ? array.Select(option => option is JsonValue value && value.TryGetValue<string>(out var text) ? text : null).OfType<string>().ToList()
                : [];
            read.Add(new ItemField(Text(field["title"]), Text(field["type"]), field["value"], options));
        }

        return read;
    }

    private static string Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : string.Empty;
}
