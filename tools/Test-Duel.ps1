param(
    [string]$Build = (Join-Path $PSScriptRoot '..\Builds\Windows\RailArena.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\Validation\Duel'),
    [int]$Port = 17777,
    [int]$Duration = 45
)
$ErrorActionPreference = 'Stop'
$railBuildPath = (Resolve-Path -LiteralPath $Build).Path
$railOutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($railOutputDirectory) | Out-Null
$railStarted = [DateTime]::UtcNow
$railProcesses = @()
try {
    foreach ($railRole in @('host', 'client')) {
        $railOutput = Join-Path $railOutputDirectory ($railRole + '.json')
        $railArguments = @('--validate-duel-role', $railRole, '--validation-port', $Port,
            '--validation-duration', $Duration, '--validation-output', ('"' + $railOutput + '"'),
            '-logFile', ('"' + $railOutput + '.log"'), '-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0')
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
        if (-not $railReport.passed) { throw "$railRole duel checks failed." }
    }
} finally {
    foreach ($railProcess in $railProcesses) { if (-not $railProcess.HasExited) { $railProcess.Kill() } }
}
