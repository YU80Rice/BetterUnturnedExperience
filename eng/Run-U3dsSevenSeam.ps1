# DEV-V6-08 (V6-T7 交付 B): U3DS 七缝装置——候选+NoOp 探针部署、没画面没客机服务器
# boot、七缝旁路结果机器判决。不靠人点平台缝;布局、手感、文案仍由人看(不在本装置判据内)。
#
# 与 eng/Run-FullSuite.ps1 是两套独立退出码体系:本脚本互不调用对方,总判行各自为政
# (FULLSUITE: PASS vs U3DS-SEVEN-SEAM: PASS)。
#
# 退出码:
#   0 = 七步全 Passed + probe-loaded 头 + chain-complete (+ 全模式身份绑哈希一致)
#   1 = 判决失败(缺任一分缝结果/ outcome≠Passed / 探针未加载 / 链未跑完 / 构建或身份不符)
#   2 = 调用中止(仓库根未定位 / U3DS 布局不齐 / 已有 Unturned 进程在跑)
#
# 用法:
#   powershell -NoProfile -ExecutionPolicy Bypass -File eng\Run-U3dsSevenSeam.ps1
#       全装置: 重建(短横线开关) → 部署候选+探针到 U3DS → 无画面无客机 boot →
#       轮询旁路文件 chain-complete → 回收服务器进程 → 收证 → 判决。
#   -Judge <dir>           只判决 <dir>\BUE-NoOpProbe-results.txt(红测接缝兼操作员 affordance)
#   -Plan -U3dsRoot <dir>  只打印组合与在位预检, 不执行
#   -U3dsRoot <dir>        U3DS 安装根(默认 E:\Steam\steamapps\common\U3DS)
#   -TimeoutSeconds <n>    boot 轮询超时(默认 300)
param(
    [string]$Judge = '',
    [switch]$Plan,
    [string]$U3dsRoot = 'E:\Steam\steamapps\common\U3DS',
    [int]$TimeoutSeconds = 300
)
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$ErrorActionPreference = 'Stop'

$SidecarName = 'BUE-NoOpProbe-results.txt'
$LinePrefix = 'BUE-NOOP-PROBE-V1'
# 冻结链序(NoOpModule.Start 的七缝顺序;判决只认此七名,缺一即红)。
$FrozenSteps = @('bootstrap', 'events', 'lifecycle', 'network', 'hosttick', 'settings', 'logger')

