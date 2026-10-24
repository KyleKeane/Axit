using System.Globalization;
using System.Text.Json.Nodes;

namespace AxTask.Core;

/// <summary>What came of a typed value (IA-5.1): the value for the file, or why the type refuses it.</summary>
public sealed record Parsed(JsonNode? Value, string? Refused);

/// <summary>
/// The field types (docs/axit/item-apps.md IA-4.2): their names in the file and as spoken, the empty value, how a value
/// is read out, and how a typed text becomes a value. Dates are read and typed in English whatever the system's
/// language, since the program runs without the globalization data.
/// </summary>
public static class FieldTypes
{
    public const string Text = "text";
    public const string LongText = "longtext";
    public const string Number = "number";
    public const string Date = "date";
    public const string DateTime = "datetime";
    public const string Check = "check";
    public const string Choice = "choice";
    public const string Link = "link";

    /// <summary>Every type, in the order New field offers them.</summary>
    public static readonly IReadOnlyList<string> All = [Text, LongText, Number, Date, DateTime, Check, Choice, Link];

    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    /// <summary>"long text", "date and time": the type as the detail pane says it; an unknown type as it is written.</summary>
    public static string Spoken(string type) => type switch
    {
        LongText => "long text",
        DateTime => "date and time",
        _ => type,
    };

    public static bool Known(string type) => All.Contains(type);

    /// <summary>The empty value of a type (IA-4.2): false for a check, an empty string otherwise.</summary>
    public static JsonNode? Empty(string type, IReadOnlyList<string> options) => type == Check ? JsonValue.Create(false) : JsonValue.Create(string.Empty);

    /// <summary>The value read out (IA-4.2): "30 October 2026, Friday", "done", the first line of a long text; "empty" for nothing.</summary>
    public static string Spoken(ItemField field, Func<string, string>? link = null)
    {
        var text = TextOf(field.Value);
        switch (field.Type)
        {
            case Check:
                return IsTrue(field.Value) ? "done" : "not done";
            case Date when ReadDate(field.Value) is { } date:
                return SpokenDate(date);
            case DateTime when ReadDateTime(field.Value) is { } moment:
                return SpokenDate(DateOnly.FromDateTime(moment)) + ", " + moment.ToString("HH:mm", English);
            case LongText:
                var lines = text.ReplaceLineEndings("\n").Split('\n');
                return text.Length == 0 ? "empty" : lines.Length > 1 ? $"{lines[0]}, and {lines.Length - 1} more {(lines.Length == 2 ? "line" : "lines")}" : lines[0];
            case Link when text.Length > 0 && link is not null:
                return link(text);
        }

        return field.Value is JsonValue number && number.GetValueKind() == System.Text.Json.JsonValueKind.Number
            ? number.ToJsonString()
            : text.Length == 0 ? "empty" : text;
    }

    /// <summary>"30 October 2026, Friday".</summary>
    public static string SpokenDate(DateOnly date) => date.ToString("d MMMM yyyy, dddd", English);

    /// <summary>"30 October": the short form of a row's summary (TK-4.3), with the year when it is not this one.</summary>
    public static string ShortDate(DateOnly date, DateOnly today) =>
        date.ToString(date.Year == today.Year ? "d MMMM" : "d MMMM yyyy", English);

