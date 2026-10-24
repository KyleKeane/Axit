<#
.SYNOPSIS
  Builds Axit and starts axProcess: run.ps1 with -App process. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axprocess.ps1                          # build and start axProcess
  .\run-axprocess.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App process @args
exit $LASTEXITCODE
