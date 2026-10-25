<#
.SYNOPSIS
  Installs Axit for the current user, or removes it with -Uninstall. No administrator rights are needed.

.DESCRIPTION
  Run it from the folder that holds Axit.exe: the extracted zip, or publish\win-x64 after publish.ps1.
  Axit is one program with several apps inside (docs/axit/SPEC.md AX-2). It copies Axit to
  %LOCALAPPDATA%\Programs\Axit and creates the ways to start each app, all from the list of apps that Axit.exe apps
  prints (Dispatch.Apps; $apps below):
    - a Start menu entry per app, "Axit axClaude", "Axit axDown" and so on (type axit in the Start menu to list them),
    - one submenu in File Explorer's right-click menu, "Open with Axit", on folders (and on a folder's background) and
      on files, listing the apps that open what was right-clicked: axClaude, axGIT, axTerminal and axFile on folders,
      axDown, axFile and axPreview on every file, axJSON on .json files, axMedia on sound and video files; and axDown
      under Windows' own "Open with" for .md, .markdown and .txt, axJSON for .json, axMedia for the sound and video
      kinds, axPreview for .txt, .md, .markdown, .log and .json, so that the user can make an app the default program
      for a kind in Settings, Default apps,
    - axStart, the program launcher, and axClip, the clipboard history, running in the background from sign-in (a value
      under the user's Run key that each writes itself): they are stopped before an install or a removal, and started
      again, hidden, after an install,
    - the console commands axclaude, axdown, axjson, axgit, axterminal, axfile, axmedia, axpreview, axstart, axprocess, axtask, axpass, axcalc, axclip and axit (a folder
      opens axClaude, a file axDown, a .json file axJSON, a sound or video file axMedia): one-line shims in
      %USERPROFILE%\.local\bin, put on the user's PATH when it is not there yet (Claude Code's installer uses the same
      folder; a removal leaves the PATH, since the folder may hold other programs' commands).
  It also lists Axit in Settings, Apps (Add or remove programs), whose Uninstall runs install.cmd -Uninstall.
  -Uninstall removes all of that. The settings in %APPDATA%\Axit and the logs in %LOCALAPPDATA%\Axit stay.
  An installation of axClaude 1.x, from before the bundle, is removed completely first; its settings file stays in
  %APPDATA%\AxClaude, and axClaude copies it on its first start under Axit.

.EXAMPLE
  .\install.ps1                 # install or update
  .\install.ps1 -Uninstall      # remove
  .\install.ps1 -NoContextMenu  # install without the File Explorer entries
  .\install.ps1 -NoStartMenu    # install without the Start menu entries

  install.cmd, next to this script, runs it with the execution policy bypassed: double-click it, or install.cmd -Uninstall.
  Downloaded on its own from the releases page, install.cmd fetches the latest release and runs this script from it.

  The apps' own Update (the Axit menu) runs the downloaded version's copy of this script as
    install.ps1 -InPlace -LogFile <file>
  while the app keeps running (AX-2.8): the new files are written over the old ones, what earlier versions left goes
  (their File Explorer entries, axClaude 1.x's folder and entries, files this version does not ship), and the
  running Axit.exe is renamed Axit.old.exe first, since a running program file can be renamed but not overwritten;
  the app then starts the new version itself, and the next start removes the old file. Copies before 2.3.0 call
    install.ps1 -WaitForProcess <pid> [-Start <folder> [-ContinueConversation]] [-StartFile <file>] -LogFile <file>
  after they have closed; those parameters stay (AX-2.7), which is also how axClaude 1.7.0 becomes Axit.
#>
[CmdletBinding()]
param(
    [switch]$Uninstall,
    [switch]$NoContextMenu,
    [switch]$NoStartMenu,
    [switch]$InPlace,
    [int]$WaitForProcess,
    [string]$Start,
    [switch]$ContinueConversation,
    [string]$StartFile,
    [string]$LogFile
)

$ErrorActionPreference = 'Stop'

function Say([string]$text) {
    Write-Host $text
    if ($LogFile) {
        try { Add-Content -Path $LogFile -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $text" -Encoding utf8 } catch { }
    }
}

# Any error that nothing below handles is said and logged: the updater runs this script without a window, so the
# log is the only place it would show.
trap {
    # Where it failed, so that the log and the app's notice say which step it was (until 3.7.2, the message alone).
    $where = if ($_.InvocationInfo -and $_.InvocationInfo.ScriptLineNumber) { " (install.ps1 line $($_.InvocationInfo.ScriptLineNumber): $($_.InvocationInfo.Line.Trim()))" } else { '' }
    Say "Axit was not installed: $($_.Exception.Message)$where"
    exit 1
}

if ($WaitForProcess) {
    Say "Waiting for the app (process $WaitForProcess) to close..."
    try { Wait-Process -Id $WaitForProcess -Timeout 120 -ErrorAction Stop } catch { }
    if (Get-Process -Id $WaitForProcess -ErrorAction SilentlyContinue) {
        Say "The app is still running after two minutes. The update was not installed; start it from the Axit menu again."
        exit 1
    }
}

$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $env:LOCALAPPDATA 'Programs\Axit'
$exe = Join-Path $target 'Axit.exe'
$bin = Join-Path $env:USERPROFILE '.local\bin'
$programs = [Environment]::GetFolderPath('Programs')
$classes = 'HKCU:\Software\Classes'
# The entry in Settings, Apps (Add or remove programs) for this user; its Uninstall runs install.cmd -Uninstall.
$appsKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Axit'

# The apps (AX-2), from the program itself: Axit.exe apps prints Dispatch.Apps (src/Axit.Core/Dispatch.cs) as JSON,
# so a new app of the bundle needs nothing here (docs/axit/app-kit.md AK-4.1). The installation keeps that listing as
# apps.json, which an uninstall reads when the program next to it cannot be run. Per app: Name, which gives the Start
# menu entry "Axit <Name>", the console command <name> and the ProgID Axit.<Name>. Verb: what Axit.exe takes. Icon:
# the file the Start menu entry and the File Explorer entries show, <Project>.ico. Opens: what the app opens from
# File Explorer's "Open with Axit" submenu (AX-2.3): 'folders' (a folder, and a folder's background), 'files', or
# both. Types: the file types a files entry is shown on; every file when absent. OpenWith: the file types the app is
# registered as a program for, under Windows' own "Open with" and in Settings, Default apps (B-9), with the TypeName
# that Windows shows for them.
$listingFile = 'apps.json'

# The first line of every installation's log, before any step that can fail: what is installed on what, for a report
# when something goes wrong.
if (-not $Uninstall) {
    $newExe = Join-Path $source 'Axit.exe'
    $newVersion = if (Test-Path -LiteralPath $newExe) { (Get-Item -LiteralPath $newExe).VersionInfo.FileVersion } else { 'with no Axit.exe beside install.ps1' }
    Say "Installing Axit $newVersion$(if ($InPlace) { ' in place, the app running' }) on Windows $([Environment]::OSVersion.Version), PowerShell $($PSVersionTable.PSVersion)"
}

function Get-AppListing {
    $program = Join-Path $source 'Axit.exe'
    $json = $null
    $reason = $null
    if (Test-Path -LiteralPath $program) {
        # Through a process with its output redirected: the program has no console of its own, so & would neither
        # wait for it nor catch what it writes. What went wrong is kept for the message: until 3.7.2 it was dropped,
        # and a failed update said only that the list could not be read.
        try {
            $start = New-Object System.Diagnostics.ProcessStartInfo $program
            $start.Arguments = 'apps'
            $start.UseShellExecute = $false
            $start.RedirectStandardOutput = $true
            $start.RedirectStandardError = $true
            $start.CreateNoWindow = $true
            $start.WorkingDirectory = $source
            $process = [System.Diagnostics.Process]::Start($start)
            $errors = $process.StandardError.ReadToEndAsync()
            $json = $process.StandardOutput.ReadToEnd()
            if (-not $process.WaitForExit(30000)) {
                $reason = 'it did not end within 30 seconds'
                $json = $null
            } elseif ($process.ExitCode -ne 0) {
                $reason = "it ended with exit code $($process.ExitCode)"
                if ($errors.Result.Trim()) { $reason += ": $($errors.Result.Trim())" }
                $json = $null
            } elseif (-not $json -or -not $json.Trim().StartsWith('[')) {
                $reason = "it ended without the list (it wrote $(if ($json) { $json.Length } else { 0 }) characters)"
                $json = $null
            }
        } catch {
            $reason = "it could not be run: $($_.Exception.Message)"
            $json = $null
        }
    } else {
        $reason = 'it is not there'
    }
    $saved = Join-Path $source $listingFile
    if (-not $json -and (Test-Path -LiteralPath $saved)) {
        Say "Axit.exe apps did not answer ($reason); the list of apps is read from $listingFile instead."
        $json = Get-Content -LiteralPath $saved -Raw
    }
    if (-not $json) { throw "the list of apps could not be read from $program (Axit.exe apps): $reason, and there is no $listingFile beside it" }
    return $json
}
$listingJson = Get-AppListing
$apps = @()
foreach ($entry in ($listingJson | ConvertFrom-Json)) {
    $iconFile = "$($entry.project).ico"
    $app = @{ Name = $entry.name; Verb = $entry.verb; Description = $entry.description; Opens = @($entry.explorer)
        Icon = if (Test-Path -LiteralPath (Join-Path $source $iconFile)) { Join-Path $target $iconFile } else { $exe } }
    if (@($entry.types).Count -gt 0) { $app.Types = @($entry.types) }
    if (@($entry.openWith).Count -gt 0) { $app.OpenWith = @($entry.openWith); $app.TypeName = $entry.typeName }
    $apps += $app
}
# The places that follow from the name.
foreach ($app in $apps) {
    $app.Start = Join-Path $programs "Axit $($app.Name).lnk"
    $app.Shim = Join-Path $bin ($app.Name.ToLower() + '.cmd')
    $app.ProgId = "Axit.$($app.Name)"
    # Windows' Open with lists one program per program file, so an app with file types has a hard link of Axit.exe
    # under its own name for its ProgID; started so, the program hands its arguments to Axit.exe (src/Axit/Program.cs).
    $app.Link = Join-Path $target "$($app.Name).exe"
}
$linkNames = @($apps | Where-Object { $_.OpenWith } | ForEach-Object { Split-Path -Leaf $_.Link })

# File Explorer's "Open with Axit" submenu (AX-2.3): on folders, on a folder's background and on files, each listing
# the apps whose Opens says so, in the order of their names. The registry shows an app only where it belongs, so
# no app has to check the path it is given.
$menus = @(
    @{ Key = "$classes\Directory\shell\Axit"; Opens = 'folders'; Argument = '%1' },
    @{ Key = "$classes\Directory\Background\shell\Axit"; Opens = 'folders'; Argument = '%V' },
    @{ Key = "$classes\*\shell\Axit"; Opens = 'files'; Argument = '%1' }
)

# What earlier versions put on the machine: one entry per app in File Explorer's menus, before the submenu (up to
# 2.13.0), and axClaude 1.x under its own names; the rest of 1.x's places are the same as Axit's.
$oldMenuKeys = @("$classes\Directory\shell\AxClaude", "$classes\Directory\Background\shell\AxClaude",
    "$classes\Directory\shell\AxGit", "$classes\Directory\Background\shell\AxGit",
    "$classes\Directory\shell\AxTerminal", "$classes\Directory\Background\shell\AxTerminal", "$classes\*\shell\AxDown")
$oldTarget = Join-Path $env:LOCALAPPDATA 'Programs\AxClaude'
$oldExe = Join-Path $oldTarget 'AxClaude.exe'
$oldAppsKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AxClaude'
$oldStartMenu = Join-Path $programs 'AxClaude.lnk'
# Open with values of earlier versions that this one does not write again: axPreview's own file types up to 3.7.1,
# every file since.
$oldOpenWith = @{ 'Axit.axPreview' = @('.txt', '.md', '.markdown', '.log', '.json') }
function Remove-OldOpenWith {
    foreach ($progId in $oldOpenWith.Keys) {
        foreach ($extension in $oldOpenWith[$progId]) {
            $key = "$classes\$extension\OpenWithProgids"
            if ((Test-Path -LiteralPath $key) -and (Get-ItemProperty -LiteralPath $key).PSObject.Properties[$progId]) {
                Remove-ItemProperty -LiteralPath $key -Name $progId
            }
        }
    }
}

# Everything an installation puts on the machine, this version's and the earlier ones', so that one removal clears
# all of them. The Open with values live inside the file types' own keys and are removed one by one
# (Remove-Installation).
$places = @($target, $oldTarget, $appsKey, $oldAppsKey, $oldStartMenu) + @($menus | ForEach-Object { $_.Key }) + $oldMenuKeys +
    @($apps | Where-Object { $_.OpenWith } | ForEach-Object { "$classes\$($_.ProgId)" }) +
    @($apps | ForEach-Object { $_.Start }) + @(Join-Path $bin 'axit.cmd') + @($apps | ForEach-Object { $_.Shim })

function Get-Version([string]$path) {
    (Get-Item $path).VersionInfo.FileVersion -replace '^(\d+\.\d+\.\d+).*', '$1'
}

# axStart and axClip run in the background from sign-in (docs/axstart/SPEC.md AS-1.3, AS-1.5; docs/axclip/SPEC.md
# CP-1.2): they are the apps that would always be running here, so they are stopped before anything is changed and
# started again, hidden, afterwards. Their processes are told by their verbs; axStart holds nothing unsaved, axClip
# writes a kept history as it ends. One table, the verb, the name, the settings file and what is said.
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$background = @(
    @{ Verb = 'start'; Name = 'axStart'; Settings = 'axstart.json'; What = 'the launcher'; Started = 'axStart, the launcher, runs in the background: Ctrl+Shift+Space, or the hotkey you chose, brings up its search field' },
    @{ Verb = 'clip'; Name = 'axClip'; Settings = 'axclip.json'; What = 'the clipboard history'; Started = 'axClip, the clipboard history, runs in the background: Windows+Alt+V, or the hotkey you chose, brings it up' }
)
function Stop-Background {
    foreach ($app in $background) {
        $running = @(Get-CimInstance Win32_Process -Filter "Name = 'Axit.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -match ('^"?[^"]*\\Axit[^"\\]*\.exe"?\s+' + $app.Verb + '(\s|$)') })
        foreach ($process in $running) {
            try { Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop; Wait-Process -Id $process.ProcessId -Timeout 10 -ErrorAction SilentlyContinue } catch { }
        }
        if ($running) { Say "Stopped $($app.Name), $($app.What), for the installation" }
    }
}

function Start-Background {
    foreach ($app in $background) {
        # Not when the user turned Start with Windows off in the app's settings.
        $settingsFile = Join-Path $env:APPDATA "Axit\$($app.Settings)"
        if (Test-Path -LiteralPath $settingsFile) {
            try { if ((Get-Content -LiteralPath $settingsFile -Raw | ConvertFrom-Json).startWithWindows -eq $false) { continue } } catch { }
        }
        Start-Process -FilePath $exe -ArgumentList @($app.Verb, '--hidden') -WorkingDirectory $target
        Say $app.Started
    }
}

function Test-Running {
    $names = @('Axit', 'axClaude') + @($linkNames | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) })
    [bool](Get-Process -Name $names -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe -or $_.Path -eq $oldExe -or ($_.Path -and (Split-Path -Parent $_.Path) -eq $target) })
}

# An app's Open with link (AX-2.3), made again on every install so that it is the Axit.exe just written: one still
# running is the old program file and is renamed aside as the program file is, for the next start to remove.
function Set-OpenWithLink($app) {
    if (Test-Path -LiteralPath $app.Link) {
        try { Remove-Item -LiteralPath $app.Link -Force -ErrorAction Stop }
        catch {
            $aside = Join-Path $target ("$($app.Name).old." + (Get-Date -Format 'HHmmss') + '.exe')
            Move-Item -LiteralPath $app.Link -Destination $aside -Force
        }
    }
    try {
        New-Item -ItemType HardLink -Path $app.Link -Target $exe -ErrorAction Stop | Out-Null
        return $app.Link
    } catch {
        # A drive without hard links: the ProgID names Axit.exe, and Windows' Open with lists only one of these apps.
        return $exe
    }
}

# Removes everything an installation put on the machine, whatever version made it. The settings and the logs stay.
# Windows can hold the executable for a moment after the app exits, so a folder is tried again for a few seconds.
# Registry paths go through -LiteralPath: the file keys have a * in them, which PowerShell would otherwise expand.
function Remove-Installation {
    foreach ($folder in $target, $oldTarget) {
        for ($attempt = 1; Test-Path -LiteralPath $folder; $attempt++) {
            try { Remove-Item -LiteralPath $folder -Recurse -Force }
            catch {
                if ($attempt -ge 10) { throw }
                Start-Sleep -Seconds 1
            }
        }
    }
    foreach ($item in $places) {
        if (Test-Path -LiteralPath $item) { Remove-Item -LiteralPath $item -Recurse -Force }
    }
    foreach ($app in $background) {
        $value = "Axit $($app.Name)"
        if ((Test-Path -LiteralPath $runKey) -and (Get-ItemProperty -LiteralPath $runKey).PSObject.Properties[$value]) {
            Remove-ItemProperty -LiteralPath $runKey -Name $value
        }
    }
    foreach ($app in ($apps | Where-Object { $_.OpenWith })) {
        foreach ($extension in $app.OpenWith) {
            $openWith = "$classes\$extension\OpenWithProgids"
            if ((Test-Path -LiteralPath $openWith) -and (Get-ItemProperty -LiteralPath $openWith).PSObject.Properties[$app.ProgId]) {
                Remove-ItemProperty -LiteralPath $openWith -Name $app.ProgId
            }
        }
    }
    Remove-OldOpenWith
}

if ($Uninstall) {
    # A running app holds its folder: then nothing is removed, so the Apps entry and install.cmd stay to try again.
    Stop-Background
    if (Test-Running) {
        Say "Axit is still running, so nothing was removed. Close it and uninstall again."
        exit 1
    }

    Remove-Installation
    Say "Axit was removed. Settings in $env:APPDATA\Axit and the logs in $env:LOCALAPPDATA\Axit were kept."
    exit 0
}

$sourceExe = Join-Path $source 'Axit.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) {
    Say "Axit.exe was not found next to this script in $source."
    exit 1
}
Stop-Background
$sameFolder = [string]::Equals((Resolve-Path $source).Path.TrimEnd('\'), $target.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)
if (-not $sameFolder) {
    # A previous installation, of Axit or of axClaude 1.x, is removed completely first, so nothing of it stays
    # behind; then this one is installed fresh. The older updaters come here too, after the running app has closed.
    # In place (-InPlace, the apps' own update) the app keeps running: the files are written over the old ones, the
    # running program file is renamed, since it can be renamed but not overwritten or deleted, and what earlier
    # versions left goes afterwards, so that an update leaves the machine as a fresh install would.
    $found = @($places | Where-Object { Test-Path -LiteralPath $_ })
    if ($found -and -not $InPlace) {
        if (Test-Running) {
            Say "Axit is still running, so nothing was changed. Close it and run this again."
            exit 1
        }

        $what = if (Test-Path -LiteralPath $exe) { 'Axit ' + (Get-Version $exe) }
            elseif (Test-Path -LiteralPath $oldExe) { 'axClaude ' + (Get-Version $oldExe) }
            else { 'what is left of an earlier installation' }
        Say "Removing $what before installing..."
        Remove-Installation
    }

    New-Item -ItemType Directory -Force $target | Out-Null
    $files = @('Axit.exe', 'README.md', 'LICENSE', 'install.ps1', 'install.cmd', $listingFile) + @($apps | ForEach-Object { Split-Path -Leaf $_.Icon }) | Select-Object -Unique
    foreach ($name in $files) {
        $file = Join-Path $source $name
        if (-not (Test-Path -LiteralPath $file)) { continue }
        $destination = Join-Path $target $name
        if ($InPlace -and $name -eq 'Axit.exe' -and (Test-Path -LiteralPath $destination)) {
            $old = Join-Path $target 'Axit.old.exe'
            if (Test-Path -LiteralPath $old) { try { Remove-Item -LiteralPath $old -Force } catch { } }
            if (Test-Path -LiteralPath $old) { $old = Join-Path $target ('Axit.old.' + (Get-Date -Format 'HHmmss') + '.exe') }
            Move-Item -LiteralPath $destination -Destination $old -Force
            Say "Moved the running program file aside as $(Split-Path -Leaf $old); the next start removes it."
        }
        Copy-Item -LiteralPath $file -Destination $target -Force
    }

    if ($InPlace) {
        # What earlier versions left and this one does not write again: axClaude 1.x's folder, Apps entry and Start
        # menu entry, and files in the folder this version does not ship (the program file moved aside goes at the
        # next start). The File Explorer entries of earlier versions go below, with the menu.
        foreach ($item in $oldTarget, $oldAppsKey, $oldStartMenu) {
            if (Test-Path -LiteralPath $item) { try { Remove-Item -LiteralPath $item -Recurse -Force } catch { } }
        }
        foreach ($stale in Get-ChildItem -LiteralPath $target -File | Where-Object { $files -notcontains $_.Name -and $linkNames -notcontains $_.Name -and $_.Name -notlike '*.old*.exe' }) {
            try { Remove-Item -LiteralPath $stale.FullName -Force } catch { }
        }
    }
}

# The list of apps this installation was made from, for an uninstall that cannot run the program (Get-AppListing).
[System.IO.File]::WriteAllText((Join-Path $target $listingFile), $listingJson)

# Files extracted from a downloaded zip carry the "downloaded from the internet" mark, which makes SmartScreen warn
# about an unknown publisher on every start. The installed copies lose the mark, so the warning does not come back.
try { Get-ChildItem $target -File | Unblock-File -ErrorAction Stop } catch { }

New-Item -ItemType Directory -Force $bin | Out-Null
Set-Content -Path (Join-Path $bin 'axit.cmd') -Value @('@echo off', "`"$exe`" %*") -Encoding ascii
foreach ($app in $apps) {
    Set-Content -Path $app.Shim -Value @('@echo off', "`"$exe`" $($app.Verb) %*") -Encoding ascii
}

# The commands work in a console only when their folder is on the PATH. Claude Code's installer puts it there; without
# Claude Code (docs/axit/SPEC.md G6) this install does, in the user's PATH, which needs no rights. The registry value
# is read as it is stored (unexpanded) and written back as an expandable string, so that an entry like
# %USERPROFILE%\... stays one; [Environment]::SetEnvironmentVariable would expand them all into a plain string. The
# removal leaves the PATH as it is: the folder may hold other programs' commands.
$environment = Get-Item -LiteralPath 'HKCU:\Environment'
$userPath = [string]$environment.GetValue('Path', '', 'DoNotExpandEnvironmentNames')
$onPath = @([Environment]::ExpandEnvironmentVariables($userPath) -split ';' | ForEach-Object { $_.Trim().TrimEnd('\') })
if ($onPath -notcontains $bin.TrimEnd('\')) {
    $entry = '%USERPROFILE%\.local\bin'
    $newPath = if ($userPath.Trim()) { $userPath.TrimEnd(';') + ';' + $entry } else { $entry }
    Set-ItemProperty -LiteralPath 'HKCU:\Environment' -Name 'Path' -Value $newPath -Type ExpandString
    # Running programs (Explorer, so that a console opened from the Start menu from now on has the folder) learn of
    # the change from the settings-change message, which [Environment]::SetEnvironmentVariable sends after any write,
    # the removal of a value that does not exist included: that is this line's whole effect.
    [Environment]::SetEnvironmentVariable('AXIT_PATH_CHANGED', $null, 'User')
    Say "Added $entry to your PATH: a console opened from now on finds the axit commands."
}

if (-not $NoStartMenu) {
    $shell = New-Object -ComObject WScript.Shell
    foreach ($app in $apps) {
        # Removed first: a file written over one of the same name keeps the old one's capitals, and the shortcut's
        # name is what the Start menu shows and NVDA reads ("Axit axClaude", once "Axit AxClaude").
        Remove-Item -LiteralPath $app.Start -ErrorAction SilentlyContinue
        $link = $shell.CreateShortcut($app.Start)
        $link.TargetPath = $exe
        $link.Arguments = $app.Verb
        $link.WorkingDirectory = $target
        $link.Description = $app.Description
        $link.IconLocation = "$($app.Icon),0"
        $link.Save()
    }
}

if (-not $NoContextMenu) {
    # A static cascading menu (Windows 7 and later): the submenu's key has its name in MUIVerb and an empty
    # SubCommands, and the entries are verbs under its shell subkey. AppliesTo keeps an entry to the file types it
    # names (Advanced Query Syntax, as Windows' own verbs do). The entries of earlier versions go first, on every
    # path through here, the in-place update's included.
    foreach ($key in $oldMenuKeys) {
        if (Test-Path -LiteralPath $key) { Remove-Item -LiteralPath $key -Recurse -Force }
    }

    foreach ($menu in $menus) {
        New-Item -Path $menu.Key -Force | Out-Null
        Set-ItemProperty -LiteralPath $menu.Key -Name 'MUIVerb' -Value 'Open with Axit'
        Set-ItemProperty -LiteralPath $menu.Key -Name 'Icon' -Value "`"$exe`",0"
        Set-ItemProperty -LiteralPath $menu.Key -Name 'SubCommands' -Value ''
        foreach ($app in ($apps | Where-Object { $_.Opens -contains $menu.Opens })) {
            $key = Join-Path $menu.Key "shell\$($app.Name)"
            New-Item -Path (Join-Path $key 'command') -Force | Out-Null
            Set-ItemProperty -LiteralPath $key -Name 'MUIVerb' -Value $app.Name
            Set-ItemProperty -LiteralPath $key -Name 'Icon' -Value "`"$($app.Icon)`",0"
            if ($app.Types) {
                Set-ItemProperty -LiteralPath $key -Name 'AppliesTo' -Value (($app.Types | ForEach-Object { "System.FileExtension:=`"$_`"" }) -join ' OR ')
            }
            Set-ItemProperty -LiteralPath (Join-Path $key 'command') -Name '(default)' -Value "`"$exe`" $($app.Verb) `"$($menu.Argument)`""
        }
    }

    # The ProgID that Open with and Default apps know an app by (AX-2.3, B-9): the user makes it the default, never
    # this script. The type * is every file (HKCU\Software\Classes\*\OpenWithProgids).
    Remove-OldOpenWith
    foreach ($app in ($apps | Where-Object { $_.OpenWith })) {
        $program = Set-OpenWithLink $app
        $progIdKey = "$classes\$($app.ProgId)"
        New-Item -Path $progIdKey -Force | Out-Null
        Set-ItemProperty -LiteralPath $progIdKey -Name '(default)' -Value $app.TypeName
        Set-ItemProperty -LiteralPath $progIdKey -Name 'FriendlyTypeName' -Value $app.TypeName
        New-Item -Path (Join-Path $progIdKey 'DefaultIcon') -Force | Out-Null
        Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'DefaultIcon') -Name '(default)' -Value $app.Icon
        New-Item -Path (Join-Path $progIdKey 'Application') -Force | Out-Null
        Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'Application') -Name 'ApplicationName' -Value $app.Name
        Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'Application') -Name 'ApplicationIcon' -Value $app.Icon
        New-Item -Path (Join-Path $progIdKey 'shell\open\command') -Force | Out-Null
        Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'shell\open\command') -Name '(default)' -Value "`"$program`" $($app.Verb) `"%1`""
        foreach ($extension in $app.OpenWith) {
            $openWith = "$classes\$extension\OpenWithProgids"
            # Made only when missing: New-Item -Force on an existing key empties it, which took the other programs'
            # values of the type with it, and axDown's when axPreview came next (up to 3.7.1; measured 2026-10-24).
            if (-not (Test-Path -LiteralPath $openWith)) { New-Item -Path $openWith -Force | Out-Null }
            New-ItemProperty -LiteralPath $openWith -Name $app.ProgId -Value ([byte[]]@()) -PropertyType None -Force | Out-Null
        }
    }
}

New-Item -Path $appsKey -Force | Out-Null
$uninstallCmd = Join-Path $target 'install.cmd'
$appsUninstall = if (Test-Path -LiteralPath $uninstallCmd) { "`"$uninstallCmd`" -Uninstall" }
    else { "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$(Join-Path $target 'install.ps1')`" -Uninstall" }
$entries = [ordered]@{
    DisplayName = 'Axit'
    DisplayVersion = (Get-Version $exe)
    Publisher = 'Dr. Kyle Keane'
    DisplayIcon = "`"$exe`",0"
    InstallLocation = $target
    UninstallString = $appsUninstall
    URLInfoAbout = 'https://github.com/KyleKeane/Axit'
}
foreach ($name in $entries.Keys) { Set-ItemProperty -LiteralPath $appsKey -Name $name -Value $entries[$name] }
foreach ($name in 'NoModify', 'NoRepair') { New-ItemProperty -LiteralPath $appsKey -Name $name -Value 1 -PropertyType DWord -Force | Out-Null }
New-ItemProperty -LiteralPath $appsKey -Name 'EstimatedSize' -Value ([int]((Get-Item $exe).Length / 1KB)) -PropertyType DWord -Force | Out-Null

$names = @($apps | ForEach-Object { $_.Name })
Say "Installed Axit to $target"
Say "Settings, Apps: Axit, with Uninstall"
if (-not $NoStartMenu) { Say "Start menu: $(($names | ForEach-Object { "Axit $_" }) -join ', ') (press the Windows key and type axit to list them)" }
if (-not $NoContextMenu) {
    $onFolders = @($apps | Where-Object { $_.Opens -contains 'folders' } | ForEach-Object { $_.Name }) -join ', '
    $onFiles = @($apps | Where-Object { $_.Opens -contains 'files' } | ForEach-Object { if ($_.Types) { "$($_.Name) on $($_.Types -join ', ')" } else { "$($_.Name) on every file" } }) -join ', '
    $openWith = @($apps | Where-Object { $_.OpenWith } | ForEach-Object { "$($_.Name) for $(if ($_.OpenWith -contains '*') { 'every file' } else { $_.OpenWith -join ', ' })" }) -join '; '
    Say "File Explorer: Open with Axit in the right-click menu: $onFolders on folders; $onFiles. Under Open with: $openWith"
}
Say "Console: $(($names | ForEach-Object { $_.ToLower() }) -join ', '), each taking a folder or file, or axit <folder or file> (shims in $bin)"
if (-not (($env:Path -split ';') -contains $bin)) {
    Say "Note: $bin is not on PATH in this console. Open a new console, or add it, before using the commands."
}

Start-Background

if ($Start) {
    $arguments = @('claude', "`"$Start`"")
    if ($ContinueConversation) { $arguments += @('--', '--continue') }
    Say "Starting axClaude on $Start"
    Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $target
}

if ($StartFile) {
    Say "Starting axDown on $StartFile"
    Start-Process -FilePath $exe -ArgumentList @('down', "`"$StartFile`"") -WorkingDirectory $target
}
