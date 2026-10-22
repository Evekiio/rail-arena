param(
    [string]$Build = (Join-Path $PSScriptRoot '..\Builds\Windows\RailArena.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\Validation\Duel'),
    [int]$Port = 17777,
    [int]$Duration = 45,
    [switch]$FullMatch,
    [int]$ClientPort = 0
)
$ErrorActionPreference = 'Stop'
$railBuildPath = (Resolve-Path -LiteralPath $Build).Path
$railOutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($railOutputDirectory) | Out-Null
$railStarted = [DateTime]::UtcNow
$railProcesses = @()
$railReports = @()
if ($FullMatch -and $Duration -eq 45) { $Duration = 720 }
if ($Duration -lt 20 -or $Duration -gt 900) { throw 'Duration must be 20 to 900 seconds.' }
try {
    foreach ($railRole in @('host', 'client')) {
        $railOutput = Join-Path $railOutputDirectory ($railRole + '.json')
        $railRolePort = if ($railRole -eq 'client' -and $ClientPort -gt 0) { $ClientPort } else { $Port }
        $railArguments = @('--validate-duel-role', $railRole, '--validation-port', $railRolePort,
            '--validation-duration', $Duration, '--validation-output', ('"' + $railOutput + '"'),
            '-logFile', ('"' + $railOutput + '.log"'), '-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0')
        if ($FullMatch) { $railArguments += '--validation-full-match' }
        $railProcesses += Start-Process -FilePath $railBuildPath -ArgumentList $railArguments -WindowStyle Hidden -PassThru
        if ($railRole -eq 'host') { Start-Sleep -Milliseconds 1000 }
    }
    foreach ($railProcess in $railProcesses) {
        if (-not $railProcess.WaitForExit(($Duration + 60) * 1000)) { throw 'Duel validation process timed out.' }
    }
    foreach ($railRole in @('host', 'client')) {
        $railOutput = Join-Path $railOutputDirectory ($railRole + '.json')
        if (-not (Test-Path -LiteralPath $railOutput) -or (Get-Item -LiteralPath $railOutput).LastWriteTimeUtc -lt $railStarted) {
            throw "No fresh $railRole report. Inspect $railOutput.log"
        }
        $railReport = Get-Content -LiteralPath $railOutput -Raw | ConvertFrom-Json
        $railReport | Format-List
        $railReports += $railReport
        if (-not $railReport.passed) { throw "$railRole duel checks failed." }
    }
    if ($railReports[0].fingerprint -ne $railReports[1].fingerprint) { throw 'Build fingerprints disagree.' }
    if ($FullMatch -and ($railReports[0].completedWinner -ne $railReports[1].completedWinner -or
        $railReports[0].completedTick -ne $railReports[1].completedTick -or
        ($railReports[0].completedScores -join ',') -ne ($railReports[1].completedScores -join ','))) {
        throw 'The peers disagree about the completed match result.'
    }
} finally {
    foreach ($railProcess in $railProcesses) { if (-not $railProcess.HasExited) { $railProcess.Kill() } }
}
