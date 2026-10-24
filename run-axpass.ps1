<#
.SYNOPSIS
  Builds Axit and starts axPass: run.ps1 with -App pass. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axpass.ps1 manual_tests\axpass\test.kdbx   # build and start axPass on the test database
  .\run-axpass.ps1 -NoBuild                        # without building
#>
& "$PSScriptRoot\run.ps1" -App pass @args
exit $LASTEXITCODE
