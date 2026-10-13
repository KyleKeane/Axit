namespace Axit.Forms;

/// <summary>
/// The keys that mean the same in every window of Axit (docs/axit/SPEC.md AX-8.2). Every window reads them from
/// here, so a change is one line; a window's own keys live in its own table (<c>AxDownKeys</c>, <c>AxClaudeKeys</c>),
/// and the collision test in <c>tests/Axit.Tests</c> fails when a window key equals one of these. Tab and Shift+Tab,
/// Alt+F4, Alt and F10 are Windows' own and need no constant.
/// </summary>
public static class BundleKeys
{
    public const Keys Shortcuts = Keys.F1;

    public const Keys NextControl = Keys.Control | Keys.Tab;
    public const Keys PreviousControl = Keys.Control | Keys.Shift | Keys.Tab;
    public const Keys NextControlAlso = Keys.F6;

    public const Keys Find = Keys.Control | Keys.F;
    public const Keys FindNext = Keys.F3;
    public const Keys FindPrevious = Keys.Shift | Keys.F3;

    /// <summary>What the window holds: AxClaude's conversation as a text file, AxDown's file.</summary>
    public const Keys Save = Keys.Control | Keys.S;

    public const Keys LargerText = Keys.Control | Keys.Oemplus;
    public const Keys LargerTextNumpad = Keys.Control | Keys.Add;
    public const Keys SmallerText = Keys.Control | Keys.OemMinus;
    public const Keys SmallerTextNumpad = Keys.Control | Keys.Subtract;

    /// <summary>One screen in an edit control, and to its ends, through <see cref="EditPaging"/>.</summary>
    public const Keys PageUp = Keys.PageUp;
    public const Keys PageDown = Keys.PageDown;

    /// <summary>Every key above with its name, for the collision test and the F1 texts.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Page up", PageUp),
        ("Page down", PageDown),
        ("Keyboard shortcuts", Shortcuts),
        ("Next control", NextControl),
        ("Previous control", PreviousControl),
        ("Next control (F6)", NextControlAlso),
        ("Find", Find),
        ("Find next", FindNext),
        ("Find previous", FindPrevious),
        ("Save", Save),
        ("Larger text", LargerText),
        ("Larger text (numpad)", LargerTextNumpad),
        ("Smaller text", SmallerText),
        ("Smaller text (numpad)", SmallerTextNumpad),
    ];
}
