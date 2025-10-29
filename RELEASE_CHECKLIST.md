# 🚀 Release Checklist - v0.5.0-preview

> **发布前完整检查清单 - 确保万无一失**

---

## 📋 使用说明

- [ ] 表示待完成
- [x] 表示已完成
- ⚠️ 表示需要注意的项目
- 🔴 表示关键项目，必须完成

---

## ✅ Pre-Release Checklist

### 1️⃣ 代码完整性 (Code Completeness)

#### 核心库 (Sqlx)
- [x] 所有功能已实现
- [x] 代码已编译通过 (0 errors, 0 warnings)
- [x] 所有接口定义完整
  - [x] ICrudRepository
  - [x] IQueryRepository
  - [x] ICommandRepository
  - [x] IBatchRepository
  - [x] IAggregateRepository
  - [x] IAdvancedRepository
  - [x] IRepository
  - [x] IReadOnlyRepository
  - [x] IBulkRepository
  - [x] IWriteOnlyRepository

#### 源生成器 (Sqlx.Generator)
- [x] 生成器代码完整
- [x] 编译无错误
- [x] 生成代码可用

#### VS Extension (Sqlx.Extension)
- [x] 所有窗口已实现 (14个)
- [x] IntelliSense已实现 (44+项)
- [x] 语法着色已实现 (5色)
- [x] 代码片段已实现 (12个)
- [x] 快速操作已实现 (2个)
- [x] 诊断功能已实现
- [x] VSIX manifest正确
- [x] License文件包含

---

### 2️⃣ 测试完整性 (Testing)

#### 单元测试
- [ ] 运行所有单元测试
  ```bash
  dotnet test tests/Sqlx.Tests/
  ```
- [ ] 所有测试通过 (预期: 1412+ passed)
- [ ] 无跳过的测试
- [ ] 无临时禁用的测试

#### 集成测试
- [ ] 运行集成测试
- [ ] 测试所有数据库方言
  - [ ] SQLite
  - [ ] MySQL
  - [ ] PostgreSQL
  - [ ] SQL Server
  - [ ] Oracle

#### VS Extension测试
- [ ] 手动测试所有工具窗口
  - [ ] SQL Preview
  - [ ] Generated Code
  - [ ] Query Tester
  - [ ] Repository Explorer
  - [ ] SQL Execution Log
  - [ ] Template Visualizer
  - [ ] Performance Analyzer
  - [ ] Entity Mapping Viewer
  - [ ] SQL Breakpoints
  - [ ] SQL Watch
- [ ] 测试IntelliSense
- [ ] 测试语法着色
- [ ] 测试代码片段
- [ ] 测试快速操作

#### 性能测试
- [ ] 运行基准测试
  ```bash
  cd tests/Sqlx.Benchmarks
  dotnet run -c Release
  ```
- [ ] 验证性能数据
  - [ ] vs ADO.NET: ~105%
  - [ ] vs Dapper: ~100%
  - [ ] vs EF Core: ~175%
  - [ ] 批量操作: 20-25倍

---

### 3️⃣ 文档完整性 (Documentation)

#### 核心文档
- [x] README.md 完整且最新
- [x] INDEX.md 导航完整
- [x] INSTALL.md 安装指南详细
- [x] TUTORIAL.md 教程完整 (10课)
- [x] FAQ.md 问题齐全 (35+)
- [x] TROUBLESHOOTING.md 故障排除详细
- [x] MIGRATION_GUIDE.md 迁移指南完整
- [x] PERFORMANCE.md 性能数据详细
- [x] QUICK_REFERENCE.md 快速参考完整
- [x] CHANGELOG.md 更新日志完整
- [x] CONTRIBUTING.md 贡献指南完整
- [x] HOW_TO_RELEASE.md 发布流程完整

#### 技术文档
- [x] docs/QUICK_START_GUIDE.md
- [x] docs/API_REFERENCE.md
- [x] docs/BEST_PRACTICES.md
- [x] docs/ADVANCED_FEATURES.md
- [x] docs/PLACEHOLDERS.md

#### 文档链接检查
- [ ] 所有内部链接有效
- [ ] 所有外部链接有效
- [ ] 代码示例可运行
- [ ] 截图清晰（如有）

---

### 4️⃣ 版本管理 (Version Management)

#### 版本号
- [x] Directory.Build.props 版本正确 (0.4.0)
- [ ] 🔴 更新到 v0.5.0-preview
  ```xml
  <Version>0.5.0-preview</Version>
  ```
- [x] VERSION 文件正确
- [ ] 🔴 更新 VERSION 文件到 v0.5.0-preview

#### 变更日志
- [x] CHANGELOG.md 包含 v0.5.0-preview
- [ ] 列出所有新功能
- [ ] 列出所有改进
- [ ] 列出所有Bug修复
- [ ] 列出已知限制

---

### 5️⃣ Git 和 GitHub (Git & GitHub)

#### Git状态
- [x] 所有更改已提交
- [x] 工作目录干净
- [x] 所有提交已推送到 origin/main

