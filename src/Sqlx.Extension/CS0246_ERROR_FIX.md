# 🚨 CS0246 错误修复指南

> **错误**: CS0246 - 未能找到类型或命名空间名  
> **原因**: Visual Studio SDK 引用问题  
> **解决时间**: 5-10 分钟

---

## ⚡ 快速修复（3 步）

### 方法 1: 使用自动化脚本（推荐）

在 PowerShell 中执行：

```powershell
cd C:\Users\huaji\Workplace\github\Sqlx\src\Sqlx.Extension
.\diagnose-and-fix.ps1
```

脚本会自动：
- ✅ 检查 Visual Studio 安装
- ✅ 清理输出目录
- ✅ 清理 NuGet 缓存
- ✅ 还原 NuGet 包
- ✅ 验证项目配置

然后按照脚本提示在 Visual Studio 中重新生成。

---

### 方法 2: 手动修复

#### 步骤 1: 检查 Visual Studio 工作负载

**这是最常见的原因！**

1. 打开 **Visual Studio Installer**
2. 点击 **修改** Visual Studio 2022
3. 确认已勾选：
   - ✅ **Visual Studio extension development** （必需！）
   - ✅ **.NET desktop development** （推荐）
4. 如果没有勾选，请勾选并点击 **修改** 安装

**如果缺少这个工作负载，无论如何都无法编译 VSIX 项目！**

#### 步骤 2: 完全清理

```powershell
# 在 PowerShell 中执行
cd C:\Users\huaji\Workplace\github\Sqlx

# 1. 删除 bin/obj
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force

# 2. 删除 .vs 文件夹
Remove-Item .vs -Recurse -Force -ErrorAction SilentlyContinue

# 3. 清理 NuGet 缓存
dotnet nuget locals all --clear

# 4. 删除 packages 文件夹（如果存在）
Remove-Item packages -Recurse -Force -ErrorAction SilentlyContinue
```

#### 步骤 3: 重新生成

1. 打开 Visual Studio 2022
2. 打开 `Sqlx.sln`
3. 等待 NuGet 包自动还原（右下角状态栏）
4. 如果没有自动还原：
   - 右键解决方案 → **还原 NuGet 包**
5. 等待还原完成
6. 菜单：**生成** → **清理解决方案**
7. 菜单：**生成** → **重新生成解决方案**

---

## 🔍 诊断问题

### 问题 1: "Visual Studio extension development" 未安装

**症状**:
```
error CS0246: 未能找到类型或命名空间名"AsyncPackage"
error CS0246: 未能找到类型或命名空间名"IClassifier"
```

**解决**:
1. 打开 Visual Studio Installer
2. 修改 VS 2022
3. 勾选 "Visual Studio extension development"
4. 安装（可能需要 10-30 分钟）
5. 重启 Visual Studio

### 问题 2: NuGet 包未正确还原

**症状**:
- 错误列表中有大量 CS0246 错误
- 涉及 Microsoft.VisualStudio.* 命名空间

**解决**:
```powershell
# 方法 A: 在项目目录
cd src\Sqlx.Extension
msbuild Sqlx.Extension.csproj /t:Restore /v:detailed

# 方法 B: 在 Visual Studio 中
# 工具 -> NuGet 包管理器 -> 包管理器控制台
Update-Package -reinstall -Project Sqlx.Extension
```

### 问题 3: 程序集版本冲突

**症状**:
```
warning: 检测到包降级
```

**解决**:
1. 在 Visual Studio 中
2. 工具 → 选项 → NuGet 包管理器
3. 点击 **清除所有 NuGet 缓存**
4. 关闭 Visual Studio
5. 删除 `%USERPROFILE%\.nuget\packages` 目录
6. 重新打开 Visual Studio 并还原包

---

## 📋 检查清单

在尝试构建前，确认：

