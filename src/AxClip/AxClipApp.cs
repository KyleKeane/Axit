using AxClip.Core;
using Axit.Forms;

namespace AxClip;

/// <summary>
/// axClip's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md CP-1.1): <c>Axit.exe clip [--hidden]</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/>. One axClip runs at a time, held by a named
/// mutex: a start while it runs hands "show" to its hidden window (<see cref="AppMessage"/>) and ends, or with
/// --hidden just ends; otherwise <see cref="ClipForm"/> runs, hidden with --hidden. As axStart's (AS-1.2).
/// </summary>
public static class AxClipApp
{
    public const string Name = "axClip";

    public const string Usage = """
        Usage: Axit clip [--hidden]
               axclip               the same, as the installed console command

          (nothing)         axClip's search field, in front; axClip then keeps running and its hotkey brings it back
          --hidden          axClip running with its window hidden until the hotkey, as at sign-in
          --help, --version this text, or the version
        """;

    /// <summary>Held for the life of the process, so that a second start finds the first (CP-1.1).</summary>
    private static Mutex? _one;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axclip", Usage, args, arguments =>
        {
            var hidden = false;
            foreach (var argument in arguments)
            {
                if (argument == "--hidden")
                {
                    hidden = true;
                }
                else
                {
                    AppHost.BadCommandLine(Name, $"Unknown argument {argument}.", Usage);
                    return null;
                }
            }

            _one = new Mutex(initiallyOwned: true, @"Local\Axit.AxClip", out var first);
            if (!first)
            {
                _one.Dispose();
                _one = null;
                if (!hidden && HotkeyForm.FindRunning(Name) is var running and not 0)
                {
                    AppMessage.SendTo(running, Name, AppMessage.Show, []);
                }

                return null;
            }

            var settings = ClipSettings.Load(ClipSettings.DefaultPath, out var settingsError);
            return new ClipForm(hidden, settings, settingsError);
        });
}
