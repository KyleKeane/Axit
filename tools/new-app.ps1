<#
.SYNOPSIS
  Adds a new app to the bundle: its folders, a first window, its documents and every registration place a script can
  write (docs/axit/app-kit.md AK-1.2, docs/axit/adding-an-app.md), and prints the places left to write by hand.

.DESCRIPTION
  Run it from a clean working tree on a branch of its own; review the result with git diff, then build and run the tests
  (test.ps1). It writes:
    - src\<Project> (the window: <Project>.csproj, <Project>App.cs, <Project>Keys.cs, <Short>Form.cs on the window
      base AxitForm, HelpText.cs, CLAUDE.md), src\<Project>.Core (<Short>Settings.cs, <Short>Lines.cs, CLAUDE.md) and
      tests\<Project>.Tests, all modelled on axCalc, the smallest app;
    - docs\<lower>\SPEC.md, user-guide.md and nvda-test-plan.md as stubs, run-<lower>.ps1, and
      tests\scripts\<lower>\01-start.txt for tools\AxAutomate;
    - the app's line in Dispatch.Apps with its verb constant and its usage line (src\Axit.Core\Dispatch.cs), its line
      in Program.cs's table, the KeyTests entries, the app order DispatchTests expects, the solution's three projects,
      the icon (tools\make-icon.ps1), Axit.csproj's description, the issue forms' choices, the verb and the taskbar
      id in docs\axit\SPEC.md AX-1.1 and AX-1.6;
    - drafts, to be rewritten in real words, of the app's line in README.md, docs\axit\README.md, docs\axit\SPEC.md
      section 3, the root CLAUDE.md map and CHANGELOG.md.
  What reads Dispatch.Apps needs nothing: the installer (Axit.exe apps), run.ps1, make-icon.ps1, the project
  references (a glob), the Axit menu and the other lists of the windows.

.EXAMPLE
  .\tools\new-app.ps1 axList list Organise Files -Description 'axList, lists of plain lines'
  .\tools\new-app.ps1 axOCR ocr Utilities Files -Description 'axOCR, the text of a picture' -IconLetters Ao -IconRgb 60,90,40
#>
[CmdletBinding()]
param(
    # The app's name as shown and spoken, "ax" and a capital: axList.
    [Parameter(Mandatory = $true, Position = 0)] [string]$Name,
    # What Axit.exe takes: list.
    [Parameter(Mandatory = $true, Position = 1)] [string]$Verb,
    # Its group in the Axit menu (AX-9.2): Code, Files, Organise or Utilities.
    [Parameter(Mandatory = $true, Position = 2)] [ValidateSet('Code', 'Files', 'Organise', 'Utilities')] [string]$Group,
    # What it opens from a path: Nothing, Folders, Files or FilesAndFolders.
    [Parameter(Mandatory = $true, Position = 3)] [ValidateSet('Nothing', 'Folders', 'Files', 'FilesAndFolders')] [string]$Opens,
    # The Start menu's description: the name, a comma, what the app is.
    [string]$Description,
    # The name of its code (folders, classes, icon): "Ax" and the rest of the name unless given.
    [string]$Project,
    # The letters of its requirements in its SPEC.md, AC for axCalc: the first two letters after "ax" unless given.
    [string]$Prefix,
    # The icon's two letters and its colour; a pair no other app has unless given.
    [string]$IconLetters,
    [int[]]$IconRgb = @(80, 80, 120)
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).ProviderPath
. (Join-Path $root 'tools\apps.ps1')
$existing = @(Get-AxitApps $root)

# ---- the names ----
if ($Name -cnotmatch '^ax[A-Z][A-Za-z0-9]*$') { throw "The name is 'ax' and a capital, as axList: $Name" }
if ($Verb -cnotmatch '^[a-z][a-z0-9]*$') { throw "The verb is lower-case letters and digits, as list: $Verb" }
if (-not $Project) { $Project = 'Ax' + $Name.Substring(2) }
if ($Project -cnotmatch '^Ax[A-Z][A-Za-z0-9]*$' -or $Project -like 'Axit*') { throw "The project name is 'Ax' and a capital, not Axit: $Project" }
$short = $Project.Substring(2)
$lower = $Project.ToLowerInvariant()
$constant = $Project
if (-not $Prefix) { $Prefix = $short.Substring(0, [Math]::Min(2, $short.Length)).ToUpperInvariant() }
$what = if ($Description) { $Description } else { "$Name, a new app of the bundle" }
if (-not $what.StartsWith("$Name, ")) { throw "The description starts with the name and a comma: '$Name, what it is'" }
$what = $what.Substring($Name.Length + 2)
$Description = "$Name, $what"

