<#
.SYNOPSIS
  Builds Axit and starts AxStart, the program launcher: run.ps1 with -App start. Its search field shows, and it keeps
  running in the background with its hotkey (Windows+Alt+Space unless changed) until AxStart, Quit AxStart. An installed
  AxStart already running takes the start instead: quit it first to try a development build.

.EXAMPLE
  .\run-axstart.ps1               # build, then the search field
  .\run-axstart.ps1 -NoBuild      # without building
#>
& "$PSScriptRoot\run.ps1" -App start @args
exit $LASTEXITCODE
