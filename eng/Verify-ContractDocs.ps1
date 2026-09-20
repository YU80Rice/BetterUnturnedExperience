# Gate: contract-documentation precision (DEV-V6-06, V6-T6 五项裁定的机器化).
# 契约文档必须诚实——手册解释 SDK 而不复述，SDK 的码表/未实装节/登记工件与源码事实一致，
# 摘要算法单源（玩家合并集里恰一份定义 + 受理路径调用它）。本门禁钉死四类事实：
#   1. 隔离措辞：手册只许指向 SDK 附录 A.3 并写明合作式隔离（不是沙箱），
#      不得再复述隔离范围（复述句即违规——两份文档各写一套隔离范围正是被修的缺陷）；
#   2. 宿主未就绪码 `BUE-HOST-001` 进 SDK 附录 B.2 平台自检码表（源码字面量在册）；
#   3. SDK 有且只有一个固定节「未实装（本版本）」：收录能力协商协议与 FeatureStatusChangedEvent、
#      写明能力询问窗口「能问、本版本没有可问的能力」；**登记工件 FeatureDefinitionArtifact 不得入内**
#      （它是生产登记必经 DTO，2026-09-18 追加裁决）；
#   4. 摘要单源：公开函数 `FeatureDefinitionDigest.ComputeArtifactPayloadDigest` 在玩家合并集里恰一份定义，
#      受理路径调用它，且旧的私有载荷摘要实现（`ComputePayloadDigest` 家族）清尽。
# 判据是 Ordinal 子串/正则文本锚（文档不独立创造契约，锚的是「文档与源码事实一致」）。
# 合成夹具逐规则红态由 Plugin.Tests 的 DEV-V6-06 组锁定（rules 清单在 PASS 行逐条列出）。
param([Parameter(Mandatory=$true)][string]$RepoRoot)
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

$RuleIds = @(
    'CD-ISO-POINTER', 'CD-ISO-COOP', 'CD-ISO-RESTATE', 'CD-PHASE-POINTER',
    'CD-UNWIRED-HANDBOOK-POINTER', 'CD-UNWIRED-HANDBOOK-LIST',
    'CD-HOSTCODE', 'CD-UNWIRED-SECTION', 'CD-UNWIRED-CAPABILITY', 'CD-UNWIRED-EVENT',
    'CD-UNWIRED-WINDOW', 'CD-UNWIRED-ARTIFACT', 'CD-UNWIRED-BOUNDARY', 'CD-UNWIRED-OFFSCOPE',
    'CD-UNWIRED-SURFACE', 'CD-ARTIFACT-NAME', 'CD-DIGEST-FUNCTION', 'CD-DIGEST-SINGLE-DEF',
    'CD-DIGEST-CALLSITE', 'CD-DIGEST-LEGACY'
)

function Read-Utf8Raw([string]$rel) {
    $full = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $rel))
    if (-not (Test-Path -LiteralPath $full)) { return $null }
    return Get-Content -LiteralPath $full -Raw -Encoding UTF8
}
function Test-Has([string]$hay, [string]$needle) {
    if ([string]::IsNullOrEmpty($hay)) { return $false }
    return ($hay.IndexOf($needle, [System.StringComparison]::Ordinal) -ge 0)
}
# 标题行判定=行首（去空白后）'#' 且含指定串——Get-SectionBody 与 Count-HeadingMatches 共用同一谓词。
function Test-HeadingLine([string]$line, [string]$needle) {
    return ($line.TrimStart().StartsWith('#') -and $line.Contains($needle))
}
# 节体 = 命中小节标题的那一行起，到其后第一行以 '#' 开头的标题（含标题行本身）。
function Get-SectionBody([string]$text, [string]$headingNeedle) {
    if ([string]::IsNullOrEmpty($text)) { return $null }
    $lines = $text -split "`n"
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if (Test-HeadingLine $lines[$i] $headingNeedle) { $start = $i; break }
    }
    if ($start -lt 0) { return $null }
    $end = $lines.Count
    for ($i = $start + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i].TrimStart().StartsWith('#')) { $end = $i; break }
    }
    return (($lines[$start..($end - 1)]) -join "`n")
}
# 「恰一处」判据：标题行（行首 '#'）命中次数。
function Count-HeadingMatches([string]$text, [string]$needle) {
    if ([string]::IsNullOrEmpty($text)) { return 0 }
    $count = 0
    foreach ($line in ($text -split "`n")) {
        if ($line.TrimStart().StartsWith('#') -and $line.Contains($needle)) { $count++ }
    }
    return $count
}

