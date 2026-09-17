$ErrorActionPreference='Stop'
Add-Type -Path 'E:\Steam\steamapps\common\Unturned\BepInEx\core\Mono.Cecil.dll'
$dll = 'E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\Assembly-CSharp.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
$t = $asm.MainModule.GetType('SDG.Unturned.SteamPlayerID')
Write-Output ('BaseType: ' + $t.BaseType.FullName)
foreach ($m in $t.Methods) {
  if ($m.Name -notin @('op_Equality','op_Inequality')) { continue }
  Write-Output ('===== ' + $m.Name)
  foreach ($ins in $m.Body.Instructions) { Write-Output ('  0x' + $ins.Offset.ToString('X4') + '  ' + $ins.OpCode.Name + '  ' + $ins.Operand) }
}
$sp = $asm.MainModule.GetType('SDG.Unturned.SteamPlayer')
Write-Output ('SteamPlayer BaseType: ' + $sp.BaseType.FullName)