# ── 判决器: 读旁路文件, 七缝逐条机器判决。返回 0=PASS / 1=FAIL。 ──
function Invoke-SevenSeamJudge {
    param([string]$ArtifactDir)
    $sidecar = Join-Path $ArtifactDir $SidecarName
    [Console]::Out.WriteLine("[JUDGE] sidecar=" + $sidecar)
    $reasons = @()
    $headerLoaded = $false
    $chainComplete = $false
    $steps = @{}
    $mismatches = @()
    $sidecarMissing = -not (Test-Path -LiteralPath $sidecar)
    if (-not $sidecarMissing) {
        foreach ($line in [System.IO.File]::ReadLines($sidecar)) {
            if (-not $line.StartsWith($LinePrefix + ' ')) { continue }
            if ($line.StartsWith($LinePrefix + ' mismatch=')) {
                $mismatches += ($line.Substring($LinePrefix.Length + ' mismatch='.Length))
                continue
            }
            $h = @{}
            foreach ($pair in ($line.Substring($LinePrefix.Length + 1) -split '\s+')) {
                $idx = $pair.IndexOf('=')
                if ($idx -ge 1) { $h[$pair.Substring(0, $idx)] = $pair.Substring($idx + 1) }
            }
            if ($h.ContainsKey('event')) {
                if ($h['event'] -eq 'probe-loaded') { $headerLoaded = $true }
                if ($h['event'] -eq 'chain-complete') { $chainComplete = $true }
            }
            # 同缝多行(新代际重跑)取末次;缺 outcome 记空=非 Passed。
            if ($h.ContainsKey('step')) {
                $outcome = $null
                if ($h.ContainsKey('outcome')) { $outcome = $h['outcome'] }
                $steps[$h['step']] = $outcome
            }
        }
    }
    if ($headerLoaded) { [Console]::Out.WriteLine('[JUDGE] header=probe-loaded') }
    else {
        [Console]::Out.WriteLine('[JUDGE] header=missing')
        if ($sidecarMissing) { $reasons += ('probe-not-loaded (sidecar missing: ' + $sidecar + ')') }
        else { $reasons += ('probe-not-loaded (no ' + $LinePrefix + ' event=probe-loaded header)') }
    }
    foreach ($step in $FrozenSteps) {
        if (-not $steps.ContainsKey($step)) {
            [Console]::Out.WriteLine('[JUDGE] step=' + $step + ' missing')
            $reasons += ('step=' + $step + ' missing (NotRun is not Passed)')
        }
        else {
            $outcome = $steps[$step]
            [Console]::Out.WriteLine('[JUDGE] step=' + $step + ' outcome=' + $outcome)
            if ($outcome -ne 'Passed') { $reasons += ('step=' + $step + ' outcome=' + $outcome + ' (expected Passed)') }
        }
    }
    foreach ($m in $mismatches) { [Console]::Out.WriteLine('[JUDGE] mismatch=' + $m) }
    if ($chainComplete) { [Console]::Out.WriteLine('[JUDGE] chain=complete') }
    else {
        [Console]::Out.WriteLine('[JUDGE] chain=incomplete')
        $reasons += ('chain-incomplete (no ' + $LinePrefix + ' event=chain-complete line)')
    }
    if ($reasons.Count -gt 0) {
        foreach ($r in $reasons) { [Console]::Out.WriteLine('REASON: ' + $r) }
        [Console]::Out.WriteLine('U3DS-SEVEN-SEAM: FAIL reasons=' + $reasons.Count)
        return 1
    }
    [Console]::Out.WriteLine('U3DS-SEVEN-SEAM: PASS')
    return 0
}

$RepoRoot = $null
foreach ($candidate in @((Get-Location).Path, (Join-Path $PSScriptRoot '..'))) {
    $full = [System.IO.Path]::GetFullPath($candidate)
    if (Test-Path -LiteralPath (Join-Path $full 'BetterUnturnedExperience.sln')) { $RepoRoot = $full; break }
}
if ($null -eq $RepoRoot) { Write-Output 'U3DS-SEVEN-SEAM: ABORT (仓库根未定位, 请在仓库根运行)'; exit 2 }

if ($Judge -ne '') {
    exit (Invoke-SevenSeamJudge -ArtifactDir $Judge)
}

# ── 部署/预检组合表(与全装置共用;-Plan 只打印不执行) ──
$fixtureProject = 'src/BetterUnturnedExperience.NoOpFixture/BetterUnturnedExperience.NoOpFixture.csproj'
$publishedDllRel = 'artifacts/release-candidate/DEV-V6-10-r3/BetterUnturnedExperience.dll'
$fixtureDllRel = 'src/BetterUnturnedExperience.NoOpFixture/bin/Release/BetterUnturnedExperience.NoOpFixture.dll'
$publishedDll = Join-Path $RepoRoot $publishedDllRel
$buildPlanLine = ('dotnet msbuild ' + $fixtureProject + ' -t:Rebuild -p:Configuration=Release -p:BuePublishedDll=<published candidate DLL> -v:m -nologo (探针按合并发布候选编译)')

function Get-U3dsLayoutMissing {
    # 返回缺失组件清单(空=布局齐): Unturned.exe + BepInEx 插件/核心目录。
    $missing = @()
    if (-not (Test-Path -LiteralPath (Join-Path $U3dsRoot 'Unturned.exe'))) { $missing += 'Unturned.exe' }
    if (-not (Test-Path -LiteralPath (Join-Path $U3dsRoot 'BepInEx/plugins'))) { $missing += 'BepInEx/plugins' }
    if (-not (Test-Path -LiteralPath (Join-Path $U3dsRoot 'BepInEx/core'))) { $missing += 'BepInEx/core' }
    return $missing
}

