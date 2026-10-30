param(
    [string]$Build=(Join-Path $PSScriptRoot '../Builds/Carbine-v1-Validation/RailArena.exe'),
    [string]$OutputDirectory=(Join-Path $PSScriptRoot '../Validation/Carbine'),
    [ValidateSet('mechanics','offline','duel')][string[]]$Modes=@('mechanics','offline','duel')
)
$ErrorActionPreference='Stop'
$carbineExe=(Resolve-Path -LiteralPath $Build).Path
$carbineOutput=[IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($carbineOutput) | Out-Null
function Start-CarbineCheck([string]$Name,[string[]]$Options,[int]$Timeout) {
    $report=Join-Path $carbineOutput ($Name+'.json')
    $playerArguments=@('-batchmode','-nographics','-job-worker-count','2','--validation-owner-pid',$PID,'--validation-timeout',$Timeout,
        '--validation-output',('"'+$report+'"'),'-logFile',('"'+$report+'.log"'))+$Options
    $process=Start-Process -FilePath $carbineExe -ArgumentList $playerArguments -WindowStyle Hidden -PassThru
    return @{Process=$process;Report=$report;Started=[DateTime]::UtcNow}
}
function Complete-CarbineCheck($Job,[int]$Timeout) {
    if(-not $Job.Process.WaitForExit($Timeout*1000)){throw 'Carbine standalone check timed out'}
    $Job.Process.Refresh()
    if(-not (Test-Path -LiteralPath $Job.Report)){throw "Missing report: $($Job.Report)"}
    if((Get-Item -LiteralPath $Job.Report).LastWriteTimeUtc -lt $Job.Started){throw 'Stale standalone report'}
    $report=Get-Content -LiteralPath $Job.Report -Raw | ConvertFrom-Json
    if(-not $report.passed -or $Job.Process.ExitCode -ne 0){throw "Carbine check failed: $($Job.Report)"}
    Write-Output "PASS $($Job.Report)"
}
foreach($mode in $Modes) {
    $owned=@()
    try {
        if($mode -eq 'mechanics'){$owned+=Start-CarbineCheck 'mechanics' @('--validate-mechanics') 120}
        if($mode -eq 'offline'){$owned+=Start-CarbineCheck 'offline' @('--validate-offline','--validation-seconds','45','--validation-bots','3','--validation-difficulty','1') 105}
        if($mode -eq 'duel'){
            $probe=[Net.Sockets.UdpClient]::new(0);$port=$probe.Client.LocalEndPoint.Port;$probe.Dispose()
            foreach($role in @('host','client')) {
                $owned+=Start-CarbineCheck $role @('--validate-duel-role',$role,'--validation-port',$port,'--validation-duration','45') 105
                if($role -eq 'host'){Start-Sleep -Milliseconds 1000}
            }
        }
        foreach($job in $owned){Complete-CarbineCheck $job 120}
    } finally {
        foreach($job in $owned){$job.Process.Refresh();if(-not $job.Process.HasExited){Stop-Process -Id $job.Process.Id -Force;$job.Process.WaitForExit()};$job.Process.Dispose()}
    }
}
