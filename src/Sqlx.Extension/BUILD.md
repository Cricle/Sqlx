# Sqlx.Extension 构建说明

> **项目类型**: Visual Studio Extension (VSIX)  
> **框架**: .NET Framework 4.7.2  
> **IDE要求**: Visual Studio 2022 (含 VS SDK)

---

## 🛠️ 构建环境要求

### 1. Visual Studio 2022

必须安装以下组件：

- ✅ **Visual Studio extension development** 工作负载
- ✅ **.NET Framework 4.7.2 SDK**
- ✅ **VS SDK (Visual Studio Extensibility Tools)**

### 2. 安装 VS SDK

在 Visual Studio Installer 中：

1. 打开 Visual Studio Installer
2. 选择 "Modify"
3. 切换到 "Workloads" 标签
4. 勾选 "Visual Studio extension development"
5. 点击 "Modify" 安装

---

## 🚀 构建步骤

### 方法 1: 在 Visual Studio 中构建（推荐）

1. **打开项目**
   ```
   双击 Sqlx.Extension.csproj
   或
   在 VS 中打开 Sqlx.sln 解决方案
   ```

2. **还原 NuGet 包**
   - 右键项目 → "Restore NuGet Packages"
   - 或者 `Tools` → `NuGet Package Manager` → `Restore NuGet Packages`

3. **构建项目**
   - 按 `Ctrl+Shift+B` 或
   - `Build` → `Build Sqlx.Extension`

4. **运行/调试**
   - 按 `F5` 启动调试
   - 这会打开一个新的 Visual Studio 实验实例
   - 在实验实例中测试语法着色功能

### 方法 2: 使用 MSBuild（命令行）

**前提**: 需要安装 Visual Studio 或 Build Tools

```powershell
# 1. 打开 Developer Command Prompt for VS 2022

# 2. 恢复包
msbuild Sqlx.Extension.csproj /t:Restore

# 3. 构建
msbuild Sqlx.Extension.csproj /p:Configuration=Release

# 4. 输出位置
# bin\Release\Sqlx.Extension.vsix
```

---

## 📦 输出文件

### Debug 构建
```
bin\Debug\
├── Sqlx.Extension.dll      # 主程序集
├── Sqlx.Extension.pdb      # 调试符号
└── Sqlx.Extension.vsix     # VSIX 安装包
```

### Release 构建
```
bin\Release\
├── Sqlx.Extension.dll      # 主程序集（优化）
└── Sqlx.Extension.vsix     # VSIX 安装包（用于发布）
```

---

## 🧪 测试语法着色

### 1. 启动调试（F5）

这会：
- 编译项目
- 启动新的 VS 实验实例
- 自动加载插件

### 2. 在实验实例中测试

1. 打开或创建一个 C# 项目
2. 添加对 Sqlx 的引用
3. 创建带有 `[SqlTemplate]` 属性的代码
4. 验证语法着色：
   - SQL 关键字应为蓝色
   - 占位符 `{{...}}` 应为橙色
   - 参数 `@...` 应为绿色

### 3. 测试示例

可以复制 `Examples\SyntaxHighlightingExample.cs` 中的代码进行测试。

---

## ⚠️ 常见问题

### 问题 1: "未能找到类型或命名空间名 'Microsoft.VisualStudio'"

**原因**: VS SDK 未安装

**解决**:
1. 关闭 Visual Studio
2. 打开 Visual Studio Installer
3. 安装 "Visual Studio extension development" 工作负载
4. 重启 Visual Studio

### 问题 2: "PackageReference 版本错误"

**原因**: 中央包管理配置问题

**解决**:
所有 VS SDK 包的版本都在 `Directory.Packages.props` 中定义，无需在项目文件中指定版本。

### 问题 3: 构建成功但语法着色不工作

**检查清单**:
- ✅ MEF 组件是否正确导出 (`[Export]` 属性)
- ✅ 分类类型是否正确注册
- ✅ Content Type 是否设置为 "CSharp"
- ✅ 是否在 SqlTemplate 属性中

**调试步骤**:
1. 在 `SqlTemplateClassifier.GetClassificationSpans()` 设置断点
2. F5 启动调试
3. 在实验实例中打开包含 SqlTemplate 的文件
4. 检查断点是否命中

### 问题 4: dotnet build 失败

**原因**: VS 插件项目使用旧版 MSBuild 格式，不支持 `dotnet build`

**解决**: 
- 使用 Visual Studio IDE 构建
- 或使用 `msbuild` 命令行工具

---

## 📝 项目结构

