# DEV-V6-09: external canary static gate.
# The canary is a top-level consumer of one published player DLL, not a source project.
param(
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [string]$CanaryRoot = '',
    [string]$PublishedDll = ''
)
$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
if (-not $CanaryRoot) { $CanaryRoot = Join-Path $RepoRoot 'external-canary' }
$CanaryRoot = [System.IO.Path]::GetFullPath($CanaryRoot)
Write-Output ('EXTERNAL-CANARY-GATE root=' + $CanaryRoot)
if (-not (Test-Path -LiteralPath $CanaryRoot -PathType Container)) {
    Write-Output 'VIOLATION CANARY-ROOT missing (external-canary has not been created)'
    Write-Output 'External-canary gate: FAIL violations=1'
    exit 1
}
$violations = New-Object System.Collections.Generic.List[string]
function Violate([string]$rule, [string]$detail) { $violations.Add('VIOLATION ' + $rule + ' ' + $detail) }
$csproj = Join-Path $CanaryRoot 'ExternalCanary.csproj'
$source = Join-Path $CanaryRoot 'ExternalCanaryPlugin.cs'
$readme = Join-Path $CanaryRoot 'README.md'
foreach ($required in @($csproj, $source, $readme)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { Violate 'CANARY-FILES' ('missing=' + $required) }
}
$requiredCanaryRoot = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot 'external-canary')).TrimEnd('\')
if (-not [string]::Equals($CanaryRoot.TrimEnd('\'), $requiredCanaryRoot, [System.StringComparison]::OrdinalIgnoreCase)) { Violate 'CANARY-ROOT' 'sample must live at the exact repository-top-level external-canary/ directory' }
if (Test-Path -LiteralPath $csproj) {
    $projectText = [System.IO.File]::ReadAllText($csproj)
    if ($projectText -match '<ProjectReference\b') { Violate 'CANARY-REF' 'ProjectReference is forbidden' }
    if ($projectText -match '(?i)(NuGet|PackageReference)') { Violate 'CANARY-REF' 'package references are forbidden' }
    $refs = [regex]::Matches($projectText, '<Reference\s+Include="([^"]+)"[^>]*>(.*?)</Reference>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $bueRefs = @($refs | Where-Object { $_.Groups[1].Value -eq 'BetterUnturnedExperience' })
    if ($bueRefs.Count -ne 1) { Violate 'CANARY-REF' ('expected exactly one BetterUnturnedExperience reference, found=' + $bueRefs.Count) }
    elseif ($bueRefs[0].Groups[2].Value -notmatch '<Private>\s*False\s*</Private>') { Violate 'CANARY-COPYLOCAL' 'BUE reference must set Private=False' }
    if ($projectText -match '(?i)src[\\/]') { Violate 'CANARY-REF' 'src/ paths are forbidden' }
    if ($projectText -match '(?i)BetterUnturnedExperience\.(Contracts|Core)') { Violate 'CANARY-REF' 'Contracts/Core assembly references are forbidden' }
}
if (Test-Path -LiteralPath $source) {
    $sourceText = [System.IO.File]::ReadAllText($source)
    if ($sourceText -notmatch 'FeatureId\(\s*"com\.bue\.canary\.hello"\s*\)') { Violate 'CANARY-ID' 'required external FeatureId missing' }
    if ($sourceText -match 'io\.github\.yu80rice\.bue') { Violate 'CANARY-ID' 'reserved FeatureId segment is forbidden' }
    if ($sourceText -notmatch 'BepInPlugin\(\s*"com\.bue\.canary\.plugin"') { Violate 'CANARY-GUID' 'required independent BepInPlugin GUID missing' }
    if ($sourceText -match 'BUE-[A-Z0-9-]+') { Violate 'CANARY-DIAGNOSTIC' 'BUE diagnostic prefix is forbidden for external identity' }
    if ($sourceText -match '(?i)(NoOp|Probe|chain-complete|seven-seam|IFeaturePatching|IFeaturePresentationRegistration)') { Violate 'CANARY-SCOPE' 'sample must remain a minimal registration consumer' }
}
if (-not $PublishedDll) {
    $PublishedDll = Join-Path $RepoRoot 'artifacts/repack/external-canary/BetterUnturnedExperience.dll'
}
if (-not (Test-Path -LiteralPath $PublishedDll -PathType Leaf)) {
    Violate 'CANARY-PUBLISH' ('published DLL missing=' + $PublishedDll)
} else {
    $publishedFull = [System.IO.Path]::GetFullPath($PublishedDll)
    if ($publishedFull -match '[\\/]src[\\/]') { Violate 'CANARY-PUBLISH' 'src intermediate DLL is forbidden' }
    if ([System.IO.Path]::GetFileName($publishedFull) -ne 'BetterUnturnedExperience.dll') { Violate 'CANARY-PUBLISH' 'published file must be BetterUnturnedExperience.dll' }
}
$outputDir = Join-Path $CanaryRoot 'bin/Release'
if (Test-Path -LiteralPath $outputDir) {
    $copied = @(Get-ChildItem -LiteralPath $outputDir -File -Filter 'BetterUnturnedExperience.dll')
    if ($copied.Count -gt 0) { Violate 'CANARY-COPYLOCAL' 'sample output contains a copied BUE DLL' }
}
foreach ($line in $violations) { Write-Output $line }
if ($violations.Count -gt 0) {
    Write-Output ('External-canary gate: FAIL violations=' + $violations.Count)
    exit 1
}
Write-Output 'External-canary gate: PASS violations=0'
exit 0
