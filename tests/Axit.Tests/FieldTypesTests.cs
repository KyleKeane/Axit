using System.Text.Json.Nodes;
using Axit.Core;

namespace Axit.Tests;

public class FieldTypesTests
{
    // A Saturday.
    private static readonly DateOnly Today = new(2026, 10, 24);

    [Theory]
    [InlineData("2026-10-30", "2026-10-30")]
    [InlineData("today", "2026-10-24")]
    [InlineData("Tomorrow", "2026-10-25")]
    [InlineData("yesterday", "2026-10-23")]
    [InlineData("+3", "2026-10-27")]
    [InlineData("-1", "2026-10-23")]
    [InlineData("friday", "2026-10-30")]
    [InlineData("saturday", "2026-10-31")]
    [InlineData("30 October 2026", "2026-10-30")]
    [InlineData("3 Nov 2026", "2026-11-03")]
    [InlineData("1 December", "2026-12-01")]
    [InlineData("", "")]
    public void TakesADate(string typed, string stored)
    {
        var parsed = FieldTypes.Parse(FieldTypes.Date, typed, [], Today);
        Assert.Null(parsed.Refused);
        Assert.Equal(stored, FieldTypes.TextOf(parsed.Value));
    }

    [Theory]
    [InlineData("30 Febuary")]
    [InlineData("2026-02-30")]
    [InlineData("soon")]
    public void RefusesWhatIsNotADate(string typed) =>
        Assert.StartsWith("Not a date", FieldTypes.Parse(FieldTypes.Date, typed, [], Today).Refused);

    [Fact]
    public void TakesADateAndTime()
    {
        Assert.Equal("2026-10-25T14:00", FieldTypes.TextOf(FieldTypes.Parse(FieldTypes.DateTime, "tomorrow 14:00", [], Today).Value));
        Assert.Equal("2026-10-30T09:30", FieldTypes.TextOf(FieldTypes.Parse(FieldTypes.DateTime, "2026-10-30 9:30", [], Today).Value));
        Assert.Equal("2026-10-30T00:00", FieldTypes.TextOf(FieldTypes.Parse(FieldTypes.DateTime, "2026-10-30", [], Today).Value));
    }

    [Fact]
    public void TakesANumberAndAChoice()
    {
        Assert.Equal("3", FieldTypes.Parse(FieldTypes.Number, "3", [], Today).Value!.ToJsonString());
        Assert.Equal("2.5", FieldTypes.Parse(FieldTypes.Number, "2.5", [], Today).Value!.ToJsonString());
        Assert.NotNull(FieldTypes.Parse(FieldTypes.Number, "three", [], Today).Refused);
        Assert.Equal("Normal", FieldTypes.TextOf(FieldTypes.Parse(FieldTypes.Choice, "normal", ["High", "Normal"], Today).Value));
        Assert.Equal("Not one of the options: High, Normal", FieldTypes.Parse(FieldTypes.Choice, "low", ["High", "Normal"], Today).Refused);
    }

    [Fact]
    public void ReadsValuesOut()
    {
        Assert.Equal("30 October 2026, Friday", FieldTypes.Spoken(new ItemField("Due", FieldTypes.Date, JsonValue.Create("2026-10-30"), [])));
        Assert.Equal("30 October 2026, Friday, 14:00", FieldTypes.Spoken(new ItemField("At", FieldTypes.DateTime, JsonValue.Create("2026-10-30T14:00"), [])));
        Assert.Equal("done", FieldTypes.Spoken(new ItemField("Done", FieldTypes.Check, JsonValue.Create(true), [])));
        Assert.Equal("not done", FieldTypes.Spoken(new ItemField("Done", FieldTypes.Check, JsonValue.Create(false), [])));
        Assert.Equal("empty", FieldTypes.Spoken(new ItemField("Notes", FieldTypes.LongText, JsonValue.Create(string.Empty), [])));
        Assert.Equal("First, and 2 more lines", FieldTypes.Spoken(new ItemField("Notes", FieldTypes.LongText, JsonValue.Create("First\nSecond\nThird"), [])));
        Assert.Equal("42", FieldTypes.Spoken(new ItemField("Count", FieldTypes.Number, JsonValue.Create(42), [])));
        Assert.Equal("Washer", FieldTypes.Spoken(new ItemField("Waits on", FieldTypes.Link, JsonValue.Create("../Shopping/Washer.json"), []), _ => "Washer"));
        Assert.Equal("long text", FieldTypes.Spoken(FieldTypes.LongText));
    }

    [Fact]
    public void ATextFieldHoldsOneLine() =>
        Assert.NotNull(FieldTypes.Parse(FieldTypes.Text, "one\ntwo", [], Today).Refused);
}
