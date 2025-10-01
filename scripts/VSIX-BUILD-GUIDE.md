# 🔧 Sqlx Visual Studio Extension (VSIX) 编译指南

## ⚠️ 当前状态说明

由于Visual Studio扩展项目的特殊性，VSIX的生成需要特定的构建环境。我们已经创建了3个编译脚本，但**推荐使用Visual Studio IDE进行最终的VSIX生成**。

## 📋 编译环境要求

### ✅ 必需组件
1. **Visual Studio 2022** (Community/Professional/Enterprise)
2. **Visual Studio SDK** (通过VS Installer安装)
3. **.NET Framework 4.7.2** 开发包
4. **MSBuild Tools** (通常随VS安装)

### ✅ 可选但推荐
- **PowerShell 7+** (用于运行脚本)
- **Git** (用于版本控制)

## 🚀 编译方法 (按推荐顺序)

### 方法1: Visual Studio IDE编译 (🎯 推荐)

这是最可靠的VSIX编译方法：

```
1. 打开 Visual Studio 2022
2. 点击 "打开项目或解决方案"
3. 选择 "Sqlx.VisualStudio.sln"
4. 在解决方案资源管理器中:
   - 右键点击 "Sqlx.VisualStudio" 项目
   - 选择 "生成"
5. 编译完成后，VSIX文件将位于:
   src/Sqlx.VisualStudio/bin/Release/net472/Sqlx.VisualStudio.vsix
```

### 方法2: 命令行MSBuild (⭐ 部分支持)

使用我们的PowerShell脚本：

```powershell
# 完整版脚本 (推荐)
.\scripts\build-vsix.ps1 -Configuration Release

# 简化版脚本
.\scripts\build-vsix-simple.ps1 -Configuration Release

# Windows批处理版本
.\scripts\build-vsix.cmd Release
```

**注意**: 命令行脚本可以编译DLL，但VSIX容器生成可能需要Visual Studio环境。

### 方法3: 手动MSBuild

如果你有Visual Studio但想使用命令行：

```batch
# 找到MSBuild路径
"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath

# 使用MSBuild编译
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" ^
  Sqlx.VisualStudio.sln ^
  /p:Configuration=Release ^
  /p:CreateVsixContainer=true ^
  /p:DeployExtension=false
```

## 🔍 故障排除

### 问题1: VSIX文件未生成

**症状**: 编译成功但找不到.vsix文件

**解决方案**:
1. 检查项目是否包含 `source.extension.vsixmanifest` 文件
2. 确认项目文件中设置了 `<CreateVsixContainer>true</CreateVsixContainer>`
3. 使用Visual Studio IDE进行编译
4. 检查输出目录的所有子文件夹

### 问题2: 编译错误

**症状**: 编译失败，显示缺少依赖或命名空间错误

**解决方案**:
1. 确认安装了Visual Studio SDK
2. 运行 NuGet 包恢复: `dotnet restore Sqlx.VisualStudio.sln`
3. 清理并重新生成: `dotnet clean` 然后重新编译
4. 检查.NET Framework 4.7.2是否已安装

### 问题3: PowerShell执行策略

**症状**: PowerShell脚本无法执行

**解决方案**:
```powershell
# 临时允许脚本执行
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# 或者使用绕过参数
pwsh -ExecutionPolicy Bypass .\scripts\build-vsix-simple.ps1
```

## 📁 输出文件位置

成功编译后，文件将位于:

```
src/Sqlx.VisualStudio/bin/{Configuration}/net472/
├── Sqlx.VisualStudio.dll        # 主程序集
├── Sqlx.VisualStudio.vsix       # VSIX安装包 (如果成功生成)
├── SqlxLogo.png                 # 图标文件
├── README.md                    # 说明文档
├── LICENSE.txt                  # 许可证
└── CHANGELOG.md                 # 更新日志
```

## 🎯 验证VSIX文件

### 文件存在性检查
```powershell
# 检查VSIX是否存在
$vsixPath = "src/Sqlx.VisualStudio/bin/Release/net472/Sqlx.VisualStudio.vsix"
if (Test-Path $vsixPath) {
    $info = Get-Item $vsixPath
    Write-Host "✅ VSIX文件存在: $($info.Length / 1KB) KB"
} else {
    Write-Host "❌ VSIX文件不存在，请使用Visual Studio编译"
}
```

### VSIX内容验证
VSIX文件本质上是ZIP压缩包，可以用解压工具查看内容：

```
Sqlx.VisualStudio.vsix (解压后内容)
├── [Content_Types].xml
├── extension.vsixmanifest
├── Sqlx.VisualStudio.dll
├── Sqlx.VisualStudio.pkgdef
├── SqlxLogo.png
├── README.md
├── LICENSE.txt
└── CHANGELOG.md
```

## 📦 安装编译好的VSIX

### 方法1: 双击安装
```
双击 Sqlx.VisualStudio.vsix 文件
跟随安装向导完成安装
```

### 方法2: Visual Studio内安装
```
1. 打开Visual Studio
2. 扩展 → 管理扩展
3. 点击 "从磁盘安装..."
4. 选择VSIX文件
5. 重启Visual Studio
```

### 方法3: 命令行安装
```batch
# 使用VSIXInstaller
VSIXInstaller.exe "path\to\Sqlx.VisualStudio.vsix"

# 静默安装
VSIXInstaller.exe /quiet "path\to\Sqlx.VisualStudio.vsix"
```

## 🔧 开发者提示

### Debug构建
```powershell
# Debug版本编译
.\scripts\build-vsix.ps1 -Configuration Debug -Clean -Verbose
```

### 清理构建环境
```powershell
# 清理所有生成文件
dotnet clean Sqlx.VisualStudio.sln
Remove-Item "src/Sqlx.VisualStudio/obj" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "src/Sqlx.VisualStudio/bin" -Recurse -Force -ErrorAction SilentlyContinue
```

### 版本管理
修改 `source.extension.vsixmanifest` 中的版本号：
```xml
<Identity Id="Sqlx.VisualStudio.Extension" Version="1.0.1" ... />
```

## 🎉 功能验证

安装扩展后，验证功能是否正常：

1. **SQL悬浮提示**:
   - 打开包含`[Sqlx]`特性的C#文件
   - 鼠标悬停在方法上
   - 应该显示SQL内容和参数信息

2. **智能感知**:
   - 编写Sqlx代码时应有智能提示
   - 支持`[SqlTemplate]`和`[ExpressionToSql]`

## 🚨 已知限制

1. **命令行VSIX生成**: 当前脚本可能无法生成VSIX文件，建议使用Visual Studio IDE
2. **依赖复杂性**: VSIX项目对VS SDK依赖较强，需要完整的VS环境
3. **版本兼容性**: 仅支持Visual Studio 2022 (17.0+)

## 📞 支持

如果遇到编译问题:

1. **检查环境**: 确认VS 2022和SDK已正确安装
2. **清理重建**: 删除obj和bin文件夹后重新编译
3. **使用VS IDE**: 最可靠的编译方式
4. **查看日志**: 使用详细模式查看完整错误信息

---

**最后更新**: 2025年10月1日
**适用版本**: Visual Studio 2022, .NET Framework 4.7.2
**维护者**: Sqlx团队
