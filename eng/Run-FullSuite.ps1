# One-command full suite (DEV-V6-01 交付 A, V6-T7). 仓库根为工作目录:
#   powershell -NoProfile -ExecutionPolicy Bypass -File eng\Run-FullSuite.ps1
# 组合: -t:Rebuild 重建(短横线开关, 免路径转换) + 七个测试可执行文件直跑 + 现有七道门禁
# (NoUiTokens 按 Contracts/Core 两个源根分别调用, 共八次) + 工程防火墙度量。
#
# 角色裁定(V6-T2 追加 / V6-T7; DEV-V6-02F 守卫翻转):
#   - 防火墙 = 守卫(02F): 禁方向命中即总失败, 真实仓库须零命中(02B..02E 拆净 + 02F 收编两条
#     具名非功能边 Transport→Core / NoOpFixture→Plugin 后)。度量照相态退役, 守卫绕过不可达
#     (守卫咬合由 Plugin.Tests 的 DEV-V6-02F 红测组合成树案机器锁定)。
#   - NoUiTokens(Contracts) = 已知基线命中 ContractTypes.cs:Glazier(V2-02 冻结基线, 逐字在册):
#     如实双记(步骤行 + 汇总行)且不计入总失败; 基线之外出现任何其他命中 = 真失败。
#   - 其余全部步骤失败即总失败, 退出 1。
#
# 可选参数(亦为红测接缝, 不触发重建/套件即可验语义):
#   -Plan                 只打印组合与在位预检, 不执行(组件缺失 exit 1)
#   -Steps Build,Tests,Gates,Firewall   选择执行步骤(默认全部)
#   -GateFilter <wildcard>              门禁调用 id 过滤(默认 *)
param(
    [switch]$Plan,
    [string]$Steps = 'Build,Tests,Gates,Firewall',
    [string]$GateFilter = '*'
)
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$ErrorActionPreference = 'Stop'

$RepoRoot = $null
foreach ($candidate in @((Get-Location).Path, (Join-Path $PSScriptRoot '..'))) {
    $full = [System.IO.Path]::GetFullPath($candidate)
    if (Test-Path -LiteralPath (Join-Path $full 'BetterUnturnedExperience.sln')) { $RepoRoot = $full; break }
}
if ($null -eq $RepoRoot) { Write-Output 'FULLSUITE: ABORT (仓库根未定位, 请在仓库根运行)'; exit 2 }

$stepSet = @($Steps -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$validSteps = @('Build', 'Tests', 'Gates', 'Firewall')
foreach ($s in $stepSet) {
    if ($validSteps -notcontains $s) { Write-Output ("FULLSUITE: ABORT (未知步骤 '" + $s + "', 可选: " + ($validSteps -join ',') + ")"); exit 2 }
}

# ── 组件表: 七套测试 exe + 六道门禁脚本(七次调用) + 防火墙 ──
$testSuites = @('Contracts', 'Network', 'Placement', 'Settings', 'ClientUi', 'Plugin', 'Release')
$gateDefs = @(
    @{ Id = 'DeveloperHandbook';    Script = 'eng/Verify-DeveloperHandbook.ps1';                  Args = @('-RepoRoot', $RepoRoot) },
    @{ Id = 'ContractDocs';         Script = 'eng/Verify-ContractDocs.ps1';                       Args = @('-RepoRoot', $RepoRoot) },
    @{ Id = 'NoUiTokens:Contracts'; Script = 'eng/Verify-NoUiTokens.ps1';                         Args = @('-SourceRoot', 'src/BetterUnturnedExperience.Contracts'); KnownBaseline = 'ContractTypes.cs:Glazier' },
    @{ Id = 'NoUiTokens:Core';      Script = 'eng/Verify-NoUiTokens.ps1';                         Args = @('-SourceRoot', 'src/BetterUnturnedExperience.Core') },
    @{ Id = 'RefreshModelDeduped';  Script = 'eng/Verify-RefreshModelDeduped.ps1';                Args = @('-RepoRoot', $RepoRoot) },
    @{ Id = 'SpecV4R9Ingested';     Script = 'eng/Verify-SpecV4R9Ingested.ps1';                   Args = @('-RepoRoot', $RepoRoot) },
    @{ Id = 'TestFixturesTracked';  Script = 'eng/Verify-TestFixturesTracked.ps1';                Args = @('-RepoRoot', $RepoRoot) },
    @{ Id = 'TestRunnerHostDlls';   Script = 'eng/Verify-TestRunnerHostDllsProvisioned.ps1';      Args = @('-RepoRoot', $RepoRoot) }
)
$firewallDef = @{ Id = 'Firewall'; Script = 'eng/Verify-ProjectFirewall.ps1'; Args = @('-RepoRoot', $RepoRoot) }
$powershellExe = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
if (-not (Test-Path -LiteralPath $powershellExe)) { $powershellExe = 'powershell' }

if ($Plan) {
    Write-Output ("== BUE full-suite driver plan (repo root: " + $RepoRoot + ") ==")
    $missing = 0
    Write-Output 'PLAN Build: dotnet msbuild BetterUnturnedExperience.sln -t:Rebuild -p:Configuration=Release -v:m -nologo'
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot 'BetterUnturnedExperience.sln'))) { Write-Output 'PLAN MISSING: BetterUnturnedExperience.sln'; $missing++ }
    if ($stepSet -contains 'Tests') {
        foreach ($s in $testSuites) {
            $exe = 'tests/BetterUnturnedExperience.' + $s + '.Tests/bin/Release/BetterUnturnedExperience.' + $s + '.Tests.exe'
            Write-Output ('PLAN Tests: ' + $exe)
            if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $exe))) { Write-Output ('PLAN MISSING: ' + $exe); $missing++ }
        }
    }
    if ($stepSet -contains 'Gates') {
        foreach ($g in $gateDefs) {
            if ($g.Id -notlike $GateFilter) { continue }
            Write-Output ('PLAN Gates: ' + $g.Id + ' -> ' + $g.Script + ' ' + ($g.Args -join ' '))
            if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $g.Script))) { Write-Output ('PLAN MISSING: ' + $g.Script); $missing++ }
        }
    }
    if ($stepSet -contains 'Firewall') {
        Write-Output ('PLAN Firewall: ' + $firewallDef.Script + ' (守卫态, 禁方向命中即总失败)')
        if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $firewallDef.Script))) { Write-Output ('PLAN MISSING: ' + $firewallDef.Script); $missing++ }
    }
    Write-Output "PLAN KnownBaseline: NoUiTokens:Contracts expects 'ContractTypes.cs:Glazier' (V2-02 冻结基线; 如实双记, 不计入总失败)"
    if ($missing -gt 0) { Write-Output ('PLAN: ' + $missing + ' component(s) MISSING'); exit 1 }
    Write-Output 'PLAN complete'
    exit 0
}

