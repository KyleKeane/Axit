# axStart, the window

The WinForms side of axStart, the program launcher: a background process from sign-in with one hidden window that a global hotkey shows, a search field with the caret always in it, the list of matches under it that never takes the focus, a status bar, the bundle's notices. The pure code (the entries, the matching, the hotkey as text, the settings) is `src/AxStart.Core`, with its own `CLAUDE.md`. The spec is `docs/axstart/SPEC.md` ("SPEC.md" below); the guide `docs/axstart/user-guide.md` is embedded by `AxStart.csproj` for Help → User guide; the manual test plan is `docs/axstart/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axstart.ps1                 # build Axit and start axStart with its field shown (run.ps1 -App start); quit an installed axStart first
dotnet build Axit.sln
.\test.ps1 axStart
```

## Where things are

- `AxStartApp`: the entry point `Run` that Axit's `Program` calls (bundle AX-1.7): `--hidden` read off the command line; the mutex `Local\Axit.AxStart` held for the life of the process (SPEC.md AS-1.2); a second start finds the first's hidden window (`StartForm.FindRunning`: `FindWindowExW` by the title "axStart", the owner process of the same program name) and hands it `AppMessage.Show` through `AppMessage.SendTo`, or with `--hidden` just ends; otherwise one call into `Axit.Forms/AppHost.Run` with the settings and the window.
- `StartForm` (on `Axit.Forms/HotkeyForm` since axClip, 2026-10-23, which holds the hidden window, the hotkey, the showing and hiding, the rule of the foreground and the Hotkey notice described below; `StartForm` fills in the hotkey setting, the bounds and the field on showing): the controls (the field, the list with `TabStop` off and its `GotFocus` sending the focus back to the field, AS-D2; the status bar), the menus (AS-5.2), the hooks the window base asks for, `SetVisibleCore` (the first showing that `Application.Run` asks for, before the handle exists, makes the handle and shows nothing; `OnHandleCreated` then shows the window through `ShowLauncher` unless it started with `--hidden`), `OnHandleCreated` (the Run value checked, the hotkey registered, the notice of AS-4.3 when it is taken, the programs read), `OnFormClosing` (Alt+F4 and Close all windows hide; Quit and Windows' end close), `WndProc` (`WM_HOTKEY` shows or hides, or in Hotkey… captures the key in use), `TakeHandOver` (the hand-over's `show`, answered by the base), `ProcessWindowKey` (the one key switch, from `BundleKeys` and `AxStartKeys`), `ShowLauncher` (the field emptied, the window centred on the screen of the foreground window, `Show`, `AxitMenu.Activate`, a fresh read of the programs), `HideLauncher` and `GiveForegroundBack` (the one rule of the foreground, AS-3.4), `HideIfAnotherProgramIsInFront` (on `Deactivate`, posted: hidden only when the foreground window is another process's and not NVDA's, so that the font picker and NVDA's menu do not hide it), the matches (`OnTyped`, `Fill`, `SayFirst` after the 300 ms `_speak` timer, `SelectMatch` with the edge earcon, `RefreshAsync`), the commands (`StartSelected`, hidden first; `OpenFileLocation` through `AxitMenu.Start` with axFile's verb; `SayInformation`), the hotkey (`Register`, `ShowHotkeyNotice` with the field's `KeyHook` `OnHotkeyField`, `CaptureKey`, `ApplyHotkey`); the base's `Do` runs the reads.
- `AppsFolder` (AS-2.1, AS-2.3, AS-D1): the shell's Applications folder read on an STA thread of its own (`SHCreateItemFromParsingName("shell:AppsFolder")`, `BindToHandler` with `BHID_EnumItems`, `IEnumShellItems`, each item's normal display name and parent-relative parsing name, `SHGetKnownFolderPath` for a known folder's id), and `Start` through `Process.Start` with the shell on `shell:AppsFolder\<parsing name>`, or the program file itself with `runas`; ERROR_CANCELLED (the user's No at Windows' prompt) is no error.
- `SignInStart` (AS-1.3, AS-D6) is `Axit.Core`'s since axClip (2026-10-23): the value "Axit axStart" under the user's Run key, naming the installed program only.
- `AxStartKeys`: the window's keys (AS-5.3; bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys. Page Up and Page Down are the bundle's keys, handled in the switch.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. The caret stays in the field (AS-D2): no key ever moves the focus to the list; every move through the matches is one announcement on the field, "Notepad++, 2 of 3"; the edge earcon where nothing moves.
2. Nothing is said while typing except the first match after the 300 ms pause (a setting), which is the answer to the typing; nothing is said when the window hides.
3. The programs are read on a thread of their own; the window searches the list it has meanwhile.
4. The window hides; only Quit axStart and Windows' end close it, since the hotkey lives on its handle.
5. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas already learned

- The hidden window is not in the Axit menu's window list (`AxitMenu.OpenWindows` takes visible windows only), which is why the hand-over needs `AppMessage.SendTo` and `FindRunning`.
- Hiding a window does not give the focus back by itself: Windows activated one of this process's own hidden windows, NVDA went on showing the program from before, and typing went nowhere (the user, 2026-10-22). `ShowLauncher` keeps the foreground window in `_giveBackTo` when it is another program's, and `GiveForegroundBack`, the one rule, calls `SetForegroundWindow` on it whenever this process holds the front with the window hidden, allowed because the process is in front at that moment: as the window hides, on an activation while hidden, and three seconds after a start, which leaves the front to the program started.
- Starting hidden activated the hidden window: right after `Axit.exe start --hidden` the foreground window was axStart's, invisible, and the user's typing went nowhere (found with an outside monitor of the foreground and focus, 2026-10-22). `WndProc` answers a `WM_ACTIVATE` while the window is not visible (`IsWindowVisible`, since `Visible` changes before `ShowWindow` returns) with the same rule (`GiveForegroundBack`, posted). The log says "Activated while hidden; the front given back".
- `Move` is a `Control` member (an event's raiser); the method that moves the selection is `SelectMatch`.
- The installer tells axStart's process from the other apps' by its command line (`Axit.exe start`), through `Win32_Process`, and stops it before it changes anything (AS-1.5).
