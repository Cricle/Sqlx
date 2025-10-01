# Sqlx Visual Studio Extension (VSIX) 编译指南

本目录包含了用于编译Sqlx Visual Studio扩展的多个脚本，适用于不同的使用场景和平台。

## 📁 脚本文件说明

### 1. `build-vsix.ps1` - 完整版PowerShell脚本 (推荐)
**最强大和完整的编译脚本，包含详细的错误检查和用户友好的输出。**

**特性:**
- ✅ 完整的先决条件检查
- ✅ 智能MSBuild/dotnet选择
- ✅ 详细的编译过程显示
- ✅ 文件验证和错误处理
- ✅ 彩色输出和进度指示
- ✅ 安装说明和功能介绍

**使用方法:**
```powershell
# 基本使用 (Release配置)
.\scripts\build-vsix.ps1

# Debug配置 + 清理
.\scripts\build-vsix.ps1 -Configuration Debug -Clean

# 详细输出模式
.\scripts\build-vsix.ps1 -Verbose

# 完整选项
.\scripts\build-vsix.ps1 -Configuration Release -Clean -Verbose
```

### 2. `build-vsix.cmd` - Windows批处理脚本
**适合不能运行PowerShell的Windows环境。**

**特性:**
- ✅ 纯批处理，无需PowerShell
- ✅ 基本的文件检查和错误处理
- ✅ 彩色输出支持
- ✅ 简单易用

**使用方法:**
```batch
# Release配置
.\scripts\build-vsix.cmd

# Debug配置
.\scripts\build-vsix.cmd Debug
```

### 3. `build-vsix-simple.ps1` - 简化PowerShell脚本
**跨平台PowerShell Core脚本，专注核心功能。**

**特性:**
- ✅ 跨平台支持 (Windows/Linux/macOS)
- ✅ 简洁的代码和输出
- ✅ PowerShell Core兼容
- ✅ 快速编译

**使用方法:**
```powershell
# Linux/macOS/Windows PowerShell Core
pwsh ./scripts/build-vsix-simple.ps1

# Windows PowerShell
.\scripts\build-vsix-simple.ps1 -Configuration Debug -Clean
```

## 🚀 快速开始

### Windows用户 (推荐方式)
```powershell
# 1. 以管理员身份打开PowerShell
# 2. 导航到项目根目录
cd C:\path\to\Sqlx

# 3. 运行完整版脚本
.\scripts\build-vsix.ps1
```

### 如果PowerShell被禁用
```batch
# 使用批处理版本
.\scripts\build-vsix.cmd
```

### Linux/macOS用户
```bash
# 确保安装了PowerShell Core
pwsh ./scripts/build-vsix-simple.ps1
```

## 📋 编译要求

### 必需组件
- ✅ **.NET SDK 8.0+** - 用于编译项目
- ✅ **Visual Studio 2022** - 提供MSBuild和VS SDK
- ✅ **Visual Studio SDK** - 用于VSIX开发

### 可选组件
- **MSBuild Tools** - 如果没有完整VS安装
- **PowerShell Core** - 用于跨平台脚本

### 验证安装
```powershell
# 检查.NET SDK
dotnet --version

# 检查MSBuild (Windows)
where msbuild

# 检查Visual Studio安装
vswhere -latest -property displayName
```

## 🎯 输出文件

编译成功后，VSIX文件将生成在:
```
src/Sqlx.VisualStudio/bin/{Configuration}/net472/Sqlx.VisualStudio.vsix
```

**文件结构:**
- **Release版本**: 优化的生产版本，体积较小
- **Debug版本**: 包含调试信息，便于开发调试

## 📦 VSIX安装方法

### 方法1: 双击安装 (推荐)
```
双击 Sqlx.VisualStudio.vsix 文件
```

### 方法2: Visual Studio内安装
```
1. 打开Visual Studio 2022
2. 扩展 → 管理扩展
3. 点击"从磁盘安装..."
4. 选择生成的VSIX文件
```

### 方法3: 命令行安装
```batch
vsixinstaller "path\to\Sqlx.VisualStudio.vsix"
```

### 方法4: VSIX Installer
```batch
# 静默安装
VSIXInstaller.exe /quiet "path\to\Sqlx.VisualStudio.vsix"
```

## 🔧 故障排除

### 常见问题

**问题1: "找不到.NET SDK"**
```
解决: 安装.NET SDK 8.0或更高版本
下载: https://dotnet.microsoft.com/download
```

**问题2: "找不到MSBuild"**
```
解决: 安装Visual Studio 2022或MSBuild Tools
下载: https://visualstudio.microsoft.com/
```

**问题3: "VSIX文件未生成"**
```
可能原因:
- 项目编译失败
- VSIX配置错误
- 输出路径不正确

调试步骤:
1. 检查编译错误
2. 手动运行: dotnet build --verbosity detailed
3. 检查项目文件中的CreateVsixContainer设置
```

**问题4: "权限被拒绝"**
```
解决: 以管理员身份运行脚本
或者: 修改PowerShell执行策略
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### 调试编译问题
```powershell
# 详细编译信息
dotnet build Sqlx.VisualStudio.sln --verbosity detailed

# 清理后重新编译
dotnet clean Sqlx.VisualStudio.sln
dotnet restore Sqlx.VisualStudio.sln
dotnet build Sqlx.VisualStudio.sln
```

## 📊 脚本对比

| 特性 | build-vsix.ps1 | build-vsix.cmd | build-vsix-simple.ps1 |
|------|----------------|----------------|----------------------|
| 平台支持 | Windows | Windows | 跨平台 |
| 复杂度 | 高 | 中 | 低 |
| 错误检查 | 完整 | 基本 | 简单 |
| 用户体验 | 最佳 | 良好 | 简洁 |
| 推荐场景 | 生产使用 | PowerShell受限 | 快速编译 |

## 🎯 开发建议

### 开发期间
```powershell
# 使用Debug配置，包含清理
.\scripts\build-vsix.ps1 -Configuration Debug -Clean -Verbose
```

### 发布版本
```powershell
# 使用Release配置，确保优化
.\scripts\build-vsix.ps1 -Configuration Release -Clean
```

### 持续集成
```yaml
# GitHub Actions示例
- name: Build VSIX
  run: |
    pwsh ./scripts/build-vsix-simple.ps1 -Configuration Release
```

## 💡 扩展功能

### 自动版本号
脚本可以扩展为自动更新版本号:
```powershell
# 在source.extension.vsixmanifest中更新版本
$manifest = [xml](Get-Content "src/Sqlx.VisualStudio/source.extension.vsixmanifest")
$manifest.PackageManifest.Metadata.Identity.Version = "1.0.$env:BUILD_NUMBER"
$manifest.Save("src/Sqlx.VisualStudio/source.extension.vsixmanifest")
```

### 自动发布
集成Visual Studio Marketplace发布:
```powershell
# 使用vsce工具发布到Marketplace
vsce publish --packagePath "path/to/Sqlx.VisualStudio.vsix"
```

## 📞 支持

如果遇到问题:
1. 查看本文档的故障排除部分
2. 运行详细模式获取更多信息
3. 检查项目的GitHub Issues
4. 提交新的Issue报告问题

---

**编写者:** Sqlx团队
**更新时间:** 2025年10月
**版本:** 1.0