# ── 执行 ──
$runDir = Join-Path $RepoRoot ('artifacts/fullsuite/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $runDir | Out-Null
$forbiddenTokens = @('UnityEngine', 'SDG.Unturned', 'Glazier', 'Sleek', 'BepInEx', 'Harmony', 'LaunchMultiplayerNet', 'Steamworks')

function ConvertTo-Arg([string]$a) {
    if ($a -match '\s') { return '"' + ($a.Replace('"', '\"')) + '"' }
    return $a
}

function Invoke-Step {
    param([string]$Id, [string]$Exe, [string[]]$ArgList, [int]$TimeoutSeconds = 1800)
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Exe
    $psi.Arguments = (($ArgList | ForEach-Object { ConvertTo-Arg $_ }) -join ' ')
    $psi.WorkingDirectory = $RepoRoot
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $psi.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    $proc = [System.Diagnostics.Process]::Start($psi)
    $outTask = $proc.StandardOutput.ReadToEndAsync()
    $errTask = $proc.StandardError.ReadToEndAsync()
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $logPath = Join-Path $runDir (($Id -replace '[^A-Za-z0-9._-]', '_') + '.log')
    if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
        try { & taskkill /PID $proc.Id /T /F 2>$null | Out-Null } catch { }
        return @{ Id = $Id; ExitCode = $null; Output = ''; Errors = ('timeout after ' + $TimeoutSeconds + 's'); Log = ''; Seconds = $sw.Elapsed.TotalSeconds }
    }
    $proc.WaitForExit() | Out-Null
    $out = $outTask.Result
    $err = $errTask.Result
    ($out + $err) | Set-Content -LiteralPath $logPath -Encoding UTF8
    return @{ Id = $Id; ExitCode = $proc.ExitCode; Output = $out; Errors = $err; Log = $logPath; Seconds = $sw.Elapsed.TotalSeconds }
}

# 已知基线校验: 折叠空白后, 无界面词命中必须恰为声明的那一个(文件:词), 出现任何其他命中=假。
function Test-KnownBaselineOnly {
    param([string]$GateOutput, [string]$Expected)
    # 充分判据: 折叠空白后(Write-Error 折行兜底) 恰有一个无界面词命中 且 输出含声明的 文件:词 全串。
    $collapsed = ($GateOutput -replace '\s+', '')
    if (-not $collapsed.Contains($Expected)) { return $false }
    $tokenAlt = ($forbiddenTokens | ForEach-Object { [regex]::Escape($_) }) -join '|'
    $hits = @([regex]::Matches($collapsed, '\.cs:(' + $tokenAlt + ')'))
    return ($hits.Count -eq 1)
}

$results = New-Object System.Collections.Generic.List[object]
function Add-Result([string]$Id, [string]$Status, [string]$Detail) {
    $script:results.Add(@{ Id = $Id; Status = $Status; Detail = $Detail })
    $suffix = ''
    if ($Detail) { $suffix = ' ' + $Detail }
    Write-Output ('[' + $Id + '] ' + $Status + $suffix)
}

