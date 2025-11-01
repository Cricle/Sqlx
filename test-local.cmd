@echo off
REM Sqlx本地测试脚本 (Windows)
REM 运行本地测试（仅SQLite，无需额外数据库）

echo =========================================================
echo            Sqlx本地测试 (SQLite Only)
echo =========================================================
echo.

REM 检查dotnet
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ❌ 错误: 未找到dotnet命令
    echo 请先安装.NET SDK: https://dotnet.microsoft.com/download
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo ✅ 检测到 .NET SDK: %DOTNET_VERSION%
echo.

REM 恢复依赖
echo 📦 恢复NuGet包...
dotnet restore
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%
echo.

REM 编译项目
echo 🔨 编译项目 (Release)...
dotnet build --no-restore --configuration Release
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%
echo.

REM 运行测试
echo 🧪 运行本地测试...
echo.
dotnet test --no-build --configuration Release --settings .runsettings --logger "console;verbosity=normal"
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

echo.
echo =========================================================
echo            ✅ 本地测试完成！
echo =========================================================

