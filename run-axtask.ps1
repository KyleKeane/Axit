<#
.SYNOPSIS
  Builds Axit and starts axTask: run.ps1 with -App task. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axtask.ps1 manual_tests\axtask\tasks   # build and start axTask on the test folder
  .\run-axtask.ps1 -NoBuild                    # without building
#>
& "$PSScriptRoot\run.ps1" -App task @args
exit $LASTEXITCODE
