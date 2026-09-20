# DEV-V6-09: real external consumer on a headless U3DS server.
# This device is intentionally separate from the NoOp seven-seam device.
param(
    [string]$U3dsRoot = 'E:\Steam\steamapps\common\U3DS',
    [string]$PublishedDll = '',
    [int]$TimeoutSeconds = 180,
    [switch]$Plan,
    [string]$Judge = ''
)
$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

$RepoRoot = $null
foreach ($candidate in @((Get-Location).Path, (Join-Path $PSScriptRoot '..'))) {
    $full = [System.IO.Path]::GetFullPath($candidate)
    if (Test-Path -LiteralPath (Join-Path $full 'BetterUnturnedExperience.sln')) { $RepoRoot = $full; break }
}
if (-not $RepoRoot) { Write-Output 'EXTERNAL-CANARY: ABORT (repository root not found)'; exit 2 }
if (-not $PublishedDll) { $PublishedDll = Join-Path $RepoRoot 'artifacts/repack/external-canary/BetterUnturnedExperience.dll' }
if (-not [System.IO.Path]::IsPathRooted($PublishedDll)) { $PublishedDll = Join-Path $RepoRoot $PublishedDll }
$PublishedDll = [System.IO.Path]::GetFullPath($PublishedDll)
$canaryRoot = Join-Path $RepoRoot 'external-canary'
$canaryProject = Join-Path $canaryRoot 'ExternalCanary.csproj'
$canaryDll = Join-Path $canaryRoot 'bin/Release/ExternalCanary.dll'
$pluginDir = Join-Path $U3dsRoot 'BepInEx/plugins'
$logCandidates = @((Join-Path $U3dsRoot 'BepInEx/LogOutput.log'), (Join-Path $U3dsRoot 'LogOutput.log'))
$acceptToken = 'BUE external canary featureId=com.bue.canary.hello accepted=True'