foreach ($app in $existing) {
    if ($app.Name -eq $Name -or $app.Verb -eq $Verb -or $app.Project -eq $Project) { throw "$($app.Name) already has that name, verb or project ($($app.Verb), $($app.Project))." }
}
if ($Verb -in 'axit', 'mcp', 'apps') { throw "$Verb is the bundle's own verb." }
foreach ($folder in "src\$Project", "src\$Project.Core", "tests\$Project.Tests", "docs\$lower") {
    if (Test-Path (Join-Path $root $folder)) { throw "$folder already exists." }
}

if (-not $IconLetters) {
    $taken = @($existing | ForEach-Object { $_.Icon.Letters })
    foreach ($letter in ($short.ToLowerInvariant().ToCharArray() + [char[]]'bcdefghijklmnopqrstuvwxyz')) {
        if ($taken -notcontains ('A' + $letter)) { $IconLetters = 'A' + $letter; break }
    }
}
if (@($existing | Where-Object { $_.Icon.Letters -ceq $IconLetters }).Count -gt 0) { throw "Another app's icon has the letters $IconLetters." }
if ($IconRgb.Count -ne 3) { throw '-IconRgb is three numbers: red, green, blue.' }

$opensWhat = switch ($Opens) { 'Folders' { 'folder' } 'Files' { 'file' } 'FilesAndFolders' { 'folder or file' } default { '' } }
$argumentText = if ($opensWhat) { "[<$opensWhat>]" } else { '' }
$today = Get-Date -Format 'yyyy-MM-dd'

# ---- writing ----
$utf8 = New-Object System.Text.UTF8Encoding($false)
$written = New-Object System.Collections.Generic.List[string]

function Expand([string]$template) {
    $text = $template.Replace('{{Name}}', $Name).Replace('{{Project}}', $Project).Replace('{{Short}}', $short).Replace('{{lower}}', $lower)
    $text = $text.Replace('{{Verb}}', $Verb).Replace('{{P}}', $Prefix).Replace('{{What}}', $what).Replace('{{Date}}', $today)
    return $text.Replace('{{Args}}', $argumentText).Replace('{{OpensWhat}}', $opensWhat).Replace('{{Constant}}', $constant)
}

function New-TextFile([string]$relative, [string]$template) {
    $path = Join-Path $root $relative
    New-Item -ItemType Directory -Force (Split-Path -Parent $path) | Out-Null
    $text = (Expand $template) -replace "`r?`n", "`r`n"
    [System.IO.File]::WriteAllText($path, $text.TrimEnd() + "`r`n", $utf8)
    $written.Add($relative)
}

# An existing file changed by a function of its text, its line ending and its byte order mark kept; the change must
# change something.
function Edit-TextFile([string]$relative, [scriptblock]$change) {
    $path = Join-Path $root $relative
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $text = [System.IO.File]::ReadAllText($path)
    $nl = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $new = & $change $text $nl
    if ($new -eq $text) { throw "Nothing changed in ${relative}: the place to write was not found." }
    [System.IO.File]::WriteAllText($path, $new, (New-Object System.Text.UTF8Encoding($bom)))
    $written.Add($relative)
}

# The text with $line inserted after the last line that matches $pattern.
function Add-AfterLast([string]$text, [string]$nl, [string]$pattern, [string]$line) {
    $found = [regex]::Matches($text, "(?m)$pattern(\r?)$")
    if ($found.Count -eq 0) { return $text }
    $last = $found[$found.Count - 1]
    $at = $last.Index + $last.Length
    # The new line goes before the line's own ending, which the match may have taken.
    if ($at -gt 0 -and $text[$at - 1] -eq "`r") { $at-- }
    return $text.Insert($at, $nl + $line)
}

