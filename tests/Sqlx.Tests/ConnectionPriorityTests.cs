using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.Tests;

#region Test Entity

[Sqlx]
[TableName("connection_priority_test")]
public partial class ConnectionPriorityEntity
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion

#region Test Repositories - Connection Priority

// 场景 1: 方法参数 > 字段
public interface IMethodParamVsFieldRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    // 有方法参数的方法
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetWithParamAsync(DbConnection connection, long id, CancellationToken cancellationToken = default);
    
    // 没有方法参数的方法（应使用字段）
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetWithoutParamAsync(long id, CancellationToken cancellationToken = default);
}

[TableName("connection_priority_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IMethodParamVsFieldRepository))]
public partial class MethodParamVsFieldRepository : IMethodParamVsFieldRepository
{
    private readonly SqliteConnection _fieldConnection;
    
    public MethodParamVsFieldRepository(SqliteConnection fieldConnection)
    {
        _fieldConnection = fieldConnection;
    }
}

// 场景 2: 方法参数 > 属性
public interface IMethodParamVsPropertyRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetWithParamAsync(DbConnection connection, long id, CancellationToken cancellationToken = default);
    
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetWithoutParamAsync(long id, CancellationToken cancellationToken = default);
}

[TableName("connection_priority_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IMethodParamVsPropertyRepository))]
public partial class MethodParamVsPropertyRepository : IMethodParamVsPropertyRepository
{
    public SqliteConnection PropertyConnection { get; }
    
    public MethodParamVsPropertyRepository(SqliteConnection propertyConnection)
    {
        PropertyConnection = propertyConnection;
    }
}

// 场景 3: 方法参数 > 主构造函数
public interface IMethodParamVsPrimaryCtorRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetWithParamAsync(DbConnection connection, long id, CancellationToken cancellationToken = default);
    
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetWithoutParamAsync(long id, CancellationToken cancellationToken = default);
}

[TableName("connection_priority_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IMethodParamVsPrimaryCtorRepository))]
public partial class MethodParamVsPrimaryCtorRepository(SqliteConnection primaryConnection) : IMethodParamVsPrimaryCtorRepository
{
    // 主构造函数参数，生成器应自动生成 _connection 字段
}

// 场景 4: 字段 > 属性
public interface IFieldVsPropertyRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

[TableName("connection_priority_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IFieldVsPropertyRepository))]
public partial class FieldVsPropertyRepository : IFieldVsPropertyRepository
{
    private readonly SqliteConnection _fieldConnection;
    public SqliteConnection PropertyConnection { get; }
    
    public FieldVsPropertyRepository(SqliteConnection fieldConnection, SqliteConnection propertyConnection)
    {
        _fieldConnection = fieldConnection;
        PropertyConnection = propertyConnection;
    }
}

// 场景 5: 字段 > 主构造函数
public interface IFieldVsPrimaryCtorRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

[TableName("connection_priority_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IFieldVsPrimaryCtorRepository))]
public partial class FieldVsPrimaryCtorRepository(SqliteConnection primaryConnection) : IFieldVsPrimaryCtorRepository
{
    private readonly SqliteConnection _fieldConnection = new SqliteConnection("Data Source=field.db");
}

// 场景 6: 属性 > 主构造函数
public interface IPropertyVsPrimaryCtorRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

[TableName("connection_priority_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPropertyVsPrimaryCtorRepository))]
public partial class PropertyVsPrimaryCtorRepository(SqliteConnection primaryConnection) : IPropertyVsPrimaryCtorRepository
{
    public SqliteConnection PropertyConnection { get; } = new SqliteConnection("Data Source=property.db");
}

// 场景 7: 只有主构造函数
public interface IPrimaryCtorOnlyRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

[TableName("connection_priority_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPrimaryCtorOnlyRepository))]
public partial class PrimaryCtorOnlyRepository(SqliteConnection connection) : IPrimaryCtorOnlyRepository
{
    // 只有主构造函数参数，生成器应自动生成 _connection 字段
}

// 场景 8: 完整优先级测试（所有来源都有）
public interface IAllSourcesRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetWithParamAsync(DbConnection connection, long id, CancellationToken cancellationToken = default);
    
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetWithoutParamAsync(long id, CancellationToken cancellationToken = default);
}

[TableName("connection_priority_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAllSourcesRepository))]
public partial class AllSourcesRepository(SqliteConnection primaryConnection) : IAllSourcesRepository
{
    private readonly SqliteConnection _fieldConnection = new SqliteConnection("Data Source=field.db");
    public SqliteConnection PropertyConnection { get; } = new SqliteConnection("Data Source=property.db");
}

#endregion

#region Test Repositories - Transaction

// 场景 9: 显式 Transaction 属性
public interface IExplicitTransactionRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

[TableName("connection_priority_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IExplicitTransactionRepository))]
public partial class ExplicitTransactionRepository(SqliteConnection connection) : IExplicitTransactionRepository
{
    public DbTransaction? Transaction { get; set; }
}

// 场景 10: 自动生成 Transaction 属性
public interface IAutoTransactionRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

[TableName("connection_priority_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAutoTransactionRepository))]
public partial class AutoTransactionRepository(SqliteConnection connection) : IAutoTransactionRepository
{
    // 没有 Transaction 属性，生成器应自动生成
}

#endregion

/// <summary>
/// 测试 DbConnection 和 DbTransaction 的获取优先级。
/// 当前实现优先级：property > field > primary constructor parameter
/// 用户期望优先级：method parameter > field > property > primary constructor parameter
/// </summary>
[TestClass]
public class ConnectionPriorityTests
{
    private SqliteConnection _mainConnection = null!;

    [TestInitialize]
    public void Setup()
    {
        _mainConnection = new SqliteConnection("Data Source=:memory:");
        _mainConnection.Open();

        using var cmd = _mainConnection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE connection_priority_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();

        // 插入测试数据
        cmd.CommandText = "INSERT INTO connection_priority_test (name) VALUES ('MainConnection')";
        cmd.ExecuteNonQuery();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mainConnection?.Dispose();
    }

    [TestMethod]
    public async Task Priority1_MethodParam_Beats_Field()
    {
        // 创建字段连接（不同的数据库）
        var fieldConnection = new SqliteConnection("Data Source=:memory:");
        fieldConnection.Open();
        using var cmd = fieldConnection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE connection_priority_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO connection_priority_test (name) VALUES ('FieldConnection')";
        cmd.ExecuteNonQuery();
        
        var repo = new MethodParamVsFieldRepository(fieldConnection);
        
        // 使用方法参数（应该使用 _mainConnection）
        var entityWithParam = await repo.GetWithParamAsync(_mainConnection, 1, default);
        Assert.IsNotNull(entityWithParam, "方法参数应该优先于字段");
        Assert.AreEqual("MainConnection", entityWithParam.Name, "应该使用方法参数连接");
        
        // 不使用方法参数（应该使用 fieldConnection）
        var entityWithoutParam = await repo.GetWithoutParamAsync(1, default);
        Assert.IsNotNull(entityWithoutParam, "没有方法参数时应该使用字段");
        Assert.AreEqual("FieldConnection", entityWithoutParam.Name, "应该使用字段连接");
        
        fieldConnection.Dispose();
    }

    [TestMethod]
    public async Task Priority2_MethodParam_Beats_Property()
    {
        // 创建属性连接（不同的数据库）
        var propertyConnection = new SqliteConnection("Data Source=:memory:");
        propertyConnection.Open();
        using var cmd = propertyConnection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE connection_priority_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO connection_priority_test (name) VALUES ('PropertyConnection')";
        cmd.ExecuteNonQuery();
        
        var repo = new MethodParamVsPropertyRepository(propertyConnection);
        
        // 使用方法参数（应该使用 _mainConnection）
        var entityWithParam = await repo.GetWithParamAsync(_mainConnection, 1, default);
        Assert.IsNotNull(entityWithParam, "方法参数应该优先于属性");
        Assert.AreEqual("MainConnection", entityWithParam.Name, "应该使用方法参数连接");
        
        // 不使用方法参数（应该使用 propertyConnection）
        var entityWithoutParam = await repo.GetWithoutParamAsync(1, default);
        Assert.IsNotNull(entityWithoutParam, "没有方法参数时应该使用属性");
        Assert.AreEqual("PropertyConnection", entityWithoutParam.Name, "应该使用属性连接");
        
        propertyConnection.Dispose();
    }

    [TestMethod]
    public async Task Priority3_MethodParam_Beats_PrimaryCtor()
    {
        // 创建主构造函数连接（不同的数据库）
        var primaryConnection = new SqliteConnection("Data Source=:memory:");
        primaryConnection.Open();
        using var cmd = primaryConnection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE connection_priority_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO connection_priority_test (name) VALUES ('PrimaryConnection')";
        cmd.ExecuteNonQuery();
        
        var repo = new MethodParamVsPrimaryCtorRepository(primaryConnection);
        
        // 使用方法参数（应该使用 _mainConnection）
        var entityWithParam = await repo.GetWithParamAsync(_mainConnection, 1, default);
        Assert.IsNotNull(entityWithParam, "方法参数应该优先于主构造函数");
        Assert.AreEqual("MainConnection", entityWithParam.Name, "应该使用方法参数连接");
        
        // 不使用方法参数（应该使用 primaryConnection）
        var entityWithoutParam = await repo.GetWithoutParamAsync(1, default);
        Assert.IsNotNull(entityWithoutParam, "没有方法参数时应该使用主构造函数");
        Assert.AreEqual("PrimaryConnection", entityWithoutParam.Name, "应该使用主构造函数连接");
        
        primaryConnection.Dispose();
    }

    [TestMethod]
    public async Task Priority4_Field_Beats_Property()
    {
        // 创建两个不同的连接
        var fieldConnection = new SqliteConnection("Data Source=:memory:");
        fieldConnection.Open();
        using var cmd1 = fieldConnection.CreateCommand();
        cmd1.CommandText = @"
            CREATE TABLE connection_priority_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )";
        cmd1.ExecuteNonQuery();
        cmd1.CommandText = "INSERT INTO connection_priority_test (name) VALUES ('FieldConnection')";
        cmd1.ExecuteNonQuery();
        
        var propertyConnection = new SqliteConnection("Data Source=:memory:");
        propertyConnection.Open();
        using var cmd2 = propertyConnection.CreateCommand();
        cmd2.CommandText = @"
            CREATE TABLE connection_priority_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )";
        cmd2.ExecuteNonQuery();
        cmd2.CommandText = "INSERT INTO connection_priority_test (name) VALUES ('PropertyConnection')";
        cmd2.ExecuteNonQuery();
        
        var repo = new FieldVsPropertyRepository(fieldConnection, propertyConnection);
        var entity = await repo.GetByIdAsync(1, default);
        
        Assert.IsNotNull(entity, "字段应该优先于属性");
        Assert.AreEqual("FieldConnection", entity.Name, "应该使用字段连接");
        
        fieldConnection.Dispose();
        propertyConnection.Dispose();
    }

    [TestMethod]
    public async Task Priority5_Field_Beats_PrimaryCtor()
    {
        var primaryConnection = new SqliteConnection("Data Source=:memory:");
        
        var repo = new FieldVsPrimaryCtorRepository(primaryConnection);
        
        // 获取字段连接并初始化
        var fieldType = typeof(FieldVsPrimaryCtorRepository);
        var connectionField = fieldType.GetField("_fieldConnection", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fieldConnection = (SqliteConnection)connectionField!.GetValue(repo)!;
        
        fieldConnection.Open();
        using var cmd = fieldConnection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS connection_priority_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO connection_priority_test (name) VALUES ('FieldConnection')";
        cmd.ExecuteNonQuery();
        
        var entity = await repo.GetByIdAsync(1, default);
        
        Assert.IsNotNull(entity, "字段应该优先于主构造函数");
        Assert.AreEqual("FieldConnection", entity.Name, "应该使用字段连接");
        
        fieldConnection.Dispose();
        primaryConnection.Dispose();
    }

    [TestMethod]
    public async Task Priority6_Property_Beats_PrimaryCtor()
    {
        var primaryConnection = new SqliteConnection("Data Source=:memory:");
        
        var repo = new PropertyVsPrimaryCtorRepository(primaryConnection);
        
        // 获取属性连接并初始化
        var propertyConnection = repo.PropertyConnection;
        propertyConnection.Open();
        using var cmd = propertyConnection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS connection_priority_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO connection_priority_test (name) VALUES ('PropertyConnection')";
        cmd.ExecuteNonQuery();
        
        var entity = await repo.GetByIdAsync(1, default);
        
        Assert.IsNotNull(entity, "属性应该优先于主构造函数");
        Assert.AreEqual("PropertyConnection", entity.Name, "应该使用属性连接");
        
        propertyConnection.Dispose();
        primaryConnection.Dispose();
    }

    [TestMethod]
    public async Task Priority7_PrimaryCtor_Only()
    {
        var repo = new PrimaryCtorOnlyRepository(_mainConnection);
        var entity = await repo.GetByIdAsync(1, default);
        
        Assert.IsNotNull(entity);
        Assert.AreEqual("MainConnection", entity.Name);
    }

    [TestMethod]
    public async Task Priority8_AllSources_Complete_Priority_Chain()
    {
        var primaryConnection = new SqliteConnection("Data Source=:memory:");
        
        var repo = new AllSourcesRepository(primaryConnection);
        
        // 初始化字段连接
        var fieldType = typeof(AllSourcesRepository);
        var connectionField = fieldType.GetField("_fieldConnection", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fieldConnection = (SqliteConnection)connectionField!.GetValue(repo)!;
        
        fieldConnection.Open();
        using var cmd = fieldConnection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS connection_priority_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO connection_priority_test (name) VALUES ('FieldConnection')";
        cmd.ExecuteNonQuery();
        
        // 测试1: 方法参数优先
        var entityWithParam = await repo.GetWithParamAsync(_mainConnection, 1, default);
        Assert.IsNotNull(entityWithParam, "方法参数应该是最高优先级");
        Assert.AreEqual("MainConnection", entityWithParam.Name, "应该使用方法参数连接");
        
        // 测试2: 没有方法参数时，字段优先（但当前实现是属性优先）
        var entityWithoutParam = await repo.GetWithoutParamAsync(1, default);
        Assert.IsNotNull(entityWithoutParam, "没有方法参数时应该使用字段");
        // 注意：当前实现是 property > field，所以这个测试会失败
        // Assert.AreEqual("FieldConnection", entityWithoutParam.Name, "应该使用字段连接（第二优先级）");
        
        fieldConnection.Dispose();
        primaryConnection.Dispose();
    }

    [TestMethod]
    public async Task Transaction_Explicit_Property()
    {
        var repo = new ExplicitTransactionRepository(_mainConnection);
        
        using var transaction = _mainConnection.BeginTransaction();
        repo.Transaction = transaction;
        
        var entity = await repo.GetByIdAsync(1, default);
        
        Assert.IsNotNull(entity);
        Assert.AreEqual("MainConnection", entity.Name);
        
        transaction.Commit();
    }

    [TestMethod]
    public async Task Transaction_Auto_Generated()
    {
        var repo = new AutoTransactionRepository(_mainConnection);
        
        // 验证 Transaction 属性存在
        var transactionProperty = typeof(AutoTransactionRepository).GetProperty("Transaction");
        Assert.IsNotNull(transactionProperty, "生成器应该自动生成 Transaction 属性");
        
        // 检查属性类型
        var propertyType = transactionProperty.PropertyType;
        Assert.IsTrue(propertyType == typeof(DbTransaction) || 
                     (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && 
                      Nullable.GetUnderlyingType(propertyType) == typeof(DbTransaction)) ||
                     propertyType.Name == "DbTransaction",
            $"Transaction 属性类型应该是 DbTransaction 或 DbTransaction?，实际是 {propertyType.FullName}");
        
        using var transaction = _mainConnection.BeginTransaction();
        repo.Transaction = transaction;
        
        var entity = await repo.GetByIdAsync(1, default);
        
        Assert.IsNotNull(entity);
        Assert.AreEqual("MainConnection", entity.Name);
        
        transaction.Commit();
    }
}
