# Sqlx IntelliSense - Visual Studio Extension (独立版本)

## 🎯 项目概述

这是**Sqlx IntelliSense**的独立Visual Studio扩展解决方案，专门为解决包依赖冲突而设计。

### ✨ 核心特性

- **🖱️ SQL悬浮提示**: 鼠标悬浮在标记有`[Sqlx]`、`[SqlTemplate]`、`[ExpressionToSql]`特性的方法上时，显示SQL查询内容
- **⚡ 高性能**: 基于Roslyn的实时代码分析，响应迅速
- **🛡️ 稳定可靠**: 独立包管理，避免与主项目的依赖冲突
- **🔧 简洁设计**: 专注于核心SQL提示功能，去除复杂组件

## 🚀 技术优势

### 独立解决方案架构
- ✅ **零冲突**: 独立的包版本管理，不与主Sqlx项目冲突
- ✅ **轻量级**: 仅包含VS扩展必需的组件和依赖
- ✅ **易维护**: 单一职责原则，专注于IntelliSense功能

### VS SDK集成
- 🔧 **VS 2022兼容**: 支持Community/Professional/Enterprise版本
- 🔧 **MEF组件**: 使用Managed Extensibility Framework进行组件注册
- 🔧 **Roslyn集成**: 深度集成C#代码分析服务

## 📁 项目结构

```
Sqlx.VisualStudio.sln                 # 独立解决方案文件
├── Directory.Build.props              # 统一项目属性和包版本
├── README-VS-EXTENSION.md            # 项目说明文档
└── src/Sqlx.VisualStudio/
    ├── Sqlx.VisualStudio.csproj     # VS插件项目文件
    ├── source.extension.vsixmanifest # VSIX清单文件
    ├── SqlxVSPackage.cs             # VS包主类
    ├── Core/
    │   └── SqlxAttributes.cs        # Sqlx特性定义（独立副本）
    ├── IntelliSense/
    │   └── SqlxQuickInfoProvider.cs # SQL悬浮提示提供器
    └── Assets/
        ├── SqlxLogo.png             # 插件图标
        ├── README.md                # 用户文档
        ├── CHANGELOG.md             # 版本变更记录
        └── LICENSE.txt              # 许可证文件
```

## 🔧 开发环境要求

- **Visual Studio 2022** (17.0或更高版本)
- **.NET Framework 4.7.2**
- **Visual Studio SDK** (通过NuGet自动安装)

## 🛠️ 构建说明

### 1. 克隆项目
```bash
git clone [repository-url]
cd Sqlx/
```

### 2. 使用独立解决方案
```bash
# 打开独立的VS扩展解决方案
start Sqlx.VisualStudio.sln
```

### 3. 构建和调试
1. 设置为**Debug**配置
2. 按**F5**启动调试，这会启动一个VS实验实例
3. 在实验实例中打开包含Sqlx方法的C#项目
4. 悬浮鼠标在标记有Sqlx特性的方法上查看SQL提示

## 📦 发布到Marketplace

### 1. 构建Release版本
```bash
dotnet build -c Release
```

### 2. 生成VSIX文件
构建完成后，在`src/Sqlx.VisualStudio/bin/Release/`目录中找到`Sqlx.VisualStudio.vsix`文件。

### 3. 上传到Visual Studio Marketplace
1. 访问[Visual Studio Marketplace Publisher Portal](https://marketplace.visualstudio.com/manage)
2. 登录并创建新扩展
3. 上传VSIX文件
4. 填写扩展信息和描述

## 🤝 与主项目的关系

### 独立性原则
- **代码复用**: 复制核心的Sqlx特性定义，确保独立运行
- **包管理隔离**: 使用固定的包版本，避免与主项目的中央包管理冲突
- **功能专注**: 只实现VS IntelliSense功能，不依赖主项目的其他模块

### 同步策略
- **特性定义**: 当主项目的Sqlx特性发生变化时，需要手动同步更新
- **功能扩展**: 新功能优先在主项目中实现和测试，然后移植到独立插件

## 🔍 故障排除

### 常见问题

#### 1. 构建失败：包版本冲突
**解决方案**: 确保使用独立解决方案文件`Sqlx.VisualStudio.sln`，而不是主项目的`Sqlx.sln`。

#### 2. QuickInfo不显示
**检查步骤**:
- 确保方法标记有正确的Sqlx特性
- 检查Visual Studio的IntelliSense设置
- 重启Visual Studio实验实例

#### 3. 调试时插件未加载
**解决方案**:
- 检查VSIX清单文件中的组件注册
- 确认项目属性中的调试设置正确

## 📋 版本历史

### v1.0.0 (Initial Release)
- ✅ SQL悬浮提示功能
- ✅ 支持[Sqlx]、[SqlTemplate]、[ExpressionToSql]特性
- ✅ 独立解决方案架构
- ✅ VS 2022兼容性

---

**Sqlx Team © 2025** | [GitHub](https://github.com/your-repo/Sqlx) | [Issues](https://github.com/your-repo/Sqlx/issues)
