# 🚀 CI/CD Workflows

## 工作流说明

### 📋 ci-cd.yml - 统一的 CI/CD 流程

这是唯一的工作流文件，包含完整的 CI/CD 流程：

#### 🧪 测试阶段 (test job)
**触发条件：**
- Push 到 `main` 或 `develop` 分支
- Pull Request 到 `main` 分支
- 创建 `v*` 标签

**执行步骤：**
1. 📥 检出代码
2. 🔧 设置 .NET 8.0
3. 📦 缓存和还原 NuGet 包
4. 🏗️ 构建项目 (Release 配置)
5. 🧪 运行所有测试
6. 🚀 运行综合示例验证
7. 📊 上传代码覆盖率到 Codecov
8. 📦 打包 NuGet 包 (仅 Push 事件)
9. 📤 上传构建产物

#### 🚀 发布阶段 (publish job)
**触发条件：**
- ⚠️ **仅在创建 `v*` 标签时触发** (如 `v1.0.0`, `v2.1.3-beta`)

**执行步骤：**
1. 📥 下载构建产物
2. 🏷️ 从标签提取版本号
3. 🚀 发布到 NuGet.org
4. 🎉 创建 GitHub Release

## 📋 使用指南

### 日常开发
```bash
# 推送到 develop 分支 - 只会运行测试
git push origin develop

# 创建 PR 到 main - 只会运行测试
git push origin feature/new-feature
```

### 发布版本
```bash
# 1. 确保代码在 main 分支
git checkout main
git pull origin main

# 2. 创建并推送版本标签
git tag v1.2.3
git push origin v1.2.3

# 3. CI/CD 会自动：
#    - 运行测试
#    - 构建包
#    - 发布到 NuGet
#    - 创建 GitHub Release
```

### 版本标签格式
- ✅ `v1.0.0` - 正式版本
- ✅ `v1.0.0-beta.1` - Beta 版本
- ✅ `v1.0.0-alpha.1` - Alpha 版本
- ✅ `v1.0.0-rc.1` - Release Candidate

### 环境要求

#### Secrets 配置
在 GitHub 仓库设置中配置以下 Secrets：

- `NUGET_API_KEY` - NuGet.org API 密钥
- `CODECOV_TOKEN` - Codecov 上传令牌 (可选)

#### 环境保护
`publish` job 使用 `production` 环境，建议配置：
- 需要审批
- 限制部署分支为 `main`
- 添加环境保护规则

## 🔧 优化特性

### 最新改进
1. **简化结构** - 移除重复的构建工作流
2. **智能触发** - 只有 v* 标签才会发布到 NuGet
3. **缓存优化** - NuGet 包缓存提高构建速度
4. **示例验证** - 自动运行综合示例确保功能正常
5. **性能优化** - 发布说明包含性能改进信息
6. **版本管理** - 从标签自动提取版本号

### 性能优化
- 📦 NuGet 包缓存
- 🚀 单一工作流减少复杂性
- ⏱️ 构建产物保留 7 天
- 🎯 只在必要时执行发布步骤
- 🧪 综合示例验证功能完整性

## 🚀 性能监控

工作流会自动运行性能测试并在发布说明中包含：
- ⚡ 装箱问题修复状态
- 🗑️ 垃圾回收压力改善
- 📊 性能基准测试结果