function Get-LayoutMissing {
    $missing = @()
    foreach ($path in @((Join-Path $U3dsRoot 'Unturned.exe'), (Join-Path $U3dsRoot 'BepInEx/core'), $pluginDir)) {
        if (-not (Test-Path -LiteralPath $path)) { $missing += $path }
    }
    return $missing
}
function Invoke-CanaryJudge([string]$ArtifactDir) {
    $log = Join-Path $ArtifactDir 'server-LogOutput.log.txt'
    $reasons = @()
    if (-not (Test-Path -LiteralPath $log)) { $reasons += 'server-log-missing' }
    else {
        $lines = [System.IO.File]::ReadAllText($log)
        if ($lines -notmatch [regex]::Escape($acceptToken)) { $reasons += 'accepted-registration-missing' }
        if ($lines -notmatch 'feature-state feature=com\.bue\.canary\.hello to=Running') { $reasons += 'running-state-missing' }
        if ($lines -notmatch 'assembly-identity .*BetterUnturnedExperience\.dll sha256=[0-9A-Fa-f]{64}') { $reasons += 'host-identity-missing' }
    }
    if ($reasons.Count -eq 0) { Write-Output 'EXTERNAL-CANARY: PASS (headless accepted)'; return 0 }
    foreach ($reason in $reasons) { Write-Output ('REASON: ' + $reason) }
    Write-Output ('EXTERNAL-CANARY: FAIL reasons=' + $reasons.Count)
    return 1
}
if ($Judge) {
    $judgeOutput = @(Invoke-CanaryJudge $Judge)
    $judgeExit = [int]$judgeOutput[$judgeOutput.Count - 1]
    $judgeOutput | Select-Object -SkipLast 1 | ForEach-Object { Write-Output $_ }
    exit $judgeExit
}
if ($Plan) {
    Write-Output ('== BUE external canary plan (repo root: ' + $RepoRoot + ') ==')
    Write-Output ('PLAN gate: Verify-ExternalCanary.ps1 -RepoRoot ' + $RepoRoot)
    Write-Output ('PLAN build: dotnet msbuild external-canary/ExternalCanary.csproj -t:Rebuild -p:Configuration=Release -p:BuePublishedDll=<published DLL>')
    Write-Output ('PLAN deploy: BetterUnturnedExperience.dll + ExternalCanary.dll -> ' + $pluginDir)
    Write-Output ('PLAN boot: headless Unturned.exe; require exact Accepted line and assembly identity')
    $missing = @(Get-LayoutMissing)
    if (-not (Test-Path -LiteralPath $canaryProject)) { $missing += $canaryProject }
    if (-not (Test-Path -LiteralPath $PublishedDll)) { $missing += $PublishedDll }
    if ($missing.Count -gt 0) { Write-Output ('PLAN MISSING: ' + ($missing -join '; ')); exit 1 }
    Write-Output 'PLAN complete'; exit 0
}
$missing = @(Get-LayoutMissing)
if ($missing.Count -gt 0) { Write-Output ('EXTERNAL-CANARY: ABORT (missing layout: ' + ($missing -join '; ') + ')'); exit 2 }
if (-not (Test-Path -LiteralPath $PublishedDll)) { Write-Output ('EXTERNAL-CANARY: FAIL (published DLL missing: ' + $PublishedDll + ')'); exit 1 }
if (Get-Process -Name Unturned -ErrorAction SilentlyContinue) { Write-Output 'EXTERNAL-CANARY: ABORT (Unturned process already running; not killing it)'; exit 2 }
$runDir = Join-Path $RepoRoot ('artifacts/external-canary/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $runDir | Out-Null
$buildLog = Join-Path $runDir 'build.log.txt'
$buildPsi = New-Object System.Diagnostics.ProcessStartInfo
$buildPsi.FileName = 'dotnet'
function Quote-ProcessArgument([string]$value) {
    return '"' + ($value -replace '(\\*)"', '$1$1\\"' -replace '(\\+)$', '$1$1') + '"'
}
$buildPsi.Arguments = ('msbuild ' + (Quote-ProcessArgument $canaryProject) + ' -t:Rebuild -p:Configuration=Release -p:BuePublishedDll=' + (Quote-ProcessArgument $PublishedDll) + ' -v:m -nologo')
$buildPsi.WorkingDirectory = $RepoRoot
$buildPsi.UseShellExecute = $false
$buildPsi.CreateNoWindow = $true
$buildPsi.RedirectStandardOutput = $true
$buildPsi.RedirectStandardError = $true
$buildPsi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
$buildPsi.StandardErrorEncoding = [System.Text.Encoding]::UTF8
$build = [System.Diagnostics.Process]::Start($buildPsi)
$stdout = $build.StandardOutput.ReadToEnd()
$stderr = $build.StandardError.ReadToEnd()
$build.WaitForExit()
($stdout + $stderr) | Set-Content -LiteralPath $buildLog -Encoding UTF8
if ($build.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $canaryDll)) { Write-Output ('EXTERNAL-CANARY: FAIL (sample build exit=' + $build.ExitCode + ')'); exit 1 }
$deployHash = @()
foreach ($item in @(@{ Source = $PublishedDll; Name = 'BetterUnturnedExperience.dll' }, @{ Source = $canaryDll; Name = 'ExternalCanary.dll' })) {
    $hash = (Get-FileHash -LiteralPath $item.Source -Algorithm SHA256).Hash
    Copy-Item -LiteralPath $item.Source -Destination (Join-Path $pluginDir $item.Name) -Force
    $deployHash += ($item.Name + ' sha256=' + $hash)
}
$deployHash | Set-Content -LiteralPath (Join-Path $runDir 'deployed-sha256.txt') -Encoding UTF8
foreach ($candidate in $logCandidates) { if (Test-Path -LiteralPath $candidate) { Remove-Item -LiteralPath $candidate -Force } }
$server = Start-Process -FilePath (Join-Path $U3dsRoot 'Unturned.exe') -WorkingDirectory $U3dsRoot -WindowStyle Hidden -PassThru
$sw = [System.Diagnostics.Stopwatch]::StartNew(); $accepted = $false
while ($sw.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
    Start-Sleep -Seconds 2
    foreach ($candidate in $logCandidates) {
        if ((Test-Path -LiteralPath $candidate) -and ((Get-Content -LiteralPath $candidate -Raw -ErrorAction SilentlyContinue) -match [regex]::Escape($acceptToken))) { $accepted = $true; break }
    }
    if ($accepted -or $server.HasExited) { break }
}
try { & taskkill /PID $server.Id /T /F 2>$null | Out-Null } catch { }
Start-Sleep -Seconds 2
$serverLog = $logCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ($serverLog) { Copy-Item -LiteralPath $serverLog -Destination (Join-Path $runDir 'server-LogOutput.log.txt') -Force }
else { New-Item -ItemType File -Path (Join-Path $runDir 'server-LogOutput.log.txt') | Out-Null }
$judgeOutput = @(Invoke-CanaryJudge $runDir)
$judgeExit = [int]$judgeOutput[$judgeOutput.Count - 1]
$judgeOutput | Select-Object -SkipLast 1 | ForEach-Object { Write-Output $_ }
exit $judgeExit
