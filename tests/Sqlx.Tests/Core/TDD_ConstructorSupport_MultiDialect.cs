// -----------------------------------------------------------------------
// <copyright file="TDD_ConstructorSupport_MultiDialect.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core;

// ==================== 多方言测试实体 ====================

public class DialectTestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ==================== SQLite 方言测试 ====================

public partial interface ISQLiteConstructorRepo
{
    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE id = @id")]
    Task<DialectTestUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("INSERT INTO dialect_users (name, age, email, created_at) VALUES (@name, @age, @email, @createdAt)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age, string? email, DateTime createdAt, CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users SET age = @age WHERE id = @id")]
    Task<int> UpdateAgeAsync(long id, int age, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM dialect_users WHERE id = @id")]
    Task<int> DeleteAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users")]
    Task<int> CountAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISQLiteConstructorRepo))]
public partial class SQLiteConstructorRepo(DbConnection connection) : ISQLiteConstructorRepo
{
}

// ==================== PostgreSQL 方言测试 ====================

public partial interface IPostgreSQLConstructorRepo
{
    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE id = @id")]
    Task<DialectTestUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("INSERT INTO dialect_users (name, age, email, created_at) VALUES (@name, @age, @email, @createdAt) RETURNING id")]
    Task<long> InsertAsync(string name, int age, string? email, DateTime createdAt, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE email ILIKE @pattern")]
    Task<List<DialectTestUser>> SearchByEmailAsync(string pattern, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE created_at >= @date AT TIME ZONE 'UTC'")]
    Task<List<DialectTestUser>> GetUsersAfterDateAsync(DateTime date, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IPostgreSQLConstructorRepo))]
public partial class PostgreSQLConstructorRepo(DbConnection connection) : IPostgreSQLConstructorRepo
{
}

// ==================== MySQL 方言测试 ====================

public partial interface IMySQLConstructorRepo
{
    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE id = @id")]
    Task<DialectTestUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("INSERT INTO dialect_users (name, age, email, created_at) VALUES (@name, @age, @email, @createdAt)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age, string? email, DateTime createdAt, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE name LIKE CONCAT('%', @keyword, '%')")]
    Task<List<DialectTestUser>> SearchByNameAsync(string keyword, CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users SET email = @email WHERE id = @id LIMIT 1")]
    Task<int> UpdateEmailAsync(long id, string? email, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IMySQLConstructorRepo))]
public partial class MySQLConstructorRepo(DbConnection connection) : IMySQLConstructorRepo
{
}

// ==================== SQL Server 方言测试 ====================

public partial interface ISqlServerConstructorRepo
{
    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE id = @id")]
    Task<DialectTestUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("INSERT INTO dialect_users (name, age, email, created_at) VALUES (@name, @age, @email, @createdAt); SELECT SCOPE_IDENTITY()")]
    Task<long> InsertAsync(string name, int age, string? email, DateTime createdAt, CancellationToken ct = default);

    [SqlTemplate("SELECT TOP (@top) {{columns}} FROM dialect_users ORDER BY created_at DESC")]
    Task<List<DialectTestUser>> GetTopRecentAsync(int top, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE name LIKE '%' + @keyword + '%'")]
    Task<List<DialectTestUser>> SearchByNameAsync(string keyword, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(ISqlServerConstructorRepo))]
public partial class SqlServerConstructorRepo(DbConnection connection) : ISqlServerConstructorRepo
{
}

// ==================== Oracle 方言测试 ====================