# ---- the window, src\<Project> ----
New-TextFile "src\$Project\$Project.csproj" @'
<Project Sdk="Microsoft.NET.Sdk">

  <!-- {{Name}}'s window side, as a class library inside Axit.exe (docs/axit/SPEC.md AX-1.7): the window, its key table
       and its texts; the pure code is {{Project}}.Core. -->
  <PropertyGroup>
    <TargetFramework>$(AxitDotNet)-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>{{Project}}</RootNamespace>
    <AssemblyName>{{Name}}</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\{{Project}}.Core\{{Project}}.Core.csproj" />
    <ProjectReference Include="..\Axit.Core\Axit.Core.csproj" />
    <ProjectReference Include="..\Axit.Forms\Axit.Forms.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Help, User guide shows this file. -->
    <EmbeddedResource Include="..\..\docs\{{lower}}\user-guide.md" LogicalName="UserGuide.md" />
  </ItemGroup>

</Project>
'@

if ($Opens -eq 'Nothing') {
    New-TextFile "src\$Project\${Project}App.cs" @'
using {{Project}}.Core;
using Axit.Forms;

namespace {{Project}};

/// <summary>
/// {{Name}}'s entry point inside Axit (docs/axit/SPEC.md AX-1.7): <c>Axit.exe {{Verb}}</c> calls <see cref="Run"/>, one
/// call into the bundle's <see cref="AppHost"/> with the settings and <see cref="{{Short}}Form"/>. It takes no path;
/// any argument is reported with the usage text.
/// </summary>
public static class {{Project}}App
{
    public const string Name = "{{Name}}";

    public const string Usage = """
        Usage: Axit {{Verb}}
               {{lower}}            the same, as the installed console command

          --help, --version  this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "{{lower}}", Usage, args, arguments =>
        {
            if (arguments.Length > 0)
            {
                AppHost.BadCommandLine(Name, $"{{Name}} takes no arguments: {string.Join(" ", arguments)}", Usage);
                return null;
            }

            var settings = {{Short}}Settings.Load({{Short}}Settings.DefaultPath, out var settingsError);
            return new {{Short}}Form(null, settings, settingsError);
        });
}
'@
} else {
    New-TextFile "src\$Project\${Project}App.cs" @'
using {{Project}}.Core;
using Axit.Forms;

namespace {{Project}};

/// <summary>
/// {{Name}}'s entry point inside Axit (docs/axit/SPEC.md AX-1.7): <c>Axit.exe {{Verb}} [&lt;{{OpensWhat}}&gt;]</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the one path of the command line, the
/// settings and <see cref="{{Short}}Form"/>.
/// </summary>
public static class {{Project}}App
{
    public const string Name = "{{Name}}";

    public const string Usage = """
        Usage: Axit {{Verb}} {{Args}}
               {{lower}} {{Args}}  the same, as the installed console command

          <{{OpensWhat}}>  what {{Name}} opens; without one, nothing
          --help, --version  this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "{{lower}}", Usage, args, arguments =>
        {
            if (!AppHost.OnePath(Name, "{{OpensWhat}}", Usage, arguments, out var path))
            {
                return null;
            }

            var settings = {{Short}}Settings.Load({{Short}}Settings.DefaultPath, out var settingsError);
            return new {{Short}}Form(path, settings, settingsError);
        });
}
'@
}

New-TextFile "src\$Project\${Project}Keys.cs" @'
namespace {{Project}};

/// <summary>
/// {{Name}}'s window-specific keys (SPEC.md {{P}}-7; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>{{Short}}Form</c> and the F1 text read this table. The bundle-wide keys (F1, the palette,
/// the control keys, find, Ctrl+S, text size) are <c>Axit.Forms.BundleKeys</c>. None yet.
/// </summary>
public static class {{Project}}Keys
{
    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All = [];
}
'@

New-TextFile "src\$Project\${short}Form.cs" @'
using {{Project}}.Core;
using Axit.Core;
using Axit.Forms;

namespace {{Project}};

/// <summary>
/// {{Name}}'s window (SPEC.md {{P}}-2): a menu bar and one list, on the bundle's window base (<see cref="AxitForm"/>),
/// which holds the notices, the fonts, the window's place, find and the update flow. A first draft from
/// tools/new-app.ps1; axCalc's <c>CalcForm</c> is the small example of a window grown from here.
/// </summary>
public sealed class {{Short}}Form : AxitForm
{
    private readonly MenuStrip _menu = new();
    private readonly AxitList _list = new();
    private readonly IReadOnlyList<string> _lines;