if ($Plan) {
    Write-Output ("== BUE U3DS seven-seam device plan (repo root: " + $RepoRoot + ") ==")
    $missing = 0
    Write-Output ('PLAN build: ' + $buildPlanLine)
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $fixtureProject))) { Write-Output ('PLAN MISSING: ' + $fixtureProject); $missing++ }
    if (-not (Test-Path -LiteralPath $publishedDll)) { Write-Output ('PLAN MISSING: published candidate ' + $publishedDll); $missing++ }
    Write-Output ('PLAN published candidate: ' + $publishedDllRel + ' (三轮确定性重建后的合并玩家 DLL)')
    Write-Output ('PLAN deploy: ' + $publishedDllRel + ' + ' + $fixtureDllRel + ' -> ' + (Join-Path $U3dsRoot 'BepInEx/plugins') + ' (部署前清陈年旁路文件; 不部署独立 Contracts DLL)')
    Write-Output ('PLAN boot: ' + (Join-Path $U3dsRoot 'Unturned.exe') + ' (无画面无客机 headless) poll ' + (Join-Path $U3dsRoot ('BepInEx/plugins/' + $SidecarName)) + ' for chain-complete <= ' + $TimeoutSeconds + 's')
    Write-Output ('PLAN collect: sidecar + BepInEx/LogOutput.log + deployed sha256 -> artifacts/u3ds-seven-seam/<ts>')
    Write-Output ('PLAN judge: seven steps all Passed + probe-loaded + chain-complete + 身份绑哈希一致, else nonzero (U3DS-SEVEN-SEAM: PASS/FAIL)')
    foreach ($component in (Get-U3dsLayoutMissing)) { Write-Output ('PLAN MISSING: U3DS ' + $component); $missing++ }
    if ($missing -gt 0) { Write-Output ('PLAN: ' + $missing + ' component(s) MISSING'); exit 1 }
    Write-Output 'PLAN complete'
    exit 0
}

# ── 全装置: 预检 → 重建 → 部署 → boot → 轮询 → 回收 → 收证 → 判决 ──

$layoutMissing = Get-U3dsLayoutMissing
if ($layoutMissing.Count -gt 0) { Write-Output ('U3DS-SEVEN-SEAM: ABORT (U3DS 布局缺组件: ' + ($layoutMissing -join ', ') + ')'); exit 2 }
# 已有 Unturned 进程(可能是玩家的客户端或残留服务端)绝不代杀: 中止让人处置。
$running = Get-Process -Name 'Unturned' -ErrorAction SilentlyContinue
if ($null -ne $running) {
    foreach ($p in $running) { Write-Output ('U3DS-SEVEN-SEAM: ABORT (已有 Unturned 进程在跑 pid=' + $p.Id + ', 请先关闭或处置)') }
    exit 2
}

