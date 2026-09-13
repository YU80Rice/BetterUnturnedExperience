# Gate: clean-clone runner must be self-sufficient — host DLLs are CopyLocal into test output.
# Context (POST-P4-08): test-runner projects reference the game/BepInEx host assemblies through
# HintPath items under ..\..\..\Libs\ (a folder OUTSIDE the repo). With <Private>False</Private>
# (CopyLocal off) MSBuild compiles against them but never copies them to the runner's output dir.
# The dev workspace only runs because host DLLs were historically byte-copied into bin/Release by
# hand; a clean clone has none of that, so its fresh bin cannot load BepInEx at runtime and
# Plugin.Tests dies on FileNotFoundException in Program.AssertSingleDllAssemblyClosure.
# Invariant this gate locks: any <Reference> in a project under tests/ whose HintPath resolves
# OUTSIDE the repo (the host DLLs) MUST be CopyLocal — <Private>True</Private> or Private omitted —
# so a clean-clone build reproduces a runnable output dir with zero manual copies.
# Boundary: only test-runner projects are checked. src/BetterUnturnedExperience.Plugin and
# NoOpFixture keep host references deliberately <Private>False</Private> — the shipped candidate
# DLL must not carry BepInEx/UnityEngine (the game provides them), so those stay out of scope here.
param([Parameter(Mandatory=$true)][string]$RepoRoot)
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path.TrimEnd('\', '/')
$excluded = '\\(bin|obj|\.git|\.scratch|\.agents|\.zcode|prototypes|packages|node_modules)(\\|$)'
$testsRoot = Join-Path $RepoRoot 'tests'
if (-not (Test-Path -LiteralPath $testsRoot)) {
    Write-Output "Runner-host-DLL gate PASS: no tests/ tree (nothing to provision)"
    exit 0
}
$csprojFiles = Get-ChildItem -LiteralPath $testsRoot -Recurse -File -Filter '*.csproj' |
    Where-Object { $_.FullName -notmatch $excluded }
$violations = @()
$externalRefs = 0
$provisioned = 0
$openRef = [regex]::new('<Reference\b[^/>]*?>.*?</Reference>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
foreach ($csproj in $csprojFiles) {
    $text = Get-Content -LiteralPath $csproj.FullName -Raw
    foreach ($m in $openRef.Matches($text)) {
        $block = $m.Value
        $hintMatch = [regex]::Match($block, '<HintPath>([^<]+)</HintPath>')
        if (-not $hintMatch.Success) { continue }        # no explicit path (e.g. GAC/framework ref)
        $hintRel = $hintMatch.Groups[1].Value
        $full = [System.IO.Path]::GetFullPath((Join-Path $csproj.DirectoryName $hintRel))
        if ($full.StartsWith($RepoRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
            continue                                       # inside repo -> not a host-DLL copy concern
        }
        $externalRefs++                                    # outside-repo host assembly (Libs)
        $nameMatch = [regex]::Match($block, 'Include="([^"]+)"')
        $name = if ($nameMatch.Success) { $nameMatch.Groups[1].Value } else { $hintRel }
        if ([regex]::IsMatch($block, '<Private>\s*[Ff]alse\s*</Private>')) {
            $violations += "$($csproj.Name): host reference '$name' ($hintRel) is Private=False, so it is not copied to the runner output - a clean clone cannot load it at runtime; make it CopyLocal (Private=True, or omit <Private>). See POST-P4-08."
        } else {
            $provisioned++
        }
    }
}
if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Output "VIOLATION: $_" }
    Write-Output "Runner-host-DLL gate FAIL: $($violations.Count) violation(s); $externalRefs outside-repo host reference(s) checked, $provisioned CopyLocal"
    exit 1
}
Write-Output "Runner-host-DLL gate PASS: $externalRefs outside-repo host reference(s) in test runners are CopyLocal - clean-clone output is self-sufficient"
