$ErrorActionPreference='Stop'
Add-Type -Path 'E:\Steam\steamapps\common\Unturned\BepInEx\core\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\Assembly-CSharp.dll')
$t = $asm.MainModule.GetType('SDG.Unturned.SteamPlayerID')
foreach ($p in $t.Properties) {
  if ($p.Name -notin @('characterName','playerName','streamerName')) { continue }
  Write-Output ('===== get_' + $p.Name)
  $m = $p.GetMethod
  foreach ($ins in $m.Body.Instructions) { Write-Output ('  ' + $ins.OpCode.Name + '  ' + $ins.Operand) }
}
