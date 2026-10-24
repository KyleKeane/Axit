<#
.SYNOPSIS
  Builds Axit and starts axCalc: run.ps1 with -App calc. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axcalc.ps1                          # build and start axCalc
  .\run-axcalc.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App calc @args
exit $LASTEXITCODE
