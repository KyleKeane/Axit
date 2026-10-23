using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// The automation channel of a window (docs/axit/automation.md, tier 2): on only when the environment variable
/// <c>AXIT_AUTOMATION</c> is set, as the test runner sets it for the processes it starts on its own desktop. One named
/// pipe, <c>axit-auto-&lt;process id&gt;</c>, one command per line and one JSON answer per line: <c>keys</c>,
/// <c>type</c>, <c>focus</c>, <c>text</c>, <c>said</c>, <c>state</c>, <c>menu</c>, <c>close</c>. Keys are posted to the focused
/// control through the window's own message queue with the modifiers set in the thread's keyboard state, so that
/// <c>ProcessCmdKey</c> and the controls see them as typed; nothing reaches another program.
/// </summary>
internal sealed class AutomationChannel
{
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_CHAR = 0x0102;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private readonly AxitForm _form;

    /// <summary>
    /// The window's timeline, oldest first, taken by <c>said</c>: each announcement (<c>say</c>, with whether it
    /// interrupts), each sound (<c>sound</c>, by name), each move of the focus to another control (<c>focus</c>, its
    /// name, role and value) and each change of the focused control's value (<c>value</c>: the caret's line, the
    /// selected item), which is what NVDA reads on its own; each with the milliseconds since the channel opened.
    /// </summary>
    private readonly List<object> _said = [];
    private readonly object _gate = new();
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private readonly System.Windows.Forms.Timer _focusWatch = new() { Interval = 50 };
    private nint _focused;
    private string? _value;

    private AutomationChannel(AxitForm form)
    {
        _form = form;
        Announcer.Raised += (text, interrupt) => Record(new { kind = "say", text, interrupt });
        Sounds.Played += name => Record(new { kind = "sound", text = name, interrupt = false });
        _focusWatch.Tick += (_, _) => WatchFocus();
        _focusWatch.Start();
    }

    private void Record(object item)
    {
        lock (_gate)
        {
            _said.Add(new { t = _clock.ElapsedMilliseconds, item });
        }
    }

    /// <summary>The focus and its value, compared every 50 ms on the window's thread: a move or a change goes on the timeline.</summary>
    private void WatchFocus()
    {
        var handle = GetFocus();
        var control = Control.FromChildHandle(handle);
        var value = control is null ? null : ValueOf(control);
        if (handle != _focused)
        {
            _focused = handle;
            _value = value;
            Record(new { kind = "focus", text = $"{control?.AccessibleName} ({control?.AccessibilityObject.Role}): {value}", interrupt = false });
        }
        else if (value != _value)
        {
            _value = value;
            Record(new { kind = "value", text = value ?? string.Empty, interrupt = false });
        }
    }

    public static bool Requested => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AXIT_AUTOMATION"));

    /// <summary>Starts the channel for the window when the run asks for it; once per process.</summary>
    public static void StartIfRequested(AxitForm form)
    {
        if (!Requested || _started)
        {
            return;
        }

        _started = true;
        var channel = new AutomationChannel(form);
        var thread = new Thread(channel.Serve) { IsBackground = true, Name = "Axit automation" };
        thread.Start();
        Log.Info($"Automation channel open: axit-auto-{Environment.ProcessId}");
    }

    private static bool _started;

    private void Serve()
    {
        while (true)
        {
            try
            {
                using var pipe = new NamedPipeServerStream($"axit-auto-{Environment.ProcessId}", PipeDirection.InOut, 1);
                pipe.WaitForConnection();
                using var reader = new StreamReader(pipe, new UTF8Encoding(false));
                using var writer = new StreamWriter(pipe, new UTF8Encoding(false)) { AutoFlush = true };
                while (reader.ReadLine() is { } line)
                {
                    string answer;
                    try
                    {
                        answer = (string)_form.Invoke(() => Execute(line));
                    }
                    catch (Exception ex)
                    {
                        answer = JsonSerializer.Serialize(new { error = ex.GetType().Name + ": " + ex.Message });
                    }

                    writer.WriteLine(answer);
                }
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                if (_form.IsDisposed)
                {
                    return;
                }
            }
        }
    }

    private string Execute(string line)
    {
        var space = line.IndexOf(' ');
        var verb = space < 0 ? line : line[..space];
        var argument = space < 0 ? string.Empty : line[(space + 1)..];
        object result = verb switch
        {
            "keys" => Keys(argument),
            "type" => Type(argument),
            "focus" => Focus(),
            "text" => Text(),
            "list" => List(),
            "commands" => Commands(),
            "said" => Said(),
            "state" => _form.AutomationState(),
            "menu" => Menu(argument),
            "close" => Close(),
            _ => new { error = $"unknown command {verb}" },
        };
        return JsonSerializer.Serialize(result);
    }

    /// <summary>Key presses, separated by spaces: "ctrl+shift+p", "escape", "down down enter".</summary>
    private object Keys(string text)
    {
        foreach (var press in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var (key, modifiers) = ParseKey(press);
            var target = GetFocus();
            if (target == 0)
            {
                target = _form.Handle;
            }

            var alt = modifiers.HasFlag(System.Windows.Forms.Keys.Alt);
            SetModifiers(modifiers);
            PostMessage(target, alt ? WM_SYSKEYDOWN : WM_KEYDOWN, (nint)key, alt ? 1 << 29 : 0);
            PostMessage(target, alt ? WM_SYSKEYUP : WM_KEYUP, (nint)key, alt ? (1 << 29) | unchecked((int)0xC0000000) : unchecked((int)0xC0000001));
            // The modifiers are released once the posted keys have been handled: after them in the same queue.
            _form.BeginInvoke(() => SetModifiers(System.Windows.Forms.Keys.None));
            Application.DoEvents();
        }

