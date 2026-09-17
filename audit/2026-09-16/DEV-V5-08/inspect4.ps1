$ErrorActionPreference='Stop'
Add-Type -Path 'E:\Steam\steamapps\common\Unturned\BepInEx\core\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\Assembly-CSharp.dll')
foreach ($tn in @('SDG.Unturned.PlayerSave','SDG.Unturned.Channel','SDG.Unturned.SteamPlayer','SDG.Unturned.Player')) {
  $t = $asm.MainModule.GetType($tn)
  Write-Output ('===== ' + $tn + ' (' + ($t -ne $null) + ')')
  if ($t -eq $null) { continue }
  foreach ($f in $t.Fields) { if ($f.Name -match 'name|owner|channel|playerID') { Write-Output ('  FIELD ' + $f.Name + ' : ' + $f.FieldType.Name) } }
  foreach ($p in $t.Properties) { if ($p.Name -match 'name|owner|channel|playerID') { Write-Output ('  PROP  ' + $p.Name + ' : ' + $p.PropertyType.Name) } }
}
