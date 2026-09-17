$ErrorActionPreference='Stop'
Add-Type -Path 'E:\Steam\steamapps\common\Unturned\BepInEx\core\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\Assembly-CSharp.dll')
foreach ($tn in @('SDG.Unturned.SteamPlayerID','SDG.Unturned.SteamChannel')) {
  $t = $asm.MainModule.GetType($tn)
  Write-Output ('===== ' + $tn + ' (' + ($t -ne $null) + ')')
  if ($t -eq $null) { continue }
  foreach ($f in $t.Fields) { Write-Output ('  FIELD ' + $f.Name + ' : ' + $f.FieldType.Name) }
  foreach ($p in $t.Properties) { Write-Output ('  PROP  ' + $p.Name + ' : ' + $p.PropertyType.Name) }
}
