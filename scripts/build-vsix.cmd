@echo off
rem =================================================================
rem Sqlx Visual Studio Extension (VSIX) 编译脚本 - 简化版本
rem =================================================================

setlocal EnableDelayedExpansion

rem 设置颜色（如果支持）
if not defined NO_COLOR (
    for /f %%A in ('"prompt $H&echo on&for %%B in (1) do rem"') do set BS=%%A
)

rem 脚本路径设置
set SCRIPT_DIR=%~dp0
set ROOT_DIR=%SCRIPT_DIR%..
set VSIX_SOLUTION=%ROOT_DIR%\Sqlx.VisualStudio.sln
set VSIX_PROJECT=%ROOT_DIR%\src\Sqlx.VisualStudio\Sqlx.VisualStudio.csproj

rem 默认配置
set BUILD_CONFIG=Release
if not "%1"=="" set BUILD_CONFIG=%1

echo.
echo ===================================================
echo  🚀 Sqlx Visual Studio Extension 编译脚本
echo ===================================================
echo  配置: %BUILD_CONFIG%
echo  解决方案: %VSIX_SOLUTION%
echo ===================================================
echo.

rem 检查文件是否存在
if not exist "%VSIX_SOLUTION%" (
    echo ❌ 错误: 找不到解决方案文件
    echo    路径: %VSIX_SOLUTION%
    goto :error
)

if not exist "%VSIX_PROJECT%" (
    echo ❌ 错误: 找不到项目文件
    echo    路径: %VSIX_PROJECT%
    goto :error
)

echo ✅ 解决方案和项目文件验证通过
echo.

rem 检查 dotnet 是否可用
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ❌ 错误: 找不到 .NET SDK
    echo    请安装 .NET SDK: https://dotnet.microsoft.com/download
    goto :error
)

echo ✅ .NET SDK 检查通过
for /f "tokens=*" %%a in ('dotnet --version') do echo    版本: %%a
echo.

rem 步骤1：清理（如果是Release构建）
if /i "%BUILD_CONFIG%"=="Release" (
    echo 🔄 清理输出目录...
    dotnet clean "%VSIX_SOLUTION%" --configuration %BUILD_CONFIG% --verbosity minimal
    if errorlevel 1 (
        echo ⚠️  清理时出现警告，但继续编译...
    ) else (
        echo ✅ 清理完成
    )
    echo.
)

rem 步骤2：恢复包
echo 🔄 恢复 NuGet 包...
dotnet restore "%VSIX_SOLUTION%" --verbosity minimal
if errorlevel 1 (
    echo ❌ 包恢复失败
    goto :error
)
echo ✅ 包恢复完成
echo.

rem 步骤3：编译项目
echo 🔄 编译 VS 扩展项目 (配置: %BUILD_CONFIG%)...
dotnet build "%VSIX_SOLUTION%" --configuration %BUILD_CONFIG% --no-restore --verbosity minimal
if errorlevel 1 (
    echo ❌ 编译失败
    goto :error
)
echo ✅ 编译完成
echo.

rem 步骤4：检查VSIX文件
set OUTPUT_DIR=%ROOT_DIR%\src\Sqlx.VisualStudio\bin\%BUILD_CONFIG%\net472
set VSIX_FILE=%OUTPUT_DIR%\Sqlx.VisualStudio.vsix

echo 🔄 检查 VSIX 文件...
if not exist "%VSIX_FILE%" (
    echo ❌ 错误: VSIX 文件未生成
    echo    预期位置: %VSIX_FILE%
    echo.
    echo 输出目录内容:
    if exist "%OUTPUT_DIR%" (
        dir /b "%OUTPUT_DIR%"
    ) else (
        echo    输出目录不存在
    )
    goto :error
)

echo ✅ VSIX 文件生成成功!
echo.

rem 获取文件信息
for %%F in ("%VSIX_FILE%") do (
    set FILE_SIZE=%%~zF
    set FILE_DATE=%%~tF
)

rem 计算文件大小（KB）
set /a FILE_SIZE_KB=!FILE_SIZE!/1024

echo ===================================================
echo  📦 编译成功完成!
echo ===================================================
echo  VSIX 文件: %VSIX_FILE%
echo  文件大小: !FILE_SIZE_KB! KB
echo  修改时间: !FILE_DATE!
echo ===================================================
echo.
echo 安装方法:
echo  1. 双击 VSIX 文件直接安装
echo  2. 在 Visual Studio 中:
echo     扩展 ^> 管理扩展 ^> 从磁盘安装
echo  3. 命令行安装:
echo     vsixinstaller "%VSIX_FILE%"
echo.
echo 功能特性:
echo  • SQL 悬浮提示 - 鼠标悬停查看 SQL 内容
echo  • 支持 [Sqlx]、[SqlTemplate]、[ExpressionToSql] 特性
echo  • 实时代码分析和缓存
echo.

goto :success

:error
echo.
echo ===================================================
echo  ❌ 编译失败
echo ===================================================
echo.
echo 故障排除建议:
echo  1. 确保已安装 Visual Studio 2022
echo  2. 确保已安装 .NET SDK
echo  3. 检查项目文件是否完整
echo  4. 尝试清理解决方案后重新编译
echo.
echo 详细编译信息请运行:
echo  dotnet build "%VSIX_SOLUTION%" --configuration %BUILD_CONFIG% --verbosity detailed
echo.
exit /b 1

:success
echo 🎉 任务完成!
echo.
pause
exit /b 0
