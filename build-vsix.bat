@echo off
REM Sqlx VSIX 构建脚本 - Windows 批处理版本
REM 直接双击运行即可

echo.
echo ========================================
echo   Sqlx VSIX 自动构建
echo ========================================
echo.

REM 检查 PowerShell
where powershell >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [错误] 找不到 PowerShell
    echo 请确保 Windows PowerShell 已安装
    pause
    exit /b 1
)

REM 运行 PowerShell 脚本
echo 正在启动构建脚本...
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0build-vsix.ps1" -Configuration Release

REM 检查结果
if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================
    echo   构建成功！
    echo ========================================
    echo.
    echo VSIX 文件已复制到当前目录
    echo 文件名: Sqlx.Extension-v0.1.0-Release.vsix
    echo.
) else (
    echo.
    echo ========================================
    echo   构建失败！
    echo ========================================
    echo.
    echo 请查看上面的错误信息
    echo.
)

pause