    public {{Short}}Form(string? path, {{Short}}Settings settings, string? settingsError)
        : base({{Project}}App.Name, settings, {{Short}}Settings.DefaultPath, settingsError)
    {
        _lines = {{Short}}Lines.For(path);
        Text = {{Short}}Lines.Title(path) + " - " + {{Project}}App.Name;

        // ---- menus and keys ({{P}}-7): one command list (docs/axit/app-kit.md AK-2), each key from the tables ----
        var first = new ToolStripMenuItem("&{{Short}}");
        first.DropDownItems.Add(CreateExitItem());

        // Options, as the other windows' (AX-6): the window's own submenus and items first, then Text, General, Developer.
        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "{{Name}} &resources",
            [("&Adding an app to Axit", "https://github.com/KyleKeane/Axit/blob/main/docs/axit/adding-an-app.md")]);

        // The bundle's Axit menu (AX-9) sits before Help in every window.
        _menu.Items.AddRange([first, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        AddKey(new Command("Find in the list", BundleKeys.Find, () => ShowFindInList(_list, () => _lines)) { Mark = CommandMark.Read });
        AddKey(new Command("Find next", BundleKeys.FindNext, () => FindInList(_list, () => _lines, 1)) { Mark = CommandMark.Read });
        AddKey(new Command("Find previous", BundleKeys.FindPrevious, () => FindInList(_list, () => _lines, -1)) { Mark = CommandMark.Read });

        // ---- the list ({{P}}-2.1) ----
        _list.IntegralHeight = false;
        _list.Dock = DockStyle.Fill;
        _list.TabIndex = 0;
        _list.AccessibleName = "{{Short}}";
        _list.Items.AddRange(_lines.Select(line => (object)line).ToArray());
        _list.SelectedIndex = 0;

        // Docked from the last control to the first: the menu takes the top, the list (or the notice over it) the rest.
        Controls.AddRange([Overlay, _list, _menu]);

        ApplyTextFont();
        RestoreWindow();
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice() => _list.Visible = false;

    protected override void ShowAfterNotice() => _list.Visible = true;

    protected override Control FocusTarget => _list;

    protected override void ApplyFont(Font font) => _list.Font = font;

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.{{Constant}}];

    protected override void OnShown(EventArgs e)
    {
        if (!NoticeOpen)
        {
            _list.Select();
        }

        base.OnShown(e);
    }
}
'@

New-TextFile "src\$Project\HelpText.cs" @'
using System.Reflection;
using Axit.Core;

namespace {{Project}};

/// <summary>The texts {{Name}} shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md {{P}}-7): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then {{Name}}'s own.</summary>
    public const string Shortcuts = """
        In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match

        ## In every window of Axit
          F1: this list
          Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it
          Ctrl+F: find a line in the list. F3 and Shift+F3: next or previous match
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close {{Name}}
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, the AI tools, Check for updates, and About

        ## {{Name}}
          Up and Down: the lines of the list
        """;

    /// <summary>docs/{{lower}}/user-guide.md, embedded by {{Project}}.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/{{lower}}/user-guide.md");
}
'@

New-TextFile "src\$Project\CLAUDE.md" @'
# {{Name}}, the window

