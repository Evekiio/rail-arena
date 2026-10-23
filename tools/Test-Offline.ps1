param(
    [string]$Build = (Join-Path $PSScriptRoot '..\Builds\Windows\RailArena.exe'),
    [string]$Output = (Join-Path $PSScriptRoot '..\Validation\offline-run.json'),
    [ValidateRange(45,1800)][int]$Duration = 45,
    [ValidateRange(0,2)][int]$Difficulty = 1,
    [ValidateRange(1,7)][int]$Bots = 7
)
$ErrorActionPreference = 'Stop'
$railBuildPath = (Resolve-Path -LiteralPath $Build).Path
$railOutputPath = [IO.Path]::GetFullPath($Output)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($railOutputPath)) | Out-Null
$railArguments = @('--validate-offline', '--validation-output', ('"' + $railOutputPath + '"'),
    '--validation-seconds', $Duration, '--validation-difficulty', $Difficulty, '--validation-bots', $Bots,
    '--validation-owner-pid', $PID, '--validation-timeout', ($Duration + 60),
    '-logFile', ('"' + $railOutputPath + '.log"'), '-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0')
$railStarted = [DateTime]::UtcNow
$railProcess = Start-Process -FilePath $railBuildPath -ArgumentList $railArguments -WindowStyle Hidden -PassThru
try {
    if (-not $railProcess.WaitForExit(($Duration + 60) * 1000)) { throw 'Offline validation process timed out.' }
    if (-not (Test-Path -LiteralPath $railOutputPath)) { throw "The game did not produce a validation report. Inspect $railOutputPath.log" }
    if ((Get-Item -LiteralPath $railOutputPath).LastWriteTimeUtc -lt $railStarted) { throw "The game did not update the validation report; the existing report is stale." }
    $railReport = Get-Content -LiteralPath $railOutputPath -Raw | ConvertFrom-Json
    $railReport | Format-List
    if (-not $railReport.passed -or $railProcess.ExitCode -ne 0) { throw "Offline smoke/soak checks failed. See $railOutputPath" }
} finally {
    if (-not $railProcess.HasExited) { $railProcess.Kill() }
}
