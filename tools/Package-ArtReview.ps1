param(
    [Parameter(Mandatory=$true)][string]$BuildDirectory,
    [Parameter(Mandatory=$true)][string]$SourceCommit
)
$ErrorActionPreference='Stop'
$railPackageRoot=(Resolve-Path -LiteralPath $BuildDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $railPackageRoot 'RailArena.exe'))) { throw 'Windows player missing' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Test-Duel.ps1') -Destination (Join-Path $railPackageRoot 'Test-Duel.ps1')
$railRepository=Split-Path -Parent $PSScriptRoot
foreach($railNotice in @('LICENSE','THIRD-PARTY-NOTICES.md')){Copy-Item -LiteralPath (Join-Path $railRepository $railNotice) -Destination (Join-Path $railPackageRoot $railNotice)}
$railLicenseRoot=Join-Path $railPackageRoot 'Licenses';[IO.Directory]::CreateDirectory($railLicenseRoot)|Out-Null
foreach($railLicense in @(@('Barlow','OFL.txt'),@('Grab3D','LICENSE.txt'),@('Quaternius','License.txt'),@('Lucide','LICENSE.txt'))){
    $railSource=Join-Path $railRepository ('RailArena/Assets/ThirdParty/'+$railLicense[0]+'/'+$railLicense[1])
    Copy-Item -LiteralPath $railSource -Destination (Join-Path $railLicenseRoot ($railLicense[0]+'-'+$railLicense[1]))
}
@'
param([ValidateSet('Mechanics','Bots','Duel','Performance')][string]$Mode='Mechanics')
$ErrorActionPreference='Stop'
$railPlayer=Join-Path $PSScriptRoot 'RailArena.exe'
$railEvidence=Join-Path $PSScriptRoot 'TestResults'
[IO.Directory]::CreateDirectory($railEvidence)|Out-Null
if($Mode -eq 'Duel') { & (Join-Path $PSScriptRoot 'Test-Duel.ps1') -Build $railPlayer -OutputDirectory (Join-Path $railEvidence 'Duel') -MovementModes; exit }
$railReport=Join-Path $railEvidence ($Mode+'.json')
$railFlag=switch($Mode){'Mechanics'{'--validate-mechanics'} 'Performance'{'--validate-art-performance'} default{'--validate-offline --validation-seconds 45 --validation-bots 3'}}
$railJob=Start-Process -FilePath $railPlayer -ArgumentList ($railFlag+' --validation-output "'+$railReport+'" -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -logFile "'+$railReport+'.log"') -WindowStyle Hidden -PassThru
try { if(-not $railJob.WaitForExit(120000)){throw 'Self-check timed out'}; if($railJob.ExitCode -ne 0){throw 'Self-check failed; inspect TestResults'}; Get-Content -LiteralPath $railReport } finally { if(-not $railJob.HasExited){$railJob.Kill()} }
'@ | Set-Content -LiteralPath (Join-Path $railPackageRoot 'Run-Checks.ps1')
@"
RAIL ARENA v1 alpha / integrated art review

Launch RailArena.exe. G opens Physics Lab. Existing control settings remain available in Settings.
Included checks: powershell -ExecutionPolicy Bypass -File .\Run-Checks.ps1 -Mode Mechanics
Use -Mode Bots for a 45-second automated match, or -Mode Duel for two local hidden peers.
Checks write TestResults; they do not replace native input, sound or artistic playtesting.

Source commit: $SourceCommit
This is a local review package. Public release and final artistic acceptance remain pending.
Broader animation replacement and full reference coverage remain in production.
"@ | Set-Content -LiteralPath (Join-Path $railPackageRoot 'README-REVIEW.txt')
$railFiles=Get-ChildItem -LiteralPath $railPackageRoot -File -Recurse | Where-Object { $_.Name -ne 'PackageFiles.sha256' -and $_.FullName -notmatch 'BackUpThisFolder_ButDontShipItWithYourGame|DoNotShip' }
$railLines=foreach($railFile in $railFiles){(Get-FileHash -LiteralPath $railFile.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$railFile.FullName.Substring($railPackageRoot.Length+1).Replace('\','/')}
$railLines | Set-Content -LiteralPath (Join-Path $railPackageRoot 'PackageFiles.sha256')
$railZip=$railPackageRoot+'.zip'
if(Test-Path -LiteralPath $railZip){throw 'Archive already exists'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$railArchive=[IO.Compression.ZipFile]::Open($railZip,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach($railFile in @($railFiles)+(Get-Item -LiteralPath (Join-Path $railPackageRoot 'PackageFiles.sha256'))){
        $railEntry=(Split-Path -Leaf $railPackageRoot)+'/'+$railFile.FullName.Substring($railPackageRoot.Length+1).Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($railArchive,$railFile.FullName,$railEntry,[IO.Compression.CompressionLevel]::Optimal)|Out-Null
    }
} finally {$railArchive.Dispose()}
Get-FileHash -LiteralPath $railZip -Algorithm SHA256
