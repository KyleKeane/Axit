using AxClaude.Core.Pty;
using Axit.Core;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- New session (FR-8.6) ----

    /// <summary>The recommended ways to start Claude, in the order of the notice; Custom comes after them.</summary>
    private static readonly OverlayPreset[] SessionPresets =
    [
        new("New conversation", []),
        new("Continue the last conversation", ["--continue"]),
        new("Choose a conversation to resume", ["--resume"]),
        new("New conversation in plan mode", ["--permission-mode", "plan"]),
        new("New conversation, edits accepted without asking", ["--permission-mode", "acceptEdits"]),
    ];

    /// <summary>
    /// Session → New session… (Ctrl+N): the folder with a Choose folder button under it, the recommended argument
    /// sets as radio buttons, Custom with a free argument field. Enter stops the current session and starts a new
    /// one in the folder with those arguments; Escape keeps the current session.
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

        ShowNotice(new Notice("New session", text,
        [
            new OverlayChoice("&Start session", StartNewSession, IsDefault: true),
            new OverlayChoice("Cancel", IsCancel: true),
        ])
        {
            Session = new OverlaySession(_folder, ChooseSessionFolder, SessionPresets, selected < 0 ? SessionPresets.Length : selected, custom),
        });
    }

    private void ChooseSessionFolder()
    {
        if (PickFolder(Overlay.SessionFolder ?? _folder) is { } folder)
        {
            Overlay.SetSessionFolder(folder);
        }
    }

    private void StartNewSession()
    {
        var folder = Overlay.SessionFolder;
        if (folder is null || !Directory.Exists(folder))
        {
            Fail("The folder no longer exists: " + folder);
            return;
        }

        var arguments = Overlay.SelectedPreset?.Arguments ?? ClaudeLauncher.SplitArguments(Overlay.CustomArguments);
        SetFolder(folder);
        _claudeArgs = [.. arguments];
        var with = arguments.Count > 0 ? " with " + ClaudeLauncher.JoinArguments(arguments) : " with no extra arguments";
        Announce($"New session in {PathName.Of(folder)}", false);
        // A new session is a new conversation (or a resumed one): the window is cleared first, and what Claude prints
        // then, a replay with its blocks included, arrives as at the first start of the app.
        Relaunch($"New session in {folder}{with}", clearConversation: true);
    }
}
