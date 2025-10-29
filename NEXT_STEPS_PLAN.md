# 🚀 Next Steps Plan

> **Sqlx项目下一步详细计划**

---

## 📊 当前状态总览

```
┌─────────────────────────────────────────┐
│   项目状态: Production Ready (95%)     │
├─────────────────────────────────────────┤
│   接口代码:      ✅ 完成 (0错误)      │
│   文档体系:      ✅ 完成 (21篇)       │
│   VS Extension:  ✅ 完成 (14窗口)     │
│   源生成器:      ⚠️  待修复 (79错误) │
│   VSIX构建:      ⏳ 待执行            │
│   发布准备:      ⏳ 待开始            │
└─────────────────────────────────────────┘
```

**日期**: 2025-10-29  
**版本**: v0.5.0-preview  
**当前阶段**: 修复与发布准备  

---

## 🎯 核心问题分析

### ⚠️ 关键阻塞问题

**问题**: 源生成器生成的代码存在79个编译错误

**错误类型**:

1. **CS4016 - 异步方法返回类型错误** (~40个)
   ```csharp
   // 错误: 双重Task包装
   public async Task<PagedResult<T>> GetPagedAsync(...)
   {
       // ...
       return await someTask; // 返回Task<PagedResult<T>>而不是PagedResult<T>
   }
   ```

2. **CS1061 - 缺少扩展方法** (~39个)
   ```csharp
   // 错误: 引用不存在的扩展方法
   predicate.ToWhereClause()  // 找不到ToWhereClause
   predicate.GetParameters()  // 找不到GetParameters
   ```

**影响范围**:
- ❌ 项目无法编译
- ❌ 无法运行测试
- ❌ 无法构建NuGet包
- ❌ 无法发布

**优先级**: 🔴 P0 (必须立即解决)

---

## 📋 分阶段执行计划

### 🔴 Phase 1: 源生成器修复 (P0 - 必须)

**目标**: 修复所有79个源生成器错误，使项目可编译

**预计时间**: 2-4小时

#### 1.1 诊断问题 (30分钟)

**任务清单**:
- [ ] 查看完整的编译错误列表
- [ ] 分析错误模式和根本原因
- [ ] 定位源生成器代码位置
- [ ] 了解当前生成逻辑

**关键文件**:
- `src/Sqlx.Generator/CSharpGenerator.cs`
- `src/Sqlx.Generator/MethodGenerationContext.cs`
- `src/Sqlx.Generator/Core/*.cs`

**诊断命令**:
```bash
cd src/Sqlx
dotnet build > build-errors.txt 2>&1
grep "error CS" build-errors.txt | sort | uniq -c
```

#### 1.2 修复CS4016错误 (1-2小时)

**问题**: 生成器为已返回Task的异步方法再包装一层Task

**修复策略**:

**选项A: 检测返回类型** (推荐)
```csharp
// 在CSharpGenerator.cs中
if (method.ReturnType.IsGenericType && 
    method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
{
    // 已经是Task<T>，直接return await
    sb.AppendLine("return await ExecuteAsync(...);");
}
else
{
    // 同步方法，包装为Task
    sb.AppendLine("return Task.FromResult(Execute(...));");
}
```

**选项B: 统一使用await** (更简单)
```csharp
// 所有异步方法统一使用
public async Task<T> MethodAsync()
{
    // 直接await，不再包装
    return await ExecuteScalarAsync<T>(...);
}
```

**修复位置**:
- `CSharpGenerator.cs` - 异步方法生成逻辑
- 检查所有生成`return`语句的地方

**验证**:
```bash
dotnet build | grep "CS4016" | wc -l
# 应该输出: 0
```

#### 1.3 修复CS1061错误 (1-2小时)

**问题**: 生成代码引用不存在的`ToWhereClause`和`GetParameters`扩展方法

**修复策略**:

