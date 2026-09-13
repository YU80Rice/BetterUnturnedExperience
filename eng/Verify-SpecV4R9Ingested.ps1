# Gate: the Phase-4 spec body must carry the V4-R9 live-injection ruling, the Phase-4 map
# pointer must say the ruling was ingested into the spec, and the Plugin.Tests pump-path
# anchors must still exist. Context (POST-P4-06): V4-R9 (Running 态经既有 Tick 泵对存活仪表
# 盘幂等补注入 + 全页在册静默短路) shipped in v5 but lived only as a map Decisions addendum;
# without this tri-directional anchor the next phase could re-implement the retired
# 「仅 ctor 注入」 semantics. Anchors are Ordinal substring checks, tied to their region /
# line: a clause drifting out of the V4-R9 block or the pointer line loses proof power and
# fails. R2 (Spec R1 finding): block-scoped spec anchors, same-line map anchors, residual
# forbidden phrases ('spec 冻结正文不动', '时并入正文') banned in both docs. R3 (Spec R2
# finding): Testing anchors are bound to the '## Testing Decisions' section slice — the
# section ends at the next heading line, so an anchor drifting below it fails too.
param([Parameter(Mandatory=$true)][string]$RepoRoot)
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Read-Utf8Raw([string]$rel) {
    $full = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $rel))
    if (-not (Test-Path -LiteralPath $full)) { return $null }
    return Get-Content -LiteralPath $full -Raw -Encoding UTF8
}

$specRel  = '.scratch/bue-v2-phase4-visual-experience/spec.md'
$mapRel   = '.scratch/bue-v2-phase4-visual-experience/map.md'
$testsRel = 'tests/BetterUnturnedExperience.Plugin.Tests/Program.cs'

# spec.md — V4-R9 clause anchors, required INSIDE the LIT-section V4-R9 block
# (ticket 06 期望行为: ctor覆盖 / 泵补注入 / 非Running不新增 / 拆除重试咬合 / 静默短路).
$specBlockStart = '**V4-R9 实时注入'
$specBlockEnd   = '- 两条 ClientPreference Choice'
$specBlockAnchors = @(
    '九态门表零改动',
    '新构造',
    '覆盖在册',
    '既有 Tick 泵',
    '16 拍节流、首拍即试',
    '幂等补注入',
    '非 Running 态不得新增按钮',
    '下次拆除重试',
    '静默短路',
    '真做事才打日志'
)
# spec.md — Testing Decisions §6 must name the frozen pump-path test group, INSIDE the
# Testing section (bounded by the next heading line), not merely before end of file.
$specTestingSection  = '## Testing Decisions'
$specTestingAnchors  = @('泵路径五锚', 'DEV-V4-09 停用→再启用经模块泵实时重注入')
# spec.md + map.md — retired-wording residue must not appear anywhere in either body.
$residueForbidden = @('spec 冻结正文不动', '时并入正文')
# map.md — on the V4-R9 decision line itself: the pointer must state ingestion, carry the
# ticket id and gate, and name the spec section it points at.
$mapLineKey    = '[V4-R9 实时注入裁决'
$mapAnchors    = @('已并入 spec', 'POST-P4-06', 'Verify-SpecV4R9Ingested', '「LIT 标题栏与全局模式/方向')
# Plugin.Tests — the pump-path behavior stays pinned by these frozen test names.
$testAnchors = @(
    'DEV-V4-09 停用→再启用经模块泵实时重注入',
    '实时注入：Start 期不注入',
    '实时注入：Running 事实下模块泵对存活仪表盘注入五页',
    '实时注入：停用后再启用由模块泵立即重注入五页',
    '实时注入：已在册页不重复注入（在册去重，不画双按钮）',
    '实时注入：全页在册后泵静默短路',
    '实时注入：非 Running 生命周期事实泵不注入'
)

$violations = @()
$spec  = Read-Utf8Raw $specRel
$map   = Read-Utf8Raw $mapRel
$tests = Read-Utf8Raw $testsRel

function Test-Missing([string]$label, [object]$text) {
    if ($text -is [string]) { return $false }
    $script:violations += "${label}: file missing or unreadable"
    return $true
}

if (-not (Test-Missing $specRel $spec)) {
    $start = $spec.IndexOf($specBlockStart, [System.StringComparison]::Ordinal)
    $end   = -1
    if ($start -ge 0) { $end = $spec.IndexOf($specBlockEnd, $start, [System.StringComparison]::Ordinal) }
    if ($start -lt 0 -or $end -le $start) {
        $violations += "${specRel}: V4-R9 block not found between '$specBlockStart' and '$specBlockEnd'"
    } else {
        $block = $spec.Substring($start, $end - $start)
        foreach ($a in $specBlockAnchors) {
            if ($block.IndexOf($a, [System.StringComparison]::Ordinal) -lt 0) {
                $violations += "${specRel}: V4-R9 clause missing INSIDE the LIT-section block: $a"
            }
        }
    }
    $testing = $spec.IndexOf($specTestingSection, [System.StringComparison]::Ordinal)
    if ($testing -lt 0) {
        $violations += "${specRel}: section '$specTestingSection' not found"
    } else {
        # R3: section slice ends at the next heading line (any # level); EOF = no boundary.
        $nextHeading = $spec.IndexOf("`n#", $testing + $specTestingSection.Length, [System.StringComparison]::Ordinal)
        $sectionEnd  = if ($nextHeading -ge 0) { $nextHeading } else { $spec.Length }
        $tail = $spec.Substring($testing, $sectionEnd - $testing)
        foreach ($a in $specTestingAnchors) {
            if ($tail.IndexOf($a, [System.StringComparison]::Ordinal) -lt 0) {
                $violations += "${specRel}: Testing §6 anchor missing inside Testing Decisions: $a"
            }
        }
    }
    foreach ($f in $residueForbidden) {
        if ($spec.IndexOf($f, [System.StringComparison]::Ordinal) -ge 0) {
            $violations += "${specRel}: retired wording residue must not return: $f"
        }
    }
}

if (-not (Test-Missing $mapRel $map)) {
    $decisionLine = $null
    foreach ($line in ($map -split "`n")) {
        if ($line.IndexOf($mapLineKey, [System.StringComparison]::Ordinal) -ge 0) { $decisionLine = $line; break }
    }
    if ($null -eq $decisionLine) {
        $violations += "${mapRel}: decision line starting '$mapLineKey' not found"
    } else {
        foreach ($a in $mapAnchors) {
            if ($decisionLine.IndexOf($a, [System.StringComparison]::Ordinal) -lt 0) {
                $violations += "${mapRel}: V4-R9 decision line lacks pointer anchor: $a"
            }
        }
    }
    foreach ($f in $residueForbidden) {
        if ($map.IndexOf($f, [System.StringComparison]::Ordinal) -ge 0) {
            $violations += "${mapRel}: stale claim still present (pointer must reference the spec body): $f"
        }
    }
}

if (-not (Test-Missing $testsRel $tests)) {
    foreach ($a in $testAnchors) {
        if ($tests.IndexOf($a, [System.StringComparison]::Ordinal) -lt 0) {
            $violations += "${testsRel}: pump-path test anchor missing: $a"
        }
    }
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Output "VIOLATION: $_" }
    Write-Output "Spec-V4R9-ingest gate FAIL: $($violations.Count) violation(s)"
    exit 1
}
Write-Output 'Spec-V4R9-ingest gate PASS: V4-R9 clauses block-scoped in spec, map decision line points at the spec section, pump-path tests anchored'
