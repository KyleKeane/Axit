using AxJSON.Core;

namespace AxJSON.Tests;

/// <summary>Format (docs/axjson/SPEC.md AJ-2.3): the layout, the repairs and their announcement, and what is left alone.</summary>
public sealed class JsonFormatTests
{
    [Fact]
    public void Valid_json_is_laid_out_two_spaces_one_member_per_line()
    {
        var (text, message) = JsonFormat.Apply("{\"a\":1,\"b\":[true,{\"c\":\"x\"}],\"d\":{},\"e\":[]}");
        Assert.Equal("{\r\n  \"a\": 1,\r\n  \"b\": [\r\n    true,\r\n    {\r\n      \"c\": \"x\"\r\n    }\r\n  ],\r\n  \"d\": {},\r\n  \"e\": []\r\n}\r\n", text);
        Assert.Equal("Formatted", message);
    }

    [Fact]
    public void Repairs_are_made_and_named()
    {
        var (text, message) = JsonFormat.Apply("// comment\r\n{ model: 'it\\'s', \"list\": [1, 2,], \"x\": 1 \"y\": 2 }");
        Assert.Equal("{\r\n  \"model\": \"it's\",\r\n  \"list\": [\r\n    1,\r\n    2\r\n  ],\r\n  \"x\": 1,\r\n  \"y\": 2\r\n}\r\n", text);
        Assert.Equal("Formatted, repaired: 1 comment, 1 single-quoted string, 1 unquoted key, 1 trailing comma, 1 missing comma", message);
    }

    [Fact]
    public void Strings_keep_their_characters_and_escapes()
    {
        var (text, _) = JsonFormat.Apply("{\"s\": \"a \\\"q\\\" \\n tab\\t \\u00e9 é\"}");
        Assert.Equal("{\r\n  \"s\": \"a \\\"q\\\" \\n tab\\t é é\"\r\n}\r\n", text);
    }

    [Fact]
    public void A_problem_leaves_the_text_and_names_the_line()
    {
        const string broken = "{\r\n  \"a\": 1,\r\n  \"b\": \r\n}";
        var (text, message) = JsonFormat.Apply(broken);
        Assert.Equal(broken, text);
        Assert.Equal("Not valid JSON: line 4, expected a value, found '}'", message);
    }

    [Fact]
    public void Formatted_text_is_recognised_and_the_line_ending_followed()
    {
        var (text, message) = JsonFormat.Apply("{\n  \"a\": 1\n}\n");
        Assert.Equal("{\n  \"a\": 1\n}\n", text);
        Assert.Equal("Already formatted", message);
        Assert.Equal("Nothing to format", JsonFormat.Apply("  ").Message);
    }

    /// <summary>S-39: a file of nested brackets is refused as too deep, never a stack overflow that ends axJSON with the edits.</summary>
    [Fact]
    public void Nesting_past_the_limit_is_refused_and_does_not_end_the_process()
    {
        var deep = new string('[', 200_000) + new string(']', 200_000);
        var (text, message) = JsonFormat.Apply(deep);
        Assert.Equal(deep, text);
        Assert.StartsWith("Not valid JSON", message);
        Assert.Contains("nested more than", message);
        Assert.Equal("Formatted", JsonFormat.Apply(new string('[', 100) + new string(']', 100)).Message);
    }
}