    /// <summary>
    /// A typed text as a value of the type (IA-5.1). A date takes 2026-10-30, 30 October 2026, today, tomorrow,
    /// yesterday, +3 or -3 (days from today), or a weekday (the next one after today); a date and time takes a date, a
    /// space and 14:00. An empty text empties the field.
    /// </summary>
    public static Parsed Parse(string type, string typed, IReadOnlyList<string> options, DateOnly today)
    {
        var text = typed.Trim();
        switch (type)
        {
            case Text:
                return text.Contains('\n') ? new Parsed(null, "A text field holds one line") : new Parsed(JsonValue.Create(text), null);
            case LongText:
                return new Parsed(JsonValue.Create(typed.ReplaceLineEndings("\n").TrimEnd('\n')), null);
            case Number:
                if (text.Length == 0)
                {
                    return new Parsed(JsonValue.Create(string.Empty), null);
                }

                return double.TryParse(text, NumberStyles.Float, English, out var number) && double.IsFinite(number)
                    ? new Parsed(number == Math.Floor(number) && Math.Abs(number) < 1e15 ? JsonValue.Create((long)number) : JsonValue.Create(number), null)
                    : new Parsed(null, $"Not a number: {text}");
            case Date:
                if (text.Length == 0)
                {
                    return new Parsed(JsonValue.Create(string.Empty), null);
                }

                return ParseDate(text, today) is { } date
                    ? new Parsed(JsonValue.Create(date.ToString("yyyy-MM-dd", English)), null)
                    : new Parsed(null, $"Not a date: {text}. Type 2026-10-30, today, tomorrow, +3 or a weekday");
            case DateTime:
                if (text.Length == 0)
                {
                    return new Parsed(JsonValue.Create(string.Empty), null);
                }

                return ParseDateTime(text, today) is { } moment
                    ? new Parsed(JsonValue.Create(moment.ToString("yyyy-MM-dd'T'HH:mm", English)), null)
                    : new Parsed(null, $"Not a date and time: {text}. Type a date, a space and 14:00");
            case Choice:
                if (options.FirstOrDefault(option => option.Equals(text, StringComparison.OrdinalIgnoreCase)) is { } chosen)
                {
                    return new Parsed(JsonValue.Create(chosen), null);
                }

                return new Parsed(null, options.Count == 0 ? "This choice has no options: add them in axJSON" : $"Not one of the options: {string.Join(", ", options)}");
            default:
                return new Parsed(JsonValue.Create(text), null);
        }
    }

    public static bool IsTrue(JsonNode? value) => value is JsonValue v && v.TryGetValue<bool>(out var flag) && flag;

    public static string TextOf(JsonNode? value) => value is JsonValue v && v.TryGetValue<string>(out var text) ? text : string.Empty;

    /// <summary>A date field's value; null when it is empty or not a date.</summary>
    public static DateOnly? ReadDate(JsonNode? value) =>
        DateOnly.TryParseExact(TextOf(value), "yyyy-MM-dd", English, DateTimeStyles.None, out var date) ? date : null;

    public static System.DateTime? ReadDateTime(JsonNode? value) =>
        System.DateTime.TryParseExact(TextOf(value), ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss"], English, DateTimeStyles.None, out var moment) ? moment : null;

    /// <summary>The day a typed date names; null when it names none.</summary>
    public static DateOnly? ParseDate(string typed, DateOnly today)
    {
        var text = typed.Trim().ToLowerInvariant();
        switch (text)
        {
            case "today":
                return today;
            case "tomorrow":
                return today.AddDays(1);
            case "yesterday":
                return today.AddDays(-1);
        }

        if (text.Length > 1 && text[0] is '+' or '-' && int.TryParse(text[1..], NumberStyles.None, English, out var days) && days <= 36500)
        {
            return today.AddDays(text[0] == '+' ? days : -days);
        }

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            if (day.ToString().Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                var ahead = ((int)day - (int)today.DayOfWeek + 7) % 7;
                return today.AddDays(ahead == 0 ? 7 : ahead);
            }
        }

        string[] forms = ["yyyy-MM-dd", "yyyy-M-d", "d MMMM yyyy", "d MMM yyyy", "MMMM d yyyy", "MMMM d, yyyy"];
        if (DateOnly.TryParseExact(typed.Trim(), forms, English, DateTimeStyles.AllowWhiteSpaces, out var date))
        {
            return date;
        }

        // "30 October" without a year: this year's.
        return DateOnly.TryParseExact(typed.Trim(), ["d MMMM", "d MMM", "MMMM d"], English, DateTimeStyles.AllowWhiteSpaces, out var noYear)
            ? new DateOnly(today.Year, noYear.Month, noYear.Day)
            : null;
    }

    /// <summary>A typed date and time: a date as <see cref="ParseDate"/> takes it, then a time (14:00, 9:30); a date alone is midnight.</summary>
    public static System.DateTime? ParseDateTime(string typed, DateOnly today)
    {
        var text = typed.Trim();
        var space = text.LastIndexOf(' ');
        if (space > 0 && TimeOnly.TryParseExact(text[(space + 1)..], ["H:mm", "HH:mm"], English, DateTimeStyles.None, out var time)
            && ParseDate(text[..space], today) is { } day)
        {
            return day.ToDateTime(time);
        }

        return ParseDate(text, today) is { } date ? date.ToDateTime(TimeOnly.MinValue) : null;
    }
}
