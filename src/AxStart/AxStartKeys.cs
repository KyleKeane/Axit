namespace AxStart;

/// <summary>
/// axStart's window-specific keys (SPEC.md AS-5.3; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>StartForm</c> and the F1 text read this table. The bundle-wide keys
/// (F1, text size, Page Up and Page Down) are <c>Axit.Forms.BundleKeys</c>; the hotkey is a setting, not a key of
/// the window. The caret stays in the field (AS-D2): every key here acts on the list of matches or on the program.
/// </summary>
public static class AxStartKeys
{
    public const Keys NextMatch = Keys.Down;
    public const Keys PreviousMatch = Keys.Up;
    public const Keys FirstMatch = Keys.Control | Keys.Home;
    public const Keys LastMatch = Keys.Control | Keys.End;

    /// <summary>The selected match started, the window hidden first.</summary>
    public const Keys Start = Keys.Return;

    public const Keys StartAsAdministrator = Keys.Control | Keys.Shift | Keys.Return;

    /// <summary>axFile on the program file's folder, the file selected.</summary>
    public const Keys OpenFileLocation = Keys.Control | Keys.O;

    /// <summary>The selected match, its kind and its path.</summary>
    public const Keys Information = Keys.Control | Keys.I;

    /// <summary>The Applications folder read again.</summary>
    public const Keys Refresh = Keys.F5;

    /// <summary>The window hidden at once, whatever the field holds.</summary>
    public const Keys Hide = Keys.Escape;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Next match", NextMatch),
        ("Previous match", PreviousMatch),
        ("First match", FirstMatch),
        ("Last match", LastMatch),
        ("Start", Start),
        ("Start as administrator", StartAsAdministrator),
        ("Open file location", OpenFileLocation),
        ("Information", Information),
        ("Refresh programs", Refresh),
        ("Hide", Hide),
    ];
}
