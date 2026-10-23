param(
    [string]$Build = (Join-Path $PSScriptRoot '..\Builds\Windows\RailArena.exe'),
    [string]$Output = (Join-Path $PSScriptRoot '..\Validation\mechanics-run.json')
)
$ErrorActionPreference = 'Stop'
$railBuildPath = (Resolve-Path -LiteralPath $Build).Path
$railOutputPath = [IO.Path]::GetFullPath($Output)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($railOutputPath)) | Out-Null
$railArguments = @('--validate-mechanics', '--validation-output', ('"' + $railOutputPath + '"'),
    '--validation-owner-pid', $PID, '--validation-timeout', 120,
    '-logFile', ('"' + $railOutputPath + '.log"'), '-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0')
$railStarted = [DateTime]::UtcNow
$railProcess = Start-Process -FilePath $railBuildPath -ArgumentList $railArguments -WindowStyle Hidden -PassThru
try {
    if (-not $railProcess.WaitForExit(120000)) { throw 'Standalone mechanics validation timed out after 120 seconds.' }
    if (-not (Test-Path -LiteralPath $railOutputPath) -or (Get-Item -LiteralPath $railOutputPath).LastWriteTimeUtc -lt $railStarted) {
        throw "No fresh mechanics report. Inspect $railOutputPath.log"
    }
    $railReport = Get-Content -LiteralPath $railOutputPath -Raw | ConvertFrom-Json
    $railReport | Format-List
    if (-not $railReport.passed -or $railProcess.ExitCode -ne 0) { throw "Standalone mechanics validation failed. Inspect $railOutputPath" }
} finally {
    if (-not $railProcess.HasExited) { $railProcess.Kill() }
}
