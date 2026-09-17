$ErrorActionPreference = 'Stop'
$bytes = [System.IO.File]::ReadAllBytes('E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\Assembly-CSharp.dll')
$a = [System.Reflection.Assembly]::ReflectionOnlyLoad($bytes)
$t = $a.GetType('SDG.Unturned.PlayerSave')
Write-Output '--- PlayerSave fields (name-ish) ---'
$flags = [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Instance
foreach ($f in $t.GetFields($flags)) { if ($f.Name -match 'name') { Write-Output ("FIELD  " + $f.Name + " : " + $f.FieldType.Name) } }
Write-Output '--- PlayerSave properties (name-ish) ---'
foreach ($p in $t.GetProperties($flags)) { if ($p.Name -match 'name') { Write-Output ("PROP   " + $p.Name + " : " + $p.PropertyType.Name) } }
Write-Output '--- Channel owner / SteamPlayer playerID shapes ---'
$ch = $a.GetType('SDG.Unturned.Channel')
foreach ($p in $ch.GetProperties($flags)) { if ($p.Name -eq 'owner') { Write-Output ("Channel PROP owner : " + $p.PropertyType.Name) } }
foreach ($f in $ch.GetFields($flags)) { if ($f.Name -match 'owner') { Write-Output ("Channel FIELD " + $f.Name) } }
$sp = $a.GetType('SDG.Unturned.SteamPlayer')
foreach ($p in $sp.GetProperties($flags)) { if ($p.Name -eq 'playerID') { Write-Output ("SteamPlayer PROP playerID : " + $p.PropertyType.Name) } }
foreach ($f in $sp.GetFields($flags)) { if ($f.Name -match 'playerID') { Write-Output ("SteamPlayer FIELD " + $f.Name) } }
$pl = $a.GetType('SDG.Unturned.Player')
foreach ($f in $pl.GetFields($flags)) { if ($f.Name -match 'channel') { Write-Output ("Player FIELD " + $f.Name) } }
foreach ($p in $pl.GetProperties($flags)) { if ($p.Name -match 'channel') { Write-Output ("Player PROP " + $p.Name) } }
