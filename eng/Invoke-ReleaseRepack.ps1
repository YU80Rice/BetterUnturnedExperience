# Release repack (DEV-V6-02F, V6-T2 合并裁决): 发布期把进玩家文件的工程合成一个 DLL。
# 工程边界已是真分工程(Plugin 经 ProjectReference 组装), 玩家侧单文件由本脚本在发布期恢复:
# 合并集=八工程封闭集——宿主(主件, 身份来源) + 契约/核心/界面/三官方功能/Bii 骨架;
# 发布资格(Release)/传输适配(Transport)/探针夹具(NoOpFixture)/测试工程/仓外样品不进合并集。
#
# 工具 = tools/ILRepack/ILRepack.exe (2.0.48, vendored, 身份哈希在案 audit/2026-09-19/DEV-V6-02F/):
# ILRepack 2.0.48 输出字节确定(输入同则输出同), 是「三轮重建逐字节一致」验收的工具前提;
# 不加 /copyattrs(只保留主件程序集属性, 避免八份 AssemblyInfo 冲突)、不加 /internalize
# (可见性原样保留——各工程唯一公开组装类型不得被批量降内)、/ndebug(DebugType=none 无符号)。
# /lib 指向仓外 Libs(游戏/框架引用解析路径, 与各 csproj HintPath 同源)。
#
# 用法: powershell -NoProfile -ExecutionPolicy Bypass -File eng\Invoke-ReleaseRepack.ps1 [-Plan] [-Rebuild] [-OutDir <dir>] [-RepoRoot <仓库根>] [-Tool <ILRepack.exe>]
#   -Plan     只预检(工具在位 + 八输入在位 + 声明合并集/排除集/重建路径), 不打包(输入缺一 exit 1)
#   -Rebuild  打包前对八源工程逐一 dotnet msbuild -t:Rebuild(短横线开关; 刻意不含测试工程——
#             测试 exe 在运行中会被 sln 全量重建的清理/重写卡死)
#   -OutDir   输出目录(默认 artifacts/repack/<时间戳>)
# 输出契约(机器可读): 输入行 "REPACK-INPUT OK|MISSING <名>"; 脚注
#   "Release-repack: out=<路径> sha256=<hex> size=<字节> inputs=8 rebuild=<True|False>"
#   (sha256=验收锚是文件哈希本身; 三轮重建逐字节一致由 Plugin.Tests 02F 红测组机器锁定)
# 退出码: 0=成功; 1=输入缺失/重建失败/合并失败/输出形状不合规(输出目录须恰一个
#         BetterUnturnedExperience.dll); 2=脚本自身无法运行(工具缺失/仓库根缺失)。
param(
    [string]$RepoRoot,
    [string]$OutDir,
    [switch]$Plan,
    [switch]$Rebuild,
    [string]$Tool
)
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) {
    foreach ($candidate in @((Get-Location).Path, (Join-Path $PSScriptRoot '..'))) {
        $full = [System.IO.Path]::GetFullPath($candidate)
        if (Test-Path -LiteralPath (Join-Path $full 'BetterUnturnedExperience.sln')) { $RepoRoot = $full; break }
    }
}
if (-not $RepoRoot -or -not (Test-Path -LiteralPath (Join-Path $RepoRoot 'BetterUnturnedExperience.sln'))) {
    Write-Output 'Release-repack: ABORT (仓库根未定位, 请在仓库根运行或传 -RepoRoot)'; exit 2
}
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

# ── 合并集(封闭, 与 eng/Verify-ProjectFirewall.ps1 的 $pluginLegalTargets 同源; 组 8 钉同源) ──
$mergeTargets = @('Contracts', 'Core', 'ClientUi', 'Lit', 'Lir', 'Lht', 'Bii')
$primaryProject = 'Plugin'
$excludedProjects = @('Release', 'Transport', 'NoOpFixture', 'test-projects')
$libsDir = Join-Path (Split-Path -Parent $RepoRoot) 'Libs'
if (-not $Tool) { $Tool = Join-Path $RepoRoot 'tools\ILRepack\ILRepack.exe' }

