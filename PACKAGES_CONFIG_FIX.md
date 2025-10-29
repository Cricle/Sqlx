# 🔧 使用 packages.config 修复引用问题

> **问题**: 包没有被引用到项目  
> **原因**: 旧式 VSIX 项目不完全兼容 PackageReference  
> **解决**: 使用传统的 packages.config 方式

---

## ✅ 我已经完成的修改

1. ✅ 修改项目为使用 `packages.config` 模式
2. ✅ 移除 `PackageReference` 元素
3. ✅ 添加 NuGet 导入目标
4. ✅ 创建了 `packages.config` 文件

---

## ⚡ 立即执行这些步骤

### 步骤 1: 拉取最新代码

```powershell
cd C:\Users\huaji\Workplace\github\Sqlx
git pull origin main
```

### 步骤 2: 删除现有的缓存

```powershell
# 删除 bin/obj
cd src\Sqlx.Extension
Remove-Item bin,obj -Recurse -Force -ErrorAction SilentlyContinue

# 删除 packages 文件夹（如果存在）
cd ..\..
Remove-Item packages -Recurse -Force -ErrorAction SilentlyContinue

# 删除 .vs
Remove-Item .vs -Recurse -Force -ErrorAction SilentlyContinue
```

### 步骤 3: 在 Visual Studio 中还原包

**重要**: 必须在 Visual Studio 中还原，不能用命令行！

1. **打开 Visual Studio 2022**
2. **打开 Sqlx.sln**
3. **右键点击解决方案** → **还原 NuGet 包**
4. **等待完成**（查看输出窗口）

应该看到：
```
正在还原 Sqlx.Extension 的 NuGet 包...
已成功安装 'Microsoft.VisualStudio.SDK 17.0.32112.339'。
已成功安装 'Microsoft.VisualStudio.Shell.15.0 17.0.32112.339'。
...
已完成包还原。
```

### 步骤 4: 验证 packages 文件夹

检查以下目录是否存在：
```
C:\Users\huaji\Workplace\github\Sqlx\packages\
  ├── Microsoft.VisualStudio.SDK.17.0.32112.339\
  ├── Microsoft.VisualStudio.Shell.15.0.17.0.32112.339\
  ├── Microsoft.CodeAnalysis.CSharp.Workspaces.4.8.0\
  └── ...
```

如果这些文件夹存在，说明包已正确下载！

### 步骤 5: 检查引用

在 Visual Studio 中：
1. 展开 **Sqlx.Extension** 项目
2. 展开 **引用** 节点
3. 应该看到这些引用（没有黄色警告）：
   - Microsoft.VisualStudio.*
   - Microsoft.CodeAnalysis.*
   - System.Collections.Immutable
   - 等等...

### 步骤 6: 重新生成

1. 菜单：**生成** → **清理解决方案**
2. 菜单：**生成** → **重新生成解决方案**
3. 检查输出窗口

---

## 🔍 验证 packages.config 模式

### 检查项目文件

确认 `src\Sqlx.Extension\Sqlx.Extension.csproj` 包含：

```xml
<RestoreProjectStyle>PackagesConfig</RestoreProjectStyle>
```

**没有** PackageReference 元素。

### 检查 packages.config 文件

确认 `src\Sqlx.Extension\packages.config` 存在且包含：

```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Microsoft.VisualStudio.SDK" version="17.0.32112.339" targetFramework="net472" />
  <package id="Microsoft.VisualStudio.Shell.15.0" version="17.0.32112.339" targetFramework="net472" />
  ...
</packages>
```

---

## 🆘 如果还原失败

### 问题 1: packages.config 不存在

**解决**: 手动创建

```powershell
cd C:\Users\huaji\Workplace\github\Sqlx\src\Sqlx.Extension
git pull origin main
```

packages.config 应该在最新的代码中。

### 问题 2: 还原时报错

**查看错误**：
1. 视图 → 输出
2. 显示输出来源：包管理器
3. 复制错误消息