**选项A: 添加扩展方法** (推荐)
```csharp
// 在src/Sqlx/ExpressionExtensions.cs (新建)
namespace Sqlx
{
    public static class ExpressionExtensions
    {
        public static string ToWhereClause<T>(
            this Expression<Func<T, bool>> predicate,
            ISqlDialect dialect)
        {
            var translator = new ExpressionToSql(dialect);
            return translator.Translate(predicate);
        }

        public static Dictionary<string, object> GetParameters<T>(
            this Expression<Func<T, bool>> predicate)
        {
            var extractor = new ParameterExtractor();
            return extractor.Extract(predicate);
        }
    }
}
```

**选项B: 修改生成逻辑** (备选)
```csharp
// 在CSharpGenerator.cs中
// 不生成扩展方法调用，直接生成内联代码
sb.AppendLine("var translator = new ExpressionToSql(dialect);");
sb.AppendLine("var whereClause = translator.Translate(predicate);");
```

**选项C: 使用现有API** (最简单)
```csharp
// 检查ExpressionToSql.cs是否已有公共API
// 直接使用现有方法而不是扩展方法
var expressionToSql = new ExpressionToSql();
var whereClause = expressionToSql.Translate(predicate);
```

**修复步骤**:
1. 检查`src/Sqlx/ExpressionToSql.cs`现有API
2. 决定使用哪个选项
3. 实现扩展方法或修改生成器
4. 更新生成器引用这些方法

**验证**:
```bash
dotnet build | grep "CS1061" | wc -l
# 应该输出: 0
```

#### 1.4 完整测试 (30分钟)

**任务清单**:
- [ ] 清理生成的文件: `dotnet clean`
- [ ] 重新生成: `dotnet build`
- [ ] 检查错误数: 应该为0
- [ ] 运行单元测试: `dotnet test`
- [ ] 检查测试通过率

**验证命令**:
```bash
# 完整构建
dotnet clean
dotnet build -c Release

# 运行测试
cd tests/Sqlx.Tests
dotnet test --verbosity normal

# 检查所有项目
cd ../..
dotnet build Sqlx.sln -c Release
```

**成功标准**:
```
✅ 0个编译错误
✅ 0个编译警告 (或仅有合理警告)
✅ 所有测试通过
✅ NuGet包可以生成
```

---

### 🟡 Phase 2: Git同步 (P1)

**目标**: 推送所有本地提交到GitHub

**预计时间**: 5分钟

#### 2.1 推送待提交内容

**当前状态**:
- 待推送提交: 2次
  1. `ae49fca` - docs: Add session 2025-10-29 final summary
  2. `ae908b0` - docs: Remove duplicate and redundant documentation files
  3. `4ac2c67` - docs: Add documentation cleanup complete report

**任务**:
```bash
# 检查待推送提交
git log origin/main..HEAD

# 推送
git push origin main

# 验证
git status
# 应该显示: Your branch is up to date with 'origin/main'
```

**备选方案** (如果网络持续有问题):
```bash
# 使用SSH而不是HTTPS
git remote set-url origin git@github.com:Cricle/Sqlx.git
git push origin main
```

---

### 🟢 Phase 3: VSIX构建与测试 (P1)

**目标**: 成功构建VSIX并进行本地测试

**预计时间**: 1-2小时

#### 3.1 VSIX构建 (30分钟)

**前置条件**:
- ✅ 源生成器错误已修复
- ✅ 所有代码已推送

**构建步骤**:
```powershell
# 使用已有的构建脚本
cd src/Sqlx.Extension
.\build-vsix.ps1

# 或者手动构建
msbuild Sqlx.Extension.csproj /t:Rebuild /p:Configuration=Release
```

**输出文件**:
- `src/Sqlx.Extension/bin/Release/Sqlx.Extension.vsix`

**验证**:
```powershell
# 检查VSIX文件是否存在
Test-Path "bin/Release/Sqlx.Extension.vsix"

# 检查文件大小 (应该 > 100KB)
(Get-Item "bin/Release/Sqlx.Extension.vsix").Length / 1KB
```