public partial interface IOracleConstructorRepo
{
    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE id = :id")]
    Task<DialectTestUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("INSERT INTO dialect_users (id, name, age, email, created_at) VALUES (dialect_users_seq.NEXTVAL, :name, :age, :email, :createdAt)")]
    Task<int> InsertAsync(string name, int age, string? email, DateTime createdAt, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE ROWNUM <= :limit ORDER BY created_at DESC")]
    Task<List<DialectTestUser>> GetRecentAsync(int limit, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE UPPER(name) LIKE UPPER(:pattern)")]
    Task<List<DialectTestUser>> SearchByNameAsync(string pattern, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.Oracle)]
[RepositoryFor(typeof(IOracleConstructorRepo))]
public partial class OracleConstructorRepo(DbConnection connection) : IOracleConstructorRepo
{
}

// ==================== 跨方言兼容性测试 ====================

public partial interface ICrossDi​alectRepo
{
    // 使用标准SQL，应该在所有方言中工作
    [SqlTemplate("SELECT {{columns}} FROM dialect_users")]
    Task<List<DialectTestUser>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM dialect_users WHERE age > @minAge")]
    Task<int> CountAboveAgeAsync(int minAge, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM dialect_users WHERE name = @name")]
    Task<DialectTestUser?> GetByNameAsync(string name, CancellationToken ct = default);

    [SqlTemplate("UPDATE dialect_users SET age = @newAge WHERE age = @oldAge")]
    Task<int> UpdateAgeRangeAsync(int oldAge, int newAge, CancellationToken ct = default);
}

// 为每个方言创建实现
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICrossDi​alectRepo))]
public partial class CrossDialectSQLiteRepo(DbConnection connection) : ICrossDi​alectRepo
{
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(ICrossDi​alectRepo))]
public partial class CrossDialectPostgreSQLRepo(DbConnection connection) : ICrossDi​alectRepo
{
}

[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(ICrossDi​alectRepo))]
public partial class CrossDialectMySQLRepo(DbConnection connection) : ICrossDi​alectRepo
{
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(ICrossDi​alectRepo))]
public partial class CrossDialectSqlServerRepo(DbConnection connection) : ICrossDi​alectRepo
{
}

[SqlDefine(SqlDefineTypes.Oracle)]
[RepositoryFor(typeof(ICrossDi​alectRepo))]
public partial class CrossDialectOracleRepo(DbConnection connection) : ICrossDi​alectRepo
{
}

// ==================== 测试类 ====================

/// <summary>
/// 主构造函数和有参构造函数的多方言测试
/// </summary>
[TestClass]
public class TDD_ConstructorSupport_MultiDialect : IDisposable
{
    private readonly DbConnection _connection;

    public TDD_ConstructorSupport_MultiDialect()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE dialect_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER NOT NULL,
                email TEXT,
                created_at TEXT NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    // ==================== SQLite 方言测试 ====================

