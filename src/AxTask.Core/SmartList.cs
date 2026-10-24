using System.Text.Json.Nodes;
using System.Text.Json;
using Axit.Core;

namespace AxTask.Core;

/// <summary>
/// A smart list (docs/axtask/SPEC.md TK-5.2): a saved filter, a <c>.filter.json</c> file in the top folder, or All
/// tasks, the one built in (TK-D1). Its conditions are read once; <see cref="Matches"/> asks them of one item and
/// <see cref="Items"/> of every item under the folder, the Archive left out.
/// </summary>
public sealed class SmartList
{
    public const string Format = "axit-filter 1";
    public const string AllTasksName = "All tasks";

    private readonly JsonArray _conditions;
    private readonly bool _any;
    private readonly IReadOnlyList<string> _order;

    private SmartList(string name, string? path, JsonArray conditions, bool any, IReadOnlyList<string> order, string? notUnderstood)
    {
        Name = name;
        Path = path;
        _conditions = conditions;
        _any = any;
        _order = order;
        NotUnderstood = notUnderstood;
    }

    public string Name { get; }

    /// <summary>The filter's file; null for All tasks.</summary>
    public string? Path { get; }

    /// <summary>What in the file was not understood (TK-5.2); null when every condition is known. Such a list shows nothing.</summary>
    public string? NotUnderstood { get; }

    /// <summary>TK-5.1: every task not done.</summary>
    public static SmartList AllTasks { get; } = new(AllTasksName, null, [new JsonObject { ["field"] = TaskRules.DoneField, ["is"] = false }], any: false, [TaskRules.DueField, ItemDocument.TitleField], null);

    /// <summary>The text a new smart list starts from (TK-5.3): All tasks' conditions in a file of its own.</summary>
    public static string NewFileText() =>
        new JsonObject
        {
            ["format"] = Format,
            ["match"] = "all",
            ["conditions"] = new JsonArray(new JsonObject { ["field"] = TaskRules.DoneField, ["is"] = false }),
            ["order"] = new JsonArray(JsonValue.Create(TaskRules.DueField), JsonValue.Create(ItemDocument.TitleField)),
        }.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\r\n") + "\r\n";

    /// <summary>The smart lists of the top folder, by name.</summary>
    public static IReadOnlyList<SmartList> InFolder(string top)
    {
        if (!Directory.Exists(top))
        {
            return [];
        }

        return Directory.GetFiles(top, "*" + ItemFolder.FilterExtension)
            .Select(Read)
            .OrderBy(list => list.Name, Axit.Core.NaturalOrder.Instance)
            .ToList();
    }

