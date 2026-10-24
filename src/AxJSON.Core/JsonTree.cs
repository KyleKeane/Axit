namespace AxJSON.Core;

/// <summary>A value of the document: an object with its members, an array with its items, or a scalar (string, number, word) as its token.</summary>
public sealed class JsonNode
{
    public JsonNode(TokenKind kind, int start, string? key)
    {
        Kind = kind;
        Start = start;
        Key = key;
    }

    /// <summary><see cref="TokenKind.OpenObject"/>, <see cref="TokenKind.OpenArray"/>, or the scalar's token kind.</summary>
    public TokenKind Kind { get; }

    /// <summary>Where the value starts in the text: the brace, the bracket, or the scalar's first character.</summary>
    public int Start { get; }

    /// <summary>The member's key when the value is a member of an object, as the characters it stands for.</summary>
    public string? Key { get; }

    /// <summary>Where the key starts, for a member; the value's start otherwise.</summary>
    public int KeyStart { get; set; }

    /// <summary>The scalar's text as written ("\"opus\"", "3", "true"); empty for a container.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The characters a string scalar stands for; the text for the other scalars.</summary>
    public string Value { get; set; } = string.Empty;

    public List<JsonNode> Children { get; } = [];

    public bool IsContainer => Kind is TokenKind.OpenObject or TokenKind.OpenArray;
}

/// <summary>Where the document stops being JSON: the place and what was expected there.</summary>
public sealed record JsonProblem(int Start, string Message);

/// <summary>The parse of a document: the root (null for an empty text), the first problem if any, and the repairs made on the way.</summary>
public sealed record JsonTree(JsonNode? Root, JsonProblem? Problem, IReadOnlyList<RepairKind> Repairs);

/// <summary>
/// Builds the tree of a JSON text from its tokens, forgivingly (docs/axjson/SPEC.md AJ-2.2, AJ-2.3): a trailing
/// comma, a missing comma between members or items, a single-quoted string and an unquoted key are taken and
/// counted as repairs; anything else stops the parse at the first problem, with what was built so far kept, so
/// that the keys before the problem still navigate. Pure code, tested in <c>tests/AxJSON.Tests</c>.
/// </summary>
public static class JsonParser
{
    /// <summary>
    /// How deep objects and arrays may nest (security audit S-39): deeper, the document is "not valid", so that a file
    /// of nested brackets is refused rather than ending the process, which no exception handler can catch when the
    /// stack runs out. 512 is far past any real document and well within the stack, for the parse, the outline and
    /// Format alike.
    /// </summary>
    public const int MaxDepth = 512;

    public static JsonTree Parse(string text)
    {
        var (tokens, repairs) = JsonScanner.Scan(text);
        var parser = new State(tokens, repairs);
        JsonNode? root = null;
        if (tokens[0].Kind != TokenKind.End)
        {
            root = parser.Value(null);
            if (parser.Problem is null && parser.Current.Kind != TokenKind.End)
            {
                parser.Fail("unexpected text after the end of the document");
            }
        }

        return new JsonTree(root, parser.Problem, repairs);
    }

    private sealed class State(IReadOnlyList<Token> tokens, List<RepairKind> repairs)
    {
        private int _at;
        private int _depth;

        public JsonProblem? Problem { get; private set; }

        public Token Current => tokens[Math.Min(_at, tokens.Count - 1)];

        private Token Next => tokens[Math.Min(_at + 1, tokens.Count - 1)];

        public void Fail(string message)
        {
            Problem ??= new JsonProblem(Current.Start, message);
        }

        private void Advance() => _at = Math.Min(_at + 1, tokens.Count - 1);

        /// <summary>A value at the current token, or null with the problem recorded.</summary>
        public JsonNode? Value(string? key)
        {
            var token = Current;
            switch (token.Kind)
            {
                case TokenKind.OpenObject or TokenKind.OpenArray when _depth >= MaxDepth:
                    Fail($"objects and arrays nested more than {MaxDepth} deep");
                    return null;
                case TokenKind.OpenObject:
                    _depth++;
                    try
                    {
                        return Object(key);
                    }
                    finally
                    {
                        _depth--;
                    }

                case TokenKind.OpenArray:
                    _depth++;
                    try
                    {
                        return Array(key);
                    }
                    finally
                    {
                        _depth--;
                    }

                case TokenKind.String:
                    Advance();
                    if (token.SingleQuoted)
                    {
                        repairs.Add(RepairKind.SingleQuotedString);
                    }

                    return new JsonNode(TokenKind.String, token.Start, key) { Text = token.Text, Value = JsonScanner.Unquote(token) };
                case TokenKind.Number:
                    Advance();
                    return new JsonNode(TokenKind.Number, token.Start, key) { Text = token.Text, Value = token.Text };
                case TokenKind.Word when token.Text is "true" or "false" or "null":
                    Advance();
                    return new JsonNode(TokenKind.Word, token.Start, key) { Text = token.Text, Value = token.Text };
                case TokenKind.End:
                    Fail("the document ends where a value was expected");
                    return null;
                default:
                    Fail($"expected a value, found {Describe(token)}");
                    return null;
            }
        }

