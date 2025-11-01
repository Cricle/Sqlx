#!/bin/bash

# Sqlx全方言测试脚本
# 启动所有数据库并运行完整测试

set -e

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "      Sqlx全方言测试 (所有数据库)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# 检查docker-compose
if ! command -v docker-compose &> /dev/null; then
    echo "❌ 错误: 未找到docker-compose命令"
    echo "请先安装Docker和Docker Compose"
    exit 1
fi

# 检查dotnet
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: 未找到dotnet命令"
    echo "请先安装.NET SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ 检测到 Docker Compose: $(docker-compose --version)"
echo "✅ 检测到 .NET SDK: $(dotnet --version)"
echo ""

# 启动数据库服务
echo "🐳 启动数据库服务..."
docker-compose up -d

echo ""
echo "⏳ 等待数据库服务就绪..."
echo "   - PostgreSQL (5432)"
echo "   - MySQL (3306)"
echo "   - SQL Server (1433)"
echo ""

# 等待PostgreSQL
echo "等待PostgreSQL..."
for i in {1..30}; do
    if docker-compose exec -T postgres pg_isready -U postgres &> /dev/null; then
        echo "✅ PostgreSQL 已就绪"
        break
    fi
    echo -n "."
    sleep 1
done

echo ""

# 等待MySQL
echo "等待MySQL..."
for i in {1..30}; do
    if docker-compose exec -T mysql mysqladmin ping -h localhost &> /dev/null; then
        echo "✅ MySQL 已就绪"
        break
    fi
    echo -n "."
    sleep 1
done

echo ""

# 等待SQL Server（需要更长时间）
echo "等待SQL Server..."
for i in {1..60}; do
    if docker-compose exec -T sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q 'SELECT 1' &> /dev/null; then
        echo "✅ SQL Server 已就绪"
        break
    fi
    echo -n "."
    sleep 2
done

echo ""
echo ""

# 恢复依赖
echo "📦 恢复NuGet包..."
dotnet restore
echo ""

# 编译项目
echo "🔨 编译项目 (Release)..."
dotnet build --no-restore --configuration Release
echo ""

# 设置环境变量
export CI=true
export POSTGRESQL_CONNECTION="Host=localhost;Port=5432;Database=sqlx_test;Username=postgres;Password=postgres"
export MYSQL_CONNECTION="Server=localhost;Port=3306;Database=sqlx_test;Uid=root;Pwd=root"
export SQLSERVER_CONNECTION="Server=localhost,1433;Database=sqlx_test;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"

# 运行测试
echo "🧪 运行全方言测试..."
echo ""
dotnet test --no-build --configuration Release \
    --settings .runsettings.ci \
    --logger "console;verbosity=normal"

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "           ✅ 全方言测试完成！"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "💡 提示: 使用 'docker-compose down' 停止数据库服务"
echo "💡 提示: 使用 'docker-compose down -v' 停止并删除数据"