$violations = @()
function Fail([string]$id, [string]$detail) { $script:violations += ('FAIL {0}: {1}' -f $id, $detail) }

$handbookRel = 'docs/developer/BetterUnturnedExperience-Developer-Handbook.md'
$sdkRel      = 'docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md'
$handbook    = Read-Utf8Raw $handbookRel
$sdk         = Read-Utf8Raw $sdkRel

# ── 1. 手册隔离措辞：指向 SDK（隔离 A.3、未实装 A.8）+ 合作式非沙箱 + 相位名；禁止复述 ──
if ([string]::IsNullOrWhiteSpace($handbook)) { Fail 'CD-HANDBOOK-MISSING' ($handbookRel + ' missing or empty') }
else {
    if (-not (Test-Has $handbook '隔离范围以 SDK 附录 A.3 为准')) { Fail 'CD-ISO-POINTER' ($handbookRel + ' lacks the isolation-scope pointer to SDK A.3（手册须指向、不得复述）') }
    if (-not ((Test-Has $handbook '合作式隔离') -and (Test-Has $handbook '不是沙箱'))) { Fail 'CD-ISO-COOP' ($handbookRel + ' lacks the cooperative-not-sandbox wording') }
    if (-not ((Test-Has $handbook '相位名以 SDK') -and (Test-Has $handbook 'FeatureRegistrationPhase'))) { Fail 'CD-PHASE-POINTER' ($handbookRel + ' lacks the phase-name pointer to FeatureRegistrationPhase') }
    # 未实装清单只许 SDK 列；手册只许指向（T6 Q4：手册不列未实装清单，只指向 SDK 该节）。
    if (-not ((Test-Has $handbook '未实装（本版本）') -and (Test-Has $handbook 'A.8'))) {
        Fail 'CD-UNWIRED-HANDBOOK-POINTER' ($handbookRel + ' must point at the SDK 「未实装（本版本）」 section (A.8) instead of listing it')
    }
    # 「只指向」的另一半：手册不得复述清单内容（点名具体未实装项即等于把清单抄了一份）。
    # token 集=A.8 条目的全部具名物：能力协商协议及其三段名（Hello/Snapshot/Ack）、
    # 状态变化事件的三式写法（英文类型名 + 两种中文意译）。文本锚的性质=具名枚举而非语义完备，
    # 未列出的等价改写由审查兜底（如实标注）。
    $handbookList = [regex]::Matches($handbook, '能力协商|Hello(?!Feature)|Snapshot|Ack|FeatureStatusChangedEvent|状态变化事件|状态变更事件')
    if ($handbookList.Count -gt 0) {
        Fail 'CD-UNWIRED-HANDBOOK-LIST' ($handbookRel + ' must not re-list unwired items（指向而不是复述）: ' + (($handbookList | ForEach-Object { $_.Value } | Select-Object -Unique) -join ', '))
    }
    $restate = [regex]::Matches($handbook, '单个功能抛异常|只隔离该功能|抛异常只隔离')
    if ($restate.Count -gt 0) { Fail 'CD-ISO-RESTATE' ($handbookRel + ' restates the isolation scope instead of pointing at the SDK: ' + (($restate | ForEach-Object { $_.Value } | Select-Object -Unique) -join ', ')) }
}

