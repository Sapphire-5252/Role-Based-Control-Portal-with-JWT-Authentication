@echo off
setlocal

set ROOT=%~dp0
set BACKEND=%ROOT%backend
set FRONTEND=%ROOT%frontend

echo Starting OBFSimple backend...
start "OBFSimple Backend" cmd /k "cd /d %BACKEND% && dotnet run"

echo Waiting for backend to start...
timeout /t 5 /nobreak >nul

echo Opening frontend in default browser...
start "" "%FRONTEND%\index.html"

echo Done. Close the backend window to stop the server.
endlocal
