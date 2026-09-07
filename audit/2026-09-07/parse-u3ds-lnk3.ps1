$ErrorActionPreference = 'Stop'
$lnkFile = Get-ChildItem 'E:\Steam\steamapps\common\U3DS' -Filter '*.lnk' | Where-Object { $_.Name -notlike 'Unity*' } | Select-Object -First 1
Write-Output ('LNK=' + $lnkFile.FullName)
$bytes = [IO.File]::ReadAllBytes($lnkFile.FullName)
Write-Output ('LNK SIZE=' + $bytes.Length)
$text = [Text.Encoding]::Unicode.GetString($bytes)
$runs = $text -split "[\x00-\x1f]" | Where-Object { $_.Length -ge 4 }
Write-Output '--- UTF16 strings in .lnk:'
$runs | Select-Object -First 24
$shell = New-Object -ComObject WScript.Shell
$s = $shell.CreateShortcut($lnkFile.FullName)
Write-Output ('TARGET=' + $s.TargetPath)
Write-Output ('ARGS=' + $s.Arguments)
Write-Output ('WORKDIR=' + $s.WorkingDirectory)
