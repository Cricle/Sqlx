#!/bin/bash
# Sqlx v2.0.0 发布脚本
# 用途：一键发布到 NuGet.org

set -e  # 遇到错误立即退出

VERSION="2.0.0"
echo "🚀 Sqlx v$VERSION 发布脚本"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# 1. 检查环境
echo "1️⃣ 检查环境..."
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: 未找到 dotnet CLI"
    exit 1
fi
echo "✅ dotnet CLI: $(dotnet --version)"
echo ""

# 2. 清理
echo "2️⃣ 清理旧的构建文件..."
dotnet clean -c Release > /dev/null 2>&1
echo "✅ 清理完成"
echo ""

# 3. 运行测试
echo "3️⃣ 运行测试..."
TEST_RESULT=$(dotnet test tests/Sqlx.Tests --logger "console;verbosity=minimal" 2>&1 | grep "通过" | tail -1)
echo "$TEST_RESULT"
if [[ ! $TEST_RESULT =~ "失败:     0" ]]; then
    echo "❌ 错误: 测试失败，取消发布"
    exit 1
fi
echo "✅ 测试通过"
echo ""

# 4. 构建 Release
echo "4️⃣ 构建 Release..."
dotnet build -c Release > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "❌ 错误: 构建失败"
    exit 1
fi
echo "✅ 构建成功"
echo ""

# 5. 打包
echo "5️⃣ 打包 NuGet..."
dotnet pack -c Release > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "❌ 错误: 打包失败"
    exit 1
fi
echo "✅ Sqlx.$VERSION.nupkg"
echo "✅ Sqlx.Generator.$VERSION.nupkg"
echo ""

# 6. 检查包
echo "6️⃣ 检查包文件..."
SQLX_PKG="src/Sqlx/bin/Release/Sqlx.$VERSION.nupkg"
GENERATOR_PKG="src/Sqlx.Generator/bin/Release/Sqlx.Generator.$VERSION.nupkg"

if [ ! -f "$SQLX_PKG" ]; then
    echo "❌ 错误: 未找到 Sqlx 包"
    exit 1
fi
if [ ! -f "$GENERATOR_PKG" ]; then
    echo "❌ 错误: 未找到 Sqlx.Generator 包"
    exit 1
fi
echo "✅ 包文件检查通过"
echo ""

# 7. 创建 Git 标签
echo "7️⃣ 创建 Git 标签..."
read -p "是否创建 Git 标签 v$VERSION? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    git tag -a "v$VERSION" -m "Release v$VERSION - 100% Test Coverage"
    echo "✅ 标签已创建"

    read -p "是否推送标签到远程? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        git push origin "v$VERSION"
        echo "✅ 标签已推送"
    fi
else
    echo "⏭️  跳过标签创建"
fi
echo ""

# 8. 发布到 NuGet
echo "8️⃣ 发布到 NuGet.org..."
read -p "是否发布到 NuGet.org? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    read -p "请输入 NuGet API Key: " NUGET_KEY
    echo ""

    echo "📦 发布 Sqlx..."
    dotnet nuget push "$SQLX_PKG" --api-key "$NUGET_KEY" --source https://api.nuget.org/v3/index.json

    echo "📦 发布 Sqlx.Generator..."
    dotnet nuget push "$GENERATOR_PKG" --api-key "$NUGET_KEY" --source https://api.nuget.org/v3/index.json

    echo "✅ 发布完成！"
else
    echo "⏭️  跳过 NuGet 发布"
    echo ""
    echo "手动发布命令:"
    echo "  dotnet nuget push \"$SQLX_PKG\" --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json"
    echo "  dotnet nuget push \"$GENERATOR_PKG\" --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json"
fi
echo ""

# 9. 完成
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎉 Sqlx v$VERSION 发布流程完成！"
echo ""
echo "下一步:"
echo "  ✅ 创建 GitHub Release"
echo "  ✅ 发布社区公告"
echo "  ✅ 更新项目网站"
echo ""
echo "详见: NEXT_STEPS.md"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

