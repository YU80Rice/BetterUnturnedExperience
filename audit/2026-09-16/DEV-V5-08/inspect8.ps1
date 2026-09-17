$ErrorActionPreference='Stop'
Add-Type -Path 'E:\Steam\steamapps\common\Unturned\BepInEx\core\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\Assembly-CSharp.dll')
$t = $asm.MainModule.GetType('SDG.Unturned.Provider')
foreach ($p in $t.Properties) {
  if ($p.Name -ne 'streamerNames') { continue }
  Write-Output '===== Provider.get_streamerNames'
  foreach ($ins in $p.GetMethod.Body.Instructions) { Write-Output ('  ' + $ins.OpCode.Name + '  ' + $ins.Operand) }
}
