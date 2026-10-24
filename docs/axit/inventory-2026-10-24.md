# Inventory of redundancy and drift, 2026-10-24

Asked by the user while axCalc and axClip were merged: "a review of any redundant code so that we can do an inventory
and clean up of the codebase to ensure there has not been too much drift during earlier work … we need to be
aggressive to ensure it is staying manageable." This is that inventory: how it was taken, what was cleaned up the same
day on the branch `calc-clip`, and what is still open, ranked. It follows `review-2026-10-23.md`, which took the
architecture; this one takes the copies, the dead code and the notes.

## 1. How it was taken

- Three read-only reviews, each over a slice: the shared projects (`Axit.Core`, `Axit.Forms`); the older apps
  (axClaude, axFile, axGIT, axTerminal, axMedia, axPreview, axDown, axJSON); the newer apps (axStart, axProcess,
  axTask, axPass), the tests and the two branches. Each counted every member's callers across `src/` and `tests/`,
  grouped every `DllImport` by name, and checked every name in the `CLAUDE.md` notes against the code.
- Then the findings were checked against the code before anything was changed. Two did not hold:
  `EditHistory.CanUndo` and `CanRedo` are used by its tests (kept), and the console windows' folder handling differs
  more than it looked (axTerminal works on the shell's current directory, axClaude on its project folder), so only
  axClaude's three "Show in" methods became one.
- The repository at the start: about 40,000 lines of C# under `src/` and 10,000 under `tests/`, fourteen apps, the
  shared projects 14,000 of the 40,000.

## 2. Cleaned up on 2026-10-24 (branch `calc-clip`, one piece per commit)

1. **Notes against the code.** Names that no longer existed (axFile's type-ahead and rename members, `RenameRow`,
   `IsWaitRow`, `Palette.Name`, `WindowSettings` for `TerminalSettings`), the rename box's gotcha moved to `AxitList`,
   `Dispatch.App`'s two summaries made one, `plan.md`'s lines for axTask, axProcess, axCalc and axClip, and
   `src/Axit.Core/CLAUDE.md` rewritten from one paragraph of 10,500 characters into one bullet per piece.
2. **Dead code.** `TaskRules.Ordered` (All tasks takes its order from its smart list), `IsFeedbackCardTitle`
   (test-only), four virtual members nothing overrides, three copies of the limit of ten recent paths
   (`RecentPaths.Max` is the one).
3. **`AxitForm.CopyText`**: one clipboard write, in place of seven copies in axFile, axPass, axProcess, axCalc,
   axClip, axClaude and Copy diagnostics. A held clipboard is the window's `Fail` everywhere.
4. **`Dispatch.EditorOf`**: the one rule of which editor a file opens in, in place of four copies.
5. **`Axit.Forms/MarkdownKeys`**: axDown's navigation keys, which axPreview held as a copy.
6. **`Axit.Forms/User32`**: six Win32 calls and five message constants declared two to four times, from nine files.
7. **`Axit.Forms/ListViewState`**: axGIT's views of its one list, which axProcess held as a copy.
8. **The F1 texts**: axTask's and axPass's Axit menu line had drifted; `KeyTests` now checks the whole line.
9. **Pure moves**: axGIT's window in four partial files (it was 1,100 lines in one), axMedia's player, the editor
   shell's file half (`TextEditorForm.Files.cs`), axFile's completion and its programs out of two long partials.
10. **`HotkeyForm` holds the search field over the matches**, which axStart and axClip had line for line: the field,
    the list that never takes the focus, the first-match timer, the moves with the edge earcon and the six match keys.
11. **App names in the menus by the naming rule**: the "Show current folder in" submenus still said AxGit and
    AxTerminal; `MenuTests` now fails on the old spelling.

Without the pure moves, the C# changed by 535 lines added and 709 removed, the added lines being mostly the new
shared pieces' comments and two new tests. The merges themselves found two more: the shared `StartupEntry` clashed
with axProcess's record of the same name (the shared one is `SignInStart` now), and axCalc's history was a plain list
box (it is the bundle's `AxitList`).

Every window touched is owed a look under NVDA: axStart, axClip, axCalc (new), axGIT, axProcess, axFile, axMedia, the
editors, axPass and axTask (the copy key), axClaude (the copy notices, Show in). Nothing a key does was meant to
change; the one spoken change is axStart saying "Reading the programs" when nothing matches during the first read.

## 3. Open, ranked by payoff against risk

1. **Adding an app touches seventeen hand-kept lists** (the issue forms, the root `CLAUDE.md`, both READMEs, the
   bundle spec in six places, `install.ps1`, `run.ps1` in four places, `Dispatch`, `Program.cs`, `Axit.csproj`, the
   test project, `KeyTests` twice, `make-icon.ps1`). `InventoryTests` catches a missing one, but each merge of two
   app branches conflicts in all of them (both merges of this day did). Proposal: let `run.ps1` and `install.ps1`
   read the apps from one data file the build also reads, and let the spec's lists point to `Dispatch.Apps` rather
   than repeat it. Needs a decision: it moves facts out of the documents.
2. **Finding a program on the PATH, written four times** (`ShellLauncher.FindOnPath`, `KeePassCli.Find`,
   `ClaudeLauncher`, and `ProgramInstall.OnPath` on the unmerged branch `installs`), with
   `ClaudeLauncher.InstallCommand` repeating `ProgramInstall.ClaudeCode`. Waits for `installs` to merge, then one
   `OnPath` for all.
3. **Running a child process with a timeout, written three times** (`GitRunner.RunAsync`, `ClaudeLauncher.VersionAsync`,
   `KeePassCli.Run`): start, read both streams, time out, end the process tree. One `Axit.Core/ProcessRun`; medium
   risk, since git's case of a pipe left open by a child must be kept.
4. **The item apps' window does less than it could**: axTask and axPass each keep the same undo stack of twenty, the
   same three-menu enabling (`MenuLetter`, `UpdateRowMenus`), and two rename hooks where one would do. Into
   `ItemForm`; after `installs` merges, which changes `PassForm`.
5. **The notes of the apps are long single paragraphs** (axFile's "Where things are" bullet is about 6,000
   characters). The same rewrite as `Axit.Core`'s, one bullet per file or section; no content change.
6. **Recently closed is remembered by four windows' own closing code**; one `PathInHand` on the window base would do
   it once.
7. **A list refilled in place, written twice** (axMedia's `FillList`, axFile's `FillPane`): `AxitList.SetLines`.
8. **The F1 texts' bundle section is typed by hand in fourteen files.** Each app's lines differ on purpose (what Find
   searches, what Tab moves between), so a generator would take parameters; for now the tests pin the lines that must
   be the same.
9. **Two size formats disagree**: axProcess says "350 MB", axFile "1.2 MB". One `Axit.Core` format needs the user's
   choice of which.
10. **axGIT's Find always searches the details**, where every other list window finds in the list when the list has
    the focus. A behaviour change; its spec first.
11. **Small merges with little payoff**: `EditKeys`, `EditPaging` and `EditParagraphs` (three static classes for the
    edit control's keys); `TextJump` and `TextJumps`.
12. **Tests**: the fixture-folder finder is written three times, the temporary-folder pattern about thirty times, and
    a few settings tests never delete their folders. One shared `TestFiles.cs` through `tests/Directory.Build.props`.

## 4. Branches and leftovers

- `installs` (Install it now for the programs an app needs) is unmerged; items 2 and 4 wait for it.
- `axlist` stays as the record of the first design of axTask and is never merged.
- The main checkout holds empty `src/AxList`, `src/AxList.Core` and `tests/AxList.Tests` folders (build output left
  by switching branches); deleting them changes nothing tracked.

## 5. Keeping it from drifting again

- A shared piece is moved, not copied, on its second use (M3). Several copies of this day were second uses that had
  been copied with a comment saying "as axGIT's".
- Notes name what is in the code; when a member moves to the shared projects, the app's notes say where it went in the
  same commit.
- The checks that read the sources (`ListTests`, `MenuTests`, `KeyTests`, `InventoryTests`, `LayoutTests`) are where a
  rule becomes cheap to keep; a rule found drifted gets one, as the Axit menu line and the app spelling did today.
