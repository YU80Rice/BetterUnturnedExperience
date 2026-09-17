$ErrorActionPreference='Stop'
Add-Type -Path 'E:\Steam\steamapps\common\Unturned\BepInEx\core\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\Assembly-CSharp.dll')
$t = $asm.MainModule.GetType('SDG.Unturned.OptionsSettings')
Write-Output ('OptionsSettings: ' + ($t -ne $null))
foreach ($p in $t.Properties) {
  if ($p.Name -ne 'ShouldAnonymizeMultiplayerDetails') { continue }
  Write-Output ('===== get_' + $p.Name)
  foreach ($ins in $p.GetMethod.Body.Instructions) { Write-Output ('  ' + $ins.OpCode.Name + '  ' + $ins.Operand) }
}
foreach ($f in $t.Fields) { if ($f.Name -match 'instance|options|anonym|streamer') { Write-Output ('  FIELD ' + $f.Name + ' : ' + $f.FieldType.Name) } }
