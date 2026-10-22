param(
    [string]$Build = (Join-Path $PSScriptRoot '..\Builds\Windows\RailArena.exe'),
    [string]$Output = (Join-Path $PSScriptRoot '..\Validation\offline-run.json')
)
$ErrorActionPreference = 'Stop'
$railBuildPath = (Resolve-Path -LiteralPath $Build).Path
$railOutputPath = [IO.Path]::GetFullPath($Output)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($railOutputPath)) | Out-Null
$railArguments = @('--validate-offline', '--validation-output', ('"' + $railOutputPath + '"'),
    '-logFile', ('"' + $railOutputPath + '.log"'), '-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0')
$railStarted = [DateTime]::UtcNow
$railProcess = Start-Process -FilePath $railBuildPath -ArgumentList $railArguments -WindowStyle Hidden -PassThru
$railProcess.WaitForExit()
if (-not (Test-Path -LiteralPath $railOutputPath)) { throw "The game did not produce a validation report. Inspect $railOutputPath.log" }
if ((Get-Item -LiteralPath $railOutputPath).LastWriteTimeUtc -lt $railStarted) { throw "The game did not update the validation report; the existing report is stale." }
$railReport = Get-Content -LiteralPath $railOutputPath -Raw | ConvertFrom-Json
$railReport | Format-List
if (-not $railReport.passed) { throw "Offline smoke/soak checks failed. See $railOutputPath" }