#### 3.2 本地安装测试 (30分钟)

**测试步骤**:

1. **卸载旧版本** (如果有)
   ```powershell
   # 在Visual Studio中: Extensions > Manage Extensions
   # 搜索 "Sqlx" > 卸载
   ```

2. **安装新版本**
   ```powershell
   # 双击VSIX文件安装
   # 或者
   & "bin/Release/Sqlx.Extension.vsix"
   ```

3. **重启Visual Studio**

4. **功能测试**
   - [ ] 打开示例项目 `samples/FullFeatureDemo`
   - [ ] 检查语法高亮是否工作
   - [ ] 测试代码片段 (输入`sqltemplate` + Tab)
   - [ ] 打开工具窗口: View > Other Windows > Sqlx Preview
   - [ ] 测试快速操作 (右键菜单)
   - [ ] 检查IntelliSense自动完成

**测试清单**:
```
✅ 语法高亮
  - SQL关键字高亮
  - 占位符高亮
  - 参数高亮

✅ 代码片段
  - SqlTemplate方法
  - Repository类
  - CRUD方法

✅ 工具窗口 (至少测试3个)
  - SQL Preview Window
  - Generated Code Window
  - Query Tester

✅ 快速操作
  - Generate Repository
  - Add CRUD Methods

✅ IntelliSense
  - SQL关键字
  - 占位符
  - 参数名
```

#### 3.3 性能测试 (可选，30分钟)

**测试场景**:
- 打开大型项目 (1000+文件)
- 编辑包含多个SqlTemplate的文件
- 监控CPU和内存使用

**性能指标**:
- 启动时间: < 2秒
- 语法高亮延迟: < 100ms
- 内存占用: < 50MB

---

### 🔵 Phase 4: 发布准备 (P2)

**目标**: 准备所有发布材料

**预计时间**: 2-3小时

#### 4.1 版本号更新 (15分钟)

**文件清单**:
```bash
# 检查需要更新版本号的文件
grep -r "0.5.0" --include="*.csproj" --include="*.props"
```

**需要更新的文件**:
- `Directory.Build.props`
- `src/Sqlx/Sqlx.csproj`
- `src/Sqlx.Generator/Sqlx.Generator.csproj`
- `src/Sqlx.Extension/source.extension.vsixmanifest`
- `VERSION`
- `CHANGELOG.md`

**验证一致性**:
```bash
# 所有版本号应该一致
grep -h "<Version>" *.props src/*/*.csproj | sort | uniq
```

#### 4.2 CHANGELOG更新 (30分钟)

**任务**:
- [ ] 更新`CHANGELOG.md`
- [ ] 添加v0.5.0-preview发布说明
- [ ] 列出所有新特性
- [ ] 列出所有修复
- [ ] 添加升级指南

**模板**:
```markdown
## [0.5.0-preview] - 2025-10-29

### 🎉 新特性

#### Visual Studio Extension
- 14个专业工具窗口
- 实时SQL语法高亮
- 智能代码片段
- 快速操作和诊断
- 44+项IntelliSense

#### 增强的Repository接口
- 10个Repository接口
- 50+预定义方法
- 支持表达式转SQL
- 批量操作支持
- 分页查询支持

### 🐛 修复
- 修复接口层编译错误 (23处)
- 修复源生成器错误 (79处)
- 优化接口继承关系

### 📚 文档
- 21篇核心文档
- 完整教程 (10课)
- FAQ (35+问题)
- 故障排除指南

### ⚠️ 已知限制
- 源生成器暂不支持泛型方法
- VSIX仅支持VS 2019/2022

### 🔄 迁移指南
详见 [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)
```

#### 4.3 截图和演示 (1小时)

**需要的截图** (5-10张):
1. SQL语法高亮示例
2. 代码片段使用
3. SQL Preview窗口
4. Generated Code窗口
5. Query Tester窗口
6. IntelliSense自动完成
7. 快速操作菜单
8. Repository Explorer

