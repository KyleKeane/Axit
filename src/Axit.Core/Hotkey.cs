namespace Axit.Core;

/// <summary>
/// A system-wide key that brings a background app up (docs/axstart/SPEC.md AS-4; from AxStart.Core on its second use,
/// axClip, 2026-10-23): the modifiers as <c>RegisterHotKey</c> takes them and a
/// virtual key code, written in the settings and said as a menu writes keys, "Ctrl+Shift+Space". Only the keys of
/// <see cref="Names"/> can be a hotkey's key.
/// </summary>
public sealed record Hotkey(int Modifiers, int Key)
{
    public const int Alt = 0x1;
    public const int Control = 0x2;
    public const int Shift = 0x4;
    public const int Windows = 0x8;

    /// <summary>Ctrl+Shift+Space, axStart's (docs/axstart/SPEC.md AS-D4); another app names its own default.</summary>
    public static readonly Hotkey Default = new(Control | Shift, 0x20);

    /// <summary>The keys a hotkey may end in, by virtual key code, with the names the text uses.</summary>
    public static readonly IReadOnlyDictionary<int, string> Names = BuildNames();

    /// <summary>"Ctrl+Windows+Alt+Shift+Space": the modifiers in that order, then the key.</summary>
    public override string ToString()
    {
        var parts = new List<string>();
        if ((Modifiers & Control) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((Modifiers & Windows) != 0)
        {
            parts.Add("Windows");
        }

        if ((Modifiers & Alt) != 0)
        {
            parts.Add("Alt");
        }

        if ((Modifiers & Shift) != 0)
        {
            parts.Add("Shift");
        }

        parts.Add(Names.TryGetValue(Key, out var name) ? name : $"Key {Key}");
        return string.Join("+", parts);
    }

    /// <summary>A hotkey back from its text, any order of the modifiers and any case; null when the text is not one.</summary>
    public static Hotkey? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split('+', StringSplitOptions.TrimEntries);
        var modifiers = 0;
        foreach (var part in parts[..^1])
        {
            modifiers |= part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => Control,
                "alt" => Alt,
                "shift" => Shift,
                "windows" or "win" => Windows,
                _ => -1,
            };
            if (modifiers < 0)
            {
                return null;
            }
        }

        var key = Names.FirstOrDefault(pair => string.Equals(pair.Value, parts[^1], StringComparison.OrdinalIgnoreCase));
        return key.Value is null ? null : new Hotkey(modifiers, key.Key);
    }

    /// <summary>
    /// Why a key cannot be the hotkey (AS-4.2), or null when it can: it needs Ctrl, Alt or the Windows key, since a key
    /// with Shift alone or nothing would be taken from every program's typing; and it must be one of <see cref="Names"/>.
    /// </summary>
    public string? Refusal()
    {
        if (!Names.ContainsKey(Key))
        {
            return "That key cannot be a hotkey";
        }

        return (Modifiers & (Control | Alt | Windows)) == 0 ? "Use Ctrl, Alt or the Windows key with it" : null;
    }

    private static Dictionary<int, string> BuildNames()
    {
        var names = new Dictionary<int, string>
        {
            [0x20] = "Space",
            [0x0D] = "Enter",
            [0x09] = "Tab",
            [0x08] = "Backspace",
            [0x2D] = "Insert",
            [0x2E] = "Delete",
            [0x24] = "Home",
            [0x23] = "End",
            [0x21] = "Page Up",
            [0x22] = "Page Down",
            [0x25] = "Left",
            [0x26] = "Up",
            [0x27] = "Right",
            [0x28] = "Down",
        };
        for (var c = 'A'; c <= 'Z'; c++)
        {
            names[c] = c.ToString();
        }

        for (var d = '0'; d <= '9'; d++)
        {
            names[d] = d.ToString();
        }

        for (var f = 1; f <= 24; f++)
        {
            names[0x6F + f] = "F" + f;
        }

        return names;
    }
}
