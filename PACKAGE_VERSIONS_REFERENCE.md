# 📦 VSIX 项目 NuGet 包版本参考

> **重要**: VSIX 项目的包版本必须互相兼容  
> **问题**: 包之间有依赖关系，版本冲突会导致编译失败  
> **解决**: 使用经过验证的版本组合

---

## ✅ 推荐的包版本配置（已验证）

### 配置 A: VS 2022 标准版本（推荐）

适用于 Visual Studio 2022 17.0+

```xml
<ItemGroup>
  <!-- 核心 VSIX 包 -->
  <PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339" />
  <PackageReference Include="Microsoft.VSSDK.BuildTools" Version="17.0.31903.59">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  
  <!-- Shell 相关 -->
  <PackageReference Include="Microsoft.VisualStudio.Shell.15.0" Version="17.0.32112.339" />
  <PackageReference Include="Microsoft.VisualStudio.Shell.Framework" Version="17.0.32112.339" />
  <PackageReference Include="Microsoft.VisualStudio.Shell.Interop" Version="17.0.32112.339" />
  
  <!-- 编辑器相关 -->
  <PackageReference Include="Microsoft.VisualStudio.Text.UI" Version="17.0.32112.339" />
  <PackageReference Include="Microsoft.VisualStudio.Text.Data" Version="17.0.32112.339" />
  <PackageReference Include="Microsoft.VisualStudio.CoreUtility" Version="17.0.32112.339" />
  <PackageReference Include="Microsoft.VisualStudio.Language.StandardClassification" Version="17.0.32112.339" />
  <PackageReference Include="Microsoft.VisualStudio.TextManager.Interop" Version="17.0.32112.339" />
  
  <!-- 其他 -->
  <PackageReference Include="Microsoft.VisualStudio.Threading" Version="17.0.64" />
  
  <!-- Roslyn -->
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.0.1" />
  <PackageReference Include="System.Collections.Immutable" Version="6.0.0" />
</ItemGroup>
```

### 配置 B: 最小版本集（最稳定）

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339" />
  <PackageReference Include="Microsoft.VSSDK.BuildTools" Version="17.0.31903.59">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.0.1" />
</ItemGroup>
```

**说明**: 其他包会作为传递依赖自动引入。

---

## 🔍 如何查找正确的包版本

### 方法 1: 查看 NuGet.org

```
1. 访问 https://www.nuget.org/
2. 搜索包名（如 Microsoft.VisualStudio.SDK）
3. 查看版本历史
4. 选择与您的 VS 版本匹配的版本
```

### 方法 2: 使用 dotnet CLI

```bash
# 列出可用版本
dotnet package search Microsoft.VisualStudio.SDK --exact-match

