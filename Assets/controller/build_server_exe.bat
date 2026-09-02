@echo off
REM Builds server.py (Flask app: movement forwarding + sound/camera tasks,
REM all merged into one script) + the controller HTML files into a single
REM standalone ControllerServer.exe (no Python install required on the
REM target machine). Run this whenever server.py or the html files change;
REM commit the result into Assets/controller/dist/ so Unity's build step can
REM pick it up.

setlocal
cd /d "%~dp0"

where pyinstaller >nul 2>nul
if errorlevel 1 (
    echo pyinstaller not found. Install it with: pip install flask cryptography pyinstaller
    exit /b 1
)

pyinstaller --onefile --console --name ControllerServer ^
    --add-data "cardSwipe.html;." ^
    --add-data "controler.html;." ^
    --add-data "doorShake.html;." ^
    --hidden-import flask ^
    --hidden-import cryptography ^
    server.py

if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo.
echo Built dist\ControllerServer.exe
endlocal