```
Sqlx.Extension/
├── Properties/
│   └── AssemblyInfo.cs              # 程序集信息
├── SyntaxColoring/
│   ├── SqlTemplateClassifier.cs     # 核心分类器（206行）
│   ├── SqlTemplateClassifierProvider.cs  # MEF提供者（20行）
│   └── SqlClassificationDefinitions.cs   # 类型和格式定义（137行）
├── Examples/
│   └── SyntaxHighlightingExample.cs # 示例代码（不编译，仅文档）
├── Snippets/
│   └── SqlxSnippets.snippet        # 代码片段
├── Sqlx.ExtensionPackage.cs        # VS 包主类
├── source.extension.vsixmanifest   # VSIX 元数据
├── README.md                       # 功能说明
├── IMPLEMENTATION_NOTES.md         # 实现细节
├── BUILD.md                        # 本文档
└── Sqlx.Extension.csproj           # 项目文件
```

---

## 🎯 代码文件说明

### 核心代码（必须编译）

| 文件 | 行数 | 说明 |
|------|------|------|
| `SqlTemplateClassifier.cs` | 206 | 分类器实现，识别SQL元素 |
| `SqlTemplateClassifierProvider.cs` | 20 | MEF提供者，创建分类器 |
| `SqlClassificationDefinitions.cs` | 137 | 5种分类和格式定义 |
| `Sqlx.ExtensionPackage.cs` | 53 | VS包主类 |
| `Properties\AssemblyInfo.cs` | - | 程序集元数据 |

### 文档文件（不编译，包含在VSIX中）

| 文件 | 说明 |
|------|------|
| `Examples\SyntaxHighlightingExample.cs` | 10+示例用例 |
| `README.md` | 功能介绍 |
| `IMPLEMENTATION_NOTES.md` | 技术细节 |
| `BUILD.md` | 构建说明 |
| `Snippets\SqlxSnippets.snippet` | 代码片段 |

---

## 🔍 验证构建

### 检查 VSIX 内容

```powershell
# VSIX 实际上是一个 ZIP 文件
# 可以解压查看内容

# 1. 复制 vsix 文件
cp bin\Release\Sqlx.Extension.vsix Sqlx.Extension.zip

# 2. 解压
Expand-Archive Sqlx.Extension.zip -DestinationPath vsix_content

# 3. 检查内容
tree vsix_content
```

### 预期内容

```
vsix_content/
├── extension.vsixmanifest
├── catalog.json
├── manifest.json
├── [Content_Types].xml
├── Sqlx.Extension.dll
├── Sqlx.Extension.pkgdef
├── Snippets/
│   └── SqlxSnippets.snippet
├── Examples/
│   └── SyntaxHighlightingExample.cs
├── README.md
└── IMPLEMENTATION_NOTES.md
```

---

## 📈 性能验证

### 测试方法

1. 打开大型 C# 文件（1000+行）
2. 添加多个 `[SqlTemplate]` 属性
3. 观察编辑器响应时间

### 预期性能

- **首次加载**: < 100ms
- **语法着色**: < 1ms per attribute
- **内存占用**: < 10MB

---

## 🐛 调试技巧

### 1. 查看 MEF 组件

```csharp
// 在 SqlTemplateClassifierProvider 构造函数中添加日志
public SqlTemplateClassifierProvider()
{
    System.Diagnostics.Debug.WriteLine("SqlTemplateClassifierProvider created!");
}
```

### 2. 查看分类器调用

```csharp
// 在 GetClassificationSpans 中添加日志
public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
{
    var text = span.GetText();
    System.Diagnostics.Debug.WriteLine($"Classifying: {text}");
    // ...
}
```

### 3. 查看输出窗口

在 VS 中：`View` → `Output` → 选择 "Debug"

---

## ✅ 构建成功标志

### 控制台输出

```
Build started...
1>------ Build started: Project: Sqlx.Extension, Configuration: Release Any CPU ------
1>Sqlx.Extension -> C:\...\bin\Release\Sqlx.Extension.dll
1>CreateVsixContainer:
1>  Created 'C:\...\bin\Release\Sqlx.Extension.vsix'
========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========
```

### 文件存在

- ✅ `bin\Release\Sqlx.Extension.dll` 存在
- ✅ `bin\Release\Sqlx.Extension.vsix` 存在
- ✅ VSIX 文件大小 > 100KB

---

## 📞 需要帮助？

如果遇到构建问题：

1. **检查环境**: 确保 VS 2022 和 VS SDK 已安装
2. **清理重建**: `Build` → `Clean Solution` 然后 `Rebuild Solution`
3. **重置实验实例**: 删除 `%LocalAppData%\Microsoft\VisualStudio\17.0_<xxx>Exp`
4. **查看日志**: 检查 VS 输出窗口的错误信息

---

**最后更新**: 2025-10-29  
**版本**: 0.5.0-dev  
**状态**: ✅ 编译配置已完成