**工具**:
- Windows截图: Win + Shift + S
- Gif录制: ScreenToGif
- 视频录制: OBS Studio (可选)

**保存位置**:
```
docs/images/
  - syntax-highlighting.png
  - code-snippets.gif
  - sql-preview.png
  - query-tester.png
  - intellisense.png
  - quick-actions.png
```

#### 4.4 README更新 (30分钟)

**更新内容**:
- [ ] 添加VS Extension徽章
- [ ] 更新功能列表
- [ ] 添加截图
- [ ] 更新安装说明
- [ ] 添加VS Extension链接

**徽章示例**:
```markdown
![Version](https://img.shields.io/badge/version-0.5.0--preview-blue)
![VS Extension](https://img.shields.io/badge/VS%20Extension-Available-green)
![License](https://img.shields.io/badge/license-MIT-blue)
```

---

### 🟣 Phase 5: GitHub Release (P2)

**目标**: 创建GitHub Release并上传VSIX

**预计时间**: 30分钟

#### 5.1 创建Release

**步骤**:
1. 访问 https://github.com/Cricle/Sqlx/releases/new
2. 创建新tag: `v0.5.0-preview`
3. 填写Release标题: `v0.5.0-preview - Visual Studio Extension`
4. 复制CHANGELOG内容到描述

**Release描述模板**:
```markdown
# 🎉 Sqlx v0.5.0-preview

> 首个包含Visual Studio Extension的预览版本！

## ✨ 主要特性

### 🔧 Visual Studio Extension
- ✅ 14个专业工具窗口
- ✅ 实时SQL语法高亮
- ✅ 44+项IntelliSense
- ✅ 智能代码片段
- ✅ 快速操作和诊断

### 📊 增强的Repository接口
- ✅ 10个Repository接口
- ✅ 50+预定义方法
- ✅ 表达式转SQL支持

## 📥 下载

### NuGet Package
```bash
dotnet add package Sqlx --version 0.5.0-preview
```

### Visual Studio Extension
- [下载 VSIX](./Sqlx.Extension.vsix)
- 支持 VS 2019/2022

## 📚 文档
- [完整文档](https://cricle.github.io/Sqlx/)
- [快速开始](./docs/QUICK_START_GUIDE.md)
- [教程](./TUTORIAL.md)
- [FAQ](./FAQ.md)

## ⚠️ 注意事项
这是预览版本，可能包含bugs。欢迎反馈！

完整变更日志: [CHANGELOG.md](./CHANGELOG.md)
```

#### 5.2 上传文件

**需要上传的文件**:
- `Sqlx.Extension.vsix` - VS扩展安装包
- `Sqlx.{version}.nupkg` - NuGet包 (可选)
- `README.md` - 项目说明
- `CHANGELOG.md` - 变更日志

**命令**:
```bash
# 打包NuGet (如果需要)
cd src/Sqlx
dotnet pack -c Release -o ../../artifacts

# 文件位置
ls -lh artifacts/
ls -lh src/Sqlx.Extension/bin/Release/
```

---

### 🟠 Phase 6: VS Marketplace发布 (P3 - 可选)

**目标**: 将扩展发布到Visual Studio Marketplace

**预计时间**: 1-2小时

#### 6.1 准备发布账号

**步骤**:
1. 访问 https://marketplace.visualstudio.com/manage
2. 创建Publisher (如果没有)
3. 获取Personal Access Token

#### 6.2 准备发布材料

**必需材料**:
- [ ] VSIX文件
- [ ] 图标 (128x128)
- [ ] 预览图片 (5-10张)
- [ ] 详细描述
- [ ] 许可协议
- [ ] 隐私政策 (可选)

#### 6.3 发布流程

**方法A: 手动发布** (推荐首次)
1. 登录 Marketplace
2. 点击 "New Extension"
3. 上传VSIX
4. 填写详细信息
5. 上传截图
6. 提交审核