The WinForms side of {{Name}}: one list and the bundle's notices, on the window base. The pure code is `src/{{Project}}.Core`, with its own `CLAUDE.md`. The spec is `docs/{{lower}}/SPEC.md` ("SPEC.md" below); the guide `docs/{{lower}}/user-guide.md` is embedded by `{{Project}}.csproj` for Help, User guide; the manual test plan is `docs/{{lower}}/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-{{lower}}.ps1                  # build Axit and start {{Name}} (run.ps1 -App {{Verb}})
.\test.ps1 {{Project}}                 # the tests in Release, as CI runs them
```

## Where things are

- `{{Project}}App`: the entry point `Run` that Axit's `Program` calls (bundle AX-1.7), one call into `Axit.Forms/AppHost.Run`.
- `{{Short}}Form`: the window (SPEC.md {{P}}-2) on the bundle's window base (`Axit.Forms/AxitForm`); its commands are `Add` and `AddKey` calls (docs/axit/app-kit.md AK-2).
- `{{Project}}Keys`: the window's keys (bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; nothing speaks on an arrow key.
2. No second window (bundle AX-7.2): questions are notices.
3. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- Written by `tools/new-app.ps1` on {{Date}}; replace this line with the first gotcha found.
'@

# ---- the pure code, src\<Project>.Core, and its tests ----
New-TextFile "src\$Project.Core\$Project.Core.csproj" @'
<Project Sdk="Microsoft.NET.Sdk">

  <!-- {{Name}}'s pure code (docs/{{lower}}/SPEC.md section 5): no window. -->
  <PropertyGroup>
    <TargetFramework>$(AxitDotNet)</TargetFramework>
    <RootNamespace>{{Project}}.Core</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Axit.Core\Axit.Core.csproj" />
  </ItemGroup>

</Project>
'@

New-TextFile "src\$Project.Core\${short}Settings.cs" @'
using Axit.Core;

namespace {{Project}}.Core;

/// <summary>
/// Everything {{Name}} remembers between runs (SPEC.md {{P}}-9), in <c>%APPDATA%\Axit\{{lower}}.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from <see cref="WindowSettings"/>.
/// </summary>
public sealed class {{Short}}Settings : WindowSettings
{
    public static string DefaultPath => AppPaths.SettingsFile("{{lower}}");

    public static {{Short}}Settings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<{{Short}}Settings>(path, out error);
        settings.SanitizeWindow();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);
}
'@

New-TextFile "src\$Project.Core\${short}Lines.cs" @'
namespace {{Project}}.Core;

/// <summary>What {{Name}}'s list shows (SPEC.md {{P}}-2.1): a first draft, the path it was started on or a line that says there is none.</summary>
public static class {{Short}}Lines
{
    public const string NothingOpen = "Nothing open";

    /// <summary>The list's lines: never empty, so that the list always has a line to read (AX-8.6).</summary>
    public static IReadOnlyList<string> For(string? path) => [path ?? NothingOpen];

    /// <summary>The window's title before " - {{Name}}": the file's or folder's name, or "{{Short}}".</summary>
    public static string Title(string? path) =>
        path is null ? "{{Short}}" : Path.GetFileName(Path.TrimEndingDirectorySeparator(path)) is { Length: > 0 } name ? name : path;
}
'@

New-TextFile "src\$Project.Core\CLAUDE.md" @'
# {{Project}}.Core, the pure code

Everything of {{Name}} that needs no window. Unit tested in `tests/{{Project}}.Tests`. The spec is `docs/{{lower}}/SPEC.md` ("SPEC.md" below); the window is `src/{{Project}}`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 {{Project}}
```

## Where things are

- `{{Short}}Lines` (SPEC.md {{P}}-2.1): the list's lines and the window's title.
- `{{Short}}Settings` (SPEC.md {{P}}-9): the settings file, the window base's settings and nothing else yet.

## Gotchas

- Written by `tools/new-app.ps1` on {{Date}}; replace this line with the first gotcha found.
'@

New-TextFile "tests\$Project.Tests\$Project.Tests.csproj" @'
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\..\src\{{Project}}.Core\{{Project}}.Core.csproj" />
  </ItemGroup>

</Project>
'@

New-TextFile "tests\$Project.Tests\${short}Tests.cs" @'
using {{Project}}.Core;

namespace {{Project}}.Tests;

/// <summary>{{Name}}'s pure code (docs/{{lower}}/SPEC.md): the list's lines, the title and the settings file.</summary>
public sealed class {{Short}}Tests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "{{lower}}-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void The_list_always_has_a_line()
    {
        Assert.Equal([{{Short}}Lines.NothingOpen], {{Short}}Lines.For(null));
        Assert.Equal(["C:\\work\\notes.md"], {{Short}}Lines.For("C:\\work\\notes.md"));
    }

    [Fact]
    public void The_title_is_the_name_of_what_is_open()
    {
        Assert.Equal("{{Short}}", {{Short}}Lines.Title(null));
        Assert.Equal("notes.md", {{Short}}Lines.Title("C:\\work\\notes.md"));
        Assert.Equal("work", {{Short}}Lines.Title("C:\\work\\"));
    }

    [Fact]
    public void A_missing_settings_file_gives_the_defaults()
    {
        var settings = {{Short}}Settings.Load(Path.Combine(_folder, "{{lower}}.json"), out var error);
        Assert.Null(error);
        Assert.NotNull(settings);
    }
}
'@

# ---- the documents and the run script ----
New-TextFile "docs\$lower\SPEC.md" @'
# {{Name}} specification

Status: first draft written by `tools/new-app.ps1` on {{Date}}; nothing below is decided yet. Requirements are
numbered {{P}}-n.m. The bundle's rules are `docs/axit/SPEC.md` (AX-7 accessibility, AX-8 keys); "SPEC.md" in
{{Name}}'s code means this file.