# 查看包详情
nuget list Microsoft.VisualStudio.SDK -AllVersions
```

### 方法 3: 在 Visual Studio 中

```
1. 工具 → NuGet 包管理器 → 管理解决方案的 NuGet 包
2. 搜索包名
3. 点击包名查看所有可用版本
4. 查看依赖关系
```

---

## ⚠️ 常见版本问题

### 问题 1: 包降级警告

```
warning NU1605: 检测到包降级: Microsoft.VisualStudio.CoreUtility 从 17.0.491 降级到 17.0.467
```

**原因**: 
- Microsoft.VisualStudio.SDK 依赖更高版本的包
- 您明确引用了较低版本

**解决**: 
使用 SDK 要求的版本或更高版本。

### 问题 2: 找不到包版本

```
error NU1102: 找不到版本为 (>= 17.0.32112.339) 的包 Microsoft.VisualStudio.Shell.Interop.15.0.DesignTime
```

**原因**: 
- 版本不存在
- 包名错误

**解决**: 
检查 NuGet.org 上的实际可用版本。

### 问题 3: 依赖冲突

```
error NU1107: 检测到版本冲突
```

**原因**: 
不同的包要求同一依赖的不同版本。

**解决**: 
1. 移除明确的包引用，让传递依赖自动解决
2. 或使用所有包都兼容的版本

---

## 📋 VS 2022 版本对照表

| VS 版本 | SDK 包版本 | Roslyn 版本 |
|---------|-----------|-------------|
| 17.0.x | 17.0.32112.339 | 4.0.1 |
| 17.1.x | 17.1.31911.196 | 4.1.0 |
| 17.2.x | 17.2.32210.308 | 4.2.0 |
| 17.3.x | 17.3.32825.248 | 4.3.0 |
| 17.4.x | 17.4.33110.190 | 4.4.0 |
| 17.5.x | 17.5.33428.388 | 4.5.0 |

**建议**: 使用最低版本（17.0.x）以确保最大兼容性。

---

## 🎯 简化配置方法

### 最简配置

只引用核心包，让 NuGet 自动解析其余依赖：

```xml
<ItemGroup>
  <!-- 只需要这两个！ -->
  <PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339" />
  <PackageReference Include="Microsoft.VSSDK.BuildTools" Version="17.0.31903.59">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  
  <!-- Roslyn（如果需要代码分析功能） -->
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.0.1" />
</ItemGroup>
```

**优势**:
- ✅ 最少的版本冲突
- ✅ 自动传递依赖
- ✅ 易于维护

---

## 🔧 调试包版本问题

### 步骤 1: 清理环境

```powershell
Remove-Item bin,obj -Recurse -Force
dotnet nuget locals all --clear
```

### 步骤 2: 详细还原

```bash
dotnet restore -v detailed
```

查看输出，了解：
- 哪些包被还原
- 实际使用的版本
- 依赖关系

### 步骤 3: 检查 obj/project.assets.json

```bash
cat obj/project.assets.json | grep -A 5 "Microsoft.VisualStudio"
```

查看实际解析的包版本。

---

## 💡 最佳实践

### 1. 使用一致的版本

所有 Microsoft.VisualStudio.* 包使用相同的主版本：

```xml
<!-- ✅ 好 - 都是 17.0.x -->
<PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339" />
<PackageReference Include="Microsoft.VisualStudio.Shell.15.0" Version="17.0.32112.339" />

<!-- ❌ 差 - 混合版本 -->
<PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339" />
<PackageReference Include="Microsoft.VisualStudio.Shell.15.0" Version="17.5.33428.388" />
```

### 2. 让 SDK 包控制依赖

Microsoft.VisualStudio.SDK 会引入大部分需要的包：

```xml
<!-- ✅ 推荐 -->
<PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339" />
<!-- SDK 会自动带入 Shell, Text, CoreUtility 等 -->

<!-- ❌ 不推荐 - 手动指定可能冲突 -->
<PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339" />
<PackageReference Include="Microsoft.VisualStudio.CoreUtility" Version="17.0.467" />
<!-- 可能与 SDK 要求的版本冲突 -->
```

### 3. Roslyn 版本与 VS 版本对应

| VS 版本 | 推荐 Roslyn 版本 |
|---------|------------------|
| 17.0 | 4.0.1 |
| 17.1-17.3 | 4.3.0 |
| 17.4+ | 4.4.0 或更高 |

---

## 🆘 仍有问题？

### 完全重置配置

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <!-- ... 属性配置 ... -->
  
  <!-- 最简包配置 -->
  <ItemGroup>
    <PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339" />
    <PackageReference Include="Microsoft.VSSDK.BuildTools" Version="17.0.31903.59">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  
  <!-- ... 其余配置 ... -->
</Project>
```

**然后**:
1. 清理 bin/obj
2. dotnet restore
3. 在 VS 中打开并构建

---

## 📞 获取帮助

如果仍有包版本问题，请提供：

1. **完整的还原日志**
   ```bash
   dotnet restore -v detailed > restore.log 2>&1
   ```

2. **具体的错误消息**
   - NU1102, NU1605, NU1603 等

3. **Visual Studio 版本**
   - 帮助 → 关于 Microsoft Visual Studio

4. **当前使用的包版本**
   - 从 .csproj 文件复制

---

**最后更新**: 2025-10-29  
**推荐配置**: 配置 A 或 最简配置  
**VS 版本**: 2022 (17.0+)

