<#
.SYNOPSIS
  Builds Axit and starts one of its apps: axClaude on a project folder (the default), axDown on a file, or axJSON on a file.

.EXAMPLE
  .\run.ps1                                  # axClaude on the current folder
  .\run.ps1 C:\src\myproject                 # axClaude on that folder
  .\run.ps1 C:\src\myproject -- --resume     # extra arguments go to claude (default: --continue)
  .\run.ps1 C:\src\myproject -New            # a new conversation (PowerShell swallows a bare --)
  .\run.ps1 -App down C:\notes\todo.md       # axDown on that file (run-axdown.ps1 says the same)
  .\run.ps1 -App down                        # axDown with an empty document (run-axdown.ps1 opens the tutorial file instead)
  .\run.ps1 -App json C:\x\settings.json     # axJSON on that file (run-axjson.ps1 says the same)
  .\run.ps1 -App git C:\src\myproject        # axGIT on the repository that holds the folder (run-axgit.ps1 says the same)
  .\run.ps1 -App terminal C:\src\myproject   # axTerminal with a shell in that folder (run-axterminal.ps1 says the same)
  .\run.ps1 -App media C:\Music\Dune         # axMedia on the folder's sound and video files (run-axmedia.ps1 says the same)
  .\run.ps1 -App preview C:\notes\README.md  # axPreview on the file, read-only (run-axpreview.ps1 says the same)
  .\run.ps1 -App start                       # axStart, the launcher: its field shown; it keeps running (run-axstart.ps1 says the same)
  .\run.ps1 -NoBuild                         # skip the build, just start
  .\run.ps1 -Test                            # run the unit tests instead (test.ps1: every test project, in Release)

  run-axclaude.ps1, run-axdown.ps1, run-axjson.ps1, run-axgit.ps1, run-axterminal.ps1, run-axfile.ps1, run-axmedia.ps1, run-axpreview.ps1 and run-axstart.ps1 next to this script pass -App for you and take the same other arguments.
  If PowerShell refuses to run scripts: powershell -ExecutionPolicy Bypass -File .\run.ps1
  Close a running Axit window first: the build writes into the folder it holds open. axStart, which runs in the
  background, is ended before the build and started again hidden after it.
#>
[CmdletBinding()]
param(
    # The app to start: the verb Axit.exe takes (docs/axit/SPEC.md AX-1). axClaude unless said otherwise.
    [ValidateSet('claude', 'down', 'json', 'git', 'terminal', 'file', 'media', 'preview', 'start')]
    [string]$App = 'claude',

    # axClaude's project folder (default: the current folder), axDown's or axJSON's file (default: an empty document),
    # a folder or file inside axGIT's repository (default: the last one opened), axTerminal's folder, axFile's folder
    # or file (default: the last folder opened), axMedia's file or folder (default: an empty queue), or axPreview's
    # file (default: an empty window). axStart takes none.
    [Parameter(Position = 0)]
    [Alias('Project')]
    [string]$Path,

    [switch]$NoBuild,
    [switch]$Test,
    [switch]$New,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ClaudeArgs
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if ($Test) {
    # test.ps1: every test project one by one, in Release, as the workflows run them (the solution would build into
    # the bin\Debug a running Axit holds open).
    & (Join-Path $root 'test.ps1')
    exit $LASTEXITCODE
}

# The .NET version is Directory.Build.props' AxitDotNet, the one place it is set.
$dotnet = ([xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)).Project.PropertyGroup.AxitDotNet
$exe = Join-Path $root "src\Axit\bin\Debug\$dotnet-windows\Axit.exe"

if (-not $NoBuild) {
    # axStart started from this build keeps running in the background after every window has closed, and holds the
    # folder the build writes into: it is ended before the build and started again, hidden, from the new one after,
    # so that the hotkey runs the new code (asked 2026-10-23). It keeps no unsaved state; its settings save as they change.
    $launcher = @(Get-CimInstance Win32_Process -Filter "Name = 'Axit.exe'" |
        Where-Object { $_.ExecutablePath -eq $exe -and $_.CommandLine -match '\sstart(\s|$)' })
    foreach ($process in $launcher) {
        Write-Host "Ending axStart (process $($process.ProcessId)), which holds the build folder; it starts again after the build."
        Stop-Process -Id $process.ProcessId -Force
        Wait-Process -Id $process.ProcessId -Timeout 10 -ErrorAction SilentlyContinue
    }

    Write-Host "Building Axit..."
    dotnet build "$root\src\Axit\Axit.csproj" -c Debug -nologo -v q
    $built = $LASTEXITCODE
    if ($launcher.Count -gt 0 -and (Test-Path $exe)) {
        Write-Host "Starting axStart again, hidden."
        Start-Process -FilePath $exe -ArgumentList 'start', '--hidden' -WindowStyle Hidden
    }

    if ($built -ne 0) {
        Write-Host "Build failed."
        exit $built
    }
}
if (-not (Test-Path $exe)) {
    Write-Host "Axit.exe was not found at $exe. Run without -NoBuild first."
    exit 1
}

if ($App -ne 'claude') {
    $name = switch ($App) { 'down' { 'axDown' } 'json' { 'axJSON' } 'terminal' { 'axTerminal' } 'file' { 'axFile' } 'media' { 'axMedia' } 'preview' { 'axPreview' } 'start' { 'axStart' } default { 'axGIT' } }
    if ($Path -and $App -ne 'start') {
        Write-Host "Starting $name on $Path"
        & $exe $App $Path
    } else {
        Write-Host "Starting $name"
        & $exe $App
    }
    exit $LASTEXITCODE
}

if (-not $Path) { $Path = (Get-Location).Path }
$Path = (Resolve-Path $Path).Path
Write-Host "Starting axClaude on $Path"
if ($New) {
    # A bare -- never reaches the app: PowerShell takes it as the end of the script's own parameters.
    & $exe claude $Path '--'
} elseif ($ClaudeArgs -and $ClaudeArgs.Count -gt 0) {
    & $exe claude $Path '--' @ClaudeArgs
} else {
    & $exe claude $Path
}
