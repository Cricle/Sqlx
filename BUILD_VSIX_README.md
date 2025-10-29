# 🏗️ VSIX 构建脚本使用指南

快速构建 Sqlx Visual Studio Extension 并将 VSIX 文件提取到根目录。

---

## 🚀 快速开始

### 方法 1: 双击运行（最简单）⭐

```
双击: build-vsix.bat
```

**就这么简单！** 脚本会自动：
1. ✅ 检查构建环境
2. ✅ 清理旧文件
3. ✅ 还原 NuGet 包
4. ✅ 构建 Release 版本
5. ✅ 复制 VSIX 到根目录
6. ✅ 生成校验和文件

---

### 方法 2: PowerShell 命令

#### 构建 Release 版本（推荐）

```powershell
.\build-vsix.ps1
```

或

```powershell
.\build-vsix.ps1 -Configuration Release
```

#### 构建 Debug 版本

```powershell
.\build-vsix.ps1 -Configuration Debug
```

---

## 📦 输出文件

构建成功后，根目录会生成以下文件：

### Release 构建

```
Sqlx.Extension-v0.1.0-Release.vsix          # VSIX 安装包
Sqlx.Extension-v0.1.0-Release.vsix.sha256   # SHA256 校验和
```

### Debug 构建

```
Sqlx.Extension-v0.1.0-Debug.vsix            # VSIX 安装包（包含调试符号）
Sqlx.Extension-v0.1.0-Debug.vsix.sha256     # SHA256 校验和
```

---

## 🔍 脚本功能详解

### 自动化流程

```
1. 环境检查
   ├─ 查找 Visual Studio 2022 (Community/Professional/Enterprise)
   ├─ 定位 MSBuild.exe
   └─ 验证项目文件

2. 清理工作
   ├─ 删除 bin 目录
   └─ 删除 obj 目录

3. 依赖还原
   └─ 运行 dotnet restore

4. 编译构建
   ├─ 调用 MSBuild
   ├─ 目标：Rebuild
   └─ 配置：Release 或 Debug

5. 验证输出
   ├─ 检查 VSIX 文件是否生成
   ├─ 显示文件大小
   └─ 验证关键内容

6. 复制文件
   ├─ 复制到根目录
   ├─ 重命名为带版本号的文件名
   └─ 生成 SHA256 校验和

7. 完成报告
   └─ 显示文件位置和后续操作建议
```

---

## ✅ 前置条件

### 必须安装

- [x] **Visual Studio 2022** (任意版本：Community/Professional/Enterprise)
- [x] **Visual Studio extension development** 工作负载
- [x] **.NET Framework 4.7.2+**
- [x] **Windows PowerShell 5.0+** (Windows 10/11 自带)

### 检查工作负载

```
Visual Studio Installer
→ 修改 Visual Studio 2022
→ 工作负载
→ 确保勾选: "Visual Studio extension development"
```

---

## 📊 输出示例

### 成功的构建输出

```
🏗️  Sqlx VSIX 构建脚本
============================================================

📋 检查构建环境...
✅ 找到 MSBuild: C:\Program Files\...\MSBuild.exe
✅ 项目目录: ...\src\Sqlx.Extension
✅ 项目文件: Sqlx.Extension.csproj

🧹 清理旧的构建输出...
✅ 已清理 bin 目录
✅ 已清理 obj 目录

📦 还原 NuGet 包...
✅ NuGet 包还原成功

🔨 开始构建 (Release 配置)...
命令: MSBuild Sqlx.Extension.csproj /t:Rebuild /p:Configuration=Release...

✅ 构建成功!

📦 检查生成的文件...
✅ VSIX 文件已生成
   位置: bin\Release\Sqlx.Extension.vsix
   大小: 1.25 MB
   时间: 2025-10-29 15:30:00

📋 复制 VSIX 到根目录...
✅ VSIX 已复制到根目录
   位置: C:\...\Sqlx.Extension-v0.1.0-Release.vsix
   文件名: Sqlx.Extension-v0.1.0-Release.vsix

🔍 验证 VSIX 内容...
   ✅ extension.vsixmanifest
   ✅ Sqlx.Extension.dll
   ✅ License.txt

🔐 生成校验和...
   SHA256: A1B2C3D4...
   ✅ 校验和已保存: ...sha256

============================================================
🎉 构建完成！
============================================================

📦 VSIX 文件信息:
   文件名: Sqlx.Extension-v0.1.0-Release.vsix
   位置: C:\...\Sqlx.Extension-v0.1.0-Release.vsix
   大小: 1.25 MB
   配置: Release

🚀 下一步操作:
   1. 安装测试:
      双击: Sqlx.Extension-v0.1.0-Release.vsix

   2. 发布到 Marketplace:
      上传: Sqlx.Extension-v0.1.0-Release.vsix

   3. 创建 GitHub Release:
      附加: Sqlx.Extension-v0.1.0-Release.vsix
      附加: Sqlx.Extension-v0.1.0-Release.vsix.sha256

✅ 所有步骤完成！
```

