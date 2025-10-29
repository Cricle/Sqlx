# 🏗️ 如何构建 VSIX 文件

> **重要**: VSIX 项目 **必须** 使用 Visual Studio 或 MSBuild 构建
> **不能** 使用 `dotnet build`

---

## ✅ 方法 1: 在 Visual Studio 2022 中构建（推荐）

### 步骤（2分钟）

1. **打开解决方案**
   ```
   双击 Sqlx.sln
   或
   Visual Studio → 文件 → 打开 → 项目/解决方案 → 选择 Sqlx.sln
   ```

2. **设置为启动项目**（可选）
   ```
   解决方案资源管理器 → 右键 Sqlx.Extension → 设为启动项目
   ```

3. **构建项目**
   ```
   方式 A: 按 Ctrl+Shift+B
   方式 B: 生成 → 重新生成解决方案
   方式 C: 右键 Sqlx.Extension → 重新生成
   ```

4. **查看输出**
   ```
   输出窗口应显示：
   ========== 全部重新生成: 成功 1 个 ==========
   ```

5. **找到 VSIX 文件**
   ```
   src\Sqlx.Extension\bin\Debug\Sqlx.Extension.vsix  ✅
   src\Sqlx.Extension\bin\Debug\Sqlx.Extension.dll   ✅
   ```

---

## ✅ 方法 2: 使用开发人员命令提示符

### 步骤

1. **打开 Developer Command Prompt for VS 2022**
   ```
   开始菜单 → Visual Studio 2022 → Developer Command Prompt for VS 2022
   ```

2. **切换到项目目录**
   ```cmd
   cd C:\Users\huaji\Workplace\github\Sqlx\src\Sqlx.Extension
   ```

3. **使用 MSBuild 构建**
   ```cmd
   msbuild Sqlx.Extension.csproj /t:Rebuild /p:Configuration=Debug
   ```

4. **检查输出**
   ```
   构建成功 1 个
   bin\Debug\Sqlx.Extension.vsix 已生成
   ```

---

## ✅ 方法 3: 使用 PowerShell 脚本

创建 `build.ps1`:

```powershell
# build.ps1
$vsPath = "C:\Program Files\Microsoft Visual Studio\2022\Community"
$msbuild = "$vsPath\MSBuild\Current\Bin\MSBuild.exe"

if (-not (Test-Path $msbuild)) {
    Write-Error "MSBuild not found at $msbuild"
    exit 1
}

Write-Host "Building Sqlx.Extension..."
& $msbuild Sqlx.Extension.csproj `
    /t:Rebuild `
    /p:Configuration=Release `
    /v:minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Build successful!" -ForegroundColor Green
    Write-Host "VSIX location: bin\Release\Sqlx.Extension.vsix" -ForegroundColor Cyan
} else {
    Write-Host "`n❌ Build failed!" -ForegroundColor Red
}
```

**运行**:
```powershell
cd src\Sqlx.Extension
.\build.ps1
```

---

## 🔍 验证 VSIX 文件

### 检查文件存在

```bash
ls -lh bin/Debug/Sqlx.Extension.vsix
```

**预期输出**:
```
-rw-r--r-- 1 User 197121 500K Oct 29 15:30 Sqlx.Extension.vsix
```

### 检查 VSIX 内容

```powershell
# 将 .vsix 重命名为 .zip
Copy-Item bin\Debug\Sqlx.Extension.vsix Sqlx.Extension.zip

# 解压查看
Expand-Archive Sqlx.Extension.zip -DestinationPath vsix_content

