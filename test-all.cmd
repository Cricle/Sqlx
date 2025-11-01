@echo off
REM Sqlx全方言测试脚本 (Windows)
REM 启动所有数据库并运行完整测试

echo =========================================================
echo       Sqlx全方言测试 (所有数据库)
echo =========================================================
echo.

REM 检查docker-compose
where docker-compose >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ❌ 错误: 未找到docker-compose命令
    echo 请先安装Docker Desktop for Windows
    exit /b 1
)

REM 检查dotnet
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ❌ 错误: 未找到dotnet命令
    echo 请先安装.NET SDK: https://dotnet.microsoft.com/download
    exit /b 1
)

for /f "tokens=*" %%i in ('docker-compose --version') do set DOCKER_VERSION=%%i
for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo ✅ 检测到 Docker Compose: %DOCKER_VERSION%
echo ✅ 检测到 .NET SDK: %DOTNET_VERSION%
echo.

REM 启动数据库服务
echo 🐳 启动数据库服务...
docker-compose up -d
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

echo.
echo ⏳ 等待数据库服务就绪...
echo    - PostgreSQL (5432)
echo    - MySQL (3306)
echo    - SQL Server (1433)
echo.

REM 等待30秒让数据库完全启动
echo 等待数据库启动... (30秒)
timeout /t 30 /nobreak >nul

echo ✅ 数据库服务应该已就绪
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

REM 设置环境变量
set CI=true
set POSTGRESQL_CONNECTION=Host=localhost;Port=5432;Database=sqlx_test;Username=postgres;Password=postgres
set MYSQL_CONNECTION=Server=localhost;Port=3306;Database=sqlx_test;Uid=root;Pwd=root
set SQLSERVER_CONNECTION=Server=localhost,1433;Database=sqlx_test;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True

REM 运行测试
echo 🧪 运行全方言测试...
echo.
dotnet test --no-build --configuration Release --settings .runsettings.ci --logger "console;verbosity=normal"
set TEST_RESULT=%ERRORLEVEL%

echo.
echo =========================================================
if %TEST_RESULT% EQU 0 (
    echo            ✅ 全方言测试完成！
) else (
    echo            ❌ 测试失败！
)
echo =========================================================
echo.
echo 💡 提示: 使用 'docker-compose down' 停止数据库服务
echo 💡 提示: 使用 'docker-compose down -v' 停止并删除数据

exit /b %TEST_RESULT%

