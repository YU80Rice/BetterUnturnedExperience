$bytes = [System.IO.File]::ReadAllBytes('E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed\Assembly-CSharp.dll')
$a = [System.Reflection.Assembly]::ReflectionOnlyLoad($bytes)
$types = $a.GetTypes()
Write-Output ("total types: " + $types.Count)
foreach ($t in $types) { if ($t.Name -match 'PlayerSave|SteamPlayer$|^Channel$') { Write-Output ($t.FullName + "  [public=" + $t.IsPublic + "]") } }