**方法B: 使用CLI**
```bash
# 安装发布工具
npm install -g vsce

# 打包
vsce package

# 发布
vsce publish -p <personal-access-token>
```

**审核时间**: 通常1-3个工作日

---

## 📊 优先级矩阵

```
┌─────────────────────────────────────────────────────┐
│                   重要性                            │
│              高        │        低                  │
│  ────────────────────────────────────────────────  │
│  紧  │  P0 Phase 1   │  P2 Phase 4              │  │
│  急  │  源生成器修复  │  发布准备                │  │
│  性  │               │                          │  │
│  ────────────────────────────────────────────────  │
│  低  │  P1 Phase 2-3 │  P3 Phase 6              │  │
│     │  Git同步+VSIX  │  Marketplace发布         │  │
│     │  构建          │                          │  │
└─────────────────────────────────────────────────────┘
```

### 优先级说明

**P0 - 必须立即执行**
- Phase 1: 源生成器修复
- 阻塞项: 无法编译、无法测试、无法发布
- 时间: 2-4小时

**P1 - 发布前必须完成**
- Phase 2: Git同步
- Phase 3: VSIX构建与测试
- 时间: 2-3小时

**P2 - 发布阶段**
- Phase 4: 发布准备
- Phase 5: GitHub Release
- 时间: 3-4小时

**P3 - 可选/延后**
- Phase 6: VS Marketplace发布
- 可以在后续版本再发布
- 时间: 1-2小时

---

## 📅 时间线建议

### 选项A: 快速发布 (推荐)

**Day 1 (今天)**
- ✅ 已完成: 接口修复 + 文档清理
- ⏳ Phase 1: 源生成器修复 (2-4小时)
- ⏳ Phase 2: Git同步 (5分钟)

**Day 2 (明天)**
- Phase 3: VSIX构建与测试 (1-2小时)
- Phase 4: 发布准备 (2-3小时)
- Phase 5: GitHub Release (30分钟)

**总计**: 1-2天

### 选项B: 完整发布

**Week 1**
- Day 1-2: Phase 1-3 (修复+构建+测试)
- Day 3-4: Phase 4 (准备材料+截图)
- Day 5: Phase 5 (GitHub Release)

**Week 2**
- Day 1-2: Phase 6 (Marketplace准备)
- Day 3: 提交审核
- Day 4-7: 等待审核+营销

**总计**: 1-2周

---

## ✅ 检查清单

### Phase 1 - 源生成器修复
- [ ] 诊断所有79个错误
- [ ] 修复CS4016错误 (~40个)
- [ ] 修复CS1061错误 (~39个)
- [ ] 完整构建无错误
- [ ] 所有测试通过

### Phase 2 - Git同步
- [ ] 推送所有待提交
- [ ] 验证远程同步

### Phase 3 - VSIX构建
- [ ] 成功构建VSIX
- [ ] 本地安装测试
- [ ] 功能测试通过
- [ ] 性能测试通过 (可选)

### Phase 4 - 发布准备
- [ ] 版本号统一
- [ ] CHANGELOG更新
- [ ] 截图准备 (5-10张)
- [ ] README更新
- [ ] 文档检查

### Phase 5 - GitHub Release
- [ ] 创建Release tag
- [ ] 上传VSIX
- [ ] 发布说明完整
- [ ] 链接验证

### Phase 6 - Marketplace (可选)
- [ ] Publisher账号准备
- [ ] 发布材料齐全
- [ ] 提交审核

---

## 🚨 风险与应对

### 风险1: 源生成器修复复杂

**风险等级**: 🔴 高

**可能问题**:
- 修复时间超预期
- 修复后引入新问题
- 需要重构大量代码

**应对策略**:
1. **Plan A**: 修复扩展方法 (简单快速)
   - 添加`ToWhereClause`和`GetParameters`扩展方法
   - 修复异步返回类型

