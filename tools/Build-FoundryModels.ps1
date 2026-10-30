param(
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',
    [ValidateRange(60,3600)][int]$TimeoutSeconds = 1500
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$validation = Join-Path $repo 'Validation'
New-Item -ItemType Directory -Force -Path $validation | Out-Null
$builder = Join-Path $PSScriptRoot 'build_foundry_models.py'
$job = Start-Process -FilePath $Blender -WindowStyle Hidden -PassThru `
    -ArgumentList @('--background','--threads','4','--python-exit-code','1','--python',"`"$builder`"",'--',"`"$repo`"") `
    -RedirectStandardOutput (Join-Path $validation 'Foundry-models.log') `
    -RedirectStandardError (Join-Path $validation 'Foundry-models-error.log')
try {
    if (-not $job.WaitForExit($TimeoutSeconds * 1000)) {
        throw "Foundry modeling exceeded the $TimeoutSeconds second bound."
    }
    $job.Refresh()
    if ($job.ExitCode -ne 0) { throw "Blender modeling failed with exit code $($job.ExitCode)." }
} finally {
    if (-not $job.HasExited) { Stop-Process -Id $job.Id -Force; $job.WaitForExit() }
    $job.Dispose()
}
& python (Join-Path $PSScriptRoot 'package_foundry_models.py')
if ($LASTEXITCODE -ne 0) { throw 'Foundry review packaging failed.' }
& python (Join-Path $PSScriptRoot 'audit_foundry_models.py')
if ($LASTEXITCODE -ne 0) { throw 'Foundry export audit failed.' }
