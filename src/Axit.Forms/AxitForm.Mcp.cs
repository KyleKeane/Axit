using System.Runtime.InteropServices;
using Axit.Core;
using Axit.Core.Mcp;
using static Axit.Forms.User32;

namespace Axit.Forms;

/// <summary>
/// The window's part of the AI tools' access (docs/axit/mcp.md MC-6): its pipe started as it first shows, the Axit
/// menu's AI tools submenu, what the pipe reads of the window, and the pairing notice. The pipe itself is
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

    /// <summary>
    /// Axit → AI tools ▸ (MC-6.1): the bundle's own setting, in the bundle's menu, never an app's Options. The check
    /// read as the submenu opens, since another window may have changed it.
    /// </summary>
    internal ToolStripMenuItem CreateMcpMenu()
    {
        var menu = new ToolStripMenuItem("A&I tools");
        var access = new ToolStripMenuItem("&Let AI tools read and open Axit", null, (_, _) => ToggleMcp());
        menu.DropDownItems.Add(access);
        menu.DropDownItems.Add(new ToolStripMenuItem("&Add Axit to Claude Code", null, (_, _) => AddToClaudeCode()));
        menu.DropDownItems.Add(new ToolStripMenuItem("&Forget AI tools", null, (_, _) => ForgetMcpClients()));
        menu.DropDownItems.Add(new ToolStripMenuItem("&Read the log", null, (_, _) => ReadMcpLog()));
        CommandPalette.RefreshedOnOpening(menu, () => access.Checked = McpAccess.Enabled);
        return menu;
    }

    /// <summary>On: asked once, with what it allows (MC-6.1); off: at once, said.</summary>
    private void ToggleMcp()
    {
        if (McpAccess.Enabled)
        {
            if (SetMcp(false))
            {
                Announce("AI tools off: no AI tool can read or open Axit", true);
            }

            return;
        }

        var claude = McpClaudeCode.Find();
        var text = "AI tools such as Claude Code may then ask to read what the Axit windows show (their text, lists and status) "
            + "and to open apps. They cannot change or delete anything, and they never read axPass or axClip. Each tool asks you "
            + "once, in an Axit window, before it reads anything, and every call is written in the log (AI tools, Read the log).\n\n"
            + (claude is null
                ? "Claude Code was not found here, so Turn on only; then add Axit to your AI tool with this command line:\n\n" + McpClaudeCode.AddCommandLine(Environment.ProcessPath!)
                : "Turn on and add to Claude Code also adds Axit to Claude Code for every folder; a new Claude session can then use it.");
        List<OverlayChoice> choices = claude is null
            ? [new OverlayChoice("&Turn on", () => TurnMcpOn(addToClaude: false), IsDefault: true)]
            : [new OverlayChoice("&Turn on and add to Claude Code", () => TurnMcpOn(addToClaude: true), IsDefault: true), new OverlayChoice("Turn on &only", () => TurnMcpOn(addToClaude: false))];
        choices.Add(new OverlayChoice("&Cancel", IsCancel: true));
        ShowNotice(new Notice("Turn on AI tools?", text, choices));
    }

    private void TurnMcpOn(bool addToClaude)
    {
        if (!SetMcp(true))
        {
            return;
        }

        Announce("AI tools on", true);
        if (addToClaude)
        {
            AddToClaudeCode();
        }
    }

    /// <summary>The setting written; false after the notice that says why not.</summary>
    private bool SetMcp(bool on)
    {
        try
        {
            McpAccess.SetEnabled(on);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail("The AI tools' setting was not changed: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// MC-6.8: Claude Code's own <c>claude mcp add</c> for every folder, with this very Axit.exe, an earlier entry removed
    /// first; off the window's thread, the result said. Never in a test run, whose windows must not touch the user's
    /// Claude Code settings.
    /// </summary>
    private void AddToClaudeCode()
    {
        if (AutomationEnvironment.Requested)
        {
            Announce("Not in a test run: Claude Code's settings are the user's", true);
            return;
        }

        if (McpClaudeCode.Find() is not { } claude)
        {
            ShowNotice(Notice.Plain("Claude Code was not found",
                "Claude Code is not installed here, or not where Axit looks (the PATH, and your .local\\bin folder). Add Axit to your AI tool with this command line:\n\n"
                + McpClaudeCode.AddCommandLine(Environment.ProcessPath!)));
            return;
        }

        var exe = Environment.ProcessPath!;
        Announce("Adding Axit to Claude Code", true);
        Do(async () =>
        {
            await Task.Run(() => RunClaude(claude, McpClaudeCode.RemoveArguments()));
            var (code, output) = await Task.Run(() => RunClaude(claude, McpClaudeCode.AddArguments(exe)));
            if (IsDisposed)
            {
                return;
            }

            Log.Info($"claude mcp add: exit {code}");
            if (code == 0)
            {
                Announce("Axit is added to Claude Code: a new Claude session can use it", true);
                return;
            }

            ShowNotice(Notice.Plain("Axit was not added to Claude Code",
                $"Claude Code said: {output}\n\nThe command line, to run by hand:\n\n{McpClaudeCode.AddCommandLine(exe)}"));
        });
    }

    /// <summary>Claude Code run without a window, its arguments as a list (cmd for npm's claude.cmd), at most 30 seconds; its exit code and what it printed.</summary>
    private static (int Code, string Output) RunClaude(string claude, IReadOnlyList<string> arguments)
    {
        var script = claude.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase);
        var start = new System.Diagnostics.ProcessStartInfo(script ? SystemPrograms.Cmd : claude)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (script)
        {
            // S-53 (security review, 2026-10-25): cmd reads its special characters even inside quotes, so a path or an
            // argument holding one (an Axit.exe under C:\R&D) is refused rather than run; every argument is quoted.
            if (SystemPrograms.HasCmdSpecials(claude) || arguments.Any(SystemPrograms.HasCmdSpecials))
            {
                return (-1, "a path holds one of cmd's special characters (& | < > ^ % ! and the like), so claude.cmd is not run with it; add Axit by hand with the command line shown");
            }

            start.Arguments = "/d /s /c \"\"" + claude + "\" " + string.Join(' ', arguments.Select(a => $"\"{a}\"")) + "\"";
        }
        else
        {
            foreach (var argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }
        }

        try
        {
            using var process = System.Diagnostics.Process.Start(start);
            if (process is null)
            {
                return (-1, "it did not start");
            }

            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30_000))
            {
                process.Kill(entireProcessTree: true);
                return (-1, "it did not finish within 30 seconds");
            }

            var reads = Task.WhenAll(output, error);
            var text = reads.Wait(5000) ? (output.Result + "\n" + error.Result).Trim() : string.Empty;
            return (process.ExitCode, text.ReplaceLineEndings(" ").Trim());
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return (-1, ex.Message);
        }
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

    /// <summary>The audit log (MC-6.6) in axPreview; said when there is none yet.</summary>
    private void ReadMcpLog()
    {
        if (!File.Exists(AppPaths.McpAuditFile))
        {
            Announce("No AI tool has called Axit yet: the log is empty", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxPreview, AppPaths.McpAuditFile);
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
