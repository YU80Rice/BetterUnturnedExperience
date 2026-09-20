# Gate: project firewall (DEV-V6-01, V6-T2 追加裁决). 本票角色=守卫(DEV-V6-02F 翻转): 禁方向
# 命中即违规, 退出 1, 调用方 eng/Run-FullSuite.ps1 把非零命中计为总失败(真分工程出门后回潮阻断
# 交付)。规则本体自度量态(照相)以来不变——翻转只改调用方对退出码的裁量, 不改本脚本判据。
#
# 工程模型: src/ 下每个 BetterUnturnedExperience.* 目录 = 一个工程; 目录名 = 工程标识 =
# 命名空间根。规则:
#   R1 ref-direction   工程间引用只许经契约(BetterUnturnedExperience.Contracts) + 组装根合法边
#                      (Plugin 指向官方功能 Lit/Lir/Lht、界面 ClientUi、核心 Core、契约) + 两条
#                      具名非功能边(DEV-V6-02F: Transport→Core、NoOpFixture→Plugin, 见下)。
#                      扫描源码 using 指令、全限定名 token(文本级, 含注释/字符串)与
#                      ProjectReference 工程引用边。其余一切方向(功能互指、功能指宿主、
#                      ClientUi 指整理/宿主、宿主外工程指 Core 等)逐命中在册。
#   R2 manifest-equals-dir  清单必须等于目录文件集(递归 .cs, 排除 bin/obj, 正斜杠比较):
#                      有 csproj 的工程=其 <Compile> 清单(非 ..\ 项)必须恰等目录文件集
#                      (通配 Include 视为违规, 显式清单是本规则的前提); 无 csproj 的功能目录
#                      =Plugin 平铺清单中该目录的项必须恰等目录文件集(embed-manifest 形态),
#                      目录有源码却零平铺=embed-manifest-absent。
#   R3 project-file    每个工程目录恰有一个 .csproj(缺失=project-file-absent, 多个=ambiguous)。
#   R4 foreign-embed   禁止把外目录源码编进自己: 任何工程 csproj 的 <Compile Include="..\...">。
#
# 用法: powershell -NoProfile -ExecutionPolicy Bypass -File eng\Verify-ProjectFirewall.ps1 -RepoRoot <仓库根>
# 输出契约(机器可读): 违规行前缀 "VIOLATION R<n> "; 汇总脚注
#   "Project-firewall metrics: <N> violation(s) rules-fired=<R1,R2,...|->"
# 退出码: 0=零命中(守卫态唯一通过); 1=有命中(守卫态=总失败); 2=脚本自身无法运行(src 缺失等)。
param([Parameter(Mandatory=$true)][string]$RepoRoot)
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$srcRoot = Join-Path $RepoRoot 'src'
if (-not (Test-Path -LiteralPath $srcRoot)) { Write-Output "Project-firewall metrics: src root missing: $srcRoot"; exit 2 }

$violations = New-Object System.Collections.Generic.List[string]
function Add-Violation([string]$rule, [string]$detail) { $script:violations.Add("VIOLATION $rule $detail") }
function ShortName([string]$projectId) { return $projectId.Substring('BetterUnturnedExperience.'.Length) }

# ── 工程发现: src/BetterUnturnedExperience.* 目录 ──
$projectDirs = @(Get-ChildItem -LiteralPath $srcRoot -Directory |
    Where-Object { $_.Name -like 'BetterUnturnedExperience.*' } | Sort-Object Name)
$projectIds = @($projectDirs | ForEach-Object { $_.Name })
if ($projectDirs.Count -eq 0) { Write-Output 'Project-firewall metrics: no project dirs found'; exit 2 }

