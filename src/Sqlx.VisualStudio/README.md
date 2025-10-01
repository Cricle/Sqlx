# Sqlx IntelliSense - Visual Studio Extension

## 🔍 简介

**Sqlx IntelliSense** 是专为 **Sqlx ORM** 开发者设计的Visual Studio扩展，提供丰富的IntelliSense功能，让您的SQL开发体验更加流畅高效。

## ✨ 核心功能

### 🖱️ 智能悬浮提示 (Quick Info)
- **鼠标悬浮** 在标记有 `[Sqlx]`、`[SqlTemplate]`、`[ExpressionToSql]` 特性的方法上
- **即时显示** 完整的SQL查询内容
- **详细信息** 包括方法签名、参数列表、命名空间等
- **格式化SQL** 自动格式化，提升可读性

### 🪟 专用工具窗口
- **方法列表** 显示项目中所有Sqlx方法
- **智能搜索** 支持按方法名、类名、SQL内容搜索
- **详情面板** 展开查看完整的方法和SQL信息
- **实时刷新** 代码变更时自动更新列表

### 🧭 快速导航
- **双击跳转** 在工具窗口中双击方法名直接跳转到定义
- **F12支持** 在代码编辑器中按F12导航到方法
- **精确定位** 自动跳转到具体行和列

### ⌨️ 快捷键支持
- **Ctrl+Shift+S, Q** - 打开Sqlx方法工具窗口
- **F12** - 导航到方法定义（在Sqlx方法上）

## 🚀 安装方法

### 方法1：从Visual Studio Marketplace安装（推荐）
1. 打开 **Visual Studio 2022**
2. 转到 **扩展** → **管理扩展**
3. 在 **联机** 选项卡中搜索 "**Sqlx IntelliSense**"
4. 找到 "**Sqlx IntelliSense**" 并点击 **下载**
5. 重启 Visual Studio 完成安装

### 方法2：通过Visual Studio Marketplace网站
1. 访问 [Visual Studio Marketplace](https://marketplace.visualstudio.com/)
2. 搜索 "**Sqlx IntelliSense**"
3. 点击 **Download** 按钮
4. 双击下载的 `.vsix` 文件进行安装
5. 重启 Visual Studio

### 方法3：从VSIX文件安装
1. 从 [Releases](https://github.com/your-repo/Sqlx/releases) 页面下载 `Sqlx.VisualStudio.vsix` 文件
2. 双击VSIX文件启动安装程序
3. 按照安装向导完成安装
4. 重启 Visual Studio

## 📋 系统要求

- **Visual Studio 2022** (Community/Professional/Enterprise)
- **.NET Framework 4.7.2** 或更高版本
- **Windows 10** 或更高版本
- **Sqlx ORM** 任意版本（可选，但推荐使用以获得完整体验）

## 🎯 使用指南

### 悬浮提示功能
1. 在代码中找到任何标记有Sqlx特性的方法
2. 将鼠标悬浮在方法名上
3. 自动显示包含SQL查询的详细信息窗口

```csharp
[Sqlx("SELECT * FROM Users WHERE Id = @id")]
public partial Task<User?> GetUserByIdAsync(int id);
// 鼠标悬浮在 GetUserByIdAsync 上会显示SQL详情
```

### 工具窗口功能
1. 使用快捷键 **Ctrl+Shift+S, Q** 打开工具窗口
2. 或者通过菜单：**视图** → **Sqlx Methods**
3. 在搜索框中输入关键词过滤方法
4. 双击任意方法跳转到定义

### 搜索技巧
- **方法名搜索**：输入方法名的任意部分
- **类名搜索**：搜索包含方法的类名
- **SQL内容搜索**：搜索SQL语句中的关键词
- **命名空间搜索**：按命名空间筛选

## 🛠️ 支持的特性

扩展能够识别和处理以下Sqlx特性：

- `[Sqlx]` - 基本SQL执行特性
- `[SqlTemplate]` - SQL模板特性
- `[ExpressionToSql]` - LINQ表达式转SQL特性

## 🔧 故障排除

### 扩展没有加载
1. 确认Visual Studio版本为2022或更高
2. 重启Visual Studio
3. 检查 **扩展** → **管理扩展** → **已安装** 中是否存在

### 悬浮提示不工作
1. 确认项目中引用了Sqlx ORM
2. 检查方法上是否正确标记了Sqlx特性
3. 重新构建解决方案

### 工具窗口为空
1. 点击 **刷新** 按钮
2. 确认项目中存在Sqlx方法
3. 检查项目是否正确编译

## 📞 支持与反馈

- **GitHub Issues**: [提交问题和建议](https://github.com/your-repo/Sqlx/issues)
- **文档**: [查看完整文档](https://github.com/your-repo/Sqlx/wiki)
- **示例代码**: [参考示例项目](https://github.com/your-repo/Sqlx/tree/main/samples)

## 📄 许可证

本扩展基于 **MIT 许可证** 开源，详见 [LICENSE](LICENSE.txt) 文件。

## 🎉 致谢

感谢所有Sqlx ORM用户的支持和反馈，让我们能够不断改进和优化开发体验！

---

**享受更高效的Sqlx ORM开发体验！** 🚀