function Get-ProjectDll([string]$shortName) {
    $assemblyName = 'BetterUnturnedExperience.dll'
    if ($shortName -ne $primaryProject) { $assemblyName = 'BetterUnturnedExperience.' + $shortName + '.dll' }
    return Join-Path $RepoRoot ('src\BetterUnturnedExperience.' + $shortName + '\bin\Release\' + $assemblyName)
}

if (-not (Test-Path -LiteralPath $Tool)) { Write-Output ('Release-repack: ABORT (合并工具缺失: ' + $Tool + ')'); exit 2 }

if ($Plan) {
    Write-Output ('== BUE release-repack plan (repo root: ' + $RepoRoot + ') ==')
    Write-Output ('PLAN Tool: ' + $Tool + ' (ILRepack, byte-deterministic merger)')
    Write-Output 'PLAN Rebuild: dotnet msbuild <8 source csproj> -t:Rebuild -p:Configuration=Release (短横线开关, 不含测试工程)'
    Write-Output ('PLAN Merge: primary=' + $primaryProject + ' (player DLL identity) + ' + ($mergeTargets -join ','))
    Write-Output ('PLAN Excluded: ' + ($excludedProjects -join ',') + ' (发布资格/传输适配/探针夹具/测试不进玩家文件)')
    $missing = 0
    foreach ($target in @(@($primaryProject) + $mergeTargets)) {
        $dll = Get-ProjectDll $target
        if (Test-Path -LiteralPath $dll) { Write-Output ('REPACK-INPUT OK BetterUnturnedExperience.' + $target) }
        else { Write-Output ('REPACK-INPUT MISSING BetterUnturnedExperience.' + $target + ' (缺 ' + $dll + ')'); $missing++ }
    }
    if ($missing -gt 0) { Write-Output ('PLAN: ' + $missing + ' input(s) MISSING (先构建解决方案再打包)'); exit 1 }
    Write-Output 'PLAN complete'
    exit 0
}

if ($Rebuild) {
    $projects = @($primaryProject) + $mergeTargets
    foreach ($target in $projects) {
        $proj = Join-Path $RepoRoot ('src\BetterUnturnedExperience.' + $target + '\BetterUnturnedExperience.' + $target + '.csproj')
        Write-Output ('REBUILD ' + $target + ' ...')
        & dotnet msbuild $proj -t:Rebuild -p:Configuration=Release -v:q -nologo
        if ($LASTEXITCODE -ne 0) { Write-Output ('Release-repack: ABORT (重建失败: ' + $target + ', exit=' + $LASTEXITCODE + ')'); exit 1 }
    }
}

$inputDlls = @()
$missingInputs = @()
foreach ($target in (@($primaryProject) + $mergeTargets)) {
    $dll = Get-ProjectDll $target
    if (Test-Path -LiteralPath $dll) { $inputDlls += (Resolve-Path -LiteralPath $dll).Path }
    else { $missingInputs += ('BetterUnturnedExperience.' + $target) }
}
if ($missingInputs.Count -gt 0) {
    Write-Output ('Release-repack: ABORT (合并输入缺失: ' + ($missingInputs -join ', ') + '; 先构建解决方案再打包)')
    exit 1
}

if (-not $OutDir) { $OutDir = Join-Path $RepoRoot ('artifacts\repack\' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$outDll = Join-Path $OutDir 'BetterUnturnedExperience.dll'

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $Tool
$psi.Arguments = (@('/ndebug', ('/lib:' + $libsDir), ('/out:' + $outDll)) + $inputDlls |
    ForEach-Object { '"' + $_ + '"' }) -join ' '
$psi.WorkingDirectory = $RepoRoot
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
$psi.StandardErrorEncoding = [System.Text.Encoding]::UTF8
$proc = [System.Diagnostics.Process]::Start($psi)
$stdout = $proc.StandardOutput.ReadToEnd()
$stderr = $proc.StandardError.ReadToEnd()
$proc.WaitForExit()
if ($stdout) { Write-Output $stdout }
if ($stderr) { Write-Output $stderr }
if ($proc.ExitCode -ne 0) { Write-Output ('Release-repack: ABORT (ILRepack exit=' + $proc.ExitCode + ')'); exit 1 }

# ── 输出形状守卫: 玩家侧发布物=恰一个 BetterUnturnedExperience.dll ──
$outDlls = @(Get-ChildItem -LiteralPath $OutDir -File -Filter '*.dll')
if ($outDlls.Count -ne 1 -or $outDlls[0].Name -ne 'BetterUnturnedExperience.dll') {
    Write-Output ('Release-repack: ABORT (输出形状不合规: ' + $outDlls.Count + ' 个 DLL, 须恰一个 BetterUnturnedExperience.dll)')
    exit 1
}
$sha = [System.Security.Cryptography.SHA256]::Create()
$stream = [System.IO.File]::OpenRead($outDll)
$hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '')
$stream.Close()
$sha.Dispose()
Write-Output ('Release-repack: out=' + $outDll + ' sha256=' + $hash + ' size=' + $outDlls[0].Length + ' inputs=' + $inputDlls.Count + ' rebuild=' + $Rebuild.IsPresent)
exit 0
