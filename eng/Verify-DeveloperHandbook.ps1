# Gate: the human-facing developer handbook layer (DEV-V5-01 / V5-T2 frozen shape).
# A first-time ecosystem plugin author must land on docs/developer/ (one overview
# diagram + three short chapters) and be led onward to the SDK (the single source
# of contract truth) and the NoOp fixture (the only runnable example) — instead of
# falling into retired .scratch long-form specs. This gate pins:
#   1. both handbook-layer files exist and are non-empty (short-chapter size caps);
#   2. the handbook carries the required shape anchors (总图 / 模块结构 /
#      最小接入流程 / NoOp 范例导读 + a fenced diagram block);
#   3. both files link onward to the SDK and the NoOp fixture (guide, never island);
#   4. the human-facing text copies no contract bookkeeping: no diagnostic-code
#      tokens, no phase/ticket ids, no version-ledger phrasing;
#   5. the authority declarations exist (以 SDK 为准 / 不扩展 SDK 契约);
#   6. the root README's ecosystem section points at docs/developer/ BEFORE the
#      SDK link (official-first consumption of the new entry);
#   7. the named superseded .scratch long docs carry the SUPERSEDED/历史资料 banner
#      with pointers to SDK + developer entry, inside their first 12 lines.
# Anchors are Ordinal substring/regex checks over whole files (or first-12-line
# slices for markers). The handbook explains the SDK; it must never re-define it.
param([Parameter(Mandatory=$true)][string]$RepoRoot)
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Read-Utf8Raw([string]$rel) {
    $full = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $rel))
    if (-not (Test-Path -LiteralPath $full)) { return $null }
    return Get-Content -LiteralPath $full -Raw -Encoding UTF8
}
function Test-Has([string]$hay, [string]$needle) {
    if ([string]::IsNullOrEmpty($hay)) { return $false }
    return ($hay.IndexOf($needle, [System.StringComparison]::Ordinal) -ge 0)
}

$violations = @()
function Fail([string]$id, [string]$detail) { $script:violations += ('FAIL {0}: {1}' -f $id, $detail) }
function Check-Has([string]$id, [string]$text, [string]$needle, [string]$where) {
    if ($null -eq $text) { Fail $id ('missing file for check: ' + $where); return }
    if (-not (Test-Has $text $needle)) { Fail $id ($where + ' lacks anchor: ' + $needle) }
}

$entryRel     = 'docs/developer/README.md'
$handbookRel  = 'docs/developer/BetterUnturnedExperience-Developer-Handbook.md'
$readmeRel    = 'README.md'

$entry    = Read-Utf8Raw $entryRel
$handbook = Read-Utf8Raw $handbookRel
$readme   = Read-Utf8Raw $readmeRel

# --- 1. existence + size caps (short-chapter discipline) ---
if ([string]::IsNullOrWhiteSpace($entry))    { Fail 'H-ENTRY' ($entryRel + ' missing or empty') }
if ([string]::IsNullOrWhiteSpace($handbook)) { Fail 'H-HANDBOOK' ($handbookRel + ' missing or empty') }
if (-not [string]::IsNullOrWhiteSpace($entry)) {
    $entryLines = ($entry -split "`n").Count
    if ($entryLines -gt 150) { Fail 'H-ENTRY-SIZE' ('entry page ' + $entryLines + ' lines > 150 cap') }
}
if (-not [string]::IsNullOrWhiteSpace($handbook)) {
    $hbLines = ($handbook -split "`n").Count
    if ($hbLines -gt 300) { Fail 'H-HB-SIZE' ('handbook ' + $hbLines + ' lines > 300 cap (一图三短章)') }
}

# --- 2. shape anchors inside the handbook ---
Check-Has 'HB-DIAGRAM'       $handbook '总图'              $handbookRel
Check-Has 'HB-DIAGRAM-BLOCK' $handbook '```text'           $handbookRel
Check-Has 'HB-CH1'           $handbook '模块结构'          $handbookRel
Check-Has 'HB-CH2'           $handbook '最小接入流程'      $handbookRel
Check-Has 'HB-CH3'           $handbook 'NoOp 范例导读'     $handbookRel

# --- 3. guide onward: SDK + NoOp links in BOTH files; entry links the handbook ---
Check-Has 'LINK-SDK-HB'  $handbook 'sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md' $handbookRel
Check-Has 'LINK-SDK-EN'  $entry    'sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md' $entryRel
Check-Has 'LINK-NOOP-HB' $handbook '/BetterUnturnedExperience.NoOpFixture'                 $handbookRel
Check-Has 'LINK-NOOP-EN' $entry    '/BetterUnturnedExperience.NoOpFixture'                 $entryRel
Check-Has 'LINK-HB-EN'   $entry    'BetterUnturnedExperience-Developer-Handbook.md'        $entryRel