# ── 2/3. SDK：宿主码进 B.2；未实装固定节唯一且内容在位；登记工件不入该节 ──
if ([string]::IsNullOrWhiteSpace($sdk)) { Fail 'CD-SDK-MISSING' ($sdkRel + ' missing or empty') }
else {
    $platform = Get-SectionBody $sdk 'B.2'
    if ($null -eq $platform) { Fail 'CD-HOSTCODE' ($sdkRel + ' lacks the B.2 platform self-check section') }
    elseif (-not (Test-Has $platform 'BUE-HOST-001')) { Fail 'CD-HOSTCODE' ($sdkRel + ' B.2 table lacks the host-not-ready code BUE-HOST-001') }

    $sectionCount = Count-HeadingMatches $sdk '未实装（本版本）'
    if ($sectionCount -ne 1) { Fail 'CD-UNWIRED-SECTION' ($sdkRel + ' must carry exactly one 「未实装（本版本）」 section, found ' + $sectionCount) }
    else {
        $unwired = Get-SectionBody $sdk '未实装（本版本）'
        if ($null -eq $unwired) { Fail 'CD-UNWIRED-SECTION' ($sdkRel + ' 「未实装（本版本）」 section body not extractable') }
        else {
            if (-not ((Test-Has $unwired '能力协商') -and (Test-Has $unwired 'Hello') -and (Test-Has $unwired 'Snapshot') -and (Test-Has $unwired 'Ack'))) {
                Fail 'CD-UNWIRED-CAPABILITY' ('「未实装（本版本）」 must name the capability-negotiation protocol (Hello/Snapshot/Ack)')
            }
            if (-not (Test-Has $unwired 'FeatureStatusChangedEvent')) { Fail 'CD-UNWIRED-EVENT' ('「未实装（本版本）」 must collect FeatureStatusChangedEvent') }
            if (-not ((Test-Has $unwired 'IDependencyCapabilityView') -and (Test-Has $unwired '能问') -and (Test-Has $unwired '没有可问的能力'))) {
                Fail 'CD-UNWIRED-WINDOW' ('「未实装（本版本）」 must state the query window honestly (injectable, askable, nothing to ask this version)')
            }
            if (Test-Has $unwired 'FeatureDefinitionArtifact') {
                Fail 'CD-UNWIRED-ARTIFACT' ('「未实装（本版本）」 must NOT list the registration artifact FeatureDefinitionArtifact（生产登记必经 DTO，2026-09-18 追加裁决）')
            }
            # 本节只收**契约面**未实装：词汇表层面的治理意图标未实装的地点在 CONTEXT.md
            # （T6 Q4 三层落点分工）。反向判据=节体（整段，不限条目行与标记符）不得出现治理物：
            # V6-T4 Q2 点名的 CONTEXT 未实装词条全集（规范功能定义 / 功能定义编译 / 功能定义片段 /
            # 链接功能定义包 / 定义产物 / 资格义务 / 证据案例 / 技术资格裁决）+ 治理内核通称
            # （链接管线 / 资格门）+ V6-T4 Q2 点名的七个治理内核类型名（Linker/LoadGate/Evaluator/
            # EvidenceCase/EvidencePackage/Gate/CandidateBuild）。正向判据=节体写明该归属
            # （CONTEXT.md 在场）。文本锚的性质=具名枚举而非语义完备，未列出的等价改写由审查兜底。
            if (-not (Test-Has $unwired 'CONTEXT.md')) {
                Fail 'CD-UNWIRED-BOUNDARY' ('「未实装（本版本）」 must state where vocabulary-level（非契约面）未实装各归其位（CONTEXT.md 词条）')
            }
            # 两条配合的方向（黑名单查「已知越界物」、白名单查「任何未声明的新条目」）：
            # 先用 CD-UNWIRED-OFFSCOPE 扫全节（含散文），再用 CD-UNWIRED-SURFACE 逐条目对声明清单。
            $offscope = [regex]::Matches($unwired, '链接管线|资格门|规范功能定义|功能定义编译|功能定义片段|链接功能定义包|定义产物|资格义务|证据案例|技术资格裁决|LoadSetIdentity|发布授权|FeatureDefinitionLinker|FeatureLoadGate|QualificationEvaluator|EvidenceCase|RuntimeEvidencePackage|QualificationEvidenceGate|CandidateBuild')
            if ($offscope.Count -gt 0) {
                Fail 'CD-UNWIRED-OFFSCOPE' ('「未实装（本版本）」 must stay contract-surface; vocabulary-level governance items belong to CONTEXT.md: ' + (($offscope | ForEach-Object { $_.Value } | Select-Object -Unique) -join ', '))
            }
            # 条目白名单（防「换写法塞条目」）：条目提取**不限标记符**（-/+/*/数字皆可），条目标题
            # （标记符到第一个全角冒号之间）必须**恰等于**已声明的契约面未实装项标题，且条目数恰等于
            # 声明数——未知条目、夹带条目（标题被改写）、缺失条目一律红。新增一项契约面未实装项=
            # 同时改本节与这里的声明（强制一次显式决定）；条目正文（冒号之后）不冻结。
            # 边界：节内散文段（前言/边界说明）不构成条目；在其中以散文形式罗列未实装项由审查兜底
            # （锚的性质=具名枚举 + 条目白名单，非语义完备）。
            $declaredTitles = @('**能力协商协议（Hello / Snapshot / Ack）未启用**', '**`FeatureStatusChangedEvent` 未接线**')
            $items = @()
            foreach ($line in ($unwired -split "`n")) {
                $itemMatch = [regex]::Match($line, '^\s*(?:[-+*]|\d+[.)])\s+(.*)$')
                if ($itemMatch.Success) { $items += $itemMatch.Groups[1].Value.Trim() }
            }
            $badTitles = @()
            foreach ($item in $items) {
                $colon = $item.IndexOf('：')
                $title = $(if ($colon -ge 0) { $item.Substring(0, $colon) } else { $item }).Trim()
                if ($declaredTitles -notcontains $title) { $badTitles += $title }
            }
            if ($items.Count -ne $declaredTitles.Count -or $badTitles.Count -gt 0) {
                Fail 'CD-UNWIRED-SURFACE' ('「未实装（本版本）」 entries must be exactly the declared contract-surface unwired item titles (marker-agnostic; 新增一项须同时声明进门禁清单): items=' + $items.Count + '/' + $declaredTitles.Count + ' unexpected: ' + ($badTitles -join ' | '))
            }
        }
    }

    $reference = Get-SectionBody $sdk '4. 编译期引用指引'
    if ($null -eq $reference) { Fail 'CD-ARTIFACT-NAME' ($sdkRel + ' lacks §4 编译期引用指引') }
    else {
        if (-not (Test-Has $reference 'FeatureDefinitionArtifact')) { Fail 'CD-ARTIFACT-NAME' ($sdkRel + ' §4 lacks the registration artifact type name FeatureDefinitionArtifact') }
        if (-not (Test-Has $reference 'FeatureDefinitionDigest.ComputeArtifactPayloadDigest')) { Fail 'CD-DIGEST-FUNCTION' ($sdkRel + ' §4 lacks the public digest function FeatureDefinitionDigest.ComputeArtifactPayloadDigest') }
    }
}

