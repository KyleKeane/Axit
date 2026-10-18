using AxJSON.Core;

namespace AxJSON.Tests;

/// <summary>The elements of a JSON text for the navigation keys (docs/axjson/SPEC.md AJ-2.2): what is found, where, and how it is spoken.</summary>
public sealed class JsonOutlineTests
{
    private const string Settings = """
        {
          "model": "opus",
          "permissions": { "allow": ["Bash", "Read"], "deny": [] },
          "count": 3,
          "on": true,
          "note": ""
        }
        """;

    [Fact]
    public void Keys_in_document_order_with_their_values_or_sizes()
    {
        var keys = JsonOutline.Keys(Settings);
        Assert.Equal(["model: opus", "permissions, object with 2 keys", "allow, array with 2 items", "deny, empty array", "count: 3", "on: true", "note: empty string"],
            keys.Select(k => k.Spoken));
        Assert.Equal(Settings.IndexOf("\"model\"", StringComparison.Ordinal), keys[0].Start);
        Assert.Equal(Settings.IndexOf("\"allow\"", StringComparison.Ordinal), keys[2].Start);
    }

    [Fact]
    public void Objects_arrays_and_items_are_spoken_with_their_place()
    {
        Assert.Equal(["object with 5 keys", "permissions, object with 2 keys"], JsonOutline.Objects(Settings).Select(o => o.Spoken));
        Assert.Equal(["allow, array with 2 items", "deny, empty array"], JsonOutline.Arrays(Settings).Select(a => a.Spoken));
        Assert.Equal(["Item 1 of 2, Bash", "Item 2 of 2, Read"], JsonOutline.Items(Settings).Select(i => i.Spoken));
        Assert.Equal(Settings.IndexOf("\"Read\"", StringComparison.Ordinal), JsonOutline.Items(Settings)[1].Start);
    }

    [Fact]
    public void Next_and_previous_go_by_position_and_stop_at_the_ends()
    {
        var caret = Settings.IndexOf("\"count\"", StringComparison.Ordinal);
        Assert.Equal("on: true", JsonOutline.NextKey(Settings, caret)!.Spoken);
        Assert.Equal("deny, empty array", JsonOutline.PreviousKey(Settings, caret)!.Spoken);
        Assert.Null(JsonOutline.NextKey(Settings, Settings.Length));
        Assert.Null(JsonOutline.PreviousKey(Settings, 0));
        Assert.Equal("object with 5 keys", JsonOutline.NextObject(Settings, -1)!.Spoken);
        Assert.Null(JsonOutline.NextError(Settings, 0));
    }

    [Theory]
    [InlineData("{ \"a\": 1 \"b\": 2 }", 1, "expected a comma")]
    [InlineData("{ \"a\": }", 1, "expected a value, found '}'")]
    [InlineData("{ \"a\" 1 }", 1, "expected a colon after the key a, found a number")]
    [InlineData("[1, 2", 1, "the document ends inside an array; expected a closing bracket")]
    [InlineData("}", 1, "expected a value, found '}'")]
    [InlineData("{\"a\": 1}\r\nx", 2, "unexpected text after the end of the document")]
    public void The_first_problem_is_an_element_with_its_line(string text, int line, string message)
    {
        var errors = JsonOutline.Errors(text);
        if (message == "expected a comma")
        {
            // A missing comma between two members on one line is repaired, not reported.
            Assert.Empty(errors);
            return;
        }

        var error = Assert.Single(errors);
        Assert.StartsWith($"Error, line {line}: {message}", error.Spoken);
    }

    [Fact]
    public void Keys_before_a_problem_still_navigate()
    {
        const string broken = "{ \"a\": 1, \"b\": [1, 2, \"c\": 3 }";
        // The array took 1, 2 and "c" as items before the colon stopped the parse.
        Assert.Equal(["a: 1", "b, array with 3 items"], JsonOutline.Keys(broken).Select(k => k.Spoken));
        Assert.Single(JsonOutline.Errors(broken));
    }

    [Fact]
    public void A_nearly_json_file_reads_as_json()
    {
        const string loose = "// settings\n{ model: 'opus', \"list\": [1, 2,], }";
        Assert.Equal(["model: opus", "list, array with 2 items"], JsonOutline.Keys(loose).Select(k => k.Spoken));
        Assert.Empty(JsonOutline.Errors(loose));
    }
}
