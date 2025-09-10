# Sqlx 版本发布检查清单

本文档提供了 Sqlx 项目版本发布的完整检查清单，确保每次发布都符合质量标准。

## 📋 发布前检查清单

### 🔍 代码质量检查

- [ ] **所有测试通过**
  ```bash
  # 运行完整测试套件
  dotnet test tests/Sqlx.Tests --filter "TestCategory!=Integration"
  dotnet test tests/Sqlx.IntegrationTests
  dotnet test tests/Sqlx.PerformanceTests
  dotnet test samples/RepositoryExample.Tests
  ```

- [ ] **构建成功**
  ```bash
  # 构建所有目标框架
  dotnet build --configuration Release
  ```

- [ ] **代码分析无警告**
  ```bash
  # 检查编译器警告
  dotnet build --configuration Release --verbosity normal
  ```

- [ ] **性能基准测试正常**
  ```bash
  # 运行性能基准测试
  dotnet run --project samples/PerformanceBenchmark --configuration Release
  ```

### 📚 文档更新

- [ ] **更新 CHANGELOG.md**
  - 添加新版本条目
  - 列出新功能、改进和修复
  - 标注破坏性更改

- [ ] **更新版本号**
  - [ ] `src/Sqlx/Sqlx.csproj` 中的 `<Version>`
  - [ ] `src/Sqlx/Sqlx.csproj` 中的 `<AssemblyVersion>`
  - [ ] `src/Sqlx/Sqlx.csproj` 中的 `<FileVersion>`

- [ ] **检查 README.md**
  - 确保示例代码正确
  - 更新版本徽章
  - 验证安装说明

- [ ] **API 文档更新**
  - 检查 `docs/api/` 目录
  - 确保新 API 有文档
  - 验证示例代码

### 🧪 功能验证

- [ ] **核心功能验证**
  - [ ] Repository 模式代码生成
  - [ ] SQL 方言支持 (SQL Server, MySQL, PostgreSQL, Oracle, DB2, SQLite)
  - [ ] 批量操作功能
  - [ ] 性能监控功能
  - [ ] 缓存机制

- [ ] **兼容性测试**
  - [ ] .NET 6.0 兼容性
  - [ ] .NET 8.0 兼容性
  - [ ] NativeAOT 支持验证

- [ ] **示例项目验证**
  ```bash
  # 验证示例项目可以正常运行
  dotnet run --project samples/CompilationTests
  dotnet run --project samples/ComprehensiveDemo
  dotnet run --project samples/RepositoryExample
  ```

### 🔒 安全检查

- [ ] **依赖项安全扫描**
  ```bash
  # 检查已知漏洞
  dotnet list package --vulnerable
  ```

- [ ] **代码安全审查**
  - 检查 SQL 注入防护
  - 验证参数化查询
  - 确保输入验证

### 📦 打包准备

- [ ] **NuGet 包配置检查**
  - [ ] 包元数据完整 (作者、描述、标签等)
  - [ ] License 文件包含
  - [ ] README 文件包含
  - [ ] 图标和项目 URL 正确

- [ ] **包内容验证**
  ```bash
  # 创建 NuGet 包
  dotnet pack src/Sqlx/Sqlx.csproj --configuration Release
  
  # 检查包内容
  # 使用 NuGet Package Explorer 或类似工具
  ```

## 🚀 发布流程

### 1. 准备发布

- [ ] **创建发布分支**
  ```bash
  git checkout -b release/v{version}
  ```

- [ ] **最终测试**
  ```bash
  # 运行维护脚本进行完整测试
  .\scripts\project-maintenance.ps1 -Action full-test
  ```

- [ ] **提交更改**
  ```bash
  git add .
  git commit -m "chore: prepare release v{version}"
  ```

### 2. 创建标签

- [ ] **创建版本标签**
  ```bash
  git tag -a v{version} -m "Release version {version}"
  ```

- [ ] **推送标签**
  ```bash
  git push origin v{version}
  ```

### 3. 构建发布包

- [ ] **构建 Release 版本**
  ```bash
  dotnet clean
  dotnet restore
  dotnet build --configuration Release
  ```

- [ ] **创建 NuGet 包**
  ```bash
  dotnet pack src/Sqlx/Sqlx.csproj --configuration Release --output ./nupkg
  ```

- [ ] **验证包内容**
  - 检查包大小合理
  - 验证依赖项正确
  - 确认文件完整

### 4. 发布到 NuGet

- [ ] **测试包安装**
  ```bash
  # 在测试项目中安装本地包
  dotnet add package Sqlx --source ./nupkg --version {version}
  ```

- [ ] **发布到 NuGet.org**
  ```bash
  # 使用发布脚本
  .\tools\push-nuget.ps1 -Version {version}
  
  # 或手动发布
  dotnet nuget push ./nupkg/Sqlx.{version}.nupkg --api-key {api-key} --source https://api.nuget.org/v3/index.json
  ```

### 5. GitHub 发布

- [ ] **创建 GitHub Release**
  - 使用标签创建发布
  - 添加发布说明
  - 附加 NuGet 包文件

- [ ] **更新发布说明**
  - 突出显示主要功能
  - 列出破坏性更改
  - 提供升级指南

## 📊 发布后验证

### 即时验证

- [ ] **NuGet.org 可用性**
  - 验证包在 NuGet.org 上可见
  - 测试包下载和安装

- [ ] **文档站点更新**
  - 检查 API 文档更新
  - 验证示例代码正确

### 24小时内验证

- [ ] **下载统计**
  - 监控初始下载量
  - 检查是否有异常

- [ ] **问题反馈**
  - 监控 GitHub Issues
  - 关注社区反馈

- [ ] **CI/CD 状态**
  - 检查持续集成状态
  - 验证自动化测试

## 🔄 版本类型指南

### 主版本 (Major) - X.0.0

**触发条件：**
- 破坏性 API 更改
- 重大架构调整
- 不兼容的功能更改

**额外检查：**
- [ ] 迁移指南准备
- [ ] 向后兼容性文档
- [ ] 社区通知计划

### 次版本 (Minor) - X.Y.0

**触发条件：**
- 新功能添加
- 性能改进
- 向后兼容的 API 扩展

**额外检查：**
- [ ] 功能文档完整
- [ ] 示例代码更新
- [ ] 性能基准对比

### 修订版 (Patch) - X.Y.Z

**触发条件：**
- Bug 修复
- 安全更新
- 小的改进

**额外检查：**
- [ ] 修复验证测试
- [ ] 回归测试通过
- [ ] 安全扫描 (如适用)

## 🚨 紧急发布流程

对于紧急安全修复或关键 Bug 修复：

1. **跳过非必要步骤**
   - 可以跳过某些文档更新
   - 专注于核心功能测试

2. **加速发布**
   - 直接从 hotfix 分支发布
   - 简化发布说明

3. **后续完善**
   - 发布后补充文档
   - 进行完整的回归测试

## 📞 发布支持

如果在发布过程中遇到问题：

1. **检查历史发布**
   - 查看之前的发布标签
   - 对比发布流程

2. **回滚准备**
   - 准备回滚计划
   - 保留上一个稳定版本

3. **团队协作**
   - 通知相关团队成员
   - 记录问题和解决方案

---

**注意：** 此检查清单应该根据项目的具体需求进行调整。建议在每次发布后回顾和改进此流程。
