using System.Text;

namespace Axit.Core.Vt;

/// <summary>
/// Splits a stream of characters into printable runs, C0 controls, ESC sequences, CSI sequences and OSC strings.
/// The parser keeps its state between calls, so a sequence may be split across chunks.
/// </summary>
public sealed class VtParser(Screen screen)
{
    private enum State { Ground, Escape, Csi, Osc, OscEscape, String, StringEscape }

    /// <summary>
    /// The longest an OSC string, a DCS-like string, a CSI's parameters or an escape's intermediates may run (security
    /// audit S-40): past it the sequence is dropped and what follows is text again, so that an unterminated sequence in
    /// a program's output neither grows without end nor hides everything printed after it.
    /// </summary>
    public const int MaxString = 4096;

    public const int MaxParameters = 64;

    public const int MaxIntermediates = 16;

    private State _state = State.Ground;
    private int _stringLength;
    private readonly StringBuilder _print = new();
    private readonly StringBuilder _parameters = new();
    private readonly StringBuilder _intermediates = new();
    private readonly StringBuilder _osc = new();

    public void Feed(ReadOnlySpan<char> chars)
    {
        foreach (var c in chars)
        {
            Step(c);
        }

        FlushPrint();
    }

    private void Step(char c)
    {
        switch (_state)
        {
            case State.Ground:
                if (c == '\x1b')
                {
                    FlushPrint();
                    _intermediates.Clear();
                    _state = State.Escape;
                }
                else if (c < ' ')
                {
                    FlushPrint();
                    screen.Control(c);
                }
                else if (c != '\x7f')
                {
                    _print.Append(c);
                }

                break;

            case State.Escape:
                if (c == '[')
                {
                    _parameters.Clear();
                    _intermediates.Clear();
                    _state = State.Csi;
                }
                else if (c == ']')
                {
                    _osc.Clear();
                    _state = State.Osc;
                }
                else if (c is 'P' or 'X' or '^' or '_')
                {
                    _stringLength = 0;
                    _state = State.String;
                }
                else if (c >= ' ' && c <= '/')
                {
                    if (_intermediates.Length >= MaxIntermediates)
                    {
                        _state = State.Ground;
                        break;
                    }

                    _intermediates.Append(c);
                }
                else if (c >= '0' && c <= '~')
                {
                    var intermediates = _intermediates.ToString();
                    _intermediates.Clear();
                    _state = State.Ground;
                    screen.Escape(c, intermediates);
                }
                else if (c == '\x1b')
                {
                    _intermediates.Clear();
                }
                else if (c < ' ')
                {
                    screen.Control(c);
                }
                else
                {
                    _state = State.Ground;
                }

                break;

            case State.Csi:
                if ((c >= '0' && c <= '?' && _parameters.Length >= MaxParameters) || (c >= ' ' && c <= '/' && _intermediates.Length >= MaxIntermediates))
                {
                    // S-40: a sequence too long to be a real one is dropped.
                    _state = State.Ground;
                }
                else if (c >= '0' && c <= '?')
                {
                    _parameters.Append(c);
                }
                else if (c >= ' ' && c <= '/')
                {
                    _intermediates.Append(c);
                }
                else if (c >= '@' && c <= '~')
                {
                    _state = State.Ground;
                    screen.Csi(_parameters.ToString(), _intermediates.ToString(), c);
                }
                else if (c == '\x1b')
                {
                    _intermediates.Clear();
                    _state = State.Escape;
                }
                else if (c is '\x18' or '\x1a')
                {
                    _state = State.Ground;
                }
                else if (c < ' ')
                {
                    screen.Control(c);
                }
                else if (c != '\x7f')
                {
                    _state = State.Ground;
                }

                break;

            case State.Osc:
                if (c == '\a')
                {
                    _state = State.Ground;
                    screen.Osc(_osc.ToString());
                }
                else if (c == '\x1b')
                {
                    _state = State.OscEscape;
                }
                else if (c >= ' ')
                {
                    if (_osc.Length >= MaxString)
                    {
                        // S-40: an unterminated string is dropped; what follows is text again.
                        _osc.Clear();
                        _state = State.Ground;
                        break;
                    }

                    _osc.Append(c);
                }

                break;

            case State.OscEscape:
                if (c == '\\')
                {
                    _state = State.Ground;
                    screen.Osc(_osc.ToString());
                }
                else
                {
                    // Not a string terminator: the OSC is abandoned and ESC starts a new sequence.
                    _intermediates.Clear();
                    _state = State.Escape;
                    Step(c);
                }

                break;

            case State.String:
                if (c == '\x1b')
                {
                    _state = State.StringEscape;
                }
                else if (c == '\a' || ++_stringLength > MaxString)
                {
                    _state = State.Ground;
                }

                break;

            case State.StringEscape:
                if (c == '\\')
                {
                    _state = State.Ground;
                }
                else if (c != '\x1b')
                {
                    _state = State.String;
                }

                break;
        }
    }

    private void FlushPrint()
    {
        if (_print.Length == 0)
        {
            return;
        }

        var text = _print.ToString();
        _print.Clear();
        screen.Print(text);
    }
}
