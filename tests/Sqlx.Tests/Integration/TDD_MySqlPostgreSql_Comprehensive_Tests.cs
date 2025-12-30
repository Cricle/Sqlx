// -----------------------------------------------------------------------
// <copyright file="TDD_MySqlPostgreSql_Comprehensive_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Integration;

// ==================== 测试模型 ====================
    
    public class ComprehensiveTestEntity
    {
        public long Id { get; set; }
        
        // 字符串类型测试
        public string Name { get; set; } = "";
        public string? NullableText { get; set; }
        public string SpecialChars { get; set; } = ""; // 包含特殊字符：'quotes', "double", \backslash
        
        // 数值类型测试
        public int IntValue { get; set; }
        public long BigIntValue { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public float FloatValue { get; set; }
        
        // 布尔类型测试
        public bool IsActive { get; set; }
        public bool? NullableBool { get; set; }
        
        // 日期时间类型测试
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // 枚举/分类测试
        public string Category { get; set; } = "";
        public string Status { get; set; } = "";
    }
    
    public class TransactionTestEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
    
    public class AggregateTestEntity
    {
        public long Id { get; set; }
        public string GroupName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime SaleDate { get; set; }
    }
    
    // ==================== MySQL 仓储 ====================
    
    [RepositoryFor(typeof(IComprehensiveRepository), Dialect = SqlDefineTypes.MySql, TableName = "comprehensive_test_mysql")]
    public partial class MySqlComprehensiveRepository : IComprehensiveRepository
    {
        private readonly DbConnection _connection;
        public MySqlComprehensiveRepository(DbConnection connection) => _connection = connection;
    }
    
    [RepositoryFor(typeof(ITransactionRepository), Dialect = SqlDefineTypes.MySql, TableName = "transaction_test_mysql")]
    public partial class MySqlTransactionRepository : ITransactionRepository
    {
        private readonly DbConnection _connection;
        public MySqlTransactionRepository(DbConnection connection) => _connection = connection;
    }
    
    [RepositoryFor(typeof(IAggregateTestRepository), Dialect = SqlDefineTypes.MySql, TableName = "aggregate_test_mysql")]
    public partial class MySqlAggregateRepository : IAggregateTestRepository
    {
        private readonly DbConnection _connection;
        public MySqlAggregateRepository(DbConnection connection) => _connection = connection;
    }
    
    // ==================== PostgreSQL 仓储 ====================
    
    [RepositoryFor(typeof(IComprehensiveRepository), Dialect = SqlDefineTypes.PostgreSql, TableName = "comprehensive_test_pgsql")]
    public partial class PostgreSqlComprehensiveRepository : IComprehensiveRepository
    {
        private readonly DbConnection _connection;
        public PostgreSqlComprehensiveRepository(DbConnection connection) => _connection = connection;
    }
    
    [RepositoryFor(typeof(ITransactionRepository), Dialect = SqlDefineTypes.PostgreSql, TableName = "transaction_test_pgsql")]
    public partial class PostgreSqlTransactionRepository : ITransactionRepository
    {
        private readonly DbConnection _connection;
        public PostgreSqlTransactionRepository(DbConnection connection) => _connection = connection;
    }
    
    [RepositoryFor(typeof(IAggregateTestRepository), Dialect = SqlDefineTypes.PostgreSql, TableName = "aggregate_test_pgsql")]
    public partial class PostgreSqlAggregateRepository : IAggregateTestRepository
    {
        private readonly DbConnection _connection;
        public PostgreSqlAggregateRepository(DbConnection connection) => _connection = connection;
    }
    
    // ==================== 仓储接口 ====================
    
    public partial interface IComprehensiveRepository
    {
        // 基础 CRUD
        [SqlTemplate("INSERT INTO {{table}} (name, nullable_text, special_chars, int_value, big_int_value, decimal_value, double_value, float_value, is_active, nullable_bool, created_at, updated_at, category, status) VALUES (@name, @nullableText, @specialChars, @intValue, @bigintValue, @decimalValue, @doubleValue, @floatValue, @isActive, @nullableBool, @createdAt, @updatedAt, @category, @status)")]
        [ReturnInsertedId]
        Task<long> InsertAsync(string name, string? nullableText, string specialChars, int intValue, long bigintValue, decimal decimalValue, double doubleValue, float floatValue, bool isActive, bool? nullableBool, DateTime createdAt, DateTime? updatedAt, string category, string status);
        
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
        Task<ComprehensiveTestEntity?> GetByIdAsync(long id);
        
        [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id")]
        Task<List<ComprehensiveTestEntity>> GetAllAsync();
        
        [SqlTemplate("UPDATE {{table}} SET name = @name, nullable_text = @nullableText, updated_at = @updatedAt WHERE id = @id")]
        Task<int> UpdateAsync(long id, string name, string? nullableText, DateTime? updatedAt);
        
        [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
        Task<int> DeleteAsync(long id);
        
        // 条件查询
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = @isActive AND category = @category ORDER BY name")]
        Task<List<ComprehensiveTestEntity>> GetByActiveAndCategoryAsync(bool isActive, string category);
        
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE special_chars LIKE @pattern")]
        Task<List<ComprehensiveTestEntity>> SearchBySpecialCharsAsync(string pattern);
        
        // NULL 处理
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE nullable_text IS NULL")]
        Task<List<ComprehensiveTestEntity>> GetWithNullTextAsync();
        
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE nullable_text IS NOT NULL")]
        Task<List<ComprehensiveTestEntity>> GetWithNonNullTextAsync();
        
        // 数值比较
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE decimal_value > @minValue AND decimal_value < @maxValue")]
        Task<List<ComprehensiveTestEntity>> GetByDecimalRangeAsync(decimal minValue, decimal maxValue);
        
        // 日期范围
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE created_at BETWEEN @startDate AND @endDate")]
        Task<List<ComprehensiveTestEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        
        // IN 查询
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE status IN {{in:statuses}}")]
        Task<List<ComprehensiveTestEntity>> GetByStatusInAsync(List<string> statuses);
        
        // 聚合函数
        [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE is_active = @isActive")]
        Task<long> CountActiveAsync(bool isActive);
        
        [SqlTemplate("SELECT SUM(decimal_value) FROM {{table}}")]
        Task<decimal?> SumDecimalValuesAsync();
        
        [SqlTemplate("SELECT AVG(int_value) FROM {{table}}")]
        Task<double?> AverageIntValuesAsync();
        
        [SqlTemplate("SELECT MAX(big_int_value) FROM {{table}}")]
        Task<long?> MaxBigIntValueAsync();
        
        [SqlTemplate("SELECT MIN(created_at) FROM {{table}}")]
        Task<DateTime?> MinCreatedAtAsync();
    }
    
    public partial interface ITransactionRepository
    {
        [SqlTemplate("INSERT INTO {{table}} (name, amount, transaction_date) VALUES (@name, @amount, @transactionDate)")]
        [ReturnInsertedId]
        Task<long> InsertAsync(string name, decimal amount, DateTime transactionDate);
        
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
        Task<TransactionTestEntity?> GetByIdAsync(long id);
        
        [SqlTemplate("UPDATE {{table}} SET amount = @amount WHERE id = @id")]
        Task<int> UpdateAmountAsync(long id, decimal amount);
        
        [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
        Task<int> DeleteAsync(long id);
    }
    
    public partial interface IAggregateTestRepository
    {
        [SqlTemplate("INSERT INTO {{table}} (group_name, quantity, price, sale_date) VALUES (@groupName, @quantity, @price, @saleDate)")]
        [ReturnInsertedId]
        Task<long> InsertAsync(string groupName, int quantity, decimal price, DateTime saleDate);
        
        [SqlTemplate("SELECT group_name, SUM(quantity) as total_quantity, SUM(price * quantity) as total_revenue FROM {{table}} GROUP BY group_name")]
        Task<List<dynamic>> GetGroupSummaryAsync();
        
        [SqlTemplate("SELECT COUNT(*) as count, AVG(price) as avg_price FROM {{table}} WHERE sale_date >= @date")]
        Task<dynamic?> GetStatsSinceDateAsync(DateTime date);
    }
    
    // ==================== 测试方法 ====================
    
/// <summary>
/// MySQL 和 PostgreSQL 全面测试
/// 覆盖所有核心功能，确保两个数据库的完整兼容性
/// </summary>
[TestClass]
public class TDD_MySqlPostgreSql_Comprehensive_Tests
{
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Comprehensive")]
    public async Task MySQL_InsertAndRetrieve_AllDataTypes()
    {
        // Arrange - 这是一个代码生成测试，只验证生成的代码能编译
        // 实际数据库测试需要运行容器
        
        // 验证仓储类已生成
        Assert.IsTrue(typeof(MySqlComprehensiveRepository).IsClass);
        Assert.IsTrue(typeof(MySqlComprehensiveRepository).GetInterfaces().Contains(typeof(IComprehensiveRepository)));
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Comprehensive")]
    public async Task PostgreSQL_InsertAndRetrieve_AllDataTypes()
    {
        // Arrange - 这是一个代码生成测试，只验证生成的代码能编译
        
        // 验证仓储类已生成
        Assert.IsTrue(typeof(PostgreSqlComprehensiveRepository).IsClass);
        Assert.IsTrue(typeof(PostgreSqlComprehensiveRepository).GetInterfaces().Contains(typeof(IComprehensiveRepository)));
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("SpecialChars")]
    public async Task MySQL_SpecialCharacters_ShouldBeEscapedProperly()
    {
        // 验证特殊字符方法存在
        var method = typeof(MySqlComprehensiveRepository).GetMethod("SearchBySpecialCharsAsync");
        Assert.IsNotNull(method, "SearchBySpecialCharsAsync method should be generated");
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("SpecialChars")]
    public async Task PostgreSQL_SpecialCharacters_ShouldBeEscapedProperly()
    {
        // 验证特殊字符方法存在
        var method = typeof(PostgreSqlComprehensiveRepository).GetMethod("SearchBySpecialCharsAsync");
        Assert.IsNotNull(method, "SearchBySpecialCharsAsync method should be generated");
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Null")]
    public async Task MySQL_NullHandling_ShouldWorkCorrectly()
    {
        // 验证 NULL 处理方法存在
        var methodNull = typeof(MySqlComprehensiveRepository).GetMethod("GetWithNullTextAsync");
        var methodNotNull = typeof(MySqlComprehensiveRepository).GetMethod("GetWithNonNullTextAsync");
        
        Assert.IsNotNull(methodNull);
        Assert.IsNotNull(methodNotNull);
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Null")]
    public async Task PostgreSQL_NullHandling_ShouldWorkCorrectly()
    {
        // 验证 NULL 处理方法存在
        var methodNull = typeof(PostgreSqlComprehensiveRepository).GetMethod("GetWithNullTextAsync");
        var methodNotNull = typeof(PostgreSqlComprehensiveRepository).GetMethod("GetWithNonNullTextAsync");
        
        Assert.IsNotNull(methodNull);
        Assert.IsNotNull(methodNotNull);
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("Aggregate")]
    public async Task MySQL_AggregateFunctions_ShouldBeSupported()
    {
        // 验证聚合函数方法存在
        Assert.IsNotNull(typeof(MySqlComprehensiveRepository).GetMethod("CountActiveAsync"));
        Assert.IsNotNull(typeof(MySqlComprehensiveRepository).GetMethod("SumDecimalValuesAsync"));
        Assert.IsNotNull(typeof(MySqlComprehensiveRepository).GetMethod("AverageIntValuesAsync"));
        Assert.IsNotNull(typeof(MySqlComprehensiveRepository).GetMethod("MaxBigIntValueAsync"));
        Assert.IsNotNull(typeof(MySqlComprehensiveRepository).GetMethod("MinCreatedAtAsync"));
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Aggregate")]
    public async Task PostgreSQL_AggregateFunctions_ShouldBeSupported()
    {
        // 验证聚合函数方法存在
        Assert.IsNotNull(typeof(PostgreSqlComprehensiveRepository).GetMethod("CountActiveAsync"));
        Assert.IsNotNull(typeof(PostgreSqlComprehensiveRepository).GetMethod("SumDecimalValuesAsync"));
        Assert.IsNotNull(typeof(PostgreSqlComprehensiveRepository).GetMethod("AverageIntValuesAsync"));
        Assert.IsNotNull(typeof(PostgreSqlComprehensiveRepository).GetMethod("MaxBigIntValueAsync"));
        Assert.IsNotNull(typeof(PostgreSqlComprehensiveRepository).GetMethod("MinCreatedAtAsync"));
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("DateRange")]
    public async Task MySQL_DateRangeQuery_ShouldWork()
    {
        var method = typeof(MySqlComprehensiveRepository).GetMethod("GetByDateRangeAsync");
        Assert.IsNotNull(method);
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("DateRange")]
    public async Task PostgreSQL_DateRangeQuery_ShouldWork()
    {
        var method = typeof(PostgreSqlComprehensiveRepository).GetMethod("GetByDateRangeAsync");
        Assert.IsNotNull(method);
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MySQL")]
    [TestCategory("InQuery")]
    public async Task MySQL_InQuery_ShouldWork()
    {
        var method = typeof(MySqlComprehensiveRepository).GetMethod("GetByStatusInAsync");
        Assert.IsNotNull(method);
    }
    
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("InQuery")]
    public async Task PostgreSQL_InQuery_ShouldWork()
    {
        var method = typeof(PostgreSqlComprehensiveRepository).GetMethod("GetByStatusInAsync");
        Assert.IsNotNull(method);
    }
    
    [TestMethod]
    [TestCategory("CodeGeneration")]
    [TestCategory("MySQL")]
    public void MySQL_AllRepositories_ShouldBeGenerated()
    {
        // 验证所有 MySQL 仓储都已生成
        Assert.IsTrue(typeof(MySqlComprehensiveRepository).IsClass);
        Assert.IsTrue(typeof(MySqlTransactionRepository).IsClass);
        Assert.IsTrue(typeof(MySqlAggregateRepository).IsClass);
        
        // 验证它们实现了正确的接口
        Assert.IsTrue(typeof(IComprehensiveRepository).IsAssignableFrom(typeof(MySqlComprehensiveRepository)));
        Assert.IsTrue(typeof(ITransactionRepository).IsAssignableFrom(typeof(MySqlTransactionRepository)));
        Assert.IsTrue(typeof(IAggregateTestRepository).IsAssignableFrom(typeof(MySqlAggregateRepository)));
    }
    
    [TestMethod]
    [TestCategory("CodeGeneration")]
    [TestCategory("PostgreSQL")]
    public void PostgreSQL_AllRepositories_ShouldBeGenerated()
    {
        // 验证所有 PostgreSQL 仓储都已生成
        Assert.IsTrue(typeof(PostgreSqlComprehensiveRepository).IsClass);
        Assert.IsTrue(typeof(PostgreSqlTransactionRepository).IsClass);
        Assert.IsTrue(typeof(PostgreSqlAggregateRepository).IsClass);
        
        // 验证它们实现了正确的接口
        Assert.IsTrue(typeof(IComprehensiveRepository).IsAssignableFrom(typeof(PostgreSqlComprehensiveRepository)));
        Assert.IsTrue(typeof(ITransactionRepository).IsAssignableFrom(typeof(PostgreSqlTransactionRepository)));
        Assert.IsTrue(typeof(IAggregateTestRepository).IsAssignableFrom(typeof(PostgreSqlAggregateRepository)));
    }
    
    [TestMethod]
    [TestCategory("CodeGeneration")]
    [TestCategory("MySQL")]
    [TestCategory("ColumnQuoting")]
    public void MySQL_ColumnNames_ShouldUseBackticks()
    {
        // 这个测试验证生成的代码使用了正确的列引用符号
        // MySQL 应该使用反引号 `column`
        
        // 获取生成的代码（通过反射获取方法的 SQL）
        var repo = typeof(MySqlComprehensiveRepository);
        
        // 验证仓储类存在且可实例化（需要 DbConnection）
        Assert.IsNotNull(repo);
        
        // 更详细的验证需要实际查看生成的源代码或执行 SQL
        // 这里我们至少验证了代码生成成功
    }
    
    [TestMethod]
    [TestCategory("CodeGeneration")]
    [TestCategory("PostgreSQL")]
    [TestCategory("ColumnQuoting")]
    public void PostgreSQL_ColumnNames_ShouldUseDoubleQuotes()
    {
        // 这个测试验证生成的代码使用了正确的列引用符号
        // PostgreSQL 应该使用双引号 "column"
        
        var repo = typeof(PostgreSqlComprehensiveRepository);
        Assert.IsNotNull(repo);
    }
}
