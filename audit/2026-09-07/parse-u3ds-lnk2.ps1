$ErrorActionPreference = 'Stop'
$lnk = 'E:\Steam\steamapps\common\U3DS\Unturned.exe - 快捷方式.lnk'
$bytes = [IO.File]::ReadAllBytes($lnk)
Write-Output ('LNK SIZE=' + $bytes.Length)
$text = [Text.Encoding]::Unicode.GetString($bytes)
$runs = $text -split "[\x00-\x1f]" | Where-Object { $_.Length -ge 4 -and $_ -match '^[\x20-\x7e\u4e00-\u9fff]+$' }
Write-Output '--- UTF16 strings in .lnk:'
$runs | Select-Object -First 20
Write-Output '--- Servers dir:'
Get-ChildItem 'E:\Steam\steamapps\common\U3DS\Servers' -Directory -ErrorAction SilentlyContinue | ForEach-Object { Write-Output ('INSTANCE=' + $_.Name) }
Write-Output '--- bat/cmd in U3DS root:'
Get-ChildItem 'E:\Steam\steamapps\common\U3DS' -Include '*.bat','*.cmd','*.sh' -Recurse -Depth 1 -ErrorAction SilentlyContinue | ForEach-Object { Write-Output ('SCRIPT=' + $_.FullName) }
