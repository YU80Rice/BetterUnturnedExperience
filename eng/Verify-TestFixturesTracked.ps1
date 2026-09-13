# Gate: build fixtures copied to test output must exist on disk, be un-ignored, and be tracked by git.
# Context (POST-P4-02): the global *.log ignore swallowed Plugin.Tests' dev16d log fixtures, so a
# clean clone failed with MSB3030 on CopyToOutputDirectory items. The *.log ban stays; fixtures use
# a tracked-safe suffix (e.g. .log.txt). This gate blocks that whole class of regression.
param([Parameter(Mandatory=$true)][string]$RepoRoot)
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$excluded = '\\(bin|obj|\.git|\.scratch|\.agents|\.zcode|prototypes|packages|node_modules)(\\|$)'
$csprojFiles = Get-ChildItem -LiteralPath $RepoRoot -Recurse -File -Filter '*.csproj' |
    Where-Object { $_.FullName -notmatch $excluded }
$violations = @()
$checked = 0
foreach ($csproj in $csprojFiles) {
    $text = Get-Content -LiteralPath $csproj.FullName -Raw
    foreach ($m in [regex]::Matches($text, '<None\s+[^>]*CopyToOutputDirectory[^>]*/>')) {
        $includeMatch = [regex]::Matches($m.Value, 'Include="([^"]+)"')
        if ($includeMatch.Count -ne 1) {
            $violations += "$($csproj.Name): None item with CopyToOutputDirectory has no single Include attribute: $($m.Value)"
            continue
        }
        $checked++
        $rel = $includeMatch[0].Groups[1].Value -replace '\\', '/'
        $full = [System.IO.Path]::GetFullPath((Join-Path $csproj.DirectoryName $rel))
        if (-not $full.StartsWith($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            $violations += "$($csproj.Name): fixture path escapes repo root: $rel"
            continue
        }
        $repoRel = $full.Substring($RepoRoot.Length).TrimStart('\', '/').Replace('\', '/')
        if (-not (Test-Path -LiteralPath $full)) {
            $violations += "${repoRel}: file missing on disk (referenced by $($csproj.Name))"
            continue
        }
        & git -C $RepoRoot check-ignore --quiet -- $repoRel
        if ($LASTEXITCODE -eq 0) {
            $violations += "${repoRel}: ignored by gitignore rules (clean clone will not contain it)"
        }
        $tracked = & git -C $RepoRoot ls-files -- $repoRel
        if (-not $tracked) {
            $violations += "${repoRel}: not tracked by git (clean clone will not contain it)"
        }
    }
}
if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Output "VIOLATION: $_" }
    Write-Output "Fixture-tracked gate FAIL: $($violations.Count) violation(s)"
    exit 1
}
Write-Output "Fixture-tracked gate PASS: $checked CopyToOutput fixture item(s) exist on disk and are tracked by git"
