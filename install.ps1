<#
.SYNOPSIS
  Installs Axit for the current user, or removes it with -Uninstall. No administrator rights are needed.

.DESCRIPTION
  Run it from the folder that holds Axit.exe: the extracted zip, or publish\win-x64 after publish.ps1.
  Axit is one program with several apps inside (docs/axit/SPEC.md AX-2). It copies Axit to
  %LOCALAPPDATA%\Programs\Axit and creates the ways to start each app:
    - a Start menu entry per app, "Axit AxClaude" (type axc in the Start menu to find it),
    - an "Open in AxClaude" entry in the right-click menu of folders in File Explorer (and of a folder's background),
    - the console commands axit (a folder opens AxClaude, a file AxDown) and axclaude: one-line shims in
      %USERPROFILE%\.local\bin, the folder that Claude Code's installer already puts on PATH.
  It also lists Axit in Settings, Apps (Add or remove programs), whose Uninstall runs install.cmd -Uninstall.
  -Uninstall removes all of that. The settings in %APPDATA%\Axit and the logs in %LOCALAPPDATA%\Axit stay.
  An installation of AxClaude 1.x, from before the bundle, is removed completely first; its settings file stays in
  %APPDATA%\AxClaude, and AxClaude copies it on its first start under Axit.

.EXAMPLE
  .\install.ps1                 # install or update
  .\install.ps1 -Uninstall      # remove
  .\install.ps1 -NoContextMenu  # install without the File Explorer entries
  .\install.ps1 -NoStartMenu    # install without the Start menu entries

  install.cmd, next to this script, runs it with the execution policy bypassed: double-click it, or install.cmd -Uninstall.
  Downloaded on its own from the releases page, install.cmd fetches the latest release and runs this script from it.

  The apps' own Update now (Help menu) runs the downloaded version's copy of this script as
    install.ps1 -WaitForProcess <pid> -Start <folder> [-ContinueConversation] -LogFile <file>
  which waits for the running app to end, installs, and starts AxClaude on the folder. AxClaude 1.7.0 calls it the
  same way (AX-2.7), which is how an installed AxClaude becomes Axit.
#>
[CmdletBinding()]
param(
    [switch]$Uninstall,
    [switch]$NoContextMenu,
    [switch]$NoStartMenu,
    [int]$WaitForProcess,
    [string]$Start,
    [switch]$ContinueConversation,
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
    Say "Axit was not installed: $($_.Exception.Message)"
    exit 1
}

if ($WaitForProcess) {
    Say "Waiting for the app (process $WaitForProcess) to close..."
    try { Wait-Process -Id $WaitForProcess -Timeout 120 -ErrorAction Stop } catch { }
    if (Get-Process -Id $WaitForProcess -ErrorAction SilentlyContinue) {
        Say "The app is still running after two minutes. The update was not installed; start it from the Help menu again."
        exit 1
    }
}

$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $env:LOCALAPPDATA 'Programs\Axit'
$exe = Join-Path $target 'Axit.exe'
$bin = Join-Path $env:USERPROFILE '.local\bin'
$programs = [Environment]::GetFolderPath('Programs')
# The entry in Settings, Apps (Add or remove programs) for this user; its Uninstall runs install.cmd -Uninstall.
$appsKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Axit'

# The ways to start each app: console command to the verb Axit.exe takes (none for axit itself, which decides by
# the path), Start menu entry to its verb, and the File Explorer keys. A new app adds its lines here (AX-2).
$shims = [ordered]@{ 'axit.cmd' = ''; 'axclaude.cmd' = 'claude' }
$startMenu = [ordered]@{ 'Axit AxClaude.lnk' = 'claude' }
$shellKeys = @(
    'HKCU:\Software\Classes\Directory\shell\AxClaude',
    'HKCU:\Software\Classes\Directory\Background\shell\AxClaude'
)

# What AxClaude 1.x put on the machine under its own names; the rest of its places are the same as Axit's.
$oldTarget = Join-Path $env:LOCALAPPDATA 'Programs\AxClaude'
$oldExe = Join-Path $oldTarget 'AxClaude.exe'
$oldAppsKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AxClaude'
$oldStartMenu = Join-Path $programs 'AxClaude.lnk'

# Everything an installation puts on the machine, this version's and 1.x's, so that one removal clears both.
$places = @($target, $oldTarget, $appsKey, $oldAppsKey, $oldStartMenu) + $shellKeys +
    @($startMenu.Keys | ForEach-Object { Join-Path $programs $_ }) +
    @($shims.Keys | ForEach-Object { Join-Path $bin $_ })

function Get-Version([string]$path) {
    (Get-Item $path).VersionInfo.FileVersion -replace '^(\d+\.\d+\.\d+).*', '$1'
}

function Test-Running {
    [bool](Get-Process -Name Axit, AxClaude -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe -or $_.Path -eq $oldExe })
}

# Removes everything an installation put on the machine, whatever version made it. The settings and the logs stay.
# Windows can hold the executable for a moment after the app exits, so a folder is tried again for a few seconds.
function Remove-Installation {
    foreach ($folder in $target, $oldTarget) {
        for ($attempt = 1; Test-Path $folder; $attempt++) {
            try { Remove-Item $folder -Recurse -Force }
            catch {
                if ($attempt -ge 10) { throw }
                Start-Sleep -Seconds 1
            }
        }
    }
    foreach ($item in $places) {
        if (Test-Path $item) { Remove-Item $item -Recurse -Force }
    }
}

