# The cleanup run (specified 2026-10-25, to be run later)

A long, unattended pass over the whole repository to make it lean: remove unused code and leftovers, fold copies into
the one shared piece, replace hacks with plain code, and tighten what an attacker could reach. **Nothing the user can
do today stops working, and nothing they hear changes.** Written at the user's ask for a later day: "simplify and
consolidate the entire project, delete unused code, reduce complexity for maintenance, do not reduce any
functionality, get rid of remnants of unused code or hacks, tighten everything to reduce any potential attack
surface."

## 0. How to start it

In Claude Code in the repository, on a day with time to spare:

```
ultracode Run docs/axit/cleanup-run.md, from section 1 to the end, then report.
```

The run is a workflow of several agents (`ultracode`); it may take hours and many tokens. It stops at the end of
section 6 with a report, never merges to `main`, never releases, and never touches the user's GitHub settings.

## 1. Ground rules for every agent of the run

1. **Branch.** Everything happens on the branch `cleanup`, cut from an up-to-date `main` (`git fetch origin`, then
   `git switch -c cleanup origin/main`), in a worktree under the session's scratchpad, never in the user's checkout
   (Axit runs from it; a build there writes into the running program's `bin`).
2. **No change of behaviour.** No key, menu item, announcement, notice text, file format, setting name, command-line
   option or default changes. The F1 texts, guides and specs stay true word for word; a spec changes only where it
   names code that moved (a class name, a file). When a cleanup would change what a user hears or does, it is **not
   made**: it goes in the report's "needs the user" list instead.
3. **One step per commit**, imperative subject, the reason in the body, `Co-Authored-By` as usual. Every commit builds
   in Release and passes `.\test.ps1` (the privacy test fails in a worktree on `.git` only: read its `Collection`
   line every run, a real hit can hide behind it).
4. **Shared code moves, never copies** (bundle rule 4, M3): on a second use the piece goes to `Axit.Core` (no window)
   or `Axit.Forms` (window), unchanged apart from its namespace, in a commit of its own, and every note that named it
   (`CLAUDE.md` files, specs) says where it went in the same commit.
5. **Plain code** (SPEC B-10): static classes and records, no dependency injection, no interfaces with one
   implementation, no reflection beyond WinForms', no new package of any kind. A smaller model must still be able to
   change one app with only that app's folders, spec and notes in context.
6. **Personal data**: none in code, tests, commits or output (rule 6). Test strings that look like an e-mail address
   or an account path are split (`"C:\\" + "Users..."`), as the existing tests do. Recordings only through
   PtyCapture's `--dump`.
7. **Never** delete a branch, a tag, a release or a file outside the repository; never force-push; never run
   `release.ps1`; never change `.github` permissions to be wider; never weaken a check of `SECURITY.md`'s promises.

## 2. The checks before and after (the gate)

Run at the start (the baseline, recorded in the report) and before the report, on the scratch build:

- `.\test.ps1` (every test project, Release).
- `dotnet build src\Axit\Axit.csproj -c Release -o <scratch>` with no warning (Release treats analyser findings as
  errors; 3.3.0 was lost to one).
- Every automated script: `tools\AxAutomate` on `tests\scripts` (all apps, `mcp` included; axClaude's with
  `--model haiku`), from a scratch build of `tools/AxAutomate`, as `docs/axit/automation.md` says. Known: none
  failing as of 2026-10-25 (axClaude's `09-options` was fixed that day).
- `.\publish.ps1 -NoInstall` in the worktree, and the zip holds `Axit.exe`, `install.ps1`, `install.cmd`, `apps.json`,
  the icons, `README.md` and `LICENSE`.
- Every PowerShell script parses (`[System.Management.Automation.Language.Parser]::ParseFile`; a `"$name:"` inside a
  string broke `sign-release.ps1` once).

A step whose gate fails is reverted, not patched around, and goes in the report.

## 3. What to look for (the work list)

Each agent takes one area, finds candidates, **proves** each before changing it, and keeps a list of what it left
and why. "Proves" means: no reference in the solution (`grep` across `src`, `tests`, `tools`, scripts and docs), no
use by reflection, the WinForms designer, a test, a script or `install.ps1`, and the gate passing without it.

### 3.1 Unused code
- Members, types, parameters, constants and files nothing calls; branches of a `switch` no value reaches; settings
  properties no code reads (keep any that a settings file on disk may hold, or the file loses them on the next
  save: read `SettingsFile` first); `using` lines left over.
- Empty or leftover folders (`src/AxList`, `src/AxList.Core`, `tests/AxList.Tests` in old checkouts: untracked build
  output, not in git).
- Comments that describe code no longer there, and "until 2026-10-2x" history notes that only repeat the changelog;
  a note that explains **why** something is so stays.

### 3.2 Copies to fold

The inventory's list (`inventory-2026-10-24.md` section 3: the process runner, the item apps' menus, the list
refill, the edit keys, the jumps, the tests' helpers and the notes) was done on 2026-10-25 before this run. What is
left is what a fresh search finds:
- Any further second copy the agents find (search for the same method body in two projects, and for comments
  saying "as axGIT's" or "like axFile's").

### 3.3 Hacks to make plain
- Timers used to wait for something that has an event; `BeginInvoke` chains that exist only to outwait another
  `BeginInvoke`; flags set and cleared around a call to stop re-entrance where a guard method would do. Each one is
  changed only when the reason for it is understood from the code's own comment or the notes' gotchas. **A gotcha in a
  `CLAUDE.md` is a measured fact about NVDA or Windows: never "simplify" it away** (the 250 ms hold, the key holds, the
  focus rules, `SC_KEYMENU`, the EDIT control's caret, the menu drop-downs of AX-6.6).
- Reflection, `dynamic`, `unsafe` and `#pragma warning disable` outside the places that need them: each one either
  goes or gets a one-line reason.
- Exceptions caught as `Exception` where a narrower type is meant; empty `catch` blocks without a comment saying why
  silence is right.

### 3.4 Attack surface (after `security-audit-2026-10-24.md`; its fixes stay as they are)
- **Processes**: every `Process.Start` and `ProcessStartInfo` names its program by full path through
  `SystemPrograms` or a path the app found itself, passes arguments as a list (`ArgumentList`), and sets
  `UseShellExecute` deliberately. List any that do not; fix the ones that can be fixed without a behaviour change.
- **Paths from outside** (command lines, hand-overs, MCP, zip entries, `.git` files, settings): each is checked where
  it enters (rooted, on this computer where the audit says so, inside the folder it must stay in).
- **Parsers** of files a stranger can make (JSON, Markdown, zip, Office XML in axPreview, KDBX via the tool, the
  terminal stream): each has a size or depth limit and refuses DTDs and outside entities.
- **Pipes and window messages**: the automation channel and the MCP pipes stay for the user alone, need their secret
  or proof, and check the program at the other end; `WM_COPYDATA` hand-overs stay to this very `Axit.exe`.
- **The installer and the release path**: `install.ps1` writes only the user's own registry, never empties another
  program's key (the `New-Item -Force` lesson of 3.7.2), quotes every path; the workflows keep actions pinned to
  commit hashes, read-only tokens for the build, no `pull_request_target`.
- **Secrets**: nothing typed, copied or read from a conversation in a log; the clipboard writes of axPass private;
  the release key never in the repository.
- Each finding is fixed in its own commit with a test where one can be written, and added to the audit document as
  `S-54` onwards with its commit (S-46 to S-53 were the review of 3.7.x, 2026-10-25).

## 4. Order of the work

1. Baseline gate (section 2), recorded.
2. 3.1 Unused code, one project at a time, shared projects last.
3. 3.2 Copies to fold that a fresh search finds, the lowest risk first.
4. 3.3 Hacks, only the understood ones.
5. 3.4 Attack surface.
6. The final gate, then the report.

Agents may work areas in parallel in separate worktrees, but their branches meet on `cleanup` one at a time, each
merge followed by the gate.

## 5. What the run leaves alone

- Anything a spec, the user guide or an F1 text describes as behaviour.
- The NVDA test plans and the owed passes in `nvda-test-record.md` (the user's), except to add the sections a moved
  piece makes worth hearing again.
- `docs/axit/plan.md`'s "Still to come", the specs of apps not built, and the decisions recorded as `*-D*`.
- The fixtures under `tests/fixtures` (recordings of Claude Code; regenerating them needs the record-fixture skill).
- The release key, the signing scripts' logic, and `release.ps1`'s order of steps.

## 6. The report

`docs/axit/cleanup-run-report.md` on the branch, and a short version as the run's last message:

- the baseline and final gate results (test counts, scripts passed, warnings);
- the commits, grouped by section of this document, each with one line of what and why;
- the lines of code before and after, per project (`src/*`, `tests/*`);
- what was looked at and left, with the reason;
- the "needs the user" list: cleanups that would change behaviour, and decisions;
- the NVDA sections a moved piece makes worth hearing again;
- then the user decides: merge `cleanup` into `main` after those NVDA checks, and release.