## 1. Summary

{{Name}}: {{What}}. `Axit.exe {{Verb}} {{Args}}`, Start entry `Axit {{Name}}`, command `{{lower}}`.

## 2. Goals and non-goals

## 3. Scenarios

## 4. Requirements

### {{P}}-1 Start

- {{P}}-1.1 `Axit.exe {{Verb}}` starts {{Name}} (bundle AX-1.1).

### {{P}}-2 The window

- {{P}}-2.1 One list, named "{{Short}}", with the focus at start; its title ends in " - {{Name}}".

### {{P}}-7 Keys

Every key is bundle-wide (AX-8) or window-specific, in `{{Project}}Keys`. None of the window's own yet.

### {{P}}-8 Notices and announcements

### {{P}}-9 Settings

- {{P}}-9.1 `%APPDATA%\Axit\{{lower}}.json`: the window base's settings.

## 5. Architecture

The window is `src/{{Project}}` on `Axit.Forms/AxitForm`; the pure code is `src/{{Project}}.Core`, tested in
`tests/{{Project}}.Tests`.

## 6. Decisions

## 7. Testing

The unit tests, and the NVDA test plan `docs/{{lower}}/nvda-test-plan.md`.
'@

New-TextFile "docs\$lower\user-guide.md" @'
# {{Name}} user guide

{{Name}} is {{What}}, made for screen reader users.

## Install

{{Name}} is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) gives you a Start menu entry `Axit {{Name}}` and a console command `{{lower}}`. Any Axit window starts one too: Alt+A, New window, {{Name}}.

## Start

The window ends its title in "- {{Name}}", and the focus starts in its list.

## Use

## Settings

Options, Text sets the font and its size; Options, General the update check.

## Updates

The Axit menu, Check for updates.

## If something goes wrong

Options, Developer, Copy diagnostics copies what helps a bug report.

## About

The Axit menu, About.

## Disclaimer

{{Name}} comes with no warranty; see the licence in the Axit menu, About.
'@

New-TextFile "docs\$lower\nvda-test-plan.md" @'
# {{Name}} NVDA test plan

Run with NVDA before a release, after `run-{{lower}}.ps1` (or `.\run.ps1 -App {{Verb}}`). Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/{{lower}}/SPEC.md` ({{P}}-n.m).

## 1. Start ({{P}}-1, {{P}}-2)

1. Start {{Name}}: NVDA reads the title ending in "- {{Name}} window", then "{{Short}}, list" and its line. Nothing else is spoken.
2. F1: the keyboard shortcuts as a notice; Escape closes it and the list has the focus again.
3. Alt+F4 closes the window without a question.
'@

New-TextFile "tests\scripts\$lower\01-start.txt" @'
# {{Name}}: the window at start, F1 and its commands (docs/{{lower}}/nvda-test-plan.md section 1), run by
# tools/AxAutomate on a desktop of its own (docs/axit/automation.md).
file home\Axit\{{lower}}.json {"checkForUpdates": false}

start {{Verb}}
check commands
expect focus {{Short}}
expect state "title":"{{Short}} - {{Name}}"
keys f1
expect state Keyboard shortcuts
keys escape
expect focus {{Short}}
'@

$runOpens = if ($opensWhat) { "  .\run-{{lower}}.ps1 C:\some\path            # build and start {{Name}} on a $opensWhat" } else { '' }
New-TextFile "run-$lower.ps1" (@'
<#
.SYNOPSIS
  Builds Axit and starts {{Name}}: run.ps1 with -App {{Verb}}. Every other argument goes through unchanged.

.EXAMPLE
  .\run-{{lower}}.ps1                          # build and start {{Name}}
'@ + $(if ($runOpens) { "`n" + $runOpens } else { '' }) + @'

  .\run-{{lower}}.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App {{Verb}} @args
exit $LASTEXITCODE
'@)

# ---- the icon ----
& (Join-Path $root 'tools\make-icon.ps1') -Out (Join-Path $root "src\Axit\$Project.ico") -Text $IconLetters -Rgb $IconRgb | Out-Null
$written.Add("src\Axit\$Project.ico")

