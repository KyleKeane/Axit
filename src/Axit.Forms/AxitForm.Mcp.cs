using System.Runtime.InteropServices;
using Axit.Core;
using Axit.Core.Mcp;
using static Axit.Forms.User32;

namespace Axit.Forms;

/// <summary>
/// The window's part of the AI tools' access (docs/axit/mcp.md MC-6): its pipe started as it first shows, the two
/// items of Options → General, what the pipe reads of the window, and the pairing notice. The pipe itself is
/// <see cref="McpChannel"/>; the broker, which the AI client starts, is <see cref="McpBroker"/>.
/// </summary>
public abstract partial class AxitForm
{
    /// <summary>The app's verb (<see cref="Dispatch.Apps"/>), which decides whether the window is read at all (<see cref="McpTools.NeverRead"/>).</summary>
    private string? AppVerb => Dispatch.Apps.FirstOrDefault(app => string.Equals(app.Name, AppName, StringComparison.OrdinalIgnoreCase))?.Verb;

    /// <summary>The window's commands for <c>axit_window_commands</c>.</summary>
    internal IReadOnlyList<Command> CommandList => _commands;

    /// <summary>For the pipe's <c>hello</c>: the app's name.</summary>
    internal string AppNameForTools => AppName;

    private void StartMcp()
    {
        if (AppVerb is { } verb)
        {
            McpChannel.Start(this, verb);
        }
    }

    /// <summary>What the pipe's <c>state</c> reads (MC-6.5): the title, the notice and its text, the status bar, the focused control and its value.</summary>
    internal object McpState()
    {
        var focused = Control.FromChildHandle(GetFocus());
        return new
        {
            title = Text,
            notice = NoticeOpen ? Overlay.Shown?.Title : null,
            noticeText = NoticeOpen ? Overlay.Shown?.Text : null,
            status = Controls.OfType<StatusStrip>().SelectMany(strip => strip.Items.Cast<ToolStripItem>()).Select(item => item.Text).ToArray(),
            focus = focused is null || focused.FindForm() != this ? null : new { name = focused.AccessibleName, value = WindowReading.ValueOf(focused) },
        };
    }

    /// <summary>Options → General: the access on or off (MC-6.1), its state read as the menu opens, since another window may have changed it; and Forget AI tools.</summary>
    private void AddMcpItems(ToolStripMenuItem general)
    {
        var access = new ToolStripMenuItem("Let AI tools read and open A&xit (MCP)");
        access.Click += (_, _) =>
        {
            ToggleMcp(!access.Checked);
            access.Checked = McpAccess.Enabled;
        };
        general.DropDownOpening += (_, _) => access.Checked = McpAccess.Enabled;
        general.DropDownItems.Add(new ToolStripSeparator());
        general.DropDownItems.Add(access);
        general.DropDownItems.Add(new ToolStripMenuItem("For&get AI tools", null, (_, _) => ForgetMcpClients()));
    }

    private void ToggleMcp(bool on)
    {
        try
        {
            McpAccess.SetEnabled(on);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail("The AI tools' setting was not changed: " + ex.Message);
            return;
        }

        if (!on)
        {
            Announce("AI tools off: no AI tool can read or open Axit", true);
            return;
        }

        ShowNotice(Notice.Plain("AI tools on",
            "AI tools can now ask to read what the Axit windows show and to open apps; they cannot change or delete anything. "
            + "Each tool asks you once, in an Axit window, before it reads anything. axPass and axClip are never read.\n\n"
            + "To let a tool such as Claude Code in, add this MCP server to its settings:\n\n"
            + $"Command: {Environment.ProcessPath}\nArguments: mcp\n\n"
            + $"Every call is written in {AppPaths.McpAuditFile}. Options, General, Let AI tools read and open Axit turns it off again."));
    }

    private void ForgetMcpClients()
    {
        try
        {
            McpAccess.ForgetAll();
            Announce("AI tools forgotten: each asks again before it reads anything", true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("The AI tools were not forgotten: " + ex.Message);
        }
    }

    /// <summary>
    /// The pairing notice (MC-6.4), from the pipe's thread: "Claude Code asks to use Axit …", Allow and Deny. Deny is the
    /// default and Escape, so that Enter alone never allows; the notice is unprompted (its chime, and a moment in
    /// which keys do nothing), and the window flashes on the taskbar when it is not in front. Closing the window or
    /// leaving the notice unanswered refuses, when the pipe stops waiting. False, with nothing shown, while another
    /// notice is open: a notice replaces the one shown, and the user may be in the middle of answering it.
    /// </summary>
    internal bool AskToPairMcp(string name, string program, TaskCompletionSource<bool> answer)
    {
        if (NoticeOpen)
        {
            return false;
        }

        var text = $"{name} asks to use Axit. It runs as {program}.\n\n"
            + "It can read what the Axit windows show (their text, lists and status) and open apps. It cannot change or delete anything, and it never reads axPass or axClip.\n\n"
            + "Allow it? Options, General, Forget AI tools takes this back.";
        ShowNotice(new Notice($"{name} asks to use Axit", text,
        [
            new OverlayChoice("&Allow", () =>
            {
                // The call gave up waiting (two minutes): its notice is gone, and no pairing is written for it.
                if (answer.Task.IsCompleted)
                {
                    Announce($"{name}'s request has ended: nothing allowed", true);
                    return;
                }

                try
                {
                    McpAccess.Pair(program, name);
                    answer.TrySetResult(true);
                    Announce($"{name} may read Axit", true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    answer.TrySetResult(false);
                    Fail("The pairing was not written: " + ex.Message);
                }
            }),
            new OverlayChoice("&Deny", () => answer.TrySetResult(false), IsDefault: true, IsCancel: true),
        ])
        { Unprompted = true });
        // The window closing refuses; the handler goes with the answer, so that requests do not pile handlers up.
        FormClosedEventHandler refuse = (_, _) => answer.TrySetResult(false);
        FormClosed += refuse;
        answer.Task.ContinueWith(_ =>
        {
            try
            {
                BeginInvoke(() => FormClosed -= refuse);
            }
            catch (InvalidOperationException)
            {
                // The window is gone, its handlers with it.
            }
        }, TaskScheduler.Default);

        if (GetForegroundWindow() != Handle)
        {
            var info = new FlashInfo { Size = (uint)Marshal.SizeOf<FlashInfo>(), Window = Handle, Flags = FlashAll | FlashUntilForeground };
            FlashWindowEx(ref info);
        }

        return true;
    }

    /// <summary>The pairing notice of a call that stopped waiting, closed when it is still the one shown.</summary>
    internal void EndMcpPairing(string name)
    {
        if (NoticeOpen && Overlay.Shown?.Title == $"{name} asks to use Axit")
        {
            CloseNotice(null);
            Announce($"{name}'s request has ended: nothing allowed", true);
        }
    }

    private const uint FlashAll = 3;
    private const uint FlashUntilForeground = 12;

    [StructLayout(LayoutKind.Sequential)]
    private struct FlashInfo
    {
        public uint Size;
        public nint Window;
        public uint Flags;
        public uint Count;
        public uint Timeout;
    }

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FlashInfo info);
}
