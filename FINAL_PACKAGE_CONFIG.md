# ✅ 最终可用的 NuGet 包配置

> **状态**: ✅ 已验证可用  
> **测试**: dotnet restore 0.4秒，零警告  
> **日期**: 2025-10-29

---

## 🎯 核心问题和解决方案

### 问题 1: Shell.Interop.15.0.DesignTime 包不存在

**错误**:
```
error NU1102: 找不到版本为 (>= 17.0.32112.339) 的包 Microsoft.VisualStudio.Shell.Interop.15.0.DesignTime
- 在 nuget.org 中找到 10 个版本[ 最接近版本: 16.10.31320.204 ]
```

**原因**: 这个包的 17.0.x 版本根本不存在！

**解决**: ❌ 移除这个包引用

---

### 问题 2: 包降级警告

**错误**:
```
warning NU1605: 检测到包降级: Microsoft.VisualStudio.CoreUtility 从 17.0.491 降级到 17.0.487
warning NU1605: 检测到包降级: Microsoft.VisualStudio.Text.Data 从 17.0.491 降级到 17.0.487
warning NU1605: 检测到包降级: Microsoft.VisualStudio.Text.UI 从 17.0.491 降级到 17.0.487
```

**原因**: 
- Microsoft.VisualStudio.SDK 要求这些包的版本 >= 17.0.491
- 但我们显式引用了 17.0.487 版本

**解决**: ✅ 显式引用 17.0.491 版本

---

## ✅ 最终可用配置

### 完整的 PackageReference 配置

```xml
<!-- NuGet 包 - 避免降级警告的最优配置 -->
<ItemGroup>
  <PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339">
    <ExcludeAssets>runtime</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Microsoft.VSSDK.BuildTools" Version="17.0.5232">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <!-- 显式指定版本以避免降级 -->
  <PackageReference Include="Microsoft.VisualStudio.CoreUtility" Version="17.0.491" />
  <PackageReference Include="Microsoft.VisualStudio.Text.Data" Version="17.0.491" />
  <PackageReference Include="Microsoft.VisualStudio.Text.UI" Version="17.0.491" />
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.0.1" />
</ItemGroup>
```

### 包说明

| 包 | 版本 | 作用 | 属性 |
|----|------|------|------|
| Microsoft.VisualStudio.SDK | 17.0.32112.339 | 核心 VS SDK，自动引入大部分依赖 | `ExcludeAssets=runtime` |
| Microsoft.VSSDK.BuildTools | 17.0.5232 | VSIX 构建工具 | `PrivateAssets=all` |
| Microsoft.VisualStudio.CoreUtility | 17.0.491 | 编辑器核心工具（显式版本避免降级） | 无 |
| Microsoft.VisualStudio.Text.Data | 17.0.491 | 文本数据模型（显式版本避免降级） | 无 |
| Microsoft.VisualStudio.Text.UI | 17.0.491 | 文本 UI 组件（显式版本避免降级） | 无 |
| Microsoft.CodeAnalysis.CSharp.Workspaces | 4.0.1 | Roslyn 代码分析 | 无 |

### 自动引入的传递依赖（部分）

Microsoft.VisualStudio.SDK 会自动引入：
- Microsoft.VisualStudio.Shell.15.0
- Microsoft.VisualStudio.Shell.Framework
- Microsoft.VisualStudio.Shell.Interop (各个版本)
- Microsoft.VisualStudio.TextManager.Interop
- Microsoft.VisualStudio.Threading
- Microsoft.VisualStudio.Editor
- Microsoft.VisualStudio.Language.StandardClassification
- 等等...

**优势**: 版本由 SDK 统一管理，确保兼容性。

---

## 📊 测试结果

### dotnet restore

```bash
cd src/Sqlx.Extension
dotnet restore
```

**输出**:
```
还原完成(0.2)
在 0.4 秒内生成 已成功
```

**结果**:
- ✅ 成功
- ✅ 零警告
- ✅ 零错误
- ✅ 快速（0.4秒）

### 包版本解析

所有包都解析到正确的版本：
- ✅ 无降级
- ✅ 无冲突
- ✅ 无缺失

---

## 🔍 为什么这个配置有效？

### 1. 移除不存在的包

❌ 不要引用 `Microsoft.VisualStudio.Shell.Interop.15.0.DesignTime`
- 17.0.x 版本不存在
- 会导致 NU1102 错误

### 2. 使用 SDK 要求的版本

✅ 显式指定 17.0.491 版本
- 避免从 17.0.491 降级到 17.0.487
- 消除 NU1605 警告

### 3. 最小化显式引用

✅ 只引用必要的包
- 核心: SDK + BuildTools
- 防降级: CoreUtility, Text.Data, Text.UI
- 代码分析: CodeAnalysis.CSharp.Workspaces
- 其他包由 SDK 自动引入

### 4. 正确的 NuGet 属性

```xml
<ExcludeAssets>runtime</ExcludeAssets>
```
- VSIX 不需要运行时程序集
- VS 本身已包含

```xml
<PrivateAssets>all</PrivateAssets>
```
- BuildTools 只在构建时需要
- 不打包到 VSIX

---

## ⚠️ 常见错误配置

### ❌ 错误配置 1: 引用不存在的包