# ---- the registration places ----
$groups = 'Code', 'Files', 'Organise', 'Utilities'
Edit-TextFile 'src\Axit.Core\Dispatch.cs' {
    param($text, $nl)
    # The verb constant, after the last app's.
    $text = Add-AfterLast $text $nl '^    public const string Ax\w+ = "[^"]*";' "    public const string $constant = `"$Verb`";"
    # The entry, before the first app of a later group (or at the end), so that the groups stay in order (AX-9.2).
    $start = $text.IndexOf('public static readonly IReadOnlyList<App> Apps =')
    $end = $text.IndexOf("$nl    ];", $start)
    $rank = [array]::IndexOf($groups, $Group)
    $at = $end + $nl.Length
    foreach ($entry in [regex]::Matches($text.Substring($start, $end - $start), '(?m)^        new\("[^"]+", \w+, "\w+", AppGroup\.(\w+),')) {
        if ([array]::IndexOf($groups, $entry.Groups[1].Value) -gt $rank) { $at = $start + $entry.Index; break }
    }
    $line = "        new(`"$Name`", $constant, `"$Project`", AppGroup.$Group, Opens.$Opens)$nl        {$nl            Description = `"$Description`", Icon = new(`"$IconLetters`", $($IconRgb -join ', ')),$nl        },$nl"
    $text = $text.Insert($at, $line)
    # The usage text: the verb in the first line, the app's line before the paths, its console command.
    $text = [regex]::Replace($text, '(Usage: Axit \[[^\]]*)\]', "`$1|$Verb]")
    $usageLine = ("          Axit $Verb $argumentText").TrimEnd()
    $usageLine = $(if ($usageLine.Length -ge 38) { $usageLine + ' ' } else { $usageLine.PadRight(38) }) + "$Name, $what"
    $paths = $text.IndexOf('          Axit <folder>  ')
    if ($paths -lt 0) { throw 'The usage line "Axit <folder>" was not found in Dispatch.cs.' }
    $text = $text.Insert($paths, $usageLine + $nl)
    $text = [regex]::Replace($text, ' and (\w+) stand for ', ", `$1 and $lower stand for ")
    $text = [regex]::Replace($text, ' and (Axit \w+)\.(\r?)$', ", `$1 and Axit $Verb.`$2", 'Multiline')
    return $text
}

Edit-TextFile 'src\Axit\Program.cs' {
    param($text, $nl)
    $text = Add-AfterLast $text $nl '^using Ax\w+;' "using $Project;"
    return Add-AfterLast $text $nl '^        \[Dispatch\.\w+\] = \w+App\.Run,' "        [Dispatch.$constant] = ${Project}App.Run,"
}

Edit-TextFile 'tests\Axit.Tests\KeyTests.cs' {
    param($text, $nl)
    $text = Add-AfterLast $text $nl '^            \["[^"]+"\] = \w+\.\w+Keys\.All,' "            [`"$Name`"] = $Project.${Project}Keys.All,"
    return Add-AfterLast $text $nl '^        \["[^"]+"\] = \w+\.HelpText\.Shortcuts,' "        [`"$Name`"] = $Project.HelpText.Shortcuts,"
}

