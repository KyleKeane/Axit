<#
.SYNOPSIS
  Builds Axit and starts axClip: run.ps1 with -App clip. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axclip.ps1                          # build and start axClip
  .\run-axclip.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App clip @args
exit $LASTEXITCODE