# 查看内容
tree vsix_content /F
```

**应该包含**:
- ✅ `extension.vsixmanifest`
- ✅ `Sqlx.Extension.dll`
- ✅ `License.txt`
- ✅ `Snippets\SqlxSnippets.snippet`
- ✅ 其他文档和资源

---

## 🧪 测试 VSIX

### 安装并测试

1. **双击安装**
   ```
   bin\Debug\Sqlx.Extension.vsix
   ```

2. **或使用命令行**
   ```cmd
   VSIXInstaller.exe bin\Debug\Sqlx.Extension.vsix
   ```

3. **启动 Visual Studio**
   ```
   扩展 → 管理扩展 → 已安装
   应该看到 "Sqlx - High-Performance .NET Data Access"
   ```

### 调试测试

```
1. 在 Visual Studio 中按 F5
2. 启动 Visual Studio 实验实例
3. 在实验实例中测试功能
```

---

## ❌ 为什么不能用 dotnet build？

### 技术原因

1. **VSIX 项目类型**
   ```xml
   <ProjectTypeGuids>{82b43b9b-a64c-4715-b499-d71e9ca2bd60};...</ProjectTypeGuids>
   ```
   这个 GUID 是 VSIX 项目类型，`dotnet build` 不识别

2. **VSSDK Targets**
   ```xml
   <Import Project="$(VSToolsPath)\VSSDK\Microsoft.VsSDK.targets" />
   ```
   这些 target 只在 Visual Studio 的 MSBuild 中可用

3. **VSIX 打包**
   生成 `.vsix` 文件需要 VSSDK 工具，这些工具不在 .NET SDK 中

### 症状

如果尝试 `dotnet build`：
```
error CS0234: The type or namespace name 'VisualStudio' does not exist
error CS0246: The type or namespace name 'AsyncPackage' could not be found
```

---

## 📋 构建前检查清单

### 环境检查

- [ ] Visual Studio 2022 (17.0+) 已安装
- [ ] Visual Studio extension development 工作负载已安装
- [ ] .NET Framework 4.7.2+ 已安装

### 项目检查

- [ ] NuGet 包已还原
- [ ] `source.extension.vsixmanifest` 文件存在
- [ ] `License.txt` 文件存在
- [ ] 所有源代码文件无编译错误

### 检查工作负载安装

```
Visual Studio Installer → 修改 VS 2022 → 工作负载
确保勾选：
✅ Visual Studio extension development
```

---

## 🐛 故障排除

### 问题 1: 构建失败 - 缺少 VSSDK

**错误**:
```
Could not find file '$(VSToolsPath)\VSSDK\Microsoft.VsSDK.targets'
```

**解决**:
安装 Visual Studio extension development 工作负载

---

### 问题 2: NuGet 包错误

**错误**:
```
error NU1102: Unable to find package
```

**解决**:
```cmd
cd src\Sqlx.Extension
dotnet restore
```

---

### 问题 3: License.txt 缺失

**错误**:
```
The file 'License.txt' is not included in the VSIX
```

**解决**:
检查 `Sqlx.Extension.csproj` 包含：
```xml
<Content Include="License.txt">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  <IncludeInVSIX>true</IncludeInVSIX>
</Content>
```

---

### 问题 4: CS0246 错误

**错误**:
```
error CS0246: The type or namespace name 'VisualStudio' could not be found
```

**原因**:
使用了 `dotnet build` 而不是 MSBuild

**解决**:
使用 Visual Studio 或开发人员命令提示符中的 MSBuild

---

## 📊 构建配置

### Debug 配置

```
输出路径: bin\Debug\
优化: 关闭
调试符号: 完整
用途: 开发和调试
```

### Release 配置

```
输出路径: bin\Release\
优化: 开启
调试符号: PDB only
用途: 发布到 Marketplace
```

---

## 🚀 发布构建

### 构建 Release 版本

```cmd
# 方法 1: Visual Studio
生成 → 配置管理器 → Release → 重新生成

# 方法 2: MSBuild
msbuild Sqlx.Extension.csproj /t:Rebuild /p:Configuration=Release
```

### 验证 Release 构建

```powershell
$vsix = "bin\Release\Sqlx.Extension.vsix"

if (Test-Path $vsix) {
    $size = (Get-Item $vsix).Length / 1MB
    Write-Host "✅ VSIX created: $([math]::Round($size, 2)) MB"
} else {
    Write-Host "❌ VSIX not found"
}
```

---

## 📦 VSIX 文件结构

```
Sqlx.Extension.vsix (ZIP format)
├── [Content_Types].xml
├── extension.vsixmanifest
├── manifest.json
├── catalog.json
├── Sqlx.Extension.dll
├── Sqlx.Extension.pdb (Debug only)
├── License.txt
├── Snippets\
│   └── SqlxSnippets.snippet
├── README.md
└── ... (其他文档)
```

---

## ✅ 成功的标志

### 构建输出

```
Microsoft (R) Build Engine version 17.x
...
CreateVsixContainer:
  Successfully created package 'C:\...\Sqlx.Extension.vsix'.

========== Rebuild All: 1 succeeded, 0 failed ==========
========== Rebuild completed at 3:30 PM and took 15.234 seconds ==========
```

### 文件大小

```
典型的 VSIX 大小: 500KB - 2MB
如果 < 100KB: 可能缺少内容
如果 > 10MB: 可能包含了不必要的文件
```

---

## 🎯 快速命令参考

```bash
# 1. 在 Visual Studio 中打开
start Sqlx.sln

# 2. 检查配置
dotnet restore

# 3. 使用 Developer Command Prompt
msbuild Sqlx.Extension.csproj /t:Rebuild /p:Configuration=Release

# 4. 验证 VSIX
ls bin/Release/Sqlx.Extension.vsix -lh

# 5. 测试安装
.\bin\Release\Sqlx.Extension.vsix
```

---

**最后更新**: 2025-10-29
**VS 版本**: 2022 (17.0+)
**项目类型**: VSIX Extension
**构建工具**: MSBuild / Visual Studio

**🎯 记住**: VSIX 项目 **必须** 用 Visual Studio 构建，不能用 `dotnet build`！

