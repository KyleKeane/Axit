using AxJSON.Core;

namespace AxJSON.Tests;

/// <summary>The scanner on its own (docs/axjson/SPEC.md AJ-2.2, AJ-2.3): strings with their escapes and quotes, comments as repairs, and a lone slash as a token of its own.</summary>
public sealed class JsonScannerTests
{
    private static IReadOnlyList<Token> Tokens(string text) => JsonScanner.Scan(text).Tokens;

    [Fact]
    public void A_string_keeps_its_quotes_and_escapes_as_a_token_and_unquotes_to_its_characters()
    {
        var tokens = Tokens("\"a\\\"b\\n\"");
        Assert.Equal([TokenKind.String, TokenKind.End], tokens.Select(token => token.Kind));
        Assert.Equal("\"a\\\"b\\n\"", tokens[0].Text);
        Assert.False(tokens[0].SingleQuoted);
        Assert.Equal("a\"b\n", JsonScanner.Unquote(tokens[0]));
    }

    [Fact]
    public void A_single_quoted_string_is_a_string_marked_for_repair_and_an_unterminated_one_runs_to_the_end()
    {
        var single = Tokens("'x'")[0];
        Assert.Equal(TokenKind.String, single.Kind);
        Assert.True(single.SingleQuoted);
        Assert.Equal("x", JsonScanner.Unquote(single));

        var open = Tokens("\"abc")[0];
        Assert.Equal(TokenKind.String, open.Kind);
        Assert.Equal(4, open.End);
        Assert.Equal("abc", JsonScanner.Unquote(open));
    }

    [Fact]
    public void A_lone_slash_is_a_token_of_its_own_and_comments_are_repairs()
    {
        Assert.Equal(["/", string.Empty], Tokens("/").Select(token => token.Text));
        Assert.Equal([TokenKind.Word, TokenKind.Word, TokenKind.Word, TokenKind.End], Tokens("a/b").Select(token => token.Kind));

        var (lineComment, repairs) = JsonScanner.Scan("// note\n1");
        Assert.Equal([RepairKind.Comment], repairs);
        Assert.Equal([TokenKind.Number, TokenKind.End], lineComment.Select(token => token.Kind));

        var (blockComment, blockRepairs) = JsonScanner.Scan("/* x */ 2 /* open");
        Assert.Equal([RepairKind.Comment, RepairKind.Comment], blockRepairs);
        Assert.Equal(["2", string.Empty], blockComment.Select(token => token.Text));
    }

    [Fact]
    public void Quote_writes_a_json_string_literal()
    {
        Assert.Equal("\"a\\\"b\\n\\\\\"", JsonScanner.Quote("a\"b\n\\"));
        Assert.Equal("\"\"", JsonScanner.Quote(string.Empty));
    }
}