# --- 4. no contract bookkeeping copied into human-facing text ---
$forbidden = @(
    @{ id = 'BAN-CODES';          rx = 'BUE-[A-Za-z]+-[0-9]+' },
    @{ id = 'BAN-TICKETS';        rx = 'DEV-V[0-9]+-[0-9]+' },
    @{ id = 'BAN-DTICKETS';       rx = '\bV[0-9]+-T[0-9]+\b' },
    @{ id = 'BAN-PHASE';          rx = 'Phase-[0-9]' },
    @{ id = 'BAN-CONTRACTLEDGER'; rx = '契约版本[:：]?\s*2\.[0-9]+' }
)
foreach ($file in @(@{ rel = $entryRel; text = $entry }, @{ rel = $handbookRel; text = $handbook })) {
    if ([string]::IsNullOrWhiteSpace($file.text)) { continue }
    foreach ($ban in $forbidden) {
        $m = [regex]::Matches($file.text, $ban.rx)
        if ($m.Count -gt 0) {
            Fail $ban.id ($file.rel + ' copies bookkeeping token: ' + (($m | ForEach-Object { $_.Value } | Select-Object -Unique) -join ', '))
        }
    }
}

# --- 5. authority declarations (handbook explains, SDK rules; sample never extends) ---
Check-Has 'AUTH-SDK-HB' $handbook '以 SDK 为准'     $handbookRel
Check-Has 'AUTH-SDK-EN' $entry    '以 SDK 为准'     $entryRel
Check-Has 'AUTH-SAMPLE' $handbook '不扩展 SDK 契约' $handbookRel

# --- 6. root README ecosystem entry: developer page first, SDK second, NoOp kept ---
if ([string]::IsNullOrWhiteSpace($readme)) { Fail 'README-MISSING' 'README.md missing' }
else {
    $secStart = $readme.IndexOf('## 给生态开发者', [System.StringComparison]::Ordinal)
    if ($secStart -lt 0) { Fail 'README-SECTION' 'README lacks the ecosystem-developer section' }
    else {
        $secEnd = $readme.IndexOf("`n## ", $secStart + 3, [System.StringComparison]::Ordinal)
        if ($secEnd -lt 0) { $secEnd = $readme.Length }
        $section = $readme.Substring($secStart, $secEnd - $secStart)
        $devIdx = $section.IndexOf('docs/developer/README.md', [System.StringComparison]::Ordinal)
        $sdkIdx = $section.IndexOf('docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md', [System.StringComparison]::Ordinal)
        if ($devIdx -lt 0) { Fail 'README-ENTRY-LINK' 'ecosystem section does not link docs/developer/README.md' }
        if ($sdkIdx -lt 0) { Fail 'README-SDK-LINK'   'ecosystem section dropped the SDK link' }
        if ($devIdx -ge 0 -and $sdkIdx -ge 0 -and $devIdx -gt $sdkIdx) {
            Fail 'README-ORDER' 'docs/developer/ entry must be linked BEFORE the SDK link'
        }
        if ($section.IndexOf('BetterUnturnedExperience.NoOpFixture', [System.StringComparison]::Ordinal) -lt 0) {
            Fail 'README-NOOP-LINK' 'ecosystem section dropped the NoOp fixture link'
        }
    }
}

# --- 7. named superseded .scratch long docs carry history banners ---
$superseded = @(
    '.scratch/better-unturned-experience-architecture/spec.md',
    '.scratch/better-unturned-experience-architecture/spec.zh-CN.md',
    '.scratch/better-unturned-experience-architecture/Shared-Contract-Spec.md',
    '.scratch/better-unturned-experience-architecture/Module-Lifecycle-Isolation-Spec.md',
    '.scratch/better-unturned-experience-architecture/spec-open-runtime-feature-framework.md',
    '.scratch/better-unturned-experience-architecture/spec-open-runtime-feature-framework.zh-CN.md'
)
foreach ($rel in $superseded) {
    $text = Read-Utf8Raw $rel
    if ([string]::IsNullOrWhiteSpace($text)) { Fail 'SC-MISSING' ($rel + ' missing (must be kept, not deleted)'); continue }
    $head = (($text -split "`r?`n") | Select-Object -First 12) -join "`n"
    $hasMark = (($head.IndexOf('SUPERSEDED', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -or
                (Test-Has $head '历史资料'))
    if (-not $hasMark) { Fail 'SC-MARK' ($rel + ' first 12 lines lack SUPERSEDED/历史资料 mark') }
    if (-not (Test-Has $head 'docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md')) {
        Fail 'SC-SDK-POINTER' ($rel + ' banner does not point to the SDK contract')
    }
    if (-not (Test-Has $head 'docs/developer/README.md')) {
        Fail 'SC-DEV-POINTER' ($rel + ' banner does not point to docs/developer/README.md')
    }
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Output $_ }
    Write-Output ('Developer-handbook gate FAILED: ' + $violations.Count + ' violation(s)')
    exit 1
}
Write-Output 'Developer-handbook gate PASS: handbook layer + README entry order + 6 superseded banners'
exit 0
