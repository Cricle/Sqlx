# 🚀 Sqlx 全面功能演示

这是一个全面的 Sqlx 功能演示项目，展示了所有核心特性和最新功能。

## ✨ 功能演示

### 🎯 核心功能
- **Repository 模式自动生成** - 使用 `[RepositoryFor]` 特性自动生成实现
- **智能 SQL 推断** - 基于方法名自动推断 SQL 操作类型
- **类型安全的数据库操作** - 编译时类型检查，运行时零反射
- **高性能执行** - 优化的代码生成，最小化性能开销

### 📋 CRUD 操作
- **CREATE** - 自动推断 INSERT 操作
- **READ** - 自动推断 SELECT 操作，支持复杂查询
- **UPDATE** - 自动推断 UPDATE 操作
- **DELETE** - 自动推断 DELETE 操作

### 🏗️ 现代 C# 语法支持
- **Record 类型 (C# 9+)** - 完全支持 Record 的所有特性
- **Primary Constructor (C# 12+)** - 支持主构造函数类
- **Primary Constructor Record** - 支持组合语法

### 🔍 高级查询功能
- **自定义 SQL** - 使用 `[Sqlx]` 特性定义自定义查询
- **关联查询** - 支持 JOIN 操作和复杂关联
- **聚合查询** - 支持 COUNT、SUM 等聚合函数
- **标量查询** - 返回单个值的查询

### 📊 批量操作
- **批量插入** - 高效的批量数据插入
- **批量更新** - 批量更新多条记录
- **批量删除** - 批量删除操作

### 🎨 Expression to SQL
- **Lambda 表达式** - 将 C# Lambda 表达式转换为 SQL
- **类型安全过滤** - 使用强类型表达式进行数据过滤
- **复杂查询构建** - 动态构建复杂查询条件

## 🏗️ 项目结构

```
ComprehensiveExample/
├── Program.cs              # 主程序，演示所有功能
├── Models/
│   └── User.cs            # 实体模型（传统类、Record、Primary Constructor）
├── Services/
│   ├── IUserService.cs    # 服务接口定义
│   └── UserService.cs     # 服务实现（自动生成）
├── Data/
│   └── DatabaseSetup.cs   # 数据库设置和初始化
└── README.md              # 项目说明
```

## 🚀 运行示例

1. 确保安装了 .NET 8.0 或更高版本
2. 进入项目目录: `cd samples/ComprehensiveExample`
3. 运行项目: `dotnet run`

## 📖 演示内容

### 1. 基础 CRUD 操作
演示使用 Sqlx 进行基本的数据库操作，包括创建、读取、更新和删除用户记录。

### 2. 高级功能
展示自定义 SQL 查询、标量查询、搜索功能等高级特性。

### 3. 部门管理
演示多表操作和业务逻辑封装。

### 4. 关联查询
展示用户和部门之间的关联查询，包括统计查询。

### 5. 现代 C# 语法支持
演示 Record 类型和 Primary Constructor 的完美支持。

### 6. 批量操作
展示高效的批量数据处理能力。

### 7. Expression to SQL
演示使用 Lambda 表达式进行类型安全的查询构建。

## 🎯 主要特性演示

| 特性 | 示例类型 | 说明 |
|------|----------|------|
| 传统类 | `User`, `Department` | 标准的 C# 类，展示基础功能 |
| Record 类型 | `Product` | C# 9+ Record，展示现代语法支持 |
| Primary Constructor | `Order` | C# 12+ 主构造函数，展示最新语法 |
| 组合语法 | `OrderItem` | Primary Constructor + Record |
| 自定义 SQL | 各种复杂查询 | 展示灵活的 SQL 定制能力 |
| 批量操作 | 用户批量创建 | 展示高性能批量处理 |
| Expression API | 动态查询 | 展示类型安全的查询构建 |

## 💡 技术亮点

- **零配置** - 无需复杂的配置文件或映射设置
- **编译时生成** - 在编译时生成高性能代码，运行时无反射
- **类型安全** - 完全的编译时类型检查
- **SQL 注入防护** - 自动参数化查询，防止 SQL 注入
- **性能优化** - 最小化内存分配和 CPU 开销
- **现代语法** - 完全支持最新的 C# 语法特性

## 🔧 技术要求

- .NET 8.0 或更高版本
- C# 12.0 语言特性支持
- SQLite (示例使用内存数据库)

这个示例展示了 Sqlx 作为现代 .NET 应用程序数据访问层的强大能力，从基础的 CRUD 操作到高级的查询功能，从传统的 C# 语法到最新的语言特性，都能得到完美支持。