<#
.SYNOPSIS
  Builds Axit and starts axMedia: run.ps1 with -App media. Every other argument goes through unchanged. Without a path
  it opens with an empty queue.

.EXAMPLE
  .\run-axmedia.ps1                         # an empty queue
  .\run-axmedia.ps1 C:\Music\Dune           # the folder's sound and video files from the first
  .\run-axmedia.ps1 C:\Music\Dune\03.mp3    # the file, with the folder's others after it
  .\run-axmedia.ps1 -NoBuild                # without building
#>
& "$PSScriptRoot\run.ps1" -App media @args
exit $LASTEXITCODE