**常见错误**：
- "无法找到版本 XXX" → 包版本不存在
- "访问被拒绝" → 网络或权限问题
- "包不兼容" → .NET Framework 版本问题

### 问题 3: 引用仍然没有加载

**在 Visual Studio 中手动添加引用**：

1. 右键 **Sqlx.Extension** → **管理 NuGet 包**
2. 点击 **已安装** 选项卡
3. 确认所有包都已安装
4. 如果没有，点击 **浏览** 并搜索安装：
   - Microsoft.VisualStudio.SDK
   - Microsoft.VisualStudio.Shell.15.0
   - Microsoft.CodeAnalysis.CSharp.Workspaces

---

## 📊 packages.config vs PackageReference

### packages.config（我们现在使用的）

**优点**：
- ✅ 与旧式项目 100% 兼容
- ✅ 引用更明确
- ✅ 包下载到解决方案的 packages 文件夹
- ✅ Visual Studio 完全支持

**缺点**：
- ⚠️ 不支持传递依赖自动解析
- ⚠️ packages 文件夹较大
- ⚠️ 不是最新的方式

### PackageReference（之前尝试的）

**优点**：
- ✅ 新式项目格式的标准
- ✅ 支持传递依赖
- ✅ 包缓存在用户目录

**缺点**：
- ❌ 旧式 VSIX 项目兼容性差
- ❌ 可能导致引用不加载
- ❌ 需要额外配置

---

## ✅ 成功的标志

### 1. packages 文件夹存在

```
Sqlx\packages\
  ├── Microsoft.CodeAnalysis.CSharp.Workspaces.4.8.0\
  ├── Microsoft.VisualStudio.SDK.17.0.32112.339\
  └── ...（很多文件夹）
```

### 2. 引用已加载

在 Visual Studio 的解决方案资源管理器中：
- 展开 Sqlx.Extension → 引用
- 看到 Microsoft.VisualStudio.* 等引用
- **没有黄色警告图标**

### 3. 编译成功

```
========== 全部重新生成: 成功 1 个，失败 0 个 ==========
```

### 4. 输出文件存在

```
src\Sqlx.Extension\bin\Debug\
  ├── Sqlx.Extension.dll ✅
  ├── Sqlx.Extension.vsix ✅
  └── Microsoft.VisualStudio.*.dll（很多依赖）
```

---

## 🔧 故障排除命令

### 在包管理器控制台中执行

**工具** → **NuGet 包管理器** → **包管理器控制台**

```powershell
# 查看已安装的包
Get-Package -ProjectName Sqlx.Extension

# 重新安装特定包
Update-Package Microsoft.VisualStudio.SDK -ProjectName Sqlx.Extension -Reinstall

# 重新安装所有包
Update-Package -ProjectName Sqlx.Extension -Reinstall
```

---

## 📋 完整清理和重建步骤

如果一切都失败，执行完整重置：

```powershell
# 1. 回到项目根目录
cd C:\Users\huaji\Workplace\github\Sqlx

# 2. 拉取最新代码
git pull origin main

# 3. 完全清理
Remove-Item src\Sqlx.Extension\bin,src\Sqlx.Extension\obj -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item packages -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .vs -Recurse -Force -ErrorAction SilentlyContinue

# 4. 打开 Visual Studio 2022
# 5. 打开 Sqlx.sln
# 6. 右键解决方案 → 还原 NuGet 包
# 7. 等待 2-5 分钟
# 8. 生成 → 重新生成解决方案
```

---

## 💡 关键要点

1. **packages.config 是传统方式，但对旧式项目更可靠**
2. **必须在 Visual Studio 中还原包，不能用命令行**
3. **包会下载到 `packages\` 文件夹**
4. **检查 `packages\` 文件夹是验证包是否下载的最简单方法**
5. **引用必须在解决方案资源管理器中可见**

---

**现在请执行上面的步骤！特别是步骤 1-6！** 🚀

如果仍有问题，请告诉我：
1. `packages\` 文件夹是否存在？
2. 文件夹中有哪些包？
3. Visual Studio 的引用节点中看到什么？