    [TestMethod]
    public async Task SQLite_PrimaryConstructor_CRUD_ShouldWork()
    {
        // Arrange
        var repo = new SQLiteConstructorRepo(_connection);
        var now = DateTime.UtcNow;

        // Act - Insert
        var userId = await repo.InsertAsync("SQLiteUser", 30, "sqlite@test.com", now);

        // Assert
        Assert.IsTrue(userId > 0);

        // Act - Read
        var user = await repo.GetByIdAsync(userId);
        Assert.IsNotNull(user);
        Assert.AreEqual("SQLiteUser", user.Name);
        Assert.AreEqual(30, user.Age);

        // Act - Update
        var updated = await repo.UpdateAgeAsync(userId, 31);
        Assert.AreEqual(1, updated);

        user = await repo.GetByIdAsync(userId);
        Assert.AreEqual(31, user.Age);

        // Act - Delete
        var deleted = await repo.DeleteAsync(userId);
        Assert.AreEqual(1, deleted);

        var count = await repo.CountAsync();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task SQLite_AutoIncrement_ShouldGenerateSequentialIds()
    {
        // Arrange
        var repo = new SQLiteConstructorRepo(_connection);
        var now = DateTime.UtcNow;

        // Act
        var id1 = await repo.InsertAsync("User1", 25, null, now);
        var id2 = await repo.InsertAsync("User2", 30, null, now);
        var id3 = await repo.InsertAsync("User3", 35, null, now);

        // Assert
        Assert.AreEqual(id1 + 1, id2);
        Assert.AreEqual(id2 + 1, id3);
    }

    // ==================== PostgreSQL 方言特性测试 ====================

    [TestMethod]
    public void PostgreSQL_ConstructorCompilation_ShouldSucceed()
    {
        // Arrange & Act - 验证编译时代码生成成功
        var repoType = typeof(PostgreSQLConstructorRepo);

        // Assert
        Assert.IsNotNull(repoType);
        Assert.IsTrue(repoType.Name.Contains("PostgreSQL"));

        // 验证主构造函数参数
        var constructors = repoType.GetConstructors();
        Assert.IsTrue(constructors.Length > 0);

        var primaryCtor = constructors.FirstOrDefault(c => c.GetParameters().Length == 1);
        Assert.IsNotNull(primaryCtor);
        Assert.AreEqual(typeof(DbConnection), primaryCtor.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void PostgreSQL_InterfaceMethods_ShouldBeGenerated()
    {
        // Arrange
        var repoType = typeof(PostgreSQLConstructorRepo);
        var interfaceType = typeof(IPostgreSQLConstructorRepo);

        // Act
        var methods = repoType.GetMethods();
        var interfaceMethods = interfaceType.GetMethods();

        // Assert - 验证所有接口方法都被实现
        foreach (var interfaceMethod in interfaceMethods)
        {
            var implemented = methods.Any(m =>
                m.Name == interfaceMethod.Name &&
                m.ReturnType == interfaceMethod.ReturnType);

            Assert.IsTrue(implemented, $"Method {interfaceMethod.Name} not implemented");
        }
    }

    // ==================== MySQL 方言特性测试 ====================

    [TestMethod]
    public void MySQL_ConstructorWithConnection_ShouldInstantiate()
    {
        // Arrange & Act
        var repo = new MySQLConstructorRepo(_connection);

        // Assert
        Assert.IsNotNull(repo);
        Assert.IsInstanceOfType(repo, typeof(IMySQLConstructorRepo));
    }

    [TestMethod]
    public void MySQL_PrimaryConstructorParameter_ShouldBeAccessible()
    {
        // Arrange
        var repoType = typeof(MySQLConstructorRepo);

        // Act - 验证主构造函数参数可以被访问
        var constructor = repoType.GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Length == 1);

        // Assert
        Assert.IsNotNull(constructor);
        var param = constructor.GetParameters()[0];
        Assert.AreEqual("connection", param.Name);
        Assert.AreEqual(typeof(DbConnection), param.ParameterType);
    }

    // ==================== SQL Server 方言特性测试 ====================

    [TestMethod]
    public void SqlServer_ConstructorGeneration_ShouldSupportPrimaryConstructor()
    {
        // Arrange & Act
        var repo = new SqlServerConstructorRepo(_connection);

        // Assert
        Assert.IsNotNull(repo);

        // 验证类型实现了接口
        Assert.IsTrue(typeof(ISqlServerConstructorRepo).IsAssignableFrom(typeof(SqlServerConstructorRepo)));
    }

    [TestMethod]
    public void SqlServer_PartialClass_ShouldAllowExtension()
    {
        // Arrange & Act
        var repoType = typeof(SqlServerConstructorRepo);

        // Assert - partial类应该允许在其他部分添加成员
        Assert.IsTrue(repoType.IsClass);
        Assert.IsFalse(repoType.IsSealed);
    }

    // ==================== Oracle 方言特性测试 ====================

    [TestMethod]
    public void Oracle_ConstructorWithDbConnection_ShouldCompile()
    {
        // Arrange & Act
        var repo = new OracleConstructorRepo(_connection);

        // Assert
        Assert.IsNotNull(repo);
        Assert.IsInstanceOfType(repo, typeof(IOracleConstructorRepo));
    }

    [TestMethod]
    public void Oracle_ParameterBinding_ShouldUseColonPrefix()
    {
        // Arrange
        var interfaceType = typeof(IOracleConstructorRepo);

        // Act - 获取方法上的SqlTemplate属性
        var methods = interfaceType.GetMethods();
        var getByIdMethod = methods.FirstOrDefault(m => m.Name == "GetByIdAsync");

        // Assert
        Assert.IsNotNull(getByIdMethod);

        var sqlTemplateAttr = getByIdMethod.GetCustomAttributes(typeof(SqlTemplateAttribute), false)
            .FirstOrDefault() as SqlTemplateAttribute;

        Assert.IsNotNull(sqlTemplateAttr);
        Assert.IsTrue(sqlTemplateAttr.Template.Contains(":id"), "Oracle should use :param syntax");
    }

    // ==================== 跨方言兼容性测试 ====================

    [TestMethod]
    public async Task CrossDialect_SQLite_StandardSQL_ShouldWork()
    {
        // Arrange
        var repo = new CrossDialectSQLiteRepo(_connection);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO dialect_users (name, age, email, created_at) VALUES ('Test', 25, 'test@test.com', datetime('now'))";
        cmd.ExecuteNonQuery();

        // Act
        var users = await repo.GetAllAsync();
        var count = await repo.CountAboveAgeAsync(20);
        var user = await repo.GetByNameAsync("Test");

        // Assert
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual(1, count);
        Assert.IsNotNull(user);
        Assert.AreEqual("Test", user.Name);
    }

    [TestMethod]
    public void CrossDialect_AllImplementations_ShouldCompile()
    {
        // Arrange & Act
        var sqliteRepo = new CrossDialectSQLiteRepo(_connection);
        var postgresRepo = new CrossDialectPostgreSQLRepo(_connection);
        var mysqlRepo = new CrossDialectMySQLRepo(_connection);
        var sqlServerRepo = new CrossDialectSqlServerRepo(_connection);
        var oracleRepo = new CrossDialectOracleRepo(_connection);

        // Assert - 所有实现都应该编译成功
        Assert.IsNotNull(sqliteRepo);
        Assert.IsNotNull(postgresRepo);
        Assert.IsNotNull(mysqlRepo);
        Assert.IsNotNull(sqlServerRepo);
        Assert.IsNotNull(oracleRepo);

        // 验证它们都实现了相同的接口
        Assert.IsInstanceOfType(sqliteRepo, typeof(ICrossDi​alectRepo));
        Assert.IsInstanceOfType(postgresRepo, typeof(ICrossDi​alectRepo));
        Assert.IsInstanceOfType(mysqlRepo, typeof(ICrossDi​alectRepo));
        Assert.IsInstanceOfType(sqlServerRepo, typeof(ICrossDi​alectRepo));
        Assert.IsInstanceOfType(oracleRepo, typeof(ICrossDi​alectRepo));
    }

    [TestMethod]
    public void CrossDialect_InterfaceContract_ShouldBeConsistent()
    {
        // Arrange
        var interfaceType = typeof(ICrossDi​alectRepo);
        var methods = interfaceType.GetMethods();

        // Assert - 验证接口方法数量和签名
        Assert.AreEqual(4, methods.Length);

        Assert.IsTrue(methods.Any(m => m.Name == "GetAllAsync"));
        Assert.IsTrue(methods.Any(m => m.Name == "CountAboveAgeAsync"));
        Assert.IsTrue(methods.Any(m => m.Name == "GetByNameAsync"));
        Assert.IsTrue(methods.Any(m => m.Name == "UpdateAgeRangeAsync"));
    }

    // ==================== 构造函数反射测试 ====================

    [TestMethod]
    public void AllDialects_ShouldHavePrimaryConstructor()
    {
        // Arrange
        var dialectRepos = new[]
        {
            typeof(SQLiteConstructorRepo),
            typeof(PostgreSQLConstructorRepo),
            typeof(MySQLConstructorRepo),
            typeof(SqlServerConstructorRepo),
            typeof(OracleConstructorRepo)
        };

        // Act & Assert
        foreach (var repoType in dialectRepos)
        {
            var constructors = repoType.GetConstructors();
            var primaryCtor = constructors.FirstOrDefault(c =>
                c.GetParameters().Length == 1 &&
                c.GetParameters()[0].ParameterType == typeof(DbConnection));

            Assert.IsNotNull(primaryCtor, $"{repoType.Name} should have primary constructor");
        }
    }

    [TestMethod]
    public void AllDialects_ConstructorParameter_ShouldBeNamedConnection()
    {
        // Arrange
        var dialectRepos = new[]
        {
            typeof(SQLiteConstructorRepo),
            typeof(PostgreSQLConstructorRepo),
            typeof(MySQLConstructorRepo),
            typeof(SqlServerConstructorRepo),
            typeof(OracleConstructorRepo)
        };

        // Act & Assert
        foreach (var repoType in dialectRepos)
        {
            var constructor = repoType.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length == 1);

            Assert.IsNotNull(constructor);
            var param = constructor.GetParameters()[0];
            Assert.AreEqual("connection", param.Name, $"{repoType.Name} constructor parameter should be named 'connection'");
        }
    }

    // ==================== 编译时生成验证 ====================

    [TestMethod]
    public void AllDialects_GeneratedCode_ShouldBePartialClass()
    {
        // Arrange
        var dialectRepos = new[]
        {
            typeof(SQLiteConstructorRepo),
            typeof(PostgreSQLConstructorRepo),
            typeof(MySQLConstructorRepo),
            typeof(SqlServerConstructorRepo),
            typeof(OracleConstructorRepo),
            typeof(CrossDialectSQLiteRepo),
            typeof(CrossDialectPostgreSQLRepo),
            typeof(CrossDialectMySQLRepo),
            typeof(CrossDialectSqlServerRepo),
            typeof(CrossDialectOracleRepo)
        };

        // Act & Assert
        foreach (var repoType in dialectRepos)
        {
            Assert.IsTrue(repoType.IsClass, $"{repoType.Name} should be a class");
            Assert.IsFalse(repoType.IsSealed, $"{repoType.Name} should not be sealed (must be partial)");
            Assert.IsTrue(repoType.IsPublic, $"{repoType.Name} should be public");
        }
    }

    [TestMethod]
    public void AllDialects_ShouldImplementCorrectInterface()
    {
        // Arrange & Act & Assert
        Assert.IsTrue(typeof(ISQLiteConstructorRepo).IsAssignableFrom(typeof(SQLiteConstructorRepo)));
        Assert.IsTrue(typeof(IPostgreSQLConstructorRepo).IsAssignableFrom(typeof(PostgreSQLConstructorRepo)));
        Assert.IsTrue(typeof(IMySQLConstructorRepo).IsAssignableFrom(typeof(MySQLConstructorRepo)));
        Assert.IsTrue(typeof(ISqlServerConstructorRepo).IsAssignableFrom(typeof(SqlServerConstructorRepo)));
        Assert.IsTrue(typeof(IOracleConstructorRepo).IsAssignableFrom(typeof(OracleConstructorRepo)));
    }

    // ==================== 方言特定语法验证 ====================

    [TestMethod]
    public void DialectSpecific_ParameterPrefix_ShouldBeCorrect()
    {
        // Arrange
        var testCases = new[]
        {
            (typeof(ISQLiteConstructorRepo), "@"),      // SQLite uses @
            (typeof(IPostgreSQLConstructorRepo), "@"),  // PostgreSQL uses @
            (typeof(IMySQLConstructorRepo), "@"),       // MySQL uses @
            (typeof(ISqlServerConstructorRepo), "@"),   // SQL Server uses @
            (typeof(IOracleConstructorRepo), ":")       // Oracle uses :
        };

        // Act & Assert
        foreach (var (interfaceType, expectedPrefix) in testCases)
        {
            var methods = interfaceType.GetMethods();
            var anyMethod = methods.FirstOrDefault();
            Assert.IsNotNull(anyMethod, $"{interfaceType.Name} should have methods");

            var sqlAttr = anyMethod.GetCustomAttributes(typeof(SqlTemplateAttribute), false)
                .FirstOrDefault() as SqlTemplateAttribute;

            if (sqlAttr != null && sqlAttr.Template.Contains("@") || sqlAttr.Template.Contains(":"))
            {
                var hasCorrectPrefix = sqlAttr.Template.Contains(expectedPrefix);
                Assert.IsTrue(hasCorrectPrefix,
                    $"{interfaceType.Name} should use {expectedPrefix} for parameters");
            }
        }
    }

    // ==================== 性能和资源管理 ====================

    [TestMethod]
    public void MultipleDialects_ConcurrentInstantiation_ShouldSucceed()
    {
        // Arrange & Act
        var tasks = new Task<object>[]
        {
            Task.Run<object>(() => new SQLiteConstructorRepo(_connection)),
            Task.Run<object>(() => new PostgreSQLConstructorRepo(_connection)),
            Task.Run<object>(() => new MySQLConstructorRepo(_connection)),
            Task.Run<object>(() => new SqlServerConstructorRepo(_connection)),
            Task.Run<object>(() => new OracleConstructorRepo(_connection))
        };

        Task.WaitAll(tasks);

        // Assert - 所有实例化都应该成功
        foreach (var task in tasks)
        {
            Assert.IsNotNull(task.Result);
        }
    }

    [TestMethod]
    public void Dialects_ConnectionParameter_ShouldAcceptDerivedTypes()
    {
        // Arrange - SQLiteConnection继承自DbConnection
        var sqliteConnection = new SqliteConnection("Data Source=:memory:");
        sqliteConnection.Open();

        try
        {
            // Act - 应该能够传入派生类型
            var sqliteRepo = new SQLiteConstructorRepo(sqliteConnection);
            var postgresRepo = new PostgreSQLConstructorRepo(sqliteConnection); // 虽然类型不匹配，但编译时应该接受
            var mysqlRepo = new MySQLConstructorRepo(sqliteConnection);
            var sqlServerRepo = new SqlServerConstructorRepo(sqliteConnection);
            var oracleRepo = new OracleConstructorRepo(sqliteConnection);

            // Assert
            Assert.IsNotNull(sqliteRepo);
            Assert.IsNotNull(postgresRepo);
            Assert.IsNotNull(mysqlRepo);
            Assert.IsNotNull(sqlServerRepo);
            Assert.IsNotNull(oracleRepo);
        }
        finally
        {
            sqliteConnection.Dispose();
        }
    }

    // ==================== 元数据和属性验证 ====================

    [TestMethod]
    public void AllDialects_ShouldHaveSqlDefineAttribute()
    {
        // Arrange
        var dialectRepos = new[]
        {
            (typeof(SQLiteConstructorRepo), SqlDefineTypes.SQLite),
            (typeof(PostgreSQLConstructorRepo), SqlDefineTypes.PostgreSql),
            (typeof(MySQLConstructorRepo), SqlDefineTypes.MySql),
            (typeof(SqlServerConstructorRepo), SqlDefineTypes.SqlServer),
            (typeof(OracleConstructorRepo), SqlDefineTypes.Oracle)
        };

        // Act & Assert
        foreach (var (repoType, expectedDialect) in dialectRepos)
        {
            var attr = repoType.GetCustomAttributes(typeof(SqlDefineAttribute), false)
                .FirstOrDefault() as SqlDefineAttribute;

            Assert.IsNotNull(attr, $"{repoType.Name} should have SqlDefine attribute");
            Assert.AreEqual(expectedDialect, attr.DialectType,
                $"{repoType.Name} should be defined for {expectedDialect}");
        }
    }

    [TestMethod]
    public void AllDialects_ShouldHaveRepositoryForAttribute()
    {
        // Arrange
        var dialectRepos = new[]
        {
            typeof(SQLiteConstructorRepo),
            typeof(PostgreSQLConstructorRepo),
            typeof(MySQLConstructorRepo),
            typeof(SqlServerConstructorRepo),
            typeof(OracleConstructorRepo)
        };

        // Act & Assert
        foreach (var repoType in dialectRepos)
        {
            var attr = repoType.GetCustomAttributes(typeof(RepositoryForAttribute), false)
                .FirstOrDefault() as RepositoryForAttribute;

            Assert.IsNotNull(attr, $"{repoType.Name} should have RepositoryFor attribute");
            Assert.IsNotNull(attr.ServiceType, $"{repoType.Name} RepositoryFor should specify interface type");
        }
    }
}

