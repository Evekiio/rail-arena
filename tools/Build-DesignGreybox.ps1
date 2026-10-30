param(
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',
    [ValidateRange(30, 900)][int]$TimeoutSeconds = 240
)
$ErrorActionPreference = 'Stop'
$designRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$builderPath = Join-Path $PSScriptRoot 'build_design_greybox.py'
$validationPath = Join-Path $designRoot 'Validation'
New-Item -ItemType Directory -Path $validationPath -Force | Out-Null
$ownedJob = $null
try {
    $ownedJob = Start-Process -FilePath $Blender -ArgumentList @(
        '--background', '--factory-startup', '--threads', '4', '--python-exit-code', '1',
        '--python', ('"' + $builderPath + '"'), '--', ('"' + $designRoot + '"')
    ) -WorkingDirectory $designRoot -WindowStyle Hidden -PassThru `
      -RedirectStandardOutput (Join-Path $validationPath 'design-greybox.log') `
      -RedirectStandardError (Join-Path $validationPath 'design-greybox-error.log')
    Write-Output "Owned headless Blender PID: $($ownedJob.Id)"
    if (-not $ownedJob.WaitForExit($TimeoutSeconds * 1000)) {
        throw "Headless design build exceeded $TimeoutSeconds seconds."
    }
    $ownedJob.Refresh()
    if ($ownedJob.ExitCode -ne 0) { throw "Blender failed with exit code $($ownedJob.ExitCode). See Validation logs." }
    if (-not (Select-String -LiteralPath (Join-Path $validationPath 'design-greybox.log') -SimpleMatch 'DESIGN_GREYBOX_COMPLETE' -Quiet)) {
        throw 'Blender did not emit the design completion marker.'
    }
} finally {
    if ($null -ne $ownedJob) {
        $ownedJob.Refresh()
        if (-not $ownedJob.HasExited) {
            Stop-Process -Id $ownedJob.Id -Force
            $ownedJob.WaitForExit()
        }
        $ownedJob.Dispose()
    }
}
