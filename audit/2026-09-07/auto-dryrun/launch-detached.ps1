$ErrorActionPreference = 'Stop'
$p = Start-Process -FilePath 'E:\Steam\steamapps\common\U3DS\Unturned.exe' -ArgumentList '-NetTransport=SteamNetworking' -WorkingDirectory 'E:\Steam\steamapps\common\U3DS' -PassThru
Write-Output ('PID=' + $p.Id)