2. **Plan B**: 简化生成逻辑 (如果A失败)
   - 暂时移除有问题的方法
   - 只生成基础CRUD

3. **Plan C**: 回退版本 (最后手段)
   - 回退到v0.4稳定版
   - 单独发布VS Extension

### 风险2: VSIX构建失败

**风险等级**: 🟡 中

**可能问题**:
- MSBuild找不到
- 依赖包版本冲突
- License文件问题

**应对策略**:
- 使用VS Developer Command Prompt
- 参考`src/Sqlx.Extension/BUILD.md`
- 使用已测试的`build-vsix.ps1`脚本

### 风险3: 网络问题

**风险等级**: 🟢 低

**可能问题**:
- Git push失败
- NuGet push失败

**应对策略**:
- 使用SSH代替HTTPS
- 稍后重试
- 使用VPN

---

## 📈 成功指标

### 技术指标

```
✅ 编译成功率: 100%
✅ 测试通过率: 100%
✅ 代码覆盖率: >80%
✅ 性能基准: 达标
✅ VSIX安装率: 100%
```

### 发布指标

```
✅ GitHub Release: 已创建
✅ NuGet下载: >10/天 (第一周)
✅ VSIX下载: >5/天 (第一周)
✅ GitHub Stars: +10 (第一月)
✅ Issues响应: <24小时
```

### 质量指标

```
✅ 文档完整性: 100%
✅ 代码质量: A级
✅ 用户反馈: 积极
✅ Bug数量: <5个严重bug
```

---

## 🎯 下一步行动

### 立即执行 (接下来1小时)

1. **启动Phase 1** - 源生成器诊断
   ```bash
   cd src/Sqlx
   dotnet build > ../../build-errors.txt 2>&1
   cat ../../build-errors.txt | grep "error CS"
   ```

2. **分析错误模式**
   - 统计CS4016和CS1061数量
   - 找出错误集中的文件
   - 定位生成器相关代码

3. **准备修复方案**
   - 阅读`ExpressionToSql.cs`
   - 查看`CSharpGenerator.cs`
   - 决定修复策略

### 今天完成目标

- ✅ 修复至少50%的源生成器错误
- ✅ 制定详细修复方案
- ✅ 推送Git提交 (如果网络恢复)

### 明天目标

- ✅ 完成所有源生成器修复
- ✅ 构建VSIX
- ✅ 开始发布准备

---

## 📚 参考资料

### 内部文档
- [HOW_TO_RELEASE.md](HOW_TO_RELEASE.md) - 发布指南
- [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) - 发布检查清单
- [COMPILATION_FIX_COMPLETE.md](COMPILATION_FIX_COMPLETE.md) - 编译修复经验
- [src/Sqlx.Extension/BUILD.md](src/Sqlx.Extension/BUILD.md) - VSIX构建说明

### 外部资源
- [Roslyn Source Generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [VSIX Development Guide](https://docs.microsoft.com/en-us/visualstudio/extensibility/)
- [VS Marketplace Publishing](https://docs.microsoft.com/en-us/azure/devops/extend/publish/overview)

---

## 💡 提示和技巧

### 源生成器调试

```csharp
// 在CSharpGenerator.cs中添加调试输出
#if DEBUG
System.Diagnostics.Debugger.Launch(); // 附加调试器
Console.WriteLine($"Generating method: {method.Name}");
#endif
```

### VSIX快速测试

```powershell
# 使用实验实例
devenv /RootSuffix Exp
```

### 性能分析

```bash
# 使用BenchmarkDotNet
cd tests/Sqlx.Benchmarks
dotnet run -c Release
```

---

**计划制定时间**: 2025-10-29  
**计划制定者**: AI Assistant  
**计划状态**: ✅ 已完成  
**预计总工时**: 8-15小时  
**预计完成日期**: 2025-10-30 或 2025-10-31  

**🚀 准备就绪，开始执行！**


