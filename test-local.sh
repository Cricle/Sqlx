#!/bin/bash

# Sqlx本地测试脚本
# 运行本地测试（仅SQLite，无需额外数据库）

set -e

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "           Sqlx本地测试 (SQLite Only)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# 检查dotnet
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: 未找到dotnet命令"
    echo "请先安装.NET SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ 检测到 .NET SDK: $(dotnet --version)"
echo ""

# 恢复依赖
echo "📦 恢复NuGet包..."
dotnet restore
echo ""

# 编译项目
echo "🔨 编译项目 (Release)..."
dotnet build --no-restore --configuration Release
echo ""

# 运行测试
echo "🧪 运行本地测试..."
echo ""
dotnet test --no-build --configuration Release \
    --settings .runsettings \
    --logger "console;verbosity=normal"

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "           ✅ 本地测试完成！"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

