using System.Security.Cryptography;
using System.Text;

namespace Axit.Core.Mcp;

/// <summary>A client the user allowed (docs/axit/mcp.md MC-6.4): known by its program file; its name as it gave it, for display.</summary>
public sealed record McpClient(string Program, string Name, string Paired);

/// <summary>The shape of <see cref="AppPaths.McpFile"/>: off until the user turns it on, and the paired clients.</summary>
public sealed class McpSettings
{
    public bool Enabled { get; set; }

    public List<McpClient> Clients { get; set; } = [];

    /// <summary>HMAC-SHA256 of <see cref="Enabled"/> and the clients under the key (<see cref="McpAccess.SealOf"/>), so that a hand-written file counts for nothing.</summary>
    public string Seal { get; set; } = string.Empty;
}

/// <summary>
/// Whether AI tools may use Axit, which clients, and with what key (docs/axit/mcp.md MC-6.1, MC-6.3, MC-6.4): the
/// bundle's one setting, read again at every connection so that turning it off in one window ends every client's
/// access at its next call, and the key the broker answers the windows' challenges with. The file is written whole
/// through <see cref="SettingsFile"/>, sealed with an HMAC under the key: a file whose seal does not check, written by
/// anything but these methods (an AI tool's own file writing, say), reads as off with no clients. The pure parts (the
/// client rule, the proof, the seal, the hosts refused) are tested in <c>tests/Axit.Tests</c>.
/// </summary>
public static class McpAccess
{
    /// <summary>
    /// Programs that are never paired (MC-6.4): hosts that run whatever script they are given, so that a pairing would
    /// trust every script; a client is a program of its own (Claude Code's).
    /// </summary>
    public static readonly IReadOnlyList<string> RefusedHosts =
        ["cmd.exe", "powershell.exe", "pwsh.exe", "wscript.exe", "cscript.exe", "mshta.exe", "rundll32.exe", "regsvr32.exe", "python.exe", "pythonw.exe", "py.exe", "explorer.exe", "conhost.exe"];

    /// <summary>Whether a program may be paired at all.</summary>
    public static bool MayPair(string program) =>
        !RefusedHosts.Contains(Path.GetFileName(program), StringComparer.OrdinalIgnoreCase);

    /// <summary>The setting as written by Axit: off with no clients when there is no key or the seal does not check.</summary>
    public static McpSettings Load()
    {
        var settings = SettingsFile.Load<McpSettings>(AppPaths.McpFile, out _);
        return ReadKey() is { } key && Checks(key, settings) ? settings : new McpSettings();
    }

    public static bool Enabled => Load().Enabled;

    /// <summary>On or off; on makes the key first, so that the file can be sealed and a broker started next finds it.</summary>
    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            MakeKey();
        }

        var settings = Load();
        settings.Enabled = enabled;
        Save(settings);
    }

    /// <summary>The client paired for this program file, or null.</summary>
    public static McpClient? Find(McpSettings settings, string program) =>
        settings.Clients.FirstOrDefault(client => string.Equals(client.Program, program, StringComparison.OrdinalIgnoreCase));

    /// <summary>The client added, or its earlier pairing replaced.</summary>
    public static void Pair(string program, string name)
    {
        var settings = Load();
        settings.Clients = [.. settings.Clients.Where(client => !string.Equals(client.Program, program, StringComparison.OrdinalIgnoreCase)), new McpClient(program, name, DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture))];
        Save(settings);
    }

    /// <summary>Axit → AI tools → Forget AI tools: every client forgotten, so that each must be allowed again.</summary>
    public static void ForgetAll()
    {
        var settings = Load();
        settings.Clients = [];
        Save(settings);
    }

    /// <summary>The file written with its seal; without a key (the access never turned on) it is written off and empty.</summary>
    private static void Save(McpSettings settings)
    {
        if (ReadKey() is not { } key)
        {
            settings = new McpSettings();
            SettingsFile.Save(AppPaths.McpFile, settings);
            return;
        }

        settings.Seal = SealOf(key, settings);
        SettingsFile.Save(AppPaths.McpFile, settings);
    }

    /// <summary>The seal of the setting and the clients: one line each, in order, under HMAC-SHA256.</summary>
    public static string SealOf(byte[] key, McpSettings settings)
    {
        var text = new StringBuilder(settings.Enabled ? "on" : "off");
        foreach (var client in settings.Clients)
        {
            text.Append('\n').Append(client.Program).Append('\t').Append(client.Name).Append('\t').Append(client.Paired);
        }

        return Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(text.ToString())));
    }

    /// <summary>Whether the file's seal is the one its content has under the key, compared in constant time.</summary>
    public static bool Checks(byte[] key, McpSettings settings) =>
        settings.Seal.Length > 0
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(SealOf(key, settings)), Encoding.UTF8.GetBytes(settings.Seal));

    /// <summary>The key, made once: 32 random bytes kept with DPAPI for this user (MC-6.3).</summary>
    public static void MakeKey()
    {
        if (ReadKey() is not null)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.McpKeyFile)!);
        File.WriteAllText(AppPaths.McpKeyFile, UserSecret.Protect(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
    }

    /// <summary>The key, or null when there is none or it was made for another user.</summary>
    public static byte[]? ReadKey()
    {
        try
        {
            if (!File.Exists(AppPaths.McpKeyFile) || UserSecret.Unprotect(File.ReadAllText(AppPaths.McpKeyFile).Trim()) is not { } text)
            {
                return null;
            }

            var key = Convert.FromBase64String(text);
            return key.Length == 32 ? key : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            return null;
        }
    }

    /// <summary>A fresh challenge for a connection: 32 random bytes as base64.</summary>
    public static string NewChallenge() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>The answer to a challenge: HMAC-SHA256 of its text under the key, as base64.</summary>
    public static string Proof(byte[] key, string challenge) =>
        Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(challenge)));

    /// <summary>Whether the answer proves the key, compared in constant time.</summary>
    public static bool Proves(byte[] key, string challenge, string? answer) =>
        answer is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Proof(key, challenge)), Encoding.UTF8.GetBytes(answer));
}
