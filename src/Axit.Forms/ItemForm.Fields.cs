using System.Text.Json.Nodes;
using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// The field commands of an item app's detail pane (docs/axit/item-apps.md IA-5) and the notices they and the apps
/// use: every change makes a new document of the one shown and hands it to the app's <c>SaveFields</c>.
/// </summary>
public abstract partial class ItemForm
{
    /// <summary>Space in the detail pane (IA-5.2): a check changed at once.</summary>
    protected void CheckField()
    {
        if (SelectedField() is { } index && _shown.Document!.Fields[index].Type == FieldTypes.Check)
        {
            var field = _shown.Document.Fields[index];
            var value = !FieldTypes.IsTrue(field.Value);
            ChangeShown(document => (document.WithValue(index, JsonValue.Create(value)), $"{field.Title}: {(value ? "done" : "not done")}"), index);
        }
        else
        {
            Announce("Not a check: Enter changes the field", true);
        }
    }

    /// <summary>Enter in the detail pane (IA-5.1): the field's notice, by its type; a line's own action.</summary>
    protected void ChangeField()
    {
        if (SelectedField() is not { } index)
        {
            if (SelectedLine() is { Enter: { } enter })
            {
                enter();
            }
            else
            {
                Announce(SelectedLine()?.Label ?? "Nothing selected", true);
            }

            return;
        }

        var field = _shown.Document!.Fields[index];
        var row = SelectedRow();
        if (row is { IsList: false } && field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
        {
            AskText("Change Title", string.Empty, "Title:", "Title", FieldTypes.TextOf(field.Value), "&Change", title => RenameItem(row, title.Trim()),
                typed => typed.Trim().Length == 0 ? "Type the item's title" : null);
            return;
        }

        if (row is not null && ChangeSpecial(row, index, field))
        {
            return;
        }

        switch (field.Type)
        {
            case FieldTypes.Check:
                CheckField();
                return;
            case FieldTypes.Choice:
                ChooseOption(index, field);
                return;
            case FieldTypes.LongText:
                ChangeLongText(index, field);
                return;
        }

        if (!FieldTypes.Known(field.Type))
        {
            Announce($"A field of type {field.Type}: change it in axJSON, Ctrl+J", true);
            return;
        }

        var initial = field.Value is JsonValue value && value.GetValueKind() == System.Text.Json.JsonValueKind.Number ? value.ToJsonString() : FieldTypes.TextOf(field.Value).Replace('T', ' ');
        if (field.Type == FieldTypes.Text)
        {
            initial = FieldTypes.TextOf(field.Value);
        }

        var hint = field.Type switch
        {
            FieldTypes.Date => "A date: 2026-10-30, 30 October 2026, today, tomorrow, +3 for three days on, or a weekday. Empty for none.",
            FieldTypes.DateTime => "A date, a space and a time: tomorrow 14:00. Empty for none.",
            FieldTypes.Number => "A number. Empty for none.",
            _ => string.Empty,
        };
        Parsed? parsed = null;
        AskText("Change " + field.Title, hint, field.Title + ":", field.Title, initial, "&Change",
            _ => SetValue(index, field, parsed!.Value),
            typed =>
            {
                parsed = FieldTypes.Parse(field.Type, typed, field.Options, DateOnly.FromDateTime(DateTime.Now));
                return parsed.Refused;
            }, required: false);
    }

    /// <summary>A field's new value, saved and said ("Due: 30 October 2026, Friday").</summary>
    protected void SetValue(int index, ItemField field, JsonNode? value)
    {
        var shown = field with { Value = value };
        ChangeShown(document => (document.WithValue(index, value), $"{field.Title}: {SpokenValue(SelectedRow(), shown)}"), index);
    }

    /// <summary>A choice (IA-4.2): a list of the options, the current one selected.</summary>
    private void ChooseOption(int index, ItemField field)
    {
        if (field.Options.Count == 0)
        {
            Announce("This choice has no options: add them in axJSON, Ctrl+J", true);
            return;
        }

        var current = FieldTypes.TextOf(field.Value);
        var start = Math.Max(0, field.Options.ToList().FindIndex(option => option.Equals(current, StringComparison.OrdinalIgnoreCase)));
        ChooseFromList("Change " + field.Title, $"Now {(current.Length == 0 ? "empty" : current)}.", field.Title, field.Options, start,
            chosen => SetValue(index, field, JsonValue.Create(field.Options[chosen])));
    }

    /// <summary>
    /// A notice with a list to choose from (a choice's options, a new field's type): the bundle's list, type-to-find
    /// included, holds the focus; Enter chooses the selected line, Escape leaves it.
    /// </summary>
    protected void ChooseFromList(string title, string text, string name, IReadOnlyList<string> lines, int start, Action<int> chosen)
    {
        var list = new AxitList { AccessibleName = name, Font = Navigator.Font };
        list.Items.AddRange([.. lines]);
        list.SelectedIndex = Math.Clamp(start, 0, lines.Count - 1);
        ShowNotice(new Notice(title, text + " Up and Down move through the list, Enter chooses.",
        [
            new OverlayChoice("C&hoose", () => chosen(list.SelectedIndex), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Custom = list,
        });
        list.Select();
    }

    /// <summary>A long text (IA-4.2): a field of several lines; Enter makes a new line, Tab then Enter on Save keeps it.</summary>
    private void ChangeLongText(int index, ItemField field)
    {
        var box = new TextBox
        {
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = false,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            AccessibleName = field.Title,
            Text = FieldTypes.TextOf(field.Value).ReplaceLineEndings("\r\n"),
            Font = Navigator.Font,
        };
        box.KeyDown += EditKeys.Handle;
        ShowNotice(new Notice("Change " + field.Title, "Enter makes a new line. Tab, then Enter on Save keeps the text.",
        [
            new OverlayChoice("&Save", () => SetValue(index, field, FieldTypes.Parse(FieldTypes.LongText, box.Text, [], DateOnly.FromDateTime(DateTime.Now)).Value), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Custom = box,
        });
        box.Select();
        box.Select(box.TextLength, 0);
    }

    /// <summary>Ctrl+N in the detail pane (IA-5.3): the field's title, then its type, then a choice's options; after the selected field.</summary>
    protected void NewField()
    {
        if (_shown.Document is not { } shown)
        {
            Announce("Select an item or a list first", true);
            return;
        }

        if (FieldsFixed is { } why)
        {
            Announce(why, true);
            return;
        }

        var after = SelectedField() ?? shown.Fields.Count - 1;
        AskText("New field", string.Empty, "Title:", "Field title", string.Empty, "&Next", title =>
        {
            var trimmed = title.Trim();
            ChooseFromList("Type of " + trimmed, "What the field holds.", "Type", NewFieldTypes.Select(SpokenType).ToList(), 0,
                chosen => AddField(after, trimmed, NewFieldTypes[chosen]));
        }, typed => typed.Trim().Length == 0 ? "Type the field's title" : shown.IndexOf(typed.Trim()) >= 0 ? $"There is a field called {typed.Trim()} already" : null);
    }

    private void AddField(int after, string title, string type)
    {
        if (type != FieldTypes.Choice)
        {
            ChangeShown(document => (document.WithField(after, title, type, []), $"Added {title}, {SpokenType(type)}"), after + 1);
            return;
        }

        AskText("Options of " + title, "The options, with a comma between them: High, Normal, Low.", "Options:", "Options", string.Empty, "&Add",
            typed => ChangeShown(document => (document.WithField(after, title, type, Options(typed)), $"Added {title}, choice of {Options(typed).Count}"), after + 1),
            typed => Options(typed).Count == 0 ? "Type at least one option" : null);
    }

    private static IReadOnlyList<string> Options(string typed) =>
        typed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>F2 in the detail pane: the field's title; the Title field keeps its own.</summary>
    protected void RenameField()
    {
        if (SelectedField() is not { } index)
        {
            Announce("Select a field first", true);
            return;
        }

        var field = _shown.Document!.Fields[index];
        if (field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
        {
            Announce("The Title field keeps its name: F2 in the navigator renames the item", true);
            return;
        }

        if (FieldsFixed is { } why)
        {
            Announce(why, true);
            return;
        }

        var shown = _shown.Document;
        AskText("Rename " + field.Title, string.Empty, "Field title:", "Field title", field.Title, "&Rename",
            title => ChangeShown(document => (document.WithFieldTitle(index, title.Trim()), $"Renamed {field.Title} to {title.Trim()}"), index),
            typed => typed.Trim().Length == 0 ? "Type the field's title" : shown.IndexOf(typed.Trim()) is var i && i >= 0 && i != index ? $"There is a field called {typed.Trim()} already" : null);
    }

    /// <summary>Delete in the detail pane: the field removed; never the Title (IA-3).</summary>
    protected void RemoveField()
    {
        if (SelectedField() is not { } index)
        {
            Announce("Select a field first", true);
            return;
        }

        var field = _shown.Document!.Fields[index];
        if (field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
        {
            Announce("The Title field stays", true);
            return;
        }

        if (FieldsFixed is { } why)
        {
            Announce(why, true);
            return;
        }

        ChangeShown(document => (document.WithoutField(index), "Removed " + field.Title), Math.Min(index, _shown.Document.Fields.Count - 2));
    }

    /// <summary>Alt+Up, Alt+Down in the detail pane: the field moved; the edge sound at an end.</summary>
    protected void MoveField(int direction)
    {
        if (SelectedField() is not { } index)
        {
            Announce("Select a field first", true);
            return;
        }

        if (FieldsFixed is { } why)
        {
            Announce(why, true);
            return;
        }

        if (_shown.Document!.WithFieldMoved(index, direction) is null)
        {
            Sounds.Edge();
            Announce(direction < 0 ? "Top of the fields" : "Bottom of the fields", true);
            return;
        }

        var field = _shown.Document.Fields[index];
        ChangeShown(document => document.WithFieldMoved(index, direction) is { } moved ? (moved, (direction < 0 ? "Moved up: " : "Moved down: ") + field.Title) : null, index + direction);
    }

    /// <summary>Ctrl+Q: every field of the item in the navigator, or the whole value of the field in the detail pane.</summary>
    protected void ReadAll()
    {
        if (DetailPane.Focused)
        {
            if (SelectedField() is { } index)
            {
                var field = _shown.Document!.Fields[index];
                Announce(field.Type == FieldTypes.LongText && FieldTypes.TextOf(field.Value) is { Length: > 0 } text
                    ? $"{field.Title}: {text.ReplaceLineEndings(". ")}"
                    : FieldLabel(field), true);
            }
            else if (SelectedLine() is { } line)
            {
                Announce(line.Label, true);
            }

            return;
        }

        switch (SelectedRow())
        {
            case { IsList: false } row when _shown.Document is { } document:
                Announce(row.Name + ". " + string.Join(". ", document.Fields.Where(field => !field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
                    .Select(field => $"{field.Title}: {(field.Type == FieldTypes.LongText ? FieldTypes.TextOf(field.Value).ReplaceLineEndings(". ") : SpokenValue(row, field))}")), true);
                return;
            case { } other:
                Announce(other.Label, true);
                return;
            default:
                Announce("Nothing selected", true);
                return;
        }
    }

    /// <summary>A change to the document the detail pane shows, handed to the app with the field to select after.</summary>
    private void ChangeShown(Func<ItemDocument, (ItemDocument Document, string Said)?> change, int selectField)
    {
        if (_shown.Document is not { } document)
        {
            Announce("Nothing to change here", true);
            return;
        }

        if (change(document) is { } result)
        {
            SaveFields(_shown, result.Document, result.Said, selectField);
        }
    }

    /// <summary>
    /// A notice with one field (IA-5.1): <paramref name="refused"/> asked before the button acts, its reason said and
    /// the notice kept; then <paramref name="act"/> with the text. A field that may be left empty passes
    /// <paramref name="required"/> false.
    /// </summary>
    protected void AskText(string title, string text, string label, string name, string initial, string button, Action<string> act, Func<string, string?> refused, bool required = true)
    {
        ShowNotice(new Notice(title, text,
        [
            new OverlayChoice(button, () => act(Overlay.InputText), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput(label, name, initial, "Type something first", Required: required),
            Accepts = () =>
            {
                if (refused(Overlay.InputText) is { } why)
                {
                    Announce(why, true);
                    return false;
                }

                return true;
            },
        });
    }
}