if ($Uninstall) {
    # A running app holds its folder: then nothing is removed, so the Apps entry and install.cmd stay to try again.
    if (Test-Running) {
        Say "Axit is still running, so nothing was removed. Close it and uninstall again."
        exit 1
    }

    Remove-Installation
    Say "Axit was removed. Settings in $env:APPDATA\Axit and the logs in $env:LOCALAPPDATA\Axit were kept."
    exit 0
}

$sourceExe = Join-Path $source 'Axit.exe'
if (-not (Test-Path $sourceExe)) {
    Say "Axit.exe was not found next to this script in $source."
    exit 1
}

$sameFolder = [string]::Equals((Resolve-Path $source).Path.TrimEnd('\'), $target.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)
if (-not $sameFolder) {
    # A previous installation, of Axit or of AxClaude 1.x, is removed completely first, so nothing of it stays
    # behind; then this one is installed fresh. The updater comes here too, after the running app has closed.
    $found = @($places | Where-Object { Test-Path $_ })
    if ($found) {
        if (Test-Running) {
            Say "Axit is still running, so nothing was changed. Close it and run this again."
            exit 1
        }

        $what = if (Test-Path $exe) { 'Axit ' + (Get-Version $exe) }
            elseif (Test-Path $oldExe) { 'AxClaude ' + (Get-Version $oldExe) }
            else { 'what is left of an earlier installation' }
        Say "Removing $what before installing..."
        Remove-Installation
    }

    New-Item -ItemType Directory -Force $target | Out-Null
    foreach ($name in 'Axit.exe', 'README.md', 'LICENSE', 'install.ps1', 'install.cmd') {
        $file = Join-Path $source $name
        if (Test-Path $file) { Copy-Item $file $target -Force }
    }
}

# Files extracted from a downloaded zip carry the "downloaded from the internet" mark, which makes SmartScreen warn
# about an unknown publisher on every start. The installed copies lose the mark, so the warning does not come back.
try { Get-ChildItem $target -File | Unblock-File -ErrorAction Stop } catch { }

New-Item -ItemType Directory -Force $bin | Out-Null
foreach ($name in $shims.Keys) {
    $verb = $shims[$name]
    $line = if ($verb) { "`"$exe`" $verb %*" } else { "`"$exe`" %*" }
    Set-Content -Path (Join-Path $bin $name) -Value @('@echo off', $line) -Encoding ascii
}

if (-not $NoStartMenu) {
    $shell = New-Object -ComObject WScript.Shell
    foreach ($name in $startMenu.Keys) {
        $link = $shell.CreateShortcut((Join-Path $programs $name))
        $link.TargetPath = $exe
        $link.Arguments = $startMenu[$name]
        $link.WorkingDirectory = $target
        $link.Description = 'AxClaude, an accessible front end for Claude Code'
        $link.IconLocation = "$exe,0"
        $link.Save()
    }
}

if (-not $NoContextMenu) {
    foreach ($key in $shellKeys) {
        New-Item -Path $key -Force | Out-Null
        Set-ItemProperty -Path $key -Name '(default)' -Value 'Open in AxClaude'
        Set-ItemProperty -Path $key -Name 'Icon' -Value "`"$exe`",0"
        $command = Join-Path $key 'command'
        New-Item -Path $command -Force | Out-Null
        $argument = if ($key -like '*Background*') { '%V' } else { '%1' }
        Set-ItemProperty -Path $command -Name '(default)' -Value "`"$exe`" claude `"$argument`""
    }
}

New-Item -Path $appsKey -Force | Out-Null
$uninstallCmd = Join-Path $target 'install.cmd'
$appsUninstall = if (Test-Path $uninstallCmd) { "`"$uninstallCmd`" -Uninstall" }
    else { "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$(Join-Path $target 'install.ps1')`" -Uninstall" }
$entries = [ordered]@{
    DisplayName = 'Axit'
    DisplayVersion = (Get-Version $exe)
    Publisher = 'Dr. Kyle Keane'
    DisplayIcon = "`"$exe`",0"
    InstallLocation = $target
    UninstallString = $appsUninstall
    URLInfoAbout = 'https://github.com/KyleKeane/AxClaude'
}
foreach ($name in $entries.Keys) { Set-ItemProperty -Path $appsKey -Name $name -Value $entries[$name] }
foreach ($name in 'NoModify', 'NoRepair') { New-ItemProperty -Path $appsKey -Name $name -Value 1 -PropertyType DWord -Force | Out-Null }
New-ItemProperty -Path $appsKey -Name 'EstimatedSize' -Value ([int]((Get-Item $exe).Length / 1KB)) -PropertyType DWord -Force | Out-Null

Say "Installed Axit to $target"
Say "Settings, Apps: Axit, with Uninstall"
if (-not $NoStartMenu) { Say "Start menu: Axit AxClaude (type axc to find it)" }
if (-not $NoContextMenu) { Say "File Explorer: right-click a folder, Open in AxClaude" }
Say "Console: axclaude, or axclaude C:\path\to\project; axit C:\path opens the app for that path (shims in $bin)"
if (-not (($env:Path -split ';') -contains $bin)) {
    Say "Note: $bin is not on PATH in this console. Open a new console, or add it, before using the commands."
}

if ($Start) {
    $arguments = @('claude', "`"$Start`"")
    if ($ContinueConversation) { $arguments += @('--', '--continue') }
    Say "Starting AxClaude on $Start"
    Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $target
}