    /// <summary>A filter file; one that cannot be read or understood is a smart list with <see cref="NotUnderstood"/> set.</summary>
    public static SmartList Read(string path)
    {
        var name = System.IO.Path.GetFileName(path)[..^ItemFolder.FilterExtension.Length];
        try
        {
            if (JsonNode.Parse(Axit.Core.TextDocument.Read(path).Text, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) is not JsonObject root
                || root["conditions"] is not JsonArray conditions)
            {
                return Broken(name, path, "no list of conditions");
            }

            var match = root["match"] is JsonValue m && m.TryGetValue<string>(out var text) ? text : "all";
            if (match is not ("all" or "any"))
            {
                return Broken(name, path, $"match is {match}, not all or any");
            }

            var order = root["order"] is JsonArray keys
                ? keys.Select(key => key is JsonValue v && v.TryGetValue<string>(out var s) ? s : null).OfType<string>().ToList()
                : [TaskRules.DueField, ItemDocument.TitleField];
            foreach (var condition in conditions)
            {
                if (Check(condition) is { } problem)
                {
                    return Broken(name, path, problem);
                }
            }

            return new SmartList(name, path, (JsonArray)conditions.DeepClone(), match == "any", order, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            return Broken(name, path, "the file could not be read");
        }
    }

    /// <summary>The items under the top folder the conditions hold for, in the list's order.</summary>
    public IReadOnlyList<ItemFile> Items(string top, DateOnly today)
    {
        if (NotUnderstood is not null)
        {
            return [];
        }

        var items = ItemFolder.AllItems(top).Where(item => Matches(item, top, today));
        return Order(items, today);
    }

    public bool Matches(ItemFile item, string top, DateOnly today)
    {
        if (_conditions.Count == 0)
        {
            return true;
        }

        var results = _conditions.Select(condition => Holds((JsonObject)condition!, item, top, today));
        return _any ? results.Any(held => held) : results.All(held => held);
    }

    private IReadOnlyList<ItemFile> Order(IEnumerable<ItemFile> items, DateOnly today)
    {
        IOrderedEnumerable<ItemFile>? ordered = null;
        foreach (var key in _order)
        {
            // A date sorts by the day, the ones without one after; anything else by its text, as spoken.
            Func<ItemFile, int> missing = item => item.Document.IndexOf(key) is var i && i >= 0 && Sortable(item.Document.Fields[i]) is { Length: > 0 } ? 0 : 1;
            Func<ItemFile, string> value = item => item.Document.IndexOf(key) is var i && i >= 0 ? Sortable(item.Document.Fields[i]) : string.Empty;
            ordered = ordered is null
                ? items.OrderBy(missing).ThenBy(value, Axit.Core.NaturalOrder.Instance)
                : ordered.ThenBy(missing).ThenBy(value, Axit.Core.NaturalOrder.Instance);
        }

        return (ordered ?? items.OrderBy(ItemFolder.NameOf, Axit.Core.NaturalOrder.Instance)).ToList();
    }

    private static string Sortable(ItemField field) => field.Type switch
    {
        FieldTypes.Check => FieldTypes.IsTrue(field.Value) ? "1" : "0",
        _ => field.Value is JsonValue value && value.GetValueKind() == JsonValueKind.Number ? value.GetValue<double>().ToString("000000000000000.######", System.Globalization.CultureInfo.InvariantCulture) : FieldTypes.TextOf(field.Value),
    };

    private static bool Holds(JsonObject condition, ItemFile item, string top, DateOnly today)
    {
        if (condition["list"] is JsonValue listValue && listValue.TryGetValue<string>(out var list))
        {
            var folder = System.IO.Path.GetRelativePath(top, System.IO.Path.GetDirectoryName(item.Path)!).Replace('\\', '/');
            var wanted = list.Trim('/').Replace('\\', '/');
            return folder.Equals(wanted, StringComparison.OrdinalIgnoreCase) || folder.StartsWith(wanted + "/", StringComparison.OrdinalIgnoreCase);
        }

        var name = condition["field"] is JsonValue f && f.TryGetValue<string>(out var title) ? title : string.Empty;
        var index = item.Document.IndexOf(name);
        var field = index >= 0 ? item.Document.Fields[index] : null;
        if (condition.ContainsKey("empty"))
        {
            var empty = field is null
                || (field.Type == FieldTypes.Check ? !FieldTypes.IsTrue(field.Value) : Sortable(field).Length == 0);
            return empty == FieldTypes.IsTrue(condition["empty"]);
        }

        if (field is null)
        {
            return false;
        }

        if (condition["is"] is JsonNode wantedValue)
        {
            return field.Type == FieldTypes.Check
                ? FieldTypes.IsTrue(field.Value) == FieldTypes.IsTrue(wantedValue)
                : string.Equals(Sortable(field), wantedValue is JsonValue w && w.TryGetValue<string>(out var s) ? s : wantedValue.ToJsonString(), StringComparison.OrdinalIgnoreCase);
        }

        if (condition["has"] is JsonValue hasValue && hasValue.TryGetValue<string>(out var word))
        {
            return FieldTypes.TextOf(field.Value).Contains(word, StringComparison.OrdinalIgnoreCase);
        }

        var date = field.Type == FieldTypes.DateTime && FieldTypes.ReadDateTime(field.Value) is { } moment ? DateOnly.FromDateTime(moment) : FieldTypes.ReadDate(field.Value);
        if (condition["before"] is JsonValue before && before.TryGetValue<string>(out var beforeText))
        {
            return date is { } day && FieldTypes.ParseDate(beforeText, today) is { } limit && day <= limit;
        }

        if (condition["after"] is JsonValue after && after.TryGetValue<string>(out var afterText))
        {
            return date is { } day && FieldTypes.ParseDate(afterText, today) is { } limit && day >= limit;
        }

        return false;
    }

    /// <summary>Why a condition is not understood; null when it is.</summary>
    private static string? Check(JsonNode? node)
    {
        if (node is not JsonObject condition)
        {
            return "a condition that is not an object";
        }

        if (condition.ContainsKey("list"))
        {
            return condition["list"] is JsonValue v && v.TryGetValue<string>(out _) ? null : "a list that is not a name";
        }

        if (condition["field"] is not JsonValue f || !f.TryGetValue<string>(out _))
        {
            return "a condition without a field";
        }

        string[] known = ["is", "has", "before", "after", "empty"];
        var tests = condition.Select(pair => pair.Key).Where(key => key != "field").ToList();
        if (tests.Count != 1 || !known.Contains(tests[0]))
        {
            return $"a condition on {f} with {(tests.Count == 0 ? "nothing to test" : string.Join(" and ", tests))}";
        }

        if (tests[0] is "before" or "after" && (condition[tests[0]] is not JsonValue d || !d.TryGetValue<string>(out var text) || FieldTypes.ParseDate(text, DateOnly.FromDayNumber(0)) is null))
        {
            return $"a date that is not a date: {condition[tests[0]]?.ToJsonString()}";
        }

        return null;
    }

    private static SmartList Broken(string name, string path, string why) => new(name, path, [], any: false, [], why);
}
