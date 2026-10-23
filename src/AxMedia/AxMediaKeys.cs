namespace AxMedia;

/// <summary>
/// axMedia's window-specific keys (SPEC.md AM-5.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the key switch in <c>MediaForm.ProcessWindowKey</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the control keys, find, Ctrl+S, text size) are <c>Axit.Forms.BundleKeys</c>. Enter, Delete and Space act in
/// the list only; in a notice's field they are the field's own. Everything else works from anywhere in the window.
/// </summary>
public static class AxMediaKeys
{
    /// <summary>In the list: the file under the selection plays.</summary>
    public const Keys Play = Keys.Return;

    /// <summary>In the list: the file taken out of the queue.</summary>
    public const Keys Remove = Keys.Delete;

    /// <summary>In the list: pause, or resume (AM-D7); Ctrl+P the same from anywhere in the window.</summary>
    public const Keys PauseResume = Keys.Space;

    public const Keys PauseResumeAlso = Keys.Control | Keys.P;

    /// <summary>The file's place in the queue, and its position while it plays.</summary>
    public const Keys Information = Keys.Control | Keys.I;

    /// <summary>The Windows file picker, several files at once, appended to the queue.</summary>
    public const Keys OpenFiles = Keys.Control | Keys.O;

    /// <summary>The Windows folder picker; the folder's media files appended.</summary>
    public const Keys OpenFolder = Keys.Control | Keys.Shift | Keys.O;

    /// <summary>A new axMedia window (AX-9).</summary>
    public const Keys NewWindow = Keys.Control | Keys.N;

    /// <summary>The file playing, or the one under the selection, selected in File Explorer.</summary>
    public const Keys ShowInExplorer = Keys.Control | Keys.W;

    public const Keys Forward = Keys.Control | Keys.Right;
    public const Keys Back = Keys.Control | Keys.Left;
    public const Keys NextFile = Keys.Control | Keys.Shift | Keys.Right;
    public const Keys PreviousFile = Keys.Control | Keys.Shift | Keys.Left;
    public const Keys GoToTime = Keys.Control | Keys.T;
    public const Keys Faster = Keys.Control | Keys.Shift | Keys.OemPeriod;
    public const Keys Slower = Keys.Control | Keys.Shift | Keys.Oemcomma;
    public const Keys WhatIsPlaying = Keys.Control | Keys.Q;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Play (in the list)", Play),
        ("Remove (in the list)", Remove),
        ("Pause or resume (in the list)", PauseResume),
        ("Pause or resume", PauseResumeAlso),
        ("Information about the file", Information),
        ("Open files", OpenFiles),
        ("Open folder", OpenFolder),
        ("New window", NewWindow),
        ("Show the file in File Explorer", ShowInExplorer),
        ("Forward", Forward),
        ("Back", Back),
        ("Next file", NextFile),
        ("Previous file", PreviousFile),
        ("Go to time", GoToTime),
        ("Faster", Faster),
        ("Slower", Slower),
        ("What is playing", WhatIsPlaying),
    ];
}
