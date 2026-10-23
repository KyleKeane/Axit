using AxStart.Core;
using Axit.Core;
using Axit.Forms;

namespace AxStart;

/// <summary>
/// axStart's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AS-1.1, AS-1.2): <c>Axit.exe start
/// [--hidden]</c> calls <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/>. One axStart runs at a
/// time, held by a named mutex: a start while it runs hands "show" to its hidden window (<see cref="AppMessage"/>)
/// and ends, or with --hidden just ends; otherwise <see cref="StartForm"/> runs, hidden with --hidden.
/// </summary>
public static class AxStartApp
{
    public const string Name = "axStart";

    public const string Usage = """
        Usage: Axit start [--hidden]
               axstart              the same, as the installed console command

          (nothing)         axStart's search field, in front; axStart then keeps running and its hotkey brings the field back
          --hidden          axStart running with its window hidden until the hotkey, as at sign-in
          --help, --version this text, or the version
        """;

    /// <summary>Held for the life of the process, so that a second start finds the first (AS-1.2).</summary>
    private static Mutex? _one;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axstart", Usage, args, arguments =>
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

            _one = new Mutex(initiallyOwned: true, @"Local\Axit.AxStart", out var first);
            if (!first)
            {
                // AS-1.2: one axStart; the running one shows its field, unless this start only wanted it running.
                _one.Dispose();
                _one = null;
                if (!hidden && StartForm.FindRunning() is var running and not 0)
                {
                    AppMessage.SendTo(running, Name, AppMessage.Show, []);
                }

                return null;
            }

            var settings = StartSettings.Load(StartSettings.DefaultPath, out var settingsError);
            return new StartForm(hidden, settings, settingsError);
        });
}