- [ ] 已安装 Visual Studio 2022（17.0 或更高版本）
- [ ] 已安装 "Visual Studio extension development" 工作负载
- [ ] 已执行 `git pull origin main` 获取最新代码
- [ ] 已删除所有 bin/obj 目录
- [ ] 已删除 .vs 文件夹
- [ ] 已清理 NuGet 缓存
- [ ] 在 Visual Studio 中打开解决方案
- [ ] 已等待 NuGet 包还原完成
- [ ] 已执行"清理解决方案"
- [ ] 已执行"重新生成解决方案"

---

## 🆘 仍然失败？

### 收集诊断信息

请提供以下信息：

1. **Visual Studio 版本**
   - 帮助 → 关于 Microsoft Visual Studio
   - 记录完整版本号

2. **已安装的工作负载**
   - 打开 Visual Studio Installer
   - 截图 "已安装" 选项卡

3. **具体的错误消息**
   - 在 Visual Studio 错误列表中
   - 复制前 5-10 个 CS0246 错误
   - 包括文件名和行号

4. **NuGet 还原输出**
   - 视图 → 输出
   - 显示输出来源：包管理器
   - 复制全部内容

5. **构建输出**
   - 视图 → 输出
   - 显示输出来源：生成
   - 复制全部内容

---

## 💡 常见问题

### Q: 为什么 `dotnet build` 不能用？

**A**: VSIX 项目使用旧式 .csproj 格式，需要 Visual Studio SDK 和 MEF 组件。`dotnet build` 不支持这些，必须使用 Visual Studio 或 MSBuild。

### Q: 需要什么版本的 Visual Studio？

**A**: 
- **最低**: Visual Studio 2022 17.0
- **推荐**: Visual Studio 2022 17.8 或更高
- **版本**: Community / Professional / Enterprise 都可以

### Q: 可以在 Visual Studio Code 中构建吗？

**A**: 不可以。VS Code 不支持 VSIX 项目。必须使用完整的 Visual Studio 2022。

### Q: 为什么有这么多包引用？

**A**: VSIX 项目需要多个 Visual Studio SDK 包才能编译。每个包提供不同的 API（Shell、Text Editor、Threading 等）。

---

## 🔧 高级故障排除

### 完全重置环境

如果以上都不行，执行完全重置：

```powershell
# 1. 关闭所有 Visual Studio 实例

# 2. 清理项目
cd C:\Users\huaji\Workplace\github\Sqlx
git clean -fdx  # 警告：会删除所有未跟踪的文件！

# 3. 清理 NuGet 全局缓存
dotnet nuget locals all --clear
Remove-Item $env:USERPROFILE\.nuget\packages -Recurse -Force

# 4. 清理 Visual Studio 缓存
Remove-Item "$env:LOCALAPPDATA\Microsoft\VisualStudio\*\ComponentModelCache" -Recurse -Force
Remove-Item "$env:LOCALAPPDATA\Microsoft\VisualStudio\*\*.cache" -Force

# 5. 重新拉取代码
git pull origin main

# 6. 重启计算机（可选但推荐）

# 7. 打开 Visual Studio 并构建
```

### 使用开发者命令提示符

```cmd
# 打开 Developer Command Prompt for VS 2022

cd C:\Users\huaji\Workplace\github\Sqlx\src\Sqlx.Extension

REM 清理
msbuild Sqlx.Extension.csproj /t:Clean

REM 还原包
msbuild Sqlx.Extension.csproj /t:Restore /v:detailed

REM 构建
msbuild Sqlx.Extension.csproj /p:Configuration=Debug /v:detailed

REM 查看详细输出以诊断问题
```

---

## 📞 获取帮助

如果尝试了所有方法仍然失败：

1. 运行诊断脚本并保存输出
2. 收集上述诊断信息
3. 创建 GitHub Issue 并包含：
   - Visual Studio 版本
   - 完整的错误消息
   - 诊断脚本输出
   - 构建日志

---

**最后更新**: 2025-10-29  
**适用版本**: Visual Studio 2022 17.0+  
**项目版本**: Sqlx.Extension 0.5.0

