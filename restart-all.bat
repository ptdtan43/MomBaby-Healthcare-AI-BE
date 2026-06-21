@echo off
echo ===================================================
echo   Dang tat cac backend dang chay tren port 5265, 8001...
echo ===================================================

for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5265" ^| findstr "LISTENING"') do taskkill /f /pid %%a 2>nul
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":8001" ^| findstr "LISTENING"') do taskkill /f /pid %%a 2>nul

echo ===================================================
echo   Dang khoi chay lai cac dich vu Backend...
echo ===================================================

echo [1/2] Dang khoi chay MomOi.API (ASP.NET Core - Cong 5265/7228)...
start "MomOi.API ASP.NET Core" cmd /k "cd MomOi.API && dotnet run"

echo [2/2] Dang khoi chay MomOi.NutritionAPI (Python FastAPI - Cong 8001)...
start "MomOi.NutritionAPI FastAPI" cmd /k "cd MomOi.NutritionAPI && ..\venv\Scripts\activate.bat && uvicorn main:app --host 0.0.0.0 --port 8001 --reload"

echo ===================================================
echo   Da khoi chay lai 2 cua so Terminal rieng biet.
echo ===================================================
pause