# The order of the apps DispatchTests expects, read again from Dispatch.Apps.
$order = (@(Get-AxitApps $root) | ForEach-Object { "`"$($_.Name)`"" }) -join ', '
Edit-TextFile 'tests\Axit.Tests\DispatchTests.cs' {
    param($text, $nl)
    return [regex]::Replace($text, 'Assert\.Equal\(\["ax[^\]]*\], Dispatch\.Apps\.Select\(a => a\.Name\)\);', "Assert.Equal([$order], Dispatch.Apps.Select(a => a.Name));")
}

Edit-TextFile 'src\Axit\Axit.csproj' {
    param($text, $nl)
    return [regex]::Replace($text, ', and ([^<]*)</Description>', ", `$1, and $Description</Description>")
}

foreach ($form in '.github\ISSUE_TEMPLATE\bug.yml', '.github\ISSUE_TEMPLATE\idea.yml') {
    Edit-TextFile $form { param($text, $nl) Add-AfterLast $text $nl '^        - ax\w+' "        - $Name" }
}

Edit-TextFile 'docs\axit\SPEC.md' {
    param($text, $nl)
    $text = [regex]::Replace($text, '(- AX-1\.1 The first argument may be a verb naming an app: [^\r\n]*?`)( \(the one list is)', "`$1, ``$Verb```$2")
    $text = [regex]::Replace($text, '(`Axit\.Ax\w+`)\), so the', "`$1, ``Axit.$Project``), so the")
    # Section 3: a draft entry after the last app's.
    $section = $text.IndexOf("$nl## 4. ")
    if ($section -lt 0) { return $text }
    return $text.Insert($section - $nl.Length, "$nl- **$Name** ($today, draft from tools/new-app.ps1): ``Axit.exe $Verb $argumentText``, Start entry ``Axit $Name``, command ``$lower``: $what. Specification ``docs/$lower/SPEC.md``, guide ``docs/$lower/user-guide.md``, test plan ``docs/$lower/nvda-test-plan.md``.")
}

Edit-TextFile 'README.md' { param($text, $nl) Add-AfterLast $text $nl '^- \*\*ax\w+\*\*: .*' "- **$Name**: $what." }
Edit-TextFile 'docs\axit\README.md' { param($text, $nl) Add-AfterLast $text $nl '^- \*\*ax\w+\*\* is .*' "- **$Name** is $what." }
Edit-TextFile 'CLAUDE.md' {
    param($text, $nl)
    $map = "- **$Name**: window code in ``src/$Project`` (notes: ``src/$Project/CLAUDE.md``), pure code in ``src/$Project.Core`` (notes: ``src/$Project.Core/CLAUDE.md``), tests in ``tests/$Project.Tests``, documents in ``docs/$lower/`` (``SPEC.md``, ``user-guide.md``, ``nvda-test-plan.md``). In $Name's code and documents, `"SPEC.md`" means ``docs/$lower/SPEC.md``."
    $bundle = $text.IndexOf("$nl- **Bundle**:")
    if ($bundle -lt 0) { return $text }
    return $text.Insert($bundle, "$nl$map")
}
Edit-TextFile 'CHANGELOG.md' {
    param($text, $nl)
    $bullet = "- ${Name}: a new app, $what."
    $firstSection = [regex]::Match($text, '(?m)^## ')
    $heading = "## Unreleased$nl$nl"
    if ($text.Substring($firstSection.Index).StartsWith($heading)) {
        # As the section's first bullet; a blank line before the next section when the section was empty.
        $at = $firstSection.Index + $heading.Length
        $after = if ($text.Substring($at).StartsWith('## ')) { "$nl$nl" } else { $nl }
        return $text.Insert($at, $bullet + $after)
    }
    return $text.Insert($firstSection.Index, "$heading$bullet$nl$nl")
}

# ---- the solution ----
foreach ($projectFile in "src\$Project.Core\$Project.Core.csproj", "src\$Project\$Project.csproj", "tests\$Project.Tests\$Project.Tests.csproj") {
    dotnet sln (Join-Path $root 'Axit.sln') add (Join-Path $root $projectFile) | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet sln add $projectFile failed." }
}
$written.Add('Axit.sln')

Write-Host "Added $Name (verb $Verb, project $Project, group $Group, opens $Opens, icon $IconLetters on $($IconRgb -join ',')). Written:"
$written | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
Write-Host @"

Drafts to rewrite in real words: the lines of $Name in README.md, docs\axit\README.md, docs\axit\SPEC.md section 3,
the root CLAUDE.md map and CHANGELOG.md; Description in its Dispatch.Apps entry; the documents in docs\$lower.

Still by hand:
  - Dispatch.Apps: ShowIn: true for an app that works in a folder (then DispatchTests' Show in list), Explorer,
    Types, OpenWith and TypeName for File Explorer and Open with, and a rule in Dispatch.Decide if a path alone
    should open $Name (AX-1.2).
  - The root CLAUDE.md: $Name in the opening paragraph and in the Commands paragraph (run.ps1's -App values, the
    run-*.ps1 list); docs\axit\SPEC.md's summary sentence; docs\axit\README.md's Start entries and commands; run.ps1's
    examples; install.ps1's header comment; docs\axit\plan.md's Still to come, if $Name was there.
  - A background app (a hotkey, as axStart and axClip): install.ps1's `$background table and run.ps1's restart.
  - KeyTests: the known keys and the overrides, when the window's keys meet another window's.
  - The first build writes tests\$Project.Tests\packages.lock.json and changes tests\Axit.Tests\packages.lock.json:
    commit both with the rest.
  - Then: dotnet build, .\test.ps1, the accessibility-review skill, the NVDA test plan.
"@
