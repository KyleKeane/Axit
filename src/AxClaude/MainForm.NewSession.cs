using AxClaude.Core.Pty;
using Axit.Core;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- New session (FR-8.6) ----

    /// <summary>The recommended ways to start Claude, in the order of the notice; Custom comes after them.</summary>
    private static readonly SessionPreset ContinuePreset = new("Continue the last conversation", ["--continue"]);

    private static readonly SessionPreset[] SessionPresets =
    [
        new("New conversation", []),
        ContinuePreset,
        new("Choose a conversation to resume", ["--resume"]),
        new("New conversation in plan mode", ["--permission-mode", "plan"]),
        new("New conversation, edits accepted without asking", ["--permission-mode", "acceptEdits"]),
    ];

    /// <summary>
    /// Session → New session… (Ctrl+N): the folder with a Choose folder button under it, the recommended argument
    /// sets as radio buttons, Custom with a free argument field (<see cref="SessionControls"/>, the window's own
    /// control in the notice). Enter stops the current session and starts a new one in the folder with those
    /// arguments; Escape keeps the current session.
    /// </summary>
    private void NewSession()
    {
        var current = ClaudeArgs;
        var selected = Array.FindIndex(SessionPresets, preset => preset.Arguments.SequenceEqual(current, StringComparer.Ordinal));
        var custom = selected < 0 ? ClaudeLauncher.JoinArguments(current) : string.Empty;
        var text = "Choose how Claude starts. Starting stops the current session and starts a new one in the folder.";
        if (ClaudeBusy)
        {
            text = "Claude is still working. A new session stops it in the middle of its work.\n" + text;
        }
        else if (ClaudeBackground is { } background)
        {
            text = $"Claude has work running in the background ({background}). A new session stops it.\n" + text;
        }

        var controls = new SessionControls(_folder, SessionPresets, selected < 0 ? SessionPresets.Length : selected, custom,
            "How Claude starts", "Custom arguments",
            "Arguments for Claude Code, one or more on each line. Enter starts the session, Shift+Enter starts a new line.",
            start => PickFolder(start ?? _folder), said => Announce(said, true), Overlay.SetHeadLine, Overlay.PressDefault);
        ShowNotice(new Notice("New session", text,
        [
            new OverlayChoice("&Start session", () => StartNewSession(controls), IsDefault: true),
            new OverlayChoice("Cancel", IsCancel: true),
        ])
        {
            Custom = controls,
            Accepts = controls.Ready,
            HeadLine = controls.FolderLine,
        });
    }

    private void StartNewSession(SessionControls controls)
    {
        var folder = controls.Folder;
        if (folder is null || !Directory.Exists(folder))
        {
            Fail("The folder no longer exists: " + folder);
            return;
        }

        var arguments = controls.SelectedPreset?.Arguments ?? ClaudeLauncher.SplitArguments(controls.CustomArguments);
        SetFolder(folder);
        // The Continue preset is the default (FR-9.1), so a folder without a conversation gets the same rescue: a new one.
        _claudeArgs = controls.SelectedPreset == ContinuePreset ? null : [.. arguments];
        var with = arguments.Count > 0 ? " with " + ClaudeLauncher.JoinArguments(arguments) : " with no extra arguments";
        Announce($"New session in {PathName.Of(folder)}", false);
        // A new session is a new conversation (or a resumed one): the window is cleared first, and what Claude prints
        // then, a replay with its blocks included, arrives as at the first start of the app.
        Relaunch($"New session in {folder}{with}", clearConversation: true);
    }
}
