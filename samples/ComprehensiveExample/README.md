# Sqlx 综合示例

这个示例演示了 Sqlx 库的主要功能和使用方法。

## 功能演示

### 基本 CRUD 操作
- ✅ 查询单个实体
- ✅ 查询所有实体  
- ✅ 创建新实体
- ✅ 更新实体
- ✅ 删除实体

### 自定义 SQL 查询
- ✅ 参数化查询
- ✅ 模糊搜索
- ✅ 聚合查询

### 高级功能
- ✅ 自动 SQL 生成
- ✅ 类型安全的数据库操作
- ✅ 高性能零反射执行

## 运行示例

```bash
cd samples/ComprehensiveExample
dotnet run
```

## 项目结构

```
ComprehensiveExample/
├── Models/                 # 数据模型
│   └── User.cs            # 用户和部门模型
├── Services/              # 服务层
│   ├── IUserService.cs    # 用户服务接口
│   └── UserService.cs     # 用户服务实现
├── Data/                  # 数据访问层
│   └── DatabaseSetup.cs   # 数据库初始化
└── Program.cs             # 主程序
```

## 关键特性

1. **Repository 模式自动生成**: 使用 `[RepositoryFor]` 属性自动生成实现
2. **智能 SQL 推断**: 根据方法名自动生成 SQL 语句
3. **自定义 SQL**: 使用 `[Sqlx]` 属性指定自定义 SQL
4. **类型安全**: 编译时验证，运行时高性能