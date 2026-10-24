using System.Text.Json.Nodes;
using Axit.Core;

namespace Axit.Tests;

public class ItemDocumentTests
{
    private static readonly IReadOnlyList<ItemField> Template =
    [
        new("Done", FieldTypes.Check, JsonValue.Create(false), []),
        new("Start", FieldTypes.Date, JsonValue.Create(string.Empty), []),
        new("Due", FieldTypes.Date, JsonValue.Create(string.Empty), []),
    ];

    private const string Sample = """
        {
          "format": "axit-item 1",
          "colour": "blue",
          "fields": [
            { "title": "Title", "type": "text", "value": "Fix the tap" },
            { "title": "Done", "type": "check", "value": false, "note": "kept" },
            { "title": "Due", "type": "date", "value": "2026-10-30" },
            { "title": "Priority", "type": "choice", "value": "High", "options": ["High", "Normal", "Low"] }
          ]
        }
        """;

    [Fact]
    public void ReadsTheFieldsInOrder()
    {
        var document = ItemDocument.Parse(Sample);
        Assert.Equal(["Title", "Done", "Due", "Priority"], document.Fields.Select(field => field.Title));
        Assert.Equal("Fix the tap", document.Title);
        Assert.Equal(["High", "Normal", "Low"], document.Fields[3].Options);
    }

    [Fact]
    public void KeepsKeysItDoesNotKnow()
    {
        var changed = ItemDocument.Parse(Sample).WithValue(1, JsonValue.Create(true));
        var text = changed.ToText();
        Assert.Contains("\"colour\": \"blue\"", text);
        Assert.Contains("\"note\": \"kept\"", text);
        Assert.Contains("\"value\": true", text);
    }

    [Fact]
    public void AChangeLeavesTheDocumentReadAsItWas()
    {
        var document = ItemDocument.Parse(Sample);
        _ = document.WithValue(1, JsonValue.Create(true));
        Assert.False(FieldTypes.IsTrue(document.Fields[1].Value));
    }

    [Fact]
    public void WritesWindowsLineEndsAndReadsItsOwnText()
    {
        var text = ItemDocument.Parse(Sample).ToText();
        Assert.DoesNotContain("\n", text.Replace("\r\n", string.Empty));
        Assert.Equal(text, ItemDocument.Parse(text).ToText());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    [InlineData("{ \"format\": \"axit-item 1\" }")]
    [InlineData("{ \"fields\": [1] }")]
    public void RefusesWhatIsNotAnItem(string text) => Assert.Throws<InvalidDataException>(() => ItemDocument.Parse(text));

    [Fact]
    public void ANewItemHasItsTitleThenTheTemplate()
    {
        var item = ItemDocument.NewItem("Call the plumber", Template);
        Assert.Equal(["Title", "Done", "Start", "Due"], item.Fields.Select(field => field.Title));
        Assert.Equal("Call the plumber", item.Title);
        Assert.Contains("\"format\": \"axit-item 1\"", item.ToText());
    }

    [Fact]
    public void FieldsAreAddedRemovedRenamedAndMoved()
    {
        var document = ItemDocument.Parse(Sample);
        var added = document.WithField(0, "Notes", FieldTypes.LongText, []);
        Assert.Equal("Notes", added.Fields[1].Title);
        Assert.Equal(string.Empty, FieldTypes.TextOf(added.Fields[1].Value));
        Assert.Equal("Status", added.WithFieldTitle(1, "Status").Fields[1].Title);
        Assert.Equal(["Title", "Done", "Due", "Priority"], added.WithoutField(1).Fields.Select(field => field.Title));
        Assert.Equal("Due", document.WithFieldMoved(2, -1)!.Fields[1].Title);
        Assert.Null(document.WithFieldMoved(3, 1));
        Assert.Null(document.WithFieldMoved(0, -1));
        var choice = document.WithField(-1, "Size", FieldTypes.Choice, ["S", "M"]);
        Assert.Equal(["S", "M"], choice.Fields[^1].Options);
    }

    [Fact]
    public void HoldsTellsAFieldThatMoved()
    {
        var document = ItemDocument.Parse(Sample);
        var shown = document.Fields[2];
        Assert.True(document.Holds(2, shown));
        Assert.False(document.WithFieldMoved(2, -1)!.Holds(2, shown));
        Assert.False(document.Holds(9, shown));
    }

    [Fact]
    public void AListKeepsItsTemplate()
    {
        var list = ItemDocument.NewList(Template);
        Assert.True(list.HasTemplate);
        Assert.Equal(["Done", "Start", "Due"], list.Template.Select(field => field.Title));
        var plain = list.WithTemplate([]);
        Assert.True(plain.HasTemplate);
        Assert.Empty(plain.Template);
        Assert.Contains("\"new item\": []", plain.ToText());
    }

    [Fact]
    public void KeepsTextAsWritten()
    {
        var item = ItemDocument.NewItem("Café & \"quotes\" <tag>", []);
        Assert.Contains("Café & \\\"quotes\\\" <tag>", item.ToText());
        Assert.Equal("Café & \"quotes\" <tag>", ItemDocument.Parse(item.ToText()).Title);
    }
}
