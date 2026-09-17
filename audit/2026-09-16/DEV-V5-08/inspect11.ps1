$ErrorActionPreference='Stop'
Add-Type -Path 'E:\Steam\steamapps\common\Unturned\BepInEx\core\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\Assembly-CSharp.dll')
$cur = $asm.MainModule.GetType('SDG.Unturned.SteamConnectedClientBase')
while ($cur -ne $null) { Write-Output $cur.FullName; $cur = $cur.BaseType }
Write-Output '--- SteamChannel base ---'
$cur = $asm.MainModule.GetType('SDG.Unturned.SteamChannel')
while ($cur -ne $null) { Write-Output $cur.FullName; $cur = $cur.BaseType }
