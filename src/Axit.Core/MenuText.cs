namespace Axit.Core;

/// <summary>
/// The words of a menu item or a button as they are spoken or searched (docs/axit/SPEC.md AX-6.4, AX-10.2): the
/// text without its mnemonic marker (a doubled ampersand stays one) and without a trailing ellipsis or full stop,
/// so that "Save &amp;as..." is "Save as". The palette's entries, a notice's key line and a menu choice's announcement
/// all read a text this way; until 2026-10-23 four places stripped the marker themselves.
/// </summary>
public static class MenuText
{
    public static string Plain(string text) =>
        text.Replace("&&", "\0").Replace("&", string.Empty).Replace("\0", "&").TrimEnd('.', '…').Trim();
}