        private JsonNode Object(string? key)
        {
            var node = new JsonNode(TokenKind.OpenObject, Current.Start, key);
            Advance();
            while (Problem is null)
            {
                if (Current.Kind == TokenKind.CloseObject)
                {
                    Advance();
                    return node;
                }

                if (Current.Kind == TokenKind.End)
                {
                    Fail("the document ends inside an object; expected a closing brace");
                    return node;
                }

                if (Current.Kind != TokenKind.String && Current.Kind != TokenKind.Word)
                {
                    Fail($"expected a key in quotes, found {Describe(Current)}");
                    return node;
                }

                var keyToken = Current;
                var memberKey = keyToken.Kind == TokenKind.String ? JsonScanner.Unquote(keyToken) : keyToken.Text;
                if (keyToken.Kind == TokenKind.Word)
                {
                    repairs.Add(RepairKind.UnquotedKey);
                }
                else if (keyToken.SingleQuoted)
                {
                    repairs.Add(RepairKind.SingleQuotedString);
                }

                Advance();
                if (Current.Kind != TokenKind.Colon)
                {
                    Fail($"expected a colon after the key {memberKey}, found {Describe(Current)}");
                    return node;
                }

                Advance();
                var member = Value(memberKey);
                if (member is null)
                {
                    return node;
                }

                member.KeyStart = keyToken.Start;
                node.Children.Add(member);
                if (!Separator(TokenKind.CloseObject, memberKey))
                {
                    return node;
                }
            }

            return node;
        }

        private JsonNode Array(string? key)
        {
            var node = new JsonNode(TokenKind.OpenArray, Current.Start, key);
            Advance();
            while (Problem is null)
            {
                if (Current.Kind == TokenKind.CloseArray)
                {
                    Advance();
                    return node;
                }

                if (Current.Kind == TokenKind.End)
                {
                    Fail("the document ends inside an array; expected a closing bracket");
                    return node;
                }

                var item = Value(null);
                if (item is null)
                {
                    return node;
                }

                item.KeyStart = item.Start;
                node.Children.Add(item);
                if (!Separator(TokenKind.CloseArray, null))
                {
                    return node;
                }
            }

            return node;
        }

        /// <summary>After a member or an item: a comma, or the container's close; a comma before the close is a trailing comma, a missing comma before the next member or item is put in. False with the problem recorded.</summary>
        private bool Separator(TokenKind close, string? afterKey)
        {
            if (Current.Kind == TokenKind.Comma)
            {
                Advance();
                if (Current.Kind == close)
                {
                    repairs.Add(RepairKind.TrailingComma);
                }

                return true;
            }

            if (Current.Kind == close)
            {
                return true;
            }

            if (Current.Kind == TokenKind.End)
            {
                Fail(close == TokenKind.CloseObject
                    ? "the document ends inside an object; expected a closing brace"
                    : "the document ends inside an array; expected a closing bracket");
                return false;
            }

            var startsNext = close == TokenKind.CloseObject
                ? Current.Kind is TokenKind.String or TokenKind.Word && Next.Kind == TokenKind.Colon
                : Current.Kind is TokenKind.String or TokenKind.Number or TokenKind.OpenObject or TokenKind.OpenArray || (Current.Kind == TokenKind.Word && Current.Text is "true" or "false" or "null");
            if (startsNext)
            {
                repairs.Add(RepairKind.MissingComma);
                return true;
            }

            var what = close == TokenKind.CloseObject ? "a comma or a closing brace" : "a comma or a closing bracket";
            Fail(afterKey is null ? $"expected {what}, found {Describe(Current)}" : $"expected {what} after {afterKey}, found {Describe(Current)}");
            return false;
        }

        private static string Describe(Token token) => token.Kind switch
        {
            TokenKind.End => "the end of the document",
            TokenKind.String => "a string",
            TokenKind.Number => "a number",
            _ => $"'{token.Text}'",
        };
    }
}
