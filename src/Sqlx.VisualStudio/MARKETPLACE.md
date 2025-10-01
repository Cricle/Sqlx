# Visual Studio Marketplace 发布指南

## 📦 发布准备清单

### ✅ 必需文件
- [x] `source.extension.vsixmanifest` - VSIX清单文件
- [x] `SqlxLogo.png` - 扩展图标 (32x32 或更大)
- [x] `README.md` - 详细使用说明
- [x] `LICENSE.txt` - MIT许可证
- [ ] `CHANGELOG.md` - 版本更新日志
- [ ] Screenshots/ - 功能截图文件夹

### 📋 Marketplace信息

**扩展名称**: Sqlx IntelliSense  
**Publisher**: Sqlx Team  
**类别**: Developer Tools > IntelliSense  
**标签**: Sqlx, ORM, SQL, Database, IntelliSense, Productivity, C#, .NET  

### 📸 需要的截图

1. **SQL悬浮提示** - 鼠标悬浮显示SQL查询的截图
2. **工具窗口** - Sqlx方法浏览窗口的截图  
3. **搜索功能** - 在工具窗口中搜索方法的截图
4. **代码导航** - F12跳转功能的截图

### 📝 发布描述模板

```markdown
# Sqlx IntelliSense - Enhanced SQL Development Experience

Boost your productivity when working with **Sqlx ORM** through intelligent IntelliSense features designed specifically for SQL development.

## ✨ Key Features

### 🔍 SQL Hover Tooltips
- Instantly view complete SQL queries on mouse hover
- Formatted syntax highlighting for better readability
- Method signatures and parameter details
- Support for [Sqlx], [SqlTemplate], and [ExpressionToSql] attributes

### 🪟 Dedicated Method Explorer
- Browse all Sqlx methods in your solution
- Real-time search and filtering
- Smart categorization by class and namespace
- Quick access to method details

### 🧭 Smart Navigation  
- F12 to jump directly to method definitions
- Double-click navigation from tool window
- Precise location targeting (file, line, column)

### ⚡ High Performance
- Real-time Roslyn-based code analysis
- Intelligent caching to avoid redundant parsing
- Background processing without blocking UI
- Incremental updates on code changes

## 🎯 Perfect for Sqlx ORM Users

This extension is specifically designed for developers using **Sqlx ORM** - a modern, high-performance .NET ORM with compile-time code generation and zero-reflection design.

**Supported Sqlx Attributes:**
- `[Sqlx("SELECT ...")]` - Direct SQL execution
- `[SqlTemplate("...")]` - SQL templates with placeholders  
- `[ExpressionToSql]` - LINQ expression to SQL conversion

## 🚀 Getting Started

1. Install the extension from Visual Studio Marketplace
2. Open any project that uses Sqlx ORM
3. Hover over methods with Sqlx attributes to see SQL content
4. Use **Ctrl+Shift+S, Q** to open the Sqlx Methods tool window
5. Explore, search, and navigate your SQL code with ease!

## 📋 Requirements

- Visual Studio 2022 (Community, Professional, or Enterprise)
- .NET Framework 4.7.2 or higher
- Projects using Sqlx ORM (any version)

## 🔗 Related Links

- [Sqlx ORM GitHub Repository](https://github.com/your-repo/Sqlx)
- [Sqlx Documentation](https://github.com/your-repo/Sqlx/wiki)
- [Report Issues](https://github.com/your-repo/Sqlx/issues)
```

### 🔄 发布流程

1. **构建VSIX包**:
   ```bash
   dotnet build src/Sqlx.VisualStudio -c Release
   ```

2. **测试VSIX安装**:
   - 在测试环境安装生成的 `.vsix` 文件
   - 验证所有功能正常工作

3. **准备发布资源**:
   - 创建功能演示截图
   - 编写详细的版本更新日志
   - 准备发布说明

4. **发布到Marketplace**:
   - 访问 [Visual Studio Marketplace](https://marketplace.visualstudio.com/manage)
   - 创建新的扩展或更新现有扩展
   - 上传 VSIX 文件和相关资源
   - 填写扩展信息和描述
   - 提交审核

### 🎯 发布策略

- **目标受众**: .NET开发者，特别是使用ORM的开发者
- **关键词优化**: Sqlx, ORM, SQL, IntelliSense, Database, Productivity
- **定价**: 免费
- **支持**: GitHub Issues + 社区论坛

### 📈 推广计划

1. **技术博客文章** - 介绍扩展功能和使用场景
2. **开发者社区** - 在相关论坛和群组分享
3. **Sqlx用户通知** - 在Sqlx项目文档中推荐扩展
4. **演示视频** - 创建功能演示和教程视频