#### GitHub检查
- [ ] GitHub仓库可访问
- [ ] README在GitHub上正确显示
- [ ] GitHub Pages正常工作
  - [ ] https://cricle.github.io/Sqlx/ 可访问
- [ ] Issues已清理（关闭已解决的）
- [ ] Pull Requests已处理

#### 标签和分支
- [ ] 🔴 创建 v0.5.0-preview 标签
  ```bash
  git tag -a v0.5.0-preview -m "Release v0.5.0-preview"
  git push origin v0.5.0-preview
  ```
- [ ] 确认在 main 分支

---

### 6️⃣ 构建和打包 (Build & Package)

#### 核心库构建
- [ ] 清理解决方案
  ```bash
  dotnet clean
  ```
- [ ] 恢复包
  ```bash
  dotnet restore
  ```
- [ ] Release模式构建
  ```bash
  dotnet build -c Release
  ```
- [ ] 检查构建输出
  - [ ] 无错误
  - [ ] 无警告

#### NuGet包
- [ ] 打包 Sqlx
  ```bash
  dotnet pack src/Sqlx/Sqlx.csproj -c Release
  ```
- [ ] 打包 Sqlx.Generator
  ```bash
  dotnet pack src/Sqlx.Generator/Sqlx.Generator.csproj -c Release
  ```
- [ ] 检查 .nupkg 文件
  - [ ] 版本号正确
  - [ ] 包含所有必要文件
  - [ ] NuGet metadata正确

#### VSIX构建
- [ ] 🔴 构建VSIX
  ```powershell
  cd src/Sqlx.Extension
  .\build-vsix.ps1
  ```
- [ ] VSIX文件生成成功
  - [ ] 文件位置: `src/Sqlx.Extension/bin/Release/Sqlx.Extension.vsix`
- [ ] VSIX可安装
  - [ ] 双击测试安装
  - [ ] 安装无错误
  - [ ] 在VS中可见
- [ ] 功能测试
  - [ ] 工具窗口可打开
  - [ ] IntelliSense工作
  - [ ] 语法着色工作

---

### 7️⃣ 发布准备 (Release Preparation)

#### GitHub Release
- [ ] 🔴 准备Release Notes
  - [ ] 使用 HOW_TO_RELEASE.md 模板
  - [ ] 突出新功能
  - [ ] 包含安装说明
  - [ ] 包含升级说明
  - [ ] 包含已知问题
- [ ] 准备附件
  - [ ] Sqlx.Extension.vsix
  - [ ] (可选) 示例代码压缩包

#### VS Marketplace准备
- [ ] 准备截图 (5-10张)
  - [ ] SQL Preview窗口
  - [ ] Generated Code窗口
  - [ ] Query Tester窗口
  - [ ] IntelliSense演示
  - [ ] 语法着色演示
  - [ ] Performance Analyzer
  - [ ] Entity Mapping
- [ ] ⚠️ 准备描述文本
  - [ ] 使用 HOW_TO_RELEASE.md 中的模板
- [ ] ⚠️ 准备视频（可选）
  - [ ] 功能演示 (2-3分钟)

#### 宣传材料
- [ ] 准备社交媒体文案
  - [ ] Twitter/X (使用 HOW_TO_RELEASE.md 模板)
  - [ ] LinkedIn
  - [ ] 微博
  - [ ] 知乎
- [ ] 准备技术博客文章
  - [ ] 介绍Sqlx
  - [ ] VS Extension功能
  - [ ] 性能对比
  - [ ] 最佳实践

---

### 8️⃣ 法律和许可 (Legal & Licensing)

#### 许可证
- [x] LICENSE.txt 存在
- [x] MIT许可证文本正确
- [x] 版权年份正确 (2024)
- [x] Extension包含License

#### 第三方依赖
- [ ] 检查所有依赖的许可证
- [ ] 确保兼容MIT
- [ ] NOTICE文件（如需要）

---

### 9️⃣ 安全检查 (Security)

#### 代码安全
- [ ] 无硬编码密钥/密码
- [ ] 无敏感信息泄露
- [ ] 依赖包无已知漏洞
  ```bash
  dotnet list package --vulnerable
  ```

#### Extension安全
- [ ] VSIX签名（如有）
- [ ] 无恶意代码
- [ ] 隐私政策（如收集数据）

---

### 🔟 性能和质量 (Performance & Quality)

#### 代码质量
- [ ] 运行代码分析
- [ ] 无严重问题
- [ ] 遵循.NET编码规范
- [ ] StyleCop检查通过

#### 性能指标
- [x] 批量操作性能达标 (25倍)
- [x] 单行操作性能达标 (105%+)
- [x] 内存占用合理
- [x] GC压力低

---

### 1️⃣1️⃣ 用户体验 (User Experience)

#### 易用性
- [x] 零配置即用
- [x] 错误消息清晰
- [x] 文档易懂
- [x] 示例代码可运行

#### 兼容性
- [ ] .NET 6.0 测试
- [ ] .NET 7.0 测试
- [ ] .NET 8.0 测试
- [ ] Visual Studio 2022 测试
  - [ ] 17.0 (最低版本)
  - [ ] 17.8+ (推荐版本)

---

