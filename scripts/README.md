# 🛠️ Sqlx 构建脚本

## 📋 build.ps1 - 构建脚本

统一的构建脚本，用于自动化构建和测试流程。

### 🚀 快速使用

```powershell
# 完整构建（包含测试）
.\scripts\build.ps1

# Debug 模式构建
.\scripts\build.ps1 -Configuration Debug

# 跳过测试（快速构建）
.\scripts\build.ps1 -SkipTests
```

### 📖 参数说明

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `-Configuration` | 构建配置 (Debug 或 Release) | `Release` |
| `-SkipTests` | 跳过单元测试 | `false` |
| `-Help` | 显示帮助信息 | - |

### 🔄 构建流程

1. **清理** - 清理之前的构建输出
2. **还原** - 还原 NuGet 依赖包
3. **构建** - 编译所有项目
4. **测试** - 运行单元测试（可跳过）

### ✅ 构建内容

- ✅ **Sqlx** - 核心库（3个目标框架：netstandard2.0, net8.0, net9.0）
- ✅ **Sqlx.Generator** - 源代码生成器（netstandard2.0）
- ✅ **Sqlx.Tests** - 单元测试（449 个测试）
- ✅ **TodoWebApi** - Web API 示例
- ✅ **SqlxDemo** - 功能演示项目

### 📊 预期输出

```
=====================================
🚀 Sqlx 构建开始
=====================================
配置: Release

🧹 清理...
   ✅ 清理完成
📦 还原依赖...
   ✅ 依赖还原完成
🔨 构建项目...
   ✅ 构建完成
🧪 运行测试...
   ✅ 测试通过

=====================================
🎉 构建成功！
=====================================
```

---

## 🔧 NuGet 脚本

### install.ps1

NuGet 包安装时自动运行，显示安装成功信息和快速入门指南。

### uninstall.ps1

NuGet 包卸载时自动运行，提示清理生成的文件。

---

## 💡 使用建议

### 日常开发

```powershell
# 快速构建（跳过测试）
.\scripts\build.ps1 -SkipTests
```

### 提交代码前

```powershell
# 完整构建（包含测试）
.\scripts\build.ps1
```

### CI/CD 流程

```powershell
# Release 构建 + 测试
.\scripts\build.ps1 -Configuration Release
```

---

## 📚 相关文档

- [项目主页](../README.md)
- [快速开始](../docs/QUICK_START_GUIDE.md)
- [API 参考](../docs/API_REFERENCE.md)