$runDir = Join-Path $RepoRoot ('artifacts/u3ds-seven-seam/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

Write-Output ('[BUILD] ' + $buildPlanLine)
$buildLog = Join-Path $runDir 'build.log'
# 与 Run-FullSuite 的 Invoke-Step 同款直启模式: Start-Process -PassThru 在本机
# 读不到 ExitCode, System.Diagnostics.Process 直启稳定可靠。
$buildPsi = New-Object System.Diagnostics.ProcessStartInfo
$buildPsi.FileName = 'dotnet'
$buildPsi.Arguments = ('msbuild "' + $fixtureProject + '" -t:Rebuild -p:Configuration=Release -p:BuePublishedDll="' + $publishedDll + '" -v:m -nologo')
$buildPsi.WorkingDirectory = $RepoRoot
$buildPsi.UseShellExecute = $false
$buildPsi.CreateNoWindow = $true
$buildPsi.RedirectStandardOutput = $true
$buildPsi.RedirectStandardError = $true
$buildPsi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
$buildPsi.StandardErrorEncoding = [System.Text.Encoding]::UTF8
$build = [System.Diagnostics.Process]::Start($buildPsi)
$buildOutTask = $build.StandardOutput.ReadToEndAsync()
$buildErrTask = $build.StandardError.ReadToEndAsync()
if (-not $build.WaitForExit(1200 * 1000)) { try { $build.Kill() } catch { }; Write-Output 'U3DS-SEVEN-SEAM: ABORT (构建超时)'; exit 2 }
$build.WaitForExit() | Out-Null
($buildOutTask.Result + $buildErrTask.Result) | Set-Content -LiteralPath $buildLog -Encoding UTF8
if ($build.ExitCode -ne 0) {
    Write-Output ('[BUILD] FAILED exit=' + $build.ExitCode + ' (log: ' + $buildLog + ')')
    Write-Output 'U3DS-SEVEN-SEAM: FAIL reasons=1'
    Write-Output 'REASON: build-failed (重建退出码非 0, 禁静默用旧二进制)'
    exit 1
}
Write-Output ('[BUILD] exit=0 (log: ' + $buildLog + ')')

$pluginsDir = Join-Path $U3dsRoot 'BepInEx/plugins'
$sidecarTarget = Join-Path $pluginsDir $SidecarName
$deployList = @(
    @{ Src = $publishedDll;                    Name = 'BetterUnturnedExperience.dll' },
    @{ Src = Join-Path $RepoRoot $fixtureDllRel; Name = 'BetterUnturnedExperience.NoOpFixture.dll' }
)
$deployedHash = @()
foreach ($item in $deployList) {
    if (-not (Test-Path -LiteralPath $item.Src)) {
        Write-Output ('U3DS-SEVEN-SEAM: ABORT (构建产物缺失: ' + $item.Src + ')')
        exit 2
    }
    $hash = (Get-FileHash -LiteralPath $item.Src -Algorithm SHA256).Hash
    $deployedHash += ($item.Name + ' sha256=' + $hash)
    Copy-Item -LiteralPath $item.Src -Destination (Join-Path $pluginsDir $item.Name) -Force
    Write-Output ('[DEPLOY] ' + $item.Name + ' sha256=' + $hash)
}
$deployedHash | Set-Content -LiteralPath (Join-Path $runDir 'deployed-sha256.txt') -Encoding UTF8
# 部署前清陈年旁路文件: 本轮采集不得读到上一轮的分缝结果。
if (Test-Path -LiteralPath $sidecarTarget) { Remove-Item -LiteralPath $sidecarTarget -Force }
$pluginDeployedHash = ((Get-FileHash -LiteralPath $publishedDll -Algorithm SHA256).Hash)
$fixtureDeployedHash = ((Get-FileHash -LiteralPath (Join-Path $RepoRoot $fixtureDllRel) -Algorithm SHA256).Hash)

Write-Output '[BOOT] start headless U3DS (no clients)'
$server = Start-Process -FilePath (Join-Path $U3dsRoot 'Unturned.exe') -WorkingDirectory $U3dsRoot -WindowStyle Hidden -PassThru
Write-Output ('[BOOT] pid=' + $server.Id)
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$chainCompleteSeen = $false
$exitedEarly = $false
while ($sw.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
    Start-Sleep -Seconds 2
    if ($server.HasExited) { $exitedEarly = $true; break }
    if (Test-Path -LiteralPath $sidecarTarget) {
        try {
            $tail = Get-Content -LiteralPath $sidecarTarget -ErrorAction SilentlyContinue
            if ($tail -contains ($LinePrefix + ' event=chain-complete')) { $chainCompleteSeen = $true; break }
        } catch { }
    }
}
if ($chainCompleteSeen) { Write-Output ('[POLL] chain-complete after ' + [int]$sw.Elapsed.TotalSeconds + 's') }
elseif ($exitedEarly) { Write-Output ('[POLL] process exited early code=' + $server.ExitCode + ' after ' + [int]$sw.Elapsed.TotalSeconds + 's') }
else { Write-Output ('[POLL] timeout after ' + $TimeoutSeconds + 's (无 chain-complete)') }
try { & taskkill /PID $server.Id /T /F 2>$null | Out-Null } catch { }
Start-Sleep -Seconds 2

# ── 收证: 旁路文件 + 服务器日志 + 部署哈希归档 artifacts/u3ds-seven-seam/<ts>。 ──
$serverLog = $null
foreach ($candidate in @((Join-Path $U3dsRoot 'BepInEx/LogOutput.log'), (Join-Path $U3dsRoot 'LogOutput.log'))) {
    if (Test-Path -LiteralPath $candidate) { $serverLog = $candidate; break }
}
$identityOk = $true
$identityReasons = @()
if (Test-Path -LiteralPath $sidecarTarget) { Copy-Item -LiteralPath $sidecarTarget -Destination (Join-Path $runDir $SidecarName) -Force }
if ($null -ne $serverLog) { Copy-Item -LiteralPath $serverLog -Destination (Join-Path $runDir 'server-LogOutput.log') -Force }
else { $identityOk = $false; $identityReasons += 'collect-log-missing (U3DS 服务器日志未找到, 无法绑运行时身份)' }

if ($null -ne $serverLog) {
    $runtimeHash = $null
    foreach ($line in [System.IO.File]::ReadLines($serverLog)) {
        if ($line -match 'event=assembly-identity .*BetterUnturnedExperience\.dll sha256=([0-9A-Fa-f]{64})') { $runtimeHash = $Matches[1] }
    }
    if ($null -eq $runtimeHash) { $identityOk = $false; $identityReasons += 'identity-unreadable (日志无 assembly-identity sha256 行)' }
    elseif ($runtimeHash -ne $pluginDeployedHash) {
        $identityOk = $false
        $identityReasons += ('identity-mismatch (候选主工程 deployed=' + $pluginDeployedHash + ' runtime=' + $runtimeHash + ')')
    }
    else { Write-Output ('[IDENTITY] plugin deployed=' + $pluginDeployedHash + ' runtime=' + $runtimeHash + ' match=True') }
}
if (Test-Path -LiteralPath $sidecarTarget) {
    $fixtureRuntimeHash = $null
    foreach ($line in [System.IO.File]::ReadLines($sidecarTarget)) {
        if ($line.StartsWith($LinePrefix + ' event=probe-loaded ')) {
            if ($line -match 'sha256=([0-9A-Fa-f]{64})') { $fixtureRuntimeHash = $Matches[1] }
        }
    }
    if ($null -eq $fixtureRuntimeHash) { $identityOk = $false; $identityReasons += 'fixture-identity-unreadable (旁路头无 sha256)' }
    elseif ($fixtureRuntimeHash -ne $fixtureDeployedHash) {
        $identityOk = $false
        $identityReasons += ('fixture-identity-mismatch (探针 deployed=' + $fixtureDeployedHash + ' runtime=' + $fixtureRuntimeHash + ')')
    }
    else { Write-Output ('[IDENTITY] fixture deployed=' + $fixtureDeployedHash + ' runtime=' + $fixtureRuntimeHash + ' match=True') }
}

# ── arm 状态诊断(信息性, 不作判据): 目录启动走 scene-loaded 驱动、arm 晚于探针
# Start 是 U3DS 既有编排——network 缝的绿=显式不投递投影(NoSession/ChannelNotRegistered
# 皆冻结投影), 不以「运行时已 arm」为成功; 本行只把 attach 状态如实记进证据。
$armedSeen = $false
if ($null -ne $serverLog) {
    foreach ($line in [System.IO.File]::ReadLines($serverLog)) {
        if ($line -match 'bue-runtime-arm result=armed') { $armedSeen = $true; break }
    }
}
Write-Output ('[ARM] bue-runtime-arm result=armed found=' + $armedSeen + ' (diagnostic only)')

$judgeResult = Invoke-SevenSeamJudge -ArtifactDir $runDir
foreach ($r in $identityReasons) { Write-Output ('REASON: ' + $r) }
if ($identityOk -and $judgeResult -eq 0) { Write-Output 'U3DS-SEVEN-SEAM: PASS (identity-bound)'; exit 0 }
if (-not $identityOk) { Write-Output 'U3DS-SEVEN-SEAM: FAIL (identity)' }
exit 1
