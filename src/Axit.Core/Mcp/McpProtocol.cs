using System.Text.Json;
using System.Text.Json.Nodes;

namespace Axit.Core.Mcp;

/// <summary>A tool as a client sees it (docs/axit/mcp.md MC-6.5): its name, what it does, its arguments as a JSON schema.</summary>
public sealed record McpTool(string Name, string Description, JsonObject InputSchema);

/// <summary>What a tool call gives back: a text, and whether it is the reason it did not run.</summary>
public sealed record McpResult(string Text, bool IsError = false);

/// <summary>
/// The Model Context Protocol as the broker speaks it (docs/axit/mcp.md MC-1.1): JSON-RPC 2.0, one message per line on
/// standard input and output. <see cref="Handle"/> takes one line and gives the line to answer, or null for a
/// notification; the tools come from the broker through the two functions it is made with, so that this class holds
/// no state of the apps and is tested on its own. Only what a tools server needs: <c>initialize</c>, <c>ping</c>,
/// <c>tools/list</c>, <c>tools/call</c>; anything else is "method not found".
/// </summary>
public sealed class McpProtocol(Func<IReadOnlyList<McpTool>> tools, Func<string, JsonObject, McpResult> call)
{
    /// <summary>The protocol versions understood, the newest first; a client asking for another is answered with the newest.</summary>
    public static readonly IReadOnlyList<string> Versions = ["2025-06-18", "2025-03-26", "2024-11-05"];

    /// <summary>The name the client gave in <c>initialize</c>, for the pairing notice and the log; "an AI tool" until then.</summary>
    public string ClientName { get; private set; } = "an AI tool";

    // Readable on the wire: a title with "é" or "·" stays itself, as the window shows it; still valid JSON.
    private static readonly JsonSerializerOptions Compact = new() { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public string? Handle(string line)
    {
        JsonObject request;
        try
        {
            request = JsonNode.Parse(line) as JsonObject ?? throw new JsonException("not an object");
        }
        catch (JsonException)
        {
            return Error(null, -32700, "Parse error");
        }

        var id = request["id"]?.DeepClone();
        var method = Text(request["method"]);
        var parameters = request["params"] as JsonObject ?? [];
        if (method is null)
        {
            return id is null ? null : Error(id, -32600, "Invalid request");
        }

        // A notification (no id) is never answered: notifications/initialized, notifications/cancelled.
        if (id is null)
        {
            return null;
        }

        try
        {
            return method switch
            {
                "initialize" => Result(id, Initialize(parameters)),
                "ping" => Result(id, []),
                "tools/list" => Result(id, new JsonObject { ["tools"] = new JsonArray([.. tools().Select(ToJson)]) }),
                "tools/call" => Result(id, Call(parameters)),
                _ => Error(id, -32601, $"Method not found: {method}"),
            };
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            return Error(id, -32602, "Invalid params: " + ex.Message);
        }
    }

    private JsonObject Initialize(JsonObject parameters)
    {
        if (Text(parameters["clientInfo"]?["name"]) is { Length: > 0 } name)
        {
            // The client's own word, shown in the pairing notice: hidden and reordering characters made visible (S-33).
            ClientName = DisplayText.Visible(name.Length > 60 ? name[..60] : name);
        }

        var asked = Text(parameters["protocolVersion"]);
        return new JsonObject
        {
            ["protocolVersion"] = asked is not null && Versions.Contains(asked) ? asked : Versions[0],
            ["capabilities"] = new JsonObject { ["tools"] = new JsonObject { ["listChanged"] = false } },
            ["serverInfo"] = new JsonObject { ["name"] = "axit", ["version"] = BundleInfo.Version },
            ["instructions"] = "Axit's windows for a blind user of NVDA: read what they show and open apps. Every call is in the user's log; nothing here changes or deletes anything.",
        };
    }

    private JsonObject Call(JsonObject parameters)
    {
        var name = Text(parameters["name"]) ?? throw new InvalidOperationException("no tool name");
        var arguments = parameters["arguments"] as JsonObject ?? [];
        var result = call(name, arguments);
        return new JsonObject
        {
            ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = result.Text }),
            ["isError"] = result.IsError,
        };
    }

    /// <summary>A string value, or null for a missing value or one of another type (a client's message is never trusted to be well formed).</summary>
    public static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static JsonObject ToJson(McpTool tool) => new()
    {
        ["name"] = tool.Name,
        ["description"] = tool.Description,
        ["inputSchema"] = tool.InputSchema.DeepClone(),
    };

    private static string Result(JsonNode id, JsonObject result) =>
        new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result }.ToJsonString(Compact);

    private static string Error(JsonNode? id, int code, string message) =>
        new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["error"] = new JsonObject { ["code"] = code, ["message"] = message } }.ToJsonString(Compact);
}
