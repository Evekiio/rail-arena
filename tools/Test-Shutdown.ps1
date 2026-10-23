param(
    [string]$Build = (Join-Path $PSScriptRoot '..\Builds\Windows\RailArena.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\Validation\Shutdown'),
    [ValidateSet('menu-close', 'offline-close', 'host-close', 'timeout', 'owner-exit')]
    [string[]]$Scenarios = @('menu-close', 'offline-close', 'host-close', 'timeout', 'owner-exit')
)
$ErrorActionPreference = 'Stop'
$railBuildPath = (Resolve-Path -LiteralPath $Build).Path
$railOutput = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($railOutput) | Out-Null
if (-not ('RailShutdownWindow' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class RailShutdownWindow {
    private delegate bool WindowCallback(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
    public static bool Close(int process) {
        bool sent = false;
        EnumWindows((window, parameter) => {
            uint owner; GetWindowThreadProcessId(window, out owner);
            if (owner == process) sent |= PostMessage(window, 0x0010, IntPtr.Zero, IntPtr.Zero);
            return true;
        }, IntPtr.Zero);
        return sent;
    }
}
'@
}
$railResults = @()
foreach ($railCase in $Scenarios) {
    $railOwner = $null
    $railPlayer = $null
    try {
        $railOwnerId = $PID
        if ($railCase -eq 'owner-exit') {
            $railOwner = Start-Process (Get-Process -Id $PID).Path -ArgumentList '-NoProfile', '-Command', 'Start-Sleep 60' -WindowStyle Hidden -PassThru
            $railOwnerId = $railOwner.Id
        }
        $railLimit = if ($railCase -eq 'timeout') { 3 } else { 45 }
        $railArguments = @('--validation-owner-pid', $railOwnerId, '--validation-timeout', $railLimit,
            '-logFile', ('"' + (Join-Path $railOutput ($railCase + '.log')) + '"'),
            '-screen-fullscreen', '0', '-screen-width', '640', '-screen-height', '480')
        if ($railCase -eq 'offline-close') { $railArguments += @('--validate-offline', '--validation-seconds', '45') }
        if ($railCase -eq 'host-close') { $railArguments += @('--validate-duel-role', 'host', '--validation-port', '19779') }
        $railPlayer = Start-Process $railBuildPath -ArgumentList $railArguments -WindowStyle Hidden -PassThru
        if ($railCase -like '*-close') {
            Start-Sleep -Seconds 5
            if ($railPlayer.HasExited -or -not [RailShutdownWindow]::Close($railPlayer.Id)) { throw "No live player window for $railCase" }
        } elseif ($railCase -eq 'owner-exit') {
            Start-Sleep -Seconds 5
            if ($railPlayer.HasExited) { throw 'Player exited before its owner.' }
            $railOwner.Kill()
            $railOwner.WaitForExit()
        }
        if (-not $railPlayer.WaitForExit(20000)) { throw "$railCase left the player running." }
        $railExpected = if ($railCase -in @('timeout', 'owner-exit')) { 2 } else { 0 }
        if ($railPlayer.ExitCode -ne $railExpected) { throw "$railCase exit code $($railPlayer.ExitCode), expected $railExpected" }
        $railResults += [pscustomobject]@{ scenario = $railCase; passed = $true; exitCode = $railPlayer.ExitCode }
    } finally {
        foreach ($railProcess in @($railPlayer, $railOwner)) {
            if ($null -ne $railProcess) {
                if (-not $railProcess.HasExited) { $railProcess.Kill(); $railProcess.WaitForExit() }
                $railProcess.Dispose()
            }
        }
    }
}
$railResults | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $railOutput 'results.json')
$railResults | Format-Table
