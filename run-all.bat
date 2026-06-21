@echo off
title Run All Backend Services
echo ===================================================
echo   Khoi chay dong thoi cac dich vu Backend (ASP.NET Core ^& FastAPI)
echo ===================================================

echo [1/2] Dang khoi chay MomOi.API (ASP.NET Core - Cong 5265/7228)...
start "MomOi.API ASP.NET Core" cmd /k "cd MomOi.API && dotnet run"

echo [2/2] Dang khoi chay MomOi.NutritionAPI (Python FastAPI - Cong 8001)...
start "MomOi.NutritionAPI FastAPI" cmd /k "cd MomOi.NutritionAPI && ..\venv\Scripts\activate.bat && uvicorn main:app --host 0.0.0.0 --port 8001 --reload"

echo ===================================================
echo   Da khoi chay cac cua so Terminal rieng biet.
echo   Vui long khong tat bat ky cua so nao dang chay!
echo ===================================================
pause