function Get-ProjectFiles([System.IO.DirectoryInfo]$dir) {
    # 递归 .cs 相对路径集: 正斜杠、排除 bin/obj、排序保证输出确定性。
    $prefix = $dir.FullName.TrimEnd('\') + '\'
    return @(Get-ChildItem -LiteralPath $dir.FullName -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        ForEach-Object { $_.FullName.Substring($prefix.Length).Replace('\', '/') } |
        Sort-Object)
}

# 组装根合法边=封闭白名单: 宿主指向官方功能(Lit/Lir/Lht)、界面(ClientUi)、核心(Core)、契约(Contracts)、
# Bii(V6-T2 追加点名)——白名单外不放行。DEV-V6-02F 守卫态收编两条具名非功能边(封闭枚举逐条点名,
# 不开模式; 01 度量期「Release/Transport/NoOpFixture 等不放行」的暂记立场至此定形):
#   Transport→Core  传输适配器实现宿主传输缝端口(Core.Network.INetworkTransport)——适配器模式的
#                   本体关系, 非特权通道; Transport.dll 不进玩家合并集(eng/Invoke-ReleaseRepack.ps1)。
#   NoOpFixture→Plugin  保留段第二插件经宿主公开登记门(BueRuntimeHost.Register, 仅公开 API、
#                   无友元)消费宿主——与 DEV-V6-09 仓外样品对发布 DLL 的消费关系同构; 该边隐藏
#                   不得(改文件引用=从工程图蒸发), 夹具 DLL 不进玩家合并集。
$pluginLegalTargets = @(
    'BetterUnturnedExperience.Contracts',
    'BetterUnturnedExperience.Core',
    'BetterUnturnedExperience.ClientUi',
    'BetterUnturnedExperience.Lit',
    'BetterUnturnedExperience.Lir',
    'BetterUnturnedExperience.Lht',
    'BetterUnturnedExperience.Bii'
)
# DEV-V6-02F 具名非功能边(封闭枚举; 每条独立点名, 新增必须同程序式落此表并过双轴审查)。
$namedNonFeatureEdges = @(
    @{ From = 'BetterUnturnedExperience.Transport'; To = 'BetterUnturnedExperience.Core' },
    @{ From = 'BetterUnturnedExperience.NoOpFixture'; To = 'BetterUnturnedExperience.Plugin' }
)
function Test-LegalEdge([string]$From, [string]$To) {
    if ($From -eq $To) { return $true }
    if ($To -eq 'BetterUnturnedExperience.Contracts') { return $true }   # 跨工程只许经契约
    if ($From -eq 'BetterUnturnedExperience.Plugin') { return $pluginLegalTargets -contains $To }    # 组装根封闭白名单
    foreach ($edge in $namedNonFeatureEdges) {                            # 02F 具名边(非功能工程)
        if ($edge.From -eq $From -and $edge.To -eq $To) { return $true }
    }
    return $false
}

# csproj 文本级 Include 提取: 元素属性顺序无关(Condition 等在前亦收)、双/单引号皆收。
# 文本级扫描是本门禁的前提(V6-T2: 扫描含全限定名), 不做完整 XML 解析。
function Get-Includes([string]$csprojText, [string]$element) {
    $values = New-Object System.Collections.Generic.List[string]
    foreach ($m in [regex]::Matches($csprojText, '<' + $element + '\b[^>]*>')) {
        $im = [regex]::Match($m.Value, '\bInclude\s*=\s*"([^"]*)"')
        if (-not $im.Success) { $im = [regex]::Match($m.Value, "\bInclude\s*=\s*'([^']*)'") }
        if ($im.Success) { $values.Add($im.Groups[1].Value) }
    }
    return $values
}

# ── R3: 工程目录恰有一个 .csproj(先行: R2/R4 依赖其结果) ──
$csprojByProject = @{}
foreach ($dir in $projectDirs) {
    $csprojs = @(Get-ChildItem -LiteralPath $dir.FullName -File -Filter '*.csproj' | Sort-Object Name)
    if ($csprojs.Count -eq 0) {
        Add-Violation 'R3' ('project-file-absent: ' + $dir.Name)
        $csprojByProject[$dir.Name] = $null
    } elseif ($csprojs.Count -gt 1) {
        Add-Violation 'R3' ('project-file-ambiguous: ' + $dir.Name + ' count=' + $csprojs.Count)
        $csprojByProject[$dir.Name] = $null
    } else {
        $csprojByProject[$dir.Name] = $csprojs[0]
    }
}

# ── R1: 引用方向(源码 using/全限定名 + ProjectReference 工程引用边) ──
# DEV-V6-02B 豁免: InternalsVisibleTo 指向 *.Tests 的友元声明字符串不计越界——V6-T2 Q6/
# 02A 组 3 点名的测试豁免形态, 字符串里的程序集名不是引用方向; 目标不以 .Tests 结尾不豁免
# (跨功能友元照常在册, 合成夹具双案锁定)。抹除用等长空格, 行号保持不变。
function Get-ScanText([string]$text) {
    $scan = $text
    foreach ($m in [regex]::Matches($text, 'InternalsVisibleTo\s*\(\s*"([^"]+)"\s*\)')) {
        if ($m.Groups[1].Value -like '*.Tests') {
            $len = $m.Groups[1].Length
            $scan = $scan.Remove($m.Groups[1].Index, $len).Insert($m.Groups[1].Index, (' ' * $len))
        }
    }
    return $scan
}
$fqnRegex = [regex]'BetterUnturnedExperience\.[A-Za-z0-9]+'
foreach ($dir in $projectDirs) {
    $from = $dir.Name
    $files = @(Get-ChildItem -LiteralPath $dir.FullName -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Sort-Object FullName)
    foreach ($file in $files) {
        $text = [System.IO.File]::ReadAllText($file.FullName)
        $scan = Get-ScanText $text
        $seen = @{}
        foreach ($m in $fqnRegex.Matches($scan)) {
            $to = 'BetterUnturnedExperience.' + $m.Value.Substring('BetterUnturnedExperience.'.Length)
            if ($to -eq $from) { continue }
            if ($projectIds -notcontains $to) { continue }
            $preStart = [Math]::Max(0, $m.Index - 12)
            $pre = $scan.Substring($preStart, $m.Index - $preStart)
            $kind = if ($pre -match '(?i)using\s*$') { 'using' } else { 'fqn' }
            $line = ($scan.Substring(0, $m.Index) -split "`n").Count
            $key = "$to|$kind"
            if ($seen.ContainsKey($key)) { $seen[$key].count++ }
            else { $seen[$key] = @{ count = 1; line = $line } }
        }
        $relPath = $file.FullName.Substring($dir.FullName.Length + 1).Replace('\', '/')
        foreach ($key in ($seen.Keys | Sort-Object)) {
            $to = $key.Split('|')[0]; $kind = $key.Split('|')[1]
            if (Test-LegalEdge $from $to) { continue }
            $entry = $seen[$key]
            Add-Violation 'R1' ('ref-direction ' + (ShortName $from) + '->' + (ShortName $to) +
                ' kind=' + $kind + ' hits=' + $entry.count +
                ' @ src/' + $dir.Name + '/' + $relPath + ':' + $entry.line)
        }
    }
}
foreach ($dir in $projectDirs) {
    $csproj = $csprojByProject[$dir.Name]
    if ($null -eq $csproj) { continue }
    $from = $dir.Name
    $text = [System.IO.File]::ReadAllText($csproj.FullName)
    foreach ($inc0 in (Get-Includes $text 'ProjectReference')) {
        $inc = $inc0.Replace('\', '/')
        if ($inc -notmatch 'BetterUnturnedExperience\.([A-Za-z0-9]+)\.csproj$') { continue }
        $to = 'BetterUnturnedExperience.' + $Matches[1]
        if ($to -eq $from) { continue }
        if ($projectIds -notcontains $to) { continue }
        if (Test-LegalEdge $from $to) { continue }
        Add-Violation 'R1' ('ref-direction ' + (ShortName $from) + '->' + (ShortName $to) +
            ' kind=project-reference hits=1 @ src/' + $dir.Name + '/' + $csproj.Name)
    }
}


# ── R2: 清单等于目录文件集 ──
# 形态一: 有 csproj 的工程, <Compile> 清单(非 ..\ 项)恰等目录文件集。
foreach ($dir in $projectDirs) {
    $csproj = $csprojByProject[$dir.Name]
    if ($null -eq $csproj) { continue }
    $files = Get-ProjectFiles $dir
    $text = [System.IO.File]::ReadAllText($csproj.FullName)
    $includes = @(Get-Includes $text 'Compile')
    $own = @($includes | Where-Object { -not $_.StartsWith('..') } | ForEach-Object { ($_.Replace('\', '/') -replace '/+', '/') } | Sort-Object -Unique)
    if (@($own | Where-Object { $_.Contains('*') }).Count -gt 0) {
        Add-Violation 'R2' ('wildcard-include: ' + $dir.Name + ' (清单必须显式等于目录文件集)')
        continue
    }
    $fileSet = New-Object 'System.Collections.Generic.HashSet[string]' -ArgumentList (,[string[]]$files)
    $ownSet = New-Object 'System.Collections.Generic.HashSet[string]' -ArgumentList (,[string[]]$own)
    foreach ($f in $files) {
        if (-not $ownSet.Contains($f)) { Add-Violation 'R2' ('manifest-missing-file: ' + $dir.Name + '/' + $f + ' (在目录、不在清单)') }
    }
    foreach ($o in $own) {
        if (-not $fileSet.Contains($o)) { Add-Violation 'R2' ('manifest-stale-entry: ' + $dir.Name + '/' + $o + ' (在清单、不在目录)') }
    }
}

# ── R2(形态二)+R4: 平铺清单解析 ──
# 无 csproj 的功能目录由 Plugin 平铺清单代管: 该目录的平铺项必须恰等目录文件集;
# 任何工程的 csproj 平铺 ..\BetterUnturnedExperience.* 外目录源码 = foreign-embed(R4)。
$embedMap = @{}    # 目标工程名 -> 平铺进来的相对路径列表
$foreignEmbeds = New-Object System.Collections.Generic.List[object]
foreach ($dir in $projectDirs) {
    $csproj = $csprojByProject[$dir.Name]
    if ($null -eq $csproj) { continue }
    $text = [System.IO.File]::ReadAllText($csproj.FullName).Replace('\', '/')
    foreach ($inc0 in (Get-Includes $text 'Compile')) {
        if ($inc0 -notmatch '^\.\./+BetterUnturnedExperience\.([A-Za-z0-9]+)/+(.+)$') { continue }
        $target = 'BetterUnturnedExperience.' + $Matches[1]
        $rel = ($Matches[2] -replace '/+', '/')
        if (-not $embedMap.ContainsKey($target)) { $embedMap[$target] = New-Object System.Collections.Generic.List[string] }
        $embedMap[$target].Add($rel)
        $foreignEmbeds.Add(@{ consumer = $dir.Name; target = $target })
    }
}
foreach ($entry in ($foreignEmbeds | Group-Object { $_.consumer + '|' + $_.target } | Sort-Object Name)) {
    $parts = $entry.Name.Split('|')
    Add-Violation 'R4' ('foreign-embed: ' + $parts[0] + ' embeds ' + $parts[1] + ' files=' + $entry.Count)
}
foreach ($dir in $projectDirs) {
    if ($null -ne $csprojByProject[$dir.Name]) { continue }    # 自有 csproj 的工程走形态一
    $files = Get-ProjectFiles $dir
    if ($files.Count -eq 0) { continue }
    $embedded = @()
    if ($embedMap.ContainsKey($dir.Name)) { $embedded = @($embedMap[$dir.Name] | Sort-Object -Unique) }
    if ($embedded.Count -eq 0) {
        Add-Violation 'R2' ('embed-manifest-absent: ' + $dir.Name + ' (目录有源码但无任何平铺代管清单)')
        continue
    }
    $fileSet = New-Object 'System.Collections.Generic.HashSet[string]' -ArgumentList (,[string[]]$files)
    $embedSet = New-Object 'System.Collections.Generic.HashSet[string]' -ArgumentList (,[string[]]$embedded)
    foreach ($f in $files) {
        if (-not $embedSet.Contains($f)) { Add-Violation 'R2' ('embed-manifest-missing-file: ' + $dir.Name + '/' + $f + ' (在目录、不在平铺清单)') }
    }
    foreach ($e in $embedded) {
        if (-not $fileSet.Contains($e)) { Add-Violation 'R2' ('embed-manifest-stale-entry: ' + $dir.Name + '/' + $e + ' (在平铺清单、不在目录)') }
    }
}

# ── 汇总脚注(机器可读; 排序保证输出确定性) ──
$violations.Sort()
foreach ($v in $violations) { Write-Output $v }
$ruleCounts = @($violations | ForEach-Object { ($_ -split ' ')[1] } | Group-Object | Sort-Object Name)
foreach ($rc in $ruleCounts) { Write-Output ('RULE ' + $rc.Name + ' hits=' + $rc.Count) }
$rulesFired = if ($ruleCounts.Count -gt 0) { ($ruleCounts | ForEach-Object { $_.Name }) -join ',' } else { '-' }
Write-Output ("Project-firewall metrics: " + $violations.Count + " violation(s) rules-fired=" + $rulesFired)
if ($violations.Count -gt 0) { exit 1 }
exit 0
