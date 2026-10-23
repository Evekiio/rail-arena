param(
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',
    [switch]$RefreshExportsAndCover
)
$ErrorActionPreference = 'Stop'
$benchmarkRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$benchmarkProcess = $null
$benchmarkArguments = @('--background','--factory-startup','--threads','4','--python-exit-code','1',
    '--python',('"' + (Join-Path $PSScriptRoot 'build_production_benchmarks.py') + '"'),'--',('"' + $benchmarkRoot + '"'))
if ($RefreshExportsAndCover) { $benchmarkArguments += '--refresh-exports-cover' }
try {
    $benchmarkProcess = Start-Process -FilePath $Blender -ArgumentList $benchmarkArguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $benchmarkRoot 'Validation/production-benchmarks-final.log') `
        -RedirectStandardError (Join-Path $benchmarkRoot 'Validation/production-benchmarks-final-error.log')
    Write-Output "Owned Blender PID: $($benchmarkProcess.Id)"
    if (-not $benchmarkProcess.WaitForExit(480000)) { throw 'Benchmark Blender job exceeded eight minutes.' }
    $benchmarkProcess.Refresh()
    if ($benchmarkProcess.ExitCode -ne 0) { throw "Benchmark Blender exit $($benchmarkProcess.ExitCode)" }
    if (-not (Select-String -LiteralPath (Join-Path $benchmarkRoot 'Validation/production-benchmarks-final.log') -SimpleMatch 'PRODUCTION_BENCHMARKS_COMPLETE' -Quiet)) {
        throw 'Missing completion marker.'
    }
} finally {
    if ($null -ne $benchmarkProcess) {
        $benchmarkProcess.Refresh()
        if (-not $benchmarkProcess.HasExited) { Stop-Process -Id $benchmarkProcess.Id -Force; $benchmarkProcess.WaitForExit() }
        $benchmarkProcess.Dispose()
    }
}
