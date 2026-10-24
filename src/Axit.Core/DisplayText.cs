using System.Text;

namespace Axit.Core;

/// <summary>
/// Text from outside shown as a list's line (security audit S-42): a file's name, a commit's subject, an item's title.
/// Characters that change how the rest looks without being seen themselves (the bidirectional overrides and isolates
/// that can show "invoice‮txt.exe" as "invoiceexe.txt", zero-width characters, control characters) are written as
/// "‹U+202E›", so that the eye and braille see what NVDA reads; anything else is left as it is. Pure, tested.
/// </summary>
public static class DisplayText
{
    public static string Visible(string text)
    {
        var plain = true;
        foreach (var c in text)
        {
            if (Hidden(c))
            {
                plain = false;
                break;
            }
        }

        if (plain)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length + 16);
        foreach (var c in text)
        {
            if (Hidden(c))
            {
                builder.Append($"‹U+{(int)c:X4}›");
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    /// <summary>The bidirectional marks, overrides and isolates, the zero-width characters, and the C0 and C1 controls.</summary>
    private static bool Hidden(char c) =>
        c < ' ' || (c >= '\u007F' && c <= '\u009F')
        || c is '؜' or '​' or '‌' or '‍' or '‎' or '‏' or '⁠' or '﻿'
        || (c >= '‪' && c <= '‮') || (c >= '⁦' && c <= '⁩');
}
