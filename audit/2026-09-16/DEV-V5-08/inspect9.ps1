$ErrorActionPreference='Stop'
Add-Type -Path 'E:\Steam\steamapps\common\Unturned\BepInEx\core\Mono.Cecil.dll'
$dll = Join-Path (Get-Location) 'src\BetterUnturnedExperience.Plugin\bin\Release\BetterUnturnedExperience.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
$t = $asm.MainModule.GetType('BetterUnturnedExperience.Lir.LirSkillEngineHooks')
foreach ($m in $t.Methods) {
  if ($m.Name -ne 'CharacterKeyOfPlayer') { continue }
  Write-Output '===== CharacterKeyOfPlayer IL'
  foreach ($ins in $m.Body.Instructions) { Write-Output ('  0x' + $ins.Offset.ToString('X4') + '  ' + $ins.OpCode.Name + '  ' + $ins.Operand) }
}