```xml
<PackageReference Include="Microsoft.VisualStudio.Shell.Interop.15.0.DesignTime" Version="17.0.32112.339" />
```

**问题**: 17.0.x 版本不存在

**修复**: 移除这个包引用

---

### ❌ 错误配置 2: 版本过低导致降级

```xml
<PackageReference Include="Microsoft.VisualStudio.CoreUtility" Version="17.0.467" />
<PackageReference Include="Microsoft.VisualStudio.Text.Data" Version="17.0.487" />
```

**问题**: SDK 要求 >= 17.0.491，会产生降级警告

**修复**: 使用 17.0.491 版本

---

### ❌ 错误配置 3: 引用太多包

```xml
<PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339" />
<PackageReference Include="Microsoft.VisualStudio.Shell.15.0" Version="17.0.32112.339" />
<PackageReference Include="Microsoft.VisualStudio.Shell.Framework" Version="17.0.32112.339" />
<PackageReference Include="Microsoft.VisualStudio.Shell.Interop" Version="17.0.32112.339" />
<!-- 等等...共 14 个包 -->
```

**问题**: 
- 难以维护
- 容易产生版本冲突
- 这些包大部分是 SDK 的传递依赖

**修复**: 只引用核心包，让 NuGet 自动解析

---

## 🎯 配置演变历史

### 第 1 次: 引用所有包（❌ 失败）

```
14 个显式包引用
→ 版本冲突，降级警告
```

### 第 2 次: 最小配置（⚠️ 有警告）

```
3 个包：SDK + BuildTools + CodeAnalysis
→ 包降级警告（NU1605）
```

### 第 3 次: 显式防降级版本（✅ 成功）

```
6 个包：SDK + BuildTools + 3个防降级 + CodeAnalysis
→ 零警告，零错误
```

---

## 📋 检查清单

### 配置检查

- [x] 移除 Microsoft.VisualStudio.Shell.Interop.15.0.DesignTime
- [x] 核心包版本正确（SDK: 17.0.32112.339, BuildTools: 17.0.5232）
- [x] 防降级包版本正确（17.0.491）
- [x] CodeAnalysis 版本正确（4.0.1）
- [x] ExcludeAssets 和 PrivateAssets 设置正确

### 验证步骤

```bash
# 1. 清理
cd src/Sqlx.Extension
rm -rf bin obj

# 2. 还原
dotnet restore

# 3. 检查输出
# 应该看到：还原完成，零警告

# 4. 检查详细信息
dotnet restore -v detailed 2>&1 | grep -E "(warning|error)"
# 应该没有输出
```

---

## 🚀 下一步：在 Visual Studio 中构建

### 前置条件

```
✅ Visual Studio 2022 (17.0+)
✅ Visual Studio extension development 工作负载
✅ .NET Framework 4.7.2+
```

### 构建步骤

```
1. 打开 Visual Studio 2022
2. 打开 Sqlx.sln
3. 等待 NuGet 包自动还原
4. 按 Ctrl+Shift+B 构建
5. 检查输出：
   ========== 全部重新生成: 成功 1 个 ==========
6. 检查文件：
   bin\Debug\Sqlx.Extension.vsix ✅
```

### 预期结果

```
成功构建 VSIX 文件
大小约：500KB - 2MB
包含所有功能：
- 代码片段
- 语法着色
- 快速操作
- 参数验证
```

---

## 📚 相关文档

| 文档 | 说明 |
|------|------|
| [READY_TO_BUILD.md](READY_TO_BUILD.md) | 构建准备完成报告 |
| [VSIX_BUILD_FIXES.md](VSIX_BUILD_FIXES.md) | 所有修复汇总 |
| [PACKAGE_VERSION_FIX_COMPLETE.md](PACKAGE_VERSION_FIX_COMPLETE.md) | 包版本修复详情 |

---

## 💡 关键要点

### ✅ 做什么

1. **使用 SDK 要求的版本**
   - CoreUtility: 17.0.491
   - Text.Data: 17.0.491
   - Text.UI: 17.0.491

2. **移除不存在的包**
   - Shell.Interop.15.0.DesignTime (17.0.x 不存在)

3. **最小化显式引用**
   - 只引用核心包和防降级包

### ❌ 不要做什么

1. **不要引用所有传递依赖**
   - Shell.15.0, Shell.Framework 等会自动引入

2. **不要使用过低的版本**
   - 17.0.467, 17.0.487 会被降级

3. **不要引用不存在的包**
   - 先在 NuGet.org 上确认版本存在

---

## 🎉 成功！

### 当前状态

**包配置**: ✅ 完全正确  
**dotnet restore**: ✅ 0.4秒，零警告  
**Visual Studio build**: ⏳ 待测试  
**功能实现**: ✅ 4个 P0 功能完成

### 最终配置特点

- ✅ 简洁（6个包）
- ✅ 正确（零警告，零错误）
- ✅ 兼容（VS 2022 17.0+）
- ✅ 维护（易于理解和更新）

---

**配置完成时间**: 2025-10-29  
**测试工具**: dotnet restore  
**验证状态**: ✅ 成功  
**下一步**: 在 Visual Studio 2022 中构建

**🚀 现在可以构建了！**

