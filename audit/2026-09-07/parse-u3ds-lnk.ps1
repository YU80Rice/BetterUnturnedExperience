$ErrorActionPreference = 'Stop'
$lnk = 'E:\Steam\steamapps\common\U3DS\Unturned.exe - 快捷方式.lnk'
$shell = New-Object -ComObject WScript.Shell
$s = $shell.CreateShortcut($lnk)
Write-Output ('TARGET=' + $s.TargetPath)
Write-Output ('ARGS=' + $s.Arguments)
Write-Output ('WORKDIR=' + $s.WorkingDirectory)
