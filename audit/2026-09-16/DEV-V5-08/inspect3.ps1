$managed = 'E:\Steam\steamapps\common\Unturned\Unturned_Data\Managed'
$onResolve = [System.ResolveEventHandler]{
    param($s, $e)
    $name = (New-Object System.Reflection.AssemblyName($e.Name)).Name
    $p = Join-Path $managed ($name + '.dll')
    if (Test-Path $p) { return [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($p) }
    return $null
}
[System.AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve($onResolve)
$a = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
$flags = [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Instance
$t = $a.GetType('SDG.Unturned.PlayerSave')
Write-Output ('PlayerSave loaded: ' + ($t -ne $null))
foreach ($f in $t.GetFields($flags)) { if ($f.Name -match 'name') { Write-Output ('FIELD  ' + $f.Name + ' : ' + $f.FieldType.Name) } }
foreach ($p in $t.GetProperties($flags)) { if ($p.Name -match 'name') { Write-Output ('PROP   ' + $p.Name + ' : ' + $p.PropertyType.Name) } }