        return new { ok = true };
    }

    /// <summary>Characters into the focused control, as typed.</summary>
    private object Type(string text)
    {
        var target = GetFocus();
        foreach (var c in text)
        {
            PostMessage(target, WM_CHAR, c, 1);
        }

        Application.DoEvents();
        return new { ok = true };
    }

    private object Focus()
    {
        var control = Control.FromChildHandle(GetFocus());
        if (control is null)
        {
            return new { name = (string?)null };
        }

        return new { name = control.AccessibleName, role = control.AccessibilityObject.Role.ToString(), value = ValueOf(control) };
    }

    /// <summary>What NVDA reads of a control besides its name: the selected item of a list, the caret's line of a text, the text of the rest.</summary>
    private static string? ValueOf(Control control) => control switch
    {
        ListBox { SelectedItem: { } item } => item.ToString(),
        ListBox => string.Empty,
        TextBoxBase box => CaretLine(box),
        _ => control.Text,
    };

    /// <summary>The focused list's items, or the list named, with the selected index: a question's answers, AxFile's entries, AxGit's views.</summary>
    private static object List()
    {
        if (Control.FromChildHandle(GetFocus()) is not ListBox list)
        {
            return new { error = "the focus is not in a list" };
        }

        return new { name = list.AccessibleName, items = list.Items.Cast<object>().Take(200).Select(item => item.ToString()).ToArray(), selected = list.SelectedIndex, count = list.Items.Count };
    }

    /// <summary>Every command of the window's menus as the command palette lists it, with its menu and key.</summary>
    private object Commands() => CommandPalette.Commands(_form.MainMenuStrip, _ => { }).Select(entry => entry.Text).ToArray();

    /// <summary>The focused text control's lines, the last 60, for a script that checks what a transcript or an editor holds.</summary>
    private static object Text()
    {
        if (Control.FromChildHandle(GetFocus()) is not TextBoxBase box)
        {
            return new { error = "the focus is not in a text control" };
        }

        var lines = box.Lines;
        return new { lines = lines.Skip(Math.Max(0, lines.Length - 60)).ToArray(), caretLine = box.GetLineFromCharIndex(box.SelectionStart) };
    }

    private static string CaretLine(TextBoxBase box)
    {
        var line = box.GetLineFromCharIndex(box.SelectionStart);
        var lines = box.Lines;
        return line >= 0 && line < lines.Length ? lines[line] : string.Empty;
    }

    private object Said()
    {
        lock (_gate)
        {
            var said = _said.ToArray();
            _said.Clear();
            return said;
        }
    }

    private object Menu(string name)
    {
        var entry = CommandPalette.Commands(_form.MainMenuStrip, _ => { })
            .FirstOrDefault(entry => entry.Text.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return new { error = $"no command {name}" };
        }

        entry.Run();
        return new { ok = true, command = entry.Text };
    }

    private object Close()
    {
        _form.BeginInvoke(_form.Close);
        return new { ok = true };
    }

    private static (Keys Key, Keys Modifiers) ParseKey(string press)
    {
        var modifiers = System.Windows.Forms.Keys.None;
        var parts = press.Split('+');
        foreach (var part in parts[..^1])
        {
            modifiers |= part.ToLowerInvariant() switch
            {
                "ctrl" => System.Windows.Forms.Keys.Control,
                "shift" => System.Windows.Forms.Keys.Shift,
                "alt" => System.Windows.Forms.Keys.Alt,
                _ => throw new ArgumentException($"unknown modifier {part}"),
            };
        }

        var name = parts[^1];
        var key = name.Length == 1 && char.IsLetterOrDigit(name[0])
            ? (Keys)char.ToUpperInvariant(name[0])
            : name.ToLowerInvariant() switch
            {
                "enter" => System.Windows.Forms.Keys.Enter,
                "escape" or "esc" => System.Windows.Forms.Keys.Escape,
                "tab" => System.Windows.Forms.Keys.Tab,
                "space" => System.Windows.Forms.Keys.Space,
                "backspace" => System.Windows.Forms.Keys.Back,
                "delete" => System.Windows.Forms.Keys.Delete,
                "up" => System.Windows.Forms.Keys.Up,
                "down" => System.Windows.Forms.Keys.Down,
                "left" => System.Windows.Forms.Keys.Left,
                "right" => System.Windows.Forms.Keys.Right,
                "home" => System.Windows.Forms.Keys.Home,
                "end" => System.Windows.Forms.Keys.End,
                "pageup" => System.Windows.Forms.Keys.PageUp,
                "pagedown" => System.Windows.Forms.Keys.PageDown,
                _ when name.StartsWith('f') && int.TryParse(name[1..], out var f) && f is >= 1 and <= 12 => System.Windows.Forms.Keys.F1 + (f - 1),
                _ => throw new ArgumentException($"unknown key {name}"),
            };
        return (key, modifiers);
    }

    /// <summary>The thread's keyboard state with exactly these modifiers down, which is what WinForms reads as ModifierKeys.</summary>
    private static void SetModifiers(Keys modifiers)
    {
        var state = new byte[256];
        GetKeyboardState(state);
        Set(state, 0x11, modifiers.HasFlag(System.Windows.Forms.Keys.Control));
        Set(state, 0x10, modifiers.HasFlag(System.Windows.Forms.Keys.Shift));
        Set(state, 0x12, modifiers.HasFlag(System.Windows.Forms.Keys.Alt));
        SetKeyboardState(state);

        static void Set(byte[] state, int key, bool down) => state[key] = (byte)(down ? 0x80 : 0);
    }

    [DllImport("user32.dll")]
    private static extern nint GetFocus();

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, int msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] state);

    [DllImport("user32.dll")]
    private static extern bool SetKeyboardState(byte[] state);
}
