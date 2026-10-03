<#
.SYNOPSIS
  Builds Axit and starts one of its apps: AxClaude on a project folder (the default), or AxDown on a file.

.EXAMPLE
  .\run.ps1                                  # AxClaude on the current folder
  .\run.ps1 C:\src\myproject                 # AxClaude on that folder
  .\run.ps1 C:\src\myproject -- --resume     # extra arguments go to claude (default: --continue)
  .\run.ps1 C:\src\myproject -New            # a new conversation (PowerShell swallows a bare --)
  .\run.ps1 -App down C:\notes\todo.md       # AxDown on that file (run-axdown.ps1 says the same)
  .\run.ps1 -App down                        # AxDown with an empty document
  .\run.ps1 -NoBuild                         # skip the build, just start
  .\run.ps1 -Test                            # run the unit tests instead

  run-axclaude.ps1 and run-axdown.ps1 next to this script pass -App for you and take the same other arguments.
  If PowerShell refuses to run scripts: powershell -ExecutionPolicy Bypass -File .\run.ps1
  Close a running Axit first: the build writes into the folder it holds open.
#>
[CmdletBinding()]
param(
    # The app to start: the verb Axit.exe takes (docs/axit/SPEC.md AX-1). AxClaude unless said otherwise.
    [ValidateSet('claude', 'down')]
    [string]$App = 'claude',

    # AxClaude's project folder (default: the current folder), or AxDown's file (default: an empty document).
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
    dotnet test "$root\Axit.sln" -nologo
    exit $LASTEXITCODE
}

if (-not $NoBuild) {
    Write-Host "Building Axit..."
    dotnet build "$root\src\Axit\Axit.csproj" -c Debug -nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed."
        exit $LASTEXITCODE
    }
}

$exe = Join-Path $root 'src\Axit\bin\Debug\net10.0-windows\Axit.exe'
if (-not (Test-Path $exe)) {
    Write-Host "Axit.exe was not found at $exe. Run without -NoBuild first."
    exit 1
}

if ($App -eq 'down') {
    if ($Path) {
        Write-Host "Starting AxDown on $Path"
        & $exe down $Path
    } else {
        Write-Host "Starting AxDown"
        & $exe down
    }
    exit $LASTEXITCODE
}

if (-not $Path) { $Path = (Get-Location).Path }
$Path = (Resolve-Path $Path).Path
Write-Host "Starting AxClaude on $Path"
if ($New) {
    # A bare -- never reaches the app: PowerShell takes it as the end of the script's own parameters.
    & $exe claude $Path '--'
} elseif ($ClaudeArgs -and $ClaudeArgs.Count -gt 0) {
    & $exe claude $Path '--' @ClaudeArgs
} else {
    & $exe claude $Path
}
