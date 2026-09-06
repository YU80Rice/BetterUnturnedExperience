param([Parameter(Mandatory=$true)][string]$SourceRoot)
$ErrorActionPreference = 'Stop'
$forbidden = @('UnityEngine','SDG.Unturned','Glazier','Sleek','BepInEx','Harmony','LaunchMultiplayerNet','Steamworks')
$files = Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Filter '*.cs'
$violations = @()
foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    foreach ($token in $forbidden) { if ($text.Contains($token)) { $violations += "$($file.FullName):$token" } }
}
if ($violations.Count -gt 0) { $violations | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Output "UI/native token scan PASS: $($files.Count) C# files"
