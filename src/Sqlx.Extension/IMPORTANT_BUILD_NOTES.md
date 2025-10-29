# ⚠️ 重要构建说明

## 🚫 不要使用 `dotnet build`

**Sqlx.Extension 是一个 VSIX 项目，不能使用 `dotnet build` 构建！**

### 为什么？

VSIX 项目使用旧式 `.csproj` 格式和 Visual Studio SDK，需要：
- Visual Studio 特定的 MSBuild targets
- VSSDK Build Tools
- MEF 组件编译
- VSIX 打包工具

`dotnet build` **不支持**这些特性，会报错：
```
error CS0234: 命名空间"Microsoft"中不存在类型或命名空间名"VisualStudio"
```

---

## ✅ 正确的构建方法

### 方法 1: 在 Visual Studio 2022 中构建（推荐）

1. 打开 `Sqlx.sln`
2. 在解决方案资源管理器中找到 `Sqlx.Extension` 项目
3. 右键 → 构建，或按 `Ctrl+Shift+B`
4. 输出在 `bin\Debug\` 或 `bin\Release\`

### 方法 2: 使用 MSBuild（命令行）

**必须在 Developer Command Prompt for VS 2022 中执行：**

```cmd
# 打开 Developer Command Prompt for VS 2022
# 然后执行：

cd src\Sqlx.Extension
msbuild Sqlx.Extension.csproj /p:Configuration=Release

# 或者使用完整路径：
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" Sqlx.Extension.csproj
```

### 方法 3: 调试运行（推荐用于测试）

1. 在 Visual Studio 中打开解决方案
2. 设置 `Sqlx.Extension` 为启动项目
3. 按 `F5` 启动调试
4. 会打开一个新的 Visual Studio 实验实例

---

## 🔍 验证构建成功

构建成功后，应该能看到：

```
bin\
  Debug\
    Sqlx.Extension.vsix      ← VSIX 安装包
    Sqlx.Extension.dll
    其他依赖文件...
```

---

## ⚠️ 常见错误

### 错误：使用了 `dotnet build`

**症状**:
```
error CS0234: 命名空间"Microsoft"中不存在类型或命名空间名"VisualStudio"
```

**解决**: 使用上面的正确方法构建

---

### 错误：未安装 Visual Studio SDK

**症状**:
```
error MSB4019: 未找到导入的项目
```

**解决**: 
1. 打开 Visual Studio Installer
2. 修改 VS 2022 安装
3. 勾选 "Visual Studio extension development" 工作负载
4. 安装

---

### 错误：包版本冲突

**症状**:
```
warning NU1605: 检测到包降级
```

**解决**: 这些是警告，不影响构建，可以忽略

---

## 📋 环境要求

- ✅ Visual Studio 2022 (17.0+)
- ✅ VS SDK (Visual Studio extension development 工作负载)
- ✅ .NET Framework 4.7.2 SDK
- ❌ 不需要 .NET SDK（`dotnet` 命令）

---

## 🎯 快速检查清单

在尝试构建前，确保：

- [ ] 已安装 Visual Studio 2022
- [ ] 已安装 "Visual Studio extension development" 工作负载
- [ ] 打开的是 Visual Studio，不是 VS Code
- [ ] 使用 Visual Studio 构建，不是命令行 `dotnet build`

---

## 📚 相关文档

- [BUILD.md](BUILD.md) - 详细构建说明
- [TESTING_GUIDE.md](TESTING_GUIDE.md) - 测试指南
- [README.md](README.md) - 项目概述

---

**记住**: VSIX 项目 ≠ 普通 .NET 项目，必须使用 Visual Studio！

