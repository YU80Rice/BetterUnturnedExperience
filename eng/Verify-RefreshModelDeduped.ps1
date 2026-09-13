# Gate: F2 "rebuild catalog then paint current detail" must be a single entry.
# Context (POST-P4-07): DEV-V4-09 F2 inlined the same refresh-then-render sequence on
# save-stay, confirm-stay, and confirm-leave (dirty refresh included). Product-correct,
# but three copies. Ticket 07 collapses them to RefreshCatalogThenPaintCurrentDetail.
# Anchors are Ordinal substring checks, tied to method slices: a copy drifting back
# into CommitDraftAndStatus / ResolveConfirm, or the helper losing the lifecycle
# gate, fails. Open / clean RequestRefresh may still call refreshModel() — they are
# not the F2 triad. Plugin-draft fill copies stay out of scope.
param([Parameter(Mandatory=$true)][string]$RepoRoot)
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Read-Utf8Raw([string]$rel) {
    $full = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $rel))
    if (-not (Test-Path -LiteralPath $full)) { return $null }
    return Get-Content -LiteralPath $full -Raw -Encoding UTF8
}

function Get-MethodSlice([string]$text, [string]$signature) {
    $start = $text.IndexOf($signature, [System.StringComparison]::Ordinal)
    if ($start -lt 0) { return $null }
    $searchFrom = $start + $signature.Length
    $next = $text.IndexOf("`r`n        private void ", $searchFrom, [System.StringComparison]::Ordinal)
    if ($next -lt 0) { $next = $text.IndexOf("`n        private void ", $searchFrom, [System.StringComparison]::Ordinal) }
    if ($next -lt 0) { $next = $text.Length }
    return $text.Substring($start, $next - $start)
}

$panelRel = 'src/BetterUnturnedExperience.Plugin/BueNativeManagementPanel.cs'
$testsRel = 'tests/BetterUnturnedExperience.ClientUi.Tests/DevV4DraftTests.cs'
# Signatures include '(' so a renamed helper/test (prefix residue) cannot satisfy the anchor.
$helperSig = 'private void RefreshCatalogThenPaintCurrentDetail('
$commitSig = 'private void CommitDraftAndStatus('
$confirmSig = 'private void ResolveConfirm('
$helperCall = 'RefreshCatalogThenPaintCurrentDetail('
$helperName = 'RefreshCatalogThenPaintCurrentDetail'
$f2TestName = 'SaveCommittedLifecycleIntentFlag'
$f2Test = 'SaveCommittedLifecycleIntentFlag('
# Inline F2 copies: lifecycle-gated refreshModel() living in the three call-site methods.
$inlineResidues = @(
    'report != null && report.CommittedLifecycleIntent && refreshModel',
    'wasRefresh || (report != null && report.CommittedLifecycleIntent)'
)

$violations = @()
$panel = Read-Utf8Raw $panelRel
$tests = Read-Utf8Raw $testsRel

if ($null -eq $panel) {
    $violations += "${panelRel}: file missing or unreadable"
} else {
    $helper = Get-MethodSlice $panel $helperSig
    $commit = Get-MethodSlice $panel $commitSig
    $confirm = Get-MethodSlice $panel $confirmSig

    if ($null -eq $helper) {
        $violations += "${panelRel}: helper '$helperName' not found (F2 triad must share one entry)"
    } else {
        foreach ($a in @('CommittedLifecycleIntent', 'refreshModel', 'forceRefresh', 'RenderDraftReport')) {
            if ($helper.IndexOf($a, [System.StringComparison]::Ordinal) -lt 0) {
                $violations += "${panelRel}: helper '$helperName' missing F2 anchor: $a"
            }
        }
        if ($helper.IndexOf('forceRefresh ||', [System.StringComparison]::Ordinal) -lt 0 -and
            $helper.IndexOf('forceRefresh||', [System.StringComparison]::Ordinal) -lt 0) {
            $violations += "${panelRel}: helper '$helperName' must gate refresh on forceRefresh || CommittedLifecycleIntent (settings-only / rejected intent stay no-refresh)"
        }
    }

    if ($null -eq $commit) {
        $violations += "${panelRel}: method CommitDraftAndStatus not found"
    } else {
        if ($commit.IndexOf($helperCall, [System.StringComparison]::Ordinal) -lt 0) {
            $violations += "${panelRel}: CommitDraftAndStatus (save-stay) does not call $helperName"
        }
        if ($commit.IndexOf('refreshModel', [System.StringComparison]::Ordinal) -ge 0) {
            $violations += "${panelRel}: CommitDraftAndStatus still inlines refreshModel (must go through $helperName)"
        }
    }

    if ($null -eq $confirm) {
        $violations += "${panelRel}: method ResolveConfirm not found"
    } else {
        $callCount = 0
        $idx = 0
        while ($true) {
            $found = $confirm.IndexOf($helperCall, $idx, [System.StringComparison]::Ordinal)
            if ($found -lt 0) { break }
            $callCount++
            $idx = $found + $helperName.Length
        }
        if ($callCount -lt 2) {
            $violations += "${panelRel}: ResolveConfirm must call $helperName at least twice (confirm-stay + confirm-leave/refresh); found $callCount"
        }
        if ($confirm.IndexOf('refreshModel', [System.StringComparison]::Ordinal) -ge 0) {
            $violations += "${panelRel}: ResolveConfirm still inlines refreshModel (must go through $helperName)"
        }
    }

    if ($null -ne $helper) {
        $helperStart = $panel.IndexOf($helperSig, [System.StringComparison]::Ordinal)
        $scanOutside = New-Object System.Text.StringBuilder
        [void]$scanOutside.Append($panel.Substring(0, $helperStart))
        [void]$scanOutside.Append($panel.Substring($helperStart + $helper.Length))
        $outside = $scanOutside.ToString()
    } else {
        $outside = $panel
    }
    foreach ($r in $inlineResidues) {
        if ($outside.IndexOf($r, [System.StringComparison]::Ordinal) -ge 0) {
            $violations += "${panelRel}: inline F2 refresh copy still present outside $helperName : $r"
        }
    }
}

if ($null -eq $tests) {
    $violations += "${testsRel}: file missing or unreadable"
} elseif ($tests.IndexOf($f2Test, [System.StringComparison]::Ordinal) -lt 0) {
    $violations += "${testsRel}: F2 regression '$f2TestName' missing (save-then-state-must-not-jump-back)"
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Output "VIOLATION: $_" }
    Write-Output "RefreshModel-dedup gate FAIL: $($violations.Count) violation(s)"
    exit 1
}
Write-Output "RefreshModel-dedup gate PASS: F2 triad shares $helperName; call sites have no inline refreshModel; F2 regression $f2TestName anchored"
