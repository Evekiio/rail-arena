param(
    [string]$Build = (Join-Path $PSScriptRoot '..\Builds\Windows\RailArena.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\Validation\Duel-Impaired'),
    [int]$Port = 18777,
    [int]$Duration = 60,
    [double]$OneWayDelayMs = 25,
    [double]$JitterMs = 10,
    [double]$Loss = .05,
    [int]$Seed = 7191,
    [switch]$FullMatch
)
$ErrorActionPreference = 'Stop'
$railOutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($railOutputDirectory) | Out-Null
$railStatsPath = Join-Path $railOutputDirectory 'impairment.json'
$railStopPath = Join-Path $railOutputDirectory 'relay.stop'
if (Test-Path -LiteralPath $railStopPath) { Remove-Item -LiteralPath $railStopPath }
if ($FullMatch -and $Duration -le 60) { $Duration = 720 }
$railPython = (Get-Command python -ErrorAction Stop).Source
$railRelayPort = $Port + 1
$railStarted = [DateTime]::UtcNow
$railArguments = @(('"' + (Join-Path $PSScriptRoot 'network_impairment.py') + '"'),
    '--listen-port', $railRelayPort, '--server-port', $Port,
    '--delay-ms', $OneWayDelayMs.ToString([Globalization.CultureInfo]::InvariantCulture),
    '--jitter-ms', $JitterMs.ToString([Globalization.CultureInfo]::InvariantCulture),
    '--loss', $Loss.ToString([Globalization.CultureInfo]::InvariantCulture), '--seed', $Seed,
    '--duration', ($Duration + 60), '--stats', ('"' + $railStatsPath + '"'), '--stop-file', ('"' + $railStopPath + '"'))
$railRelay = Start-Process -FilePath $railPython -ArgumentList $railArguments -WindowStyle Hidden -PassThru `
    -RedirectStandardError (Join-Path $railOutputDirectory 'relay-errors.log')
try {
    $railReadyDeadline = [DateTime]::UtcNow.AddSeconds(5)
    do {
        if ($railRelay.HasExited) { throw 'The impairment relay failed to start. Inspect relay-errors.log.' }
        $railReady = (Test-Path -LiteralPath $railStatsPath) -and (Get-Item -LiteralPath $railStatsPath).LastWriteTimeUtc -ge $railStarted
        if (-not $railReady) { Start-Sleep -Milliseconds 50 }
    } while (-not $railReady -and [DateTime]::UtcNow -lt $railReadyDeadline)
    if (-not $railReady) { throw 'The impairment relay did not become ready.' }
    & (Join-Path $PSScriptRoot 'Test-Duel.ps1') -Build $Build -OutputDirectory $railOutputDirectory -Port $Port -ClientPort $railRelayPort -Duration $Duration -FullMatch:$FullMatch
} finally {
    [IO.File]::WriteAllText($railStopPath, 'stop')
    if (-not $railRelay.WaitForExit(5000)) { $railRelay.Kill(); throw 'The impairment relay did not stop gracefully.' }
}
$railStats = Get-Content -LiteralPath $railStatsPath -Raw | ConvertFrom-Json
$railStats | Format-List
if ($railStats.running -or $railStats.forwarded -lt 200 -or $railStats.to_host -eq 0 -or $railStats.to_client -eq 0 -or
    ($Loss -gt 0 -and $railStats.dropped -eq 0) -or $railStats.queueOverflow -gt 0 -or
    $railStats.minimumAppliedMs -lt [Math]::Max(0, $OneWayDelayMs - $JitterMs) - .1) {
    throw 'Impairment evidence did not meet the configured test conditions.'
}