if ($stepSet -contains 'Build') {
    $r = Invoke-Step -Id 'Build' -Exe 'dotnet' -ArgList @('msbuild', 'BetterUnturnedExperience.sln', '-t:Rebuild', '-p:Configuration=Release', '-v:m', '-nologo') -TimeoutSeconds 1800
    $detail = ('exit=' + $r.ExitCode + ' ' + [Math]::Round($r.Seconds, 1) + 's')
    Add-Result 'Build' ($(if ($r.ExitCode -eq 0) { 'PASS' } else { 'FAILED' })) $detail
}

if ($stepSet -contains 'Tests') {
    foreach ($s in $testSuites) {
        $exe = Join-Path $RepoRoot ('tests/BetterUnturnedExperience.' + $s + '.Tests/bin/Release/BetterUnturnedExperience.' + $s + '.Tests.exe')
        $r = Invoke-Step -Id ('Tests:' + $s) -Exe $exe -ArgList @() -TimeoutSeconds 1800
        $detail = ('exit=' + $r.ExitCode + ' ' + [Math]::Round($r.Seconds, 1) + 's')
        Add-Result ('Tests:' + $s) ($(if ($r.ExitCode -eq 0) { 'PASS' } else { 'FAILED' })) $detail
    }
}

if ($stepSet -contains 'Gates') {
    foreach ($g in $gateDefs) {
        if ($g.Id -notlike $GateFilter) { continue }
        $r = Invoke-Step -Id ('Gates:' + $g.Id) -Exe $powershellExe -ArgList (@('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $RepoRoot $g.Script)) + $g.Args) -TimeoutSeconds 600
        $detail = ('exit=' + $r.ExitCode)
        $combined = $r.Output + $r.Errors
        if ($r.ExitCode -eq 0) {
            Add-Result ('Gates:' + $g.Id) 'PASS' $detail
        } elseif ($g.ContainsKey('KnownBaseline') -and (Test-KnownBaselineOnly -GateOutput $combined -Expected $g.KnownBaseline)) {
            Add-Result ('Gates:' + $g.Id) 'KNOWN-BASELINE' ($detail + ' (基线命中 ' + $g.KnownBaseline + ' — V2-02 冻结基线, 如实双记, 不计入总失败)')
        } else {
            Add-Result ('Gates:' + $g.Id) 'FAILED' ($detail + ' (log: ' + $r.Log + ')')
        }
    }
}

if ($stepSet -contains 'Firewall') {
    # 守卫态(DEV-V6-02F): 命中即总失败。exit 0 且脚注在位=PASS; 其余(命中/脚注缺失/脚本挂)=FAILED。
    $r = Invoke-Step -Id 'Firewall' -Exe $powershellExe -ArgList (@('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $RepoRoot $firewallDef.Script)) + $firewallDef.Args) -TimeoutSeconds 600
    $combined = $r.Output + $r.Errors
    $footer = [regex]::Match($combined, 'Project-firewall metrics: (\d+) violation\(s\) rules-fired=(\S+)')
    if ($r.ExitCode -eq 0 -and $footer.Success -and $footer.Groups[1].Value -eq '0') {
        Add-Result 'Firewall' 'PASS' ('violations=0 rules-fired=' + $footer.Groups[2].Value + ' exit=' + $r.ExitCode + ' (守卫态: 禁方向命中即总失败)')
    } elseif ($footer.Success) {
        Add-Result 'Firewall' 'FAILED' ('守卫命中 violations=' + $footer.Groups[1].Value + ' rules-fired=' + $footer.Groups[2].Value + ' exit=' + $r.ExitCode + ' (log: ' + $r.Log + ')')
    } else {
        Add-Result 'Firewall' 'FAILED' ('防火墙未产出度量脚注, exit=' + $r.ExitCode + ' (log: ' + $r.Log + ')')
    }
}

# ── 汇总 ──
$failed = @($results | Where-Object { $_.Status -eq 'FAILED' })
$knownBaseline = @($results | Where-Object { $_.Status -eq 'KNOWN-BASELINE' })
Write-Output '== summary =='
Write-Output ('steps=' + $results.Count + ' pass=' + ($results.Count - $failed.Count - $knownBaseline.Count) + ' failed=' + $failed.Count + ' known-baseline=' + $knownBaseline.Count)
foreach ($kb in $knownBaseline) { Write-Output ('known-baseline exception (不计入总失败): ' + $kb.Id + ' -> ' + $kb.Detail) }
if ($failed.Count -gt 0) {
    foreach ($f in $failed) { Write-Output ('failed: ' + $f.Id + ' -> ' + $f.Detail) }
    Write-Output 'FULLSUITE: FAIL'
    exit 1
}
Write-Output 'FULLSUITE: PASS'
exit 0