# ── 4. 摘要单源：恰一份公开定义 + 受理路径调用 + 旧私有实现清尽 ──
$srcRoot = Join-Path $RepoRoot 'src'
$definitions = @()
$legacy = @()
if (Test-Path -LiteralPath $srcRoot) {
    foreach ($file in (Get-ChildItem -LiteralPath $srcRoot -Recurse -Filter '*.cs' -File)) {
        $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        if ($text -match 'static\s+Digest256\s+ComputeArtifactPayloadDigest\s*\(') { $definitions += $file.FullName }
        if ($text -match 'static\s+Digest256\s+ComputePayloadDigest\s*\(') { $legacy += $file.FullName }
    }
}
if ($definitions.Count -ne 1) {
    Fail 'CD-DIGEST-SINGLE-DEF' ('the public artifact-payload digest must be DEFINED exactly once in src/, found ' + $definitions.Count + ': ' + ($definitions -join ', '))
} elseif (-not ($definitions[0].Replace('\', '/').EndsWith('src/BetterUnturnedExperience.Contracts/ContractTypes.cs'))) {
    Fail 'CD-DIGEST-SINGLE-DEF' ('the single digest definition must live in the contract surface, found: ' + $definitions[0])
}
if ($legacy.Count -gt 0) {
    Fail 'CD-DIGEST-LEGACY' ('legacy private payload-digest implementations must be retired (双算法残留): ' + ($legacy -join ', '))
}
$runtimeFile = Join-Path $srcRoot 'BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs'
if (-not (Test-Path -LiteralPath $runtimeFile)) {
    Fail 'CD-DIGEST-CALLSITE' ('admission path source missing: ' + $runtimeFile)
} else {
    $runtimeText = Get-Content -LiteralPath $runtimeFile -Raw -Encoding UTF8
    if (-not (Test-Has $runtimeText 'FeatureDefinitionDigest.ComputeArtifactPayloadDigest')) {
        Fail 'CD-DIGEST-CALLSITE' ('the admission path must compute through the public digest function (single source)')
    }
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Output $_ }
    Write-Output ('Contract-docs gate FAILED: ' + $violations.Count + ' violation(s)')
    exit 1
}
Write-Output ('Contract-docs gate PASS (rules=' + $RuleIds.Count + '): ' + ($RuleIds -join ', '))
exit 0