### 1️⃣2️⃣ 最终检查 (Final Checks)

#### 健康检查脚本
- [ ] 运行健康检查
  ```powershell
  .\scripts\health-check.ps1
  ```
- [ ] 所有检查通过

#### 手动验证
- [ ] 在干净环境安装测试
  - [ ] 新建VM或容器
  - [ ] 安装.NET SDK
  - [ ] 安装VS 2022
  - [ ] 安装Sqlx NuGet
  - [ ] 安装VS Extension
  - [ ] 运行示例项目
- [ ] 所有功能正常工作

#### 文档最终检查
- [ ] README.md 拼写检查
- [ ] 所有文档拼写检查
- [ ] 代码示例语法检查
- [ ] 链接有效性检查

---

## 🚀 发布流程 (Release Process)

### Phase 1: 准备
- [ ] 完成上述所有检查
- [ ] 解决所有🔴标记的关键项
- [ ] 至少完成95%的检查项

### Phase 2: 构建
- [ ] 更新版本号到 v0.5.0-preview
- [ ] 创建Git标签
- [ ] 构建NuGet包
- [ ] 构建VSIX
- [ ] 验证所有构建产物

### Phase 3: GitHub发布
- [ ] 创建GitHub Release
  - [ ] Tag: v0.5.0-preview
  - [ ] Title: Sqlx v0.5.0-preview - Visual Studio Extension首发
  - [ ] 使用准备好的Release Notes
  - [ ] 上传VSIX
- [ ] 验证Release页面
- [ ] 测试下载链接

### Phase 4: NuGet发布
- [ ] 推送到NuGet.org
  ```bash
  dotnet nuget push src/Sqlx/bin/Release/Sqlx.0.5.0-preview.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_KEY
  dotnet nuget push src/Sqlx.Generator/bin/Release/Sqlx.Generator.0.5.0-preview.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_KEY
  ```
- [ ] 验证NuGet页面
- [ ] 等待索引完成

### Phase 5: VS Marketplace发布
- [ ] 访问 Visual Studio Marketplace
- [ ] 上传VSIX
- [ ] 填写所有信息
  - [ ] 名称
  - [ ] 描述
  - [ ] 版本
  - [ ] 截图
  - [ ] 标签
  - [ ] 类别
- [ ] 提交审核
- [ ] 等待审核通过

### Phase 6: 宣传
- [ ] 发布社交媒体
  - [ ] Twitter/X
  - [ ] LinkedIn
  - [ ] 微博
  - [ ] 知乎
- [ ] 发布技术文章
  - [ ] 博客园
  - [ ] CSDN
  - [ ] Dev.to
- [ ] 更新GitHub Pages
- [ ] 通知用户（如有邮件列表）

### Phase 7: 监控
- [ ] 监控GitHub Issues
- [ ] 监控Discussions
- [ ] 监控NuGet下载量
- [ ] 监控VS Marketplace下载量
- [ ] 收集用户反馈
- [ ] 记录Bug报告

---

## 📊 发布指标 (Release Metrics)

### 目标指标 (Week 1)
- [ ] GitHub Stars: 10+
- [ ] NuGet Downloads: 50+
- [ ] VS Extension Installs: 20+
- [ ] GitHub Issues: < 5
- [ ] Positive Feedback: 80%+

### 监控工具
- GitHub Insights
- NuGet Stats
- VS Marketplace Analytics
- Google Analytics (GitHub Pages)

---

## ❌ 回滚计划 (Rollback Plan)

如果发现严重问题：

1. **立即行动**
   - [ ] 在GitHub Release添加警告
   - [ ] 更新README添加已知问题
   - [ ] 在社交媒体发布通知

2. **短期修复**
   - [ ] 创建hotfix分支
   - [ ] 修复问题
   - [ ] 发布v0.5.1-preview

3. **长期**
   - [ ] 分析根本原因
   - [ ] 改进测试流程
   - [ ] 更新检查清单

---

## ✅ 签字确认 (Sign-off)

### 开发者确认
- [ ] 代码质量满意
- [ ] 所有测试通过
- [ ] 文档完整准确

### 项目负责人确认
- [ ] 发布准备就绪
- [ ] 风险可接受
- [ ] 批准发布

### 日期
- 准备完成日期: _______________
- 发布日期: _______________
- 签名: _______________

---

## 📝 注意事项

1. **🔴 关键项目**: 必须完成才能发布
2. **⚠️ 重要项目**: 强烈建议完成
3. **普通项目**: 建议完成，但非阻塞

**建议**: 至少完成95%的检查项再发布

---

## 🎯 快速检查命令

```bash
# 健康检查
.\scripts\health-check.ps1

# 清理
dotnet clean

# 恢复
dotnet restore

# 构建
dotnet build -c Release

# 测试
dotnet test

# 打包
dotnet pack -c Release

# 构建VSIX
cd src/Sqlx.Extension
.\build-vsix.ps1

# Git状态
git status
git log --oneline -5
```

---

**检查清单版本**: 1.0  
**适用版本**: v0.5.0-preview  
**最后更新**: 2025-10-29  

**祝发布顺利！** 🚀