---

## 🐛 故障排除

### 问题 1: 找不到 MSBuild

**错误**:
```
❌ 错误: 找不到 MSBuild.exe
```

**解决**:
1. 确认已安装 Visual Studio 2022
2. 检查安装路径是否为默认路径
3. 安装 "Visual Studio extension development" 工作负载

---

### 问题 2: NuGet 还原失败

**错误**:
```
❌ NuGet 包还原失败
```

**解决**:
```powershell
# 清理 NuGet 缓存
dotnet nuget locals all --clear

# 手动还原
cd src\Sqlx.Extension
dotnet restore
```

---

### 问题 3: 构建失败

**错误**:
```
❌ 构建失败!
```

**解决**:
1. 查看详细错误信息
2. 检查是否缺少文件（如 License.txt）
3. 在 Visual Studio 中打开项目手动构建查看详细错误

---

### 问题 4: 权限问题

**错误**:
```
Access denied / 拒绝访问
```

**解决**:
以管理员身份运行 PowerShell：
```powershell
# 右键 PowerShell → 以管理员身份运行
.\build-vsix.ps1
```

---

## 📝 脚本参数

### PowerShell 脚本参数

```powershell
.\build-vsix.ps1 [-Configuration <String>]
```

**参数**:
- `-Configuration`: 构建配置
  - `Release` (默认): 生产版本，优化代码
  - `Debug`: 调试版本，包含调试符号

**示例**:
```powershell
# Release 构建
.\build-vsix.ps1
.\build-vsix.ps1 -Configuration Release

# Debug 构建
.\build-vsix.ps1 -Configuration Debug
```

---

## 🔧 自定义脚本

### 修改输出文件名

编辑 `build-vsix.ps1`，找到：

```powershell
$outputFileName = "Sqlx.Extension-v0.1.0-$Configuration.vsix"
```

修改为你想要的格式：

```powershell
$outputFileName = "MyExtension-v1.0.0-$Configuration.vsix"
```

### 修改输出位置

编辑 `build-vsix.ps1`，找到：

```powershell
$rootDir = Split-Path (Split-Path $projectDir -Parent) -Parent
$outputPath = Join-Path $rootDir $outputFileName
```

修改为你想要的位置：

```powershell
$outputPath = "C:\MyOutput\$outputFileName"
```

---

## 📋 文件清单

```
项目根目录/
├── build-vsix.ps1              # PowerShell 构建脚本
├── build-vsix.bat              # 批处理快捷方式
├── BUILD_VSIX_README.md        # 本说明文档
└── src/
    └── Sqlx.Extension/
        ├── Sqlx.Extension.csproj
        ├── source.extension.vsixmanifest
        ├── License.txt
        └── ... (其他源文件)
```

---

## 🎯 使用场景

### 场景 1: 日常开发测试

```batch
REM 快速构建并测试
build-vsix.bat

REM 双击生成的 VSIX 安装
Sqlx.Extension-v0.1.0-Release.vsix
```

### 场景 2: 准备发布

```powershell
# 构建 Release 版本
.\build-vsix.ps1 -Configuration Release

# 验证 VSIX 文件
Get-FileHash .\Sqlx.Extension-v0.1.0-Release.vsix

# 创建 GitHub Release
# 附加生成的 .vsix 和 .sha256 文件
```

### 场景 3: CI/CD 集成

```yaml
# GitHub Actions 示例
- name: Build VSIX
  run: |
    pwsh build-vsix.ps1 -Configuration Release
    
- name: Upload VSIX
  uses: actions/upload-artifact@v3
  with:
    name: vsix-package
    path: Sqlx.Extension-v0.1.0-Release.vsix
```

---

## 🆘 获取帮助

### 查看详细日志

```powershell
# 启用详细输出
$VerbosePreference = "Continue"
.\build-vsix.ps1
```

### 报告问题

如果脚本遇到问题，请提供：

1. **完整的错误输出**
2. **Visual Studio 版本**
3. **Windows 版本**
4. **PowerShell 版本**: `$PSVersionTable.PSVersion`

---

## 📚 相关文档

- [HOW_TO_BUILD_VSIX.md](src/Sqlx.Extension/HOW_TO_BUILD_VSIX.md) - 详细的构建指南
- [BUILD.md](src/Sqlx.Extension/BUILD.md) - 构建说明
- [TESTING_GUIDE.md](src/Sqlx.Extension/TESTING_GUIDE.md) - 测试指南

---

## ✨ 特性

- ✅ 自动环境检查
- ✅ 智能查找 VS 2022 (Community/Professional/Enterprise)
- ✅ 自动清理旧文件
- ✅ NuGet 包还原
- ✅ 完整的构建过程
- ✅ 内容验证
- ✅ SHA256 校验和
- ✅ 彩色输出
- ✅ 详细的进度信息
- ✅ 友好的错误提示

---

**最后更新**: 2025-10-29  
**版本**: 1.0  
**作者**: Sqlx Team

**🎉 享受自动化构建！** 🚀

