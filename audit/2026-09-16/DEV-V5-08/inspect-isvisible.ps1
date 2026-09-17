$ErrorActionPreference='Stop'
Add-Type -Path 'E:\Steam\steamapps\common\Unturned\BepInEx\core\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\SDG.Glazier.Runtime.dll')
foreach ($t in $asm.MainModule.Types) {
  if ($t.Name -notmatch 'Sleek|Glazier|Button') { continue }
  foreach ($p in $t.Properties) {
    if ($p.Name -eq 'IsVisible') { Write-Output ($t.FullName + ' PROP IsVisible explicit=' + $p.GetMethod.IsPublic + ' setter=' + ($p.SetMethod -ne $null)) }
  }
}
