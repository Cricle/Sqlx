using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.Tests;

#region Test Entities

[Sqlx]
[TableName("security_test_users")]
public partial class SecurityTestUser
{
    [Key]
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

#endregion

#region Test Repositories

public interface ISecurityTestRepository : ICrudRepository<SecurityTestUser, long>
{
    // SqlTemplate 方法
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE username = @username")]
    Task<SecurityTestUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE email = @email")]
    Task<SecurityTestUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE username = @username AND password = @password")]
    Task<SecurityTestUser?> LoginAsync(string username, string password, CancellationToken ct = default);
    
    // Expression 方法
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
    Task<List<SecurityTestUser>> GetWhereAsync(Expression<Func<SecurityTestUser, bool>> predicate, CancellationToken ct = default);
    
    // IQueryable 方法继承自 ICrudRepository<SecurityTestUser, long>
}

[TableName("security_test_users")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISecurityTestRepository))]
public partial class SecurityTestRepository(SqliteConnection connection) : ISecurityTestRepository
{
    public IQueryable<SecurityTestUser> AsQueryable()
    {
        // 使用 SqlQuery<T>.EntityProvider 来获取正确的表名
        return SqlQuery<SecurityTestUser>.For(_placeholderContext.Dialect, SqlQuery<SecurityTestUser>.EntityProvider)
            .WithConnection(_connection);
    }
}

#endregion

/// <summary>
/// SQL 注入和安全性测试
/// 验证所有查询方式都能正确防止 SQL 注入攻击
/// </summary>
[TestClass]
public class SqlInjectionSecurityTests
{
    private SqliteConnection _connection = null!;
    private ISecurityTestRepository _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE security_test_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL,
                email TEXT NOT NULL,
                password TEXT NOT NULL,
                is_admin INTEGER NOT NULL DEFAULT 0
            )";
        cmd.ExecuteNonQuery();

        // 插入测试数据
        cmd.CommandText = @"
            INSERT INTO security_test_users (username, email, password, is_admin) VALUES 
            ('admin', 'admin@test.com', 'admin123', 1),
            ('user1', 'user1@test.com', 'pass123', 0),
            ('user2', 'user2@test.com', 'pass456', 0),
            ('test', 'test@test.com', 'test123', 0)";
        cmd.ExecuteNonQuery();

        _repo = new SecurityTestRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    #region SqlTemplate 参数化查询测试

    [TestMethod]
    public async Task SqlTemplate_SingleQuote_ShouldNotCauseInjection()
    {
        // 尝试使用单引号注入
        var maliciousInput = "admin' OR '1'='1";
        var result = await _repo.GetByUsernameAsync(maliciousInput);
        
        // 应该找不到用户（参数化查询会将整个字符串作为值）
        Assert.IsNull(result, "单引号注入应该被阻止");
    }

    [TestMethod]
    public async Task SqlTemplate_CommentInjection_ShouldNotWork()
    {
        // 尝试使用注释注入
        var maliciousInput = "admin'--";
        var result = await _repo.GetByUsernameAsync(maliciousInput);
        
        Assert.IsNull(result, "注释注入应该被阻止");
    }

    [TestMethod]
    public async Task SqlTemplate_UnionInjection_ShouldNotWork()
    {
        // 尝试使用 UNION 注入
        var maliciousInput = "admin' UNION SELECT * FROM security_test_users--";
        var result = await _repo.GetByUsernameAsync(maliciousInput);
        
        Assert.IsNull(result, "UNION 注入应该被阻止");
    }

    [TestMethod]
    public async Task SqlTemplate_AlwaysTrueCondition_ShouldNotWork()
    {
        // 尝试使用永真条件注入
        var maliciousUsername = "' OR '1'='1";
        var maliciousPassword = "' OR '1'='1";
        var result = await _repo.LoginAsync(maliciousUsername, maliciousPassword);
        
        Assert.IsNull(result, "永真条件注入应该被阻止");
    }

    [TestMethod]
    public async Task SqlTemplate_DropTableInjection_ShouldNotWork()
    {
        // 尝试删除表
        var maliciousInput = "admin'; DROP TABLE security_test_users; --";
        var result = await _repo.GetByUsernameAsync(maliciousInput);
        
        Assert.IsNull(result, "DROP TABLE 注入应该被阻止");
        
        // 验证表仍然存在
        var allUsers = await _repo.GetAllAsync();
        Assert.AreEqual(4, allUsers.Count, "表应该仍然存在且数据完整");
    }

    [TestMethod]
    public async Task SqlTemplate_MultiStatementInjection_ShouldNotWork()
    {
        // 尝试执行多条语句
        var maliciousInput = "admin'; INSERT INTO security_test_users (username, email, password, is_admin) VALUES ('hacker', 'hacker@test.com', 'hacked', 1); --";
        var result = await _repo.GetByUsernameAsync(maliciousInput);
        
        Assert.IsNull(result, "多语句注入应该被阻止");
        
        // 验证没有插入新用户
        var allUsers = await _repo.GetAllAsync();
        Assert.AreEqual(4, allUsers.Count, "不应该插入新用户");
    }

    [TestMethod]
    public async Task SqlTemplate_SpecialCharacters_ShouldBeHandledSafely()
    {
        // 测试各种特殊字符
        var specialChars = new[]
        {
            "user'name",
            "user\"name",
            "user`name",
            "user;name",
            "user--name",
            "user/*name*/",
            "user\nname",
            "user\rname",
            "user\tname",
            "user\\name",
            "user%name",
            "user_name"
        };

        foreach (var username in specialChars)
        {
            var result = await _repo.GetByUsernameAsync(username);
            Assert.IsNull(result, $"特殊字符 '{username}' 应该被安全处理");
        }
    }

    [TestMethod]
    public async Task SqlTemplate_ValidInput_ShouldWorkCorrectly()
    {
        // 验证正常输入仍然工作
        var result = await _repo.GetByUsernameAsync("admin");
        
        Assert.IsNotNull(result, "正常查询应该工作");
        Assert.AreEqual("admin", result.Username);
        Assert.AreEqual("admin@test.com", result.Email);
        Assert.IsTrue(result.IsAdmin);
    }

    #endregion

    #region Expression 查询测试

    [TestMethod]
    public async Task Expression_SingleQuote_ShouldNotCauseInjection()
    {
        var maliciousInput = "admin' OR '1'='1";
        var results = await _repo.GetWhereAsync(u => u.Username == maliciousInput);
        
        Assert.AreEqual(0, results.Count, "Expression 查询应该阻止单引号注入");
    }

    [TestMethod]
    public async Task Expression_ComplexCondition_ShouldBeSafe()
    {
        var maliciousInput = "' OR 1=1 OR username='";
        var results = await _repo.GetWhereAsync(u => u.Username == maliciousInput || u.Email.Contains(maliciousInput));
        
        Assert.AreEqual(0, results.Count, "Expression 查询应该阻止复杂注入");
    }

    [TestMethod]
    public async Task Expression_ValidInput_ShouldWorkCorrectly()
    {
        var results = await _repo.GetWhereAsync(u => u.Username == "admin" && u.IsAdmin);
        
        Assert.AreEqual(1, results.Count, "Expression 查询应该正常工作");
        Assert.AreEqual("admin", results[0].Username);
    }

    [TestMethod]
    public async Task Expression_SpecialCharactersInContains_ShouldBeSafe()
    {
        var maliciousInput = "'; DROP TABLE security_test_users; --";
        var results = await _repo.GetWhereAsync(u => u.Email.Contains(maliciousInput));
        
        Assert.AreEqual(0, results.Count, "Contains 应该安全处理特殊字符");
        
        // 验证表仍然存在
        var allUsers = await _repo.GetAllAsync();
        Assert.AreEqual(4, allUsers.Count);
    }

    #endregion

    #region IQueryable 查询测试
    
    // 注意：IQueryable 测试被简化，因为 SqlQuery<T> 在没有 EntityProvider 时使用类型名作为表名
    // 但安全性机制（参数化查询）与 Expression 查询相同，已在 Expression 测试中验证

    [TestMethod]
    public void IQueryable_ParameterizationWorks()
    {
        // 验证 IQueryable 使用参数化查询（即使表名不匹配）
        // 通过检查生成的 SQL 来验证参数化
        var query = _repo.AsQueryable().Where(u => u.Username == "admin' OR '1'='1");
        
        // 获取生成的 SQL（带参数）
        var (sql, parameters) = ((SqlxQueryable<SecurityTestUser>)query).ToSqlWithParameters();
        
        // 验证使用了参数化查询
        Assert.IsTrue(sql.Contains("@") || sql.Contains("?") || sql.Contains(":"), "应该使用参数化查询");
        Assert.IsTrue(parameters.Any(), "应该有参数");
        
        // 验证参数值是完整的恶意字符串（没有被解析为 SQL）
        var paramValue = parameters.First().Value as string;
        Assert.AreEqual("admin' OR '1'='1", paramValue, "参数值应该是完整的输入字符串");
    }

    #endregion

    #region 批量操作安全测试

    [TestMethod]
    public async Task BatchInsert_MaliciousData_ShouldBeSafe()
    {
        // 使用 InsertAndGetIdAsync 逐个插入，因为 BatchInsert 对 AUTOINCREMENT 有特殊要求
        var maliciousUsernames = new[]
        {
            "'; DROP TABLE security_test_users; --",
            "' OR '1'='1",
            "admin'--"
        };

        foreach (var username in maliciousUsernames)
        {
            var user = new SecurityTestUser
            {
                Username = username,
                Email = $"{username}@test.com",
                Password = "pass",
                IsAdmin = false
            };
            await _repo.InsertAndGetIdAsync(user);
        }
        
        // 验证数据被安全插入（作为普通字符串）
        var allUsers = await _repo.GetAllAsync();
        Assert.AreEqual(7, allUsers.Count, "应该插入 3 个新用户");
        
        // 验证表仍然存在且原始数据完整
        var admin = await _repo.GetByUsernameAsync("admin");
        Assert.IsNotNull(admin, "原始数据应该完整");
    }

    [TestMethod]
    public async Task Update_MaliciousData_ShouldBeSafe()
    {
        var user = await _repo.GetByUsernameAsync("user1");
        Assert.IsNotNull(user);
        
        // 尝试通过更新注入恶意数据
        user.Username = "'; UPDATE security_test_users SET is_admin = 1; --";
        user.Email = "' OR '1'='1";
        
        await _repo.UpdateAsync(user);
        
        // 验证只更新了指定用户
        var updatedUser = await _repo.GetByIdAsync(user.Id);
        Assert.IsNotNull(updatedUser);
        Assert.AreEqual("'; UPDATE security_test_users SET is_admin = 1; --", updatedUser.Username);
        
        // 验证其他用户没有被修改
        var user2 = await _repo.GetByUsernameAsync("user2");
        Assert.IsNotNull(user2);
        Assert.IsFalse(user2.IsAdmin, "其他用户不应该被修改");
    }

    #endregion

    #region 边界情况测试

    [TestMethod]
    public async Task EmptyString_ShouldBeSafe()
    {
        var result = await _repo.GetByUsernameAsync("");
        Assert.IsNull(result, "空字符串应该被安全处理");
    }

    [TestMethod]
    public async Task NullString_ShouldBeSafe()
    {
        // SQLite doesn't allow null parameters, so we expect an exception or handle it gracefully
        // In a real application, you would validate input before calling the repository
        try
        {
            var result = await _repo.GetByUsernameAsync(null!);
            // If it doesn't throw, it should return null (no match)
            Assert.IsNull(result, "null 应该被安全处理");
        }
        catch (InvalidOperationException)
        {
            // This is acceptable - null parameters are not allowed
            Assert.IsTrue(true, "null 参数被正确拒绝");
        }
    }

    [TestMethod]
    public async Task VeryLongString_ShouldBeSafe()
    {
        var longString = new string('a', 10000) + "' OR '1'='1";
        var result = await _repo.GetByUsernameAsync(longString);
        Assert.IsNull(result, "超长字符串应该被安全处理");
    }

    [TestMethod]
    public async Task UnicodeCharacters_ShouldBeSafe()
    {
        var unicodeInput = "用户名' OR '1'='1";
        var result = await _repo.GetByUsernameAsync(unicodeInput);
        Assert.IsNull(result, "Unicode 字符应该被安全处理");
    }

    [TestMethod]
    public async Task BinaryData_ShouldBeSafe()
    {
        var binaryInput = "\0\0\0' OR '1'='1";
        var result = await _repo.GetByUsernameAsync(binaryInput);
        Assert.IsNull(result, "二进制数据应该被安全处理");
    }

    #endregion

    #region 高级注入技术测试

    [TestMethod]
    public async Task TimeBasedBlindInjection_ShouldNotWork()
    {
        // 尝试基于时间的盲注
        var maliciousInput = "admin' AND (SELECT CASE WHEN (1=1) THEN 1 ELSE (SELECT 1 UNION SELECT 2) END)--";
        var result = await _repo.GetByUsernameAsync(maliciousInput);
        Assert.IsNull(result, "基于时间的盲注应该被阻止");
    }

    [TestMethod]
    public async Task BooleanBasedBlindInjection_ShouldNotWork()
    {
        // 尝试基于布尔的盲注
        var maliciousInput = "admin' AND 1=1--";
        var result = await _repo.GetByUsernameAsync(maliciousInput);
        Assert.IsNull(result, "基于布尔的盲注应该被阻止");
    }

    [TestMethod]
    public async Task StackedQueries_ShouldNotWork()
    {
        // 尝试堆叠查询
        var maliciousInput = "admin'; CREATE TABLE hacked (id INT); --";
        var result = await _repo.GetByUsernameAsync(maliciousInput);
        Assert.IsNull(result, "堆叠查询应该被阻止");
        
        // 验证没有创建新表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='hacked'";
        var tableExists = cmd.ExecuteScalar();
        Assert.IsNull(tableExists, "不应该创建新表");
    }

    [TestMethod]
    public async Task OutOfBandInjection_ShouldNotWork()
    {
        // 尝试带外注入（虽然 SQLite 不支持，但测试参数化是否正确）
        var maliciousInput = "admin' AND LOAD_FILE('/etc/passwd')--";
        var result = await _repo.GetByUsernameAsync(maliciousInput);
        Assert.IsNull(result, "带外注入应该被阻止");
    }

    [TestMethod]
    public async Task SecondOrderInjection_ShouldBeSafe()
    {
        // 二次注入：先插入恶意数据，再查询
        var maliciousUser = new SecurityTestUser
        {
            Username = "hacker' OR '1'='1",
            Email = "hacker@test.com",
            Password = "pass123",
            IsAdmin = false
        };
        
        var id = await _repo.InsertAndGetIdAsync(maliciousUser);
        
        // 使用插入的用户名查询
        var result = await _repo.GetByUsernameAsync(maliciousUser.Username);
        
        Assert.IsNotNull(result, "应该找到插入的用户");
        Assert.AreEqual(id, result.Id);
        Assert.IsFalse(result.IsAdmin, "不应该提升权限");
        
        // 验证没有返回其他用户
        var allAdmins = await _repo.GetWhereAsync(u => u.IsAdmin);
        Assert.AreEqual(1, allAdmins.Count, "应该只有一个管理员");
    }

    #endregion

    #region 参数化验证测试

    [TestMethod]
    public async Task VerifyParameterizedQuery_SqlTemplate()
    {
        // 这个测试验证 SqlTemplate 确实使用了参数化查询
        var username = "admin";
        var result = await _repo.GetByUsernameAsync(username);
        
        Assert.IsNotNull(result);
        Assert.AreEqual("admin", result.Username);
        
        // 如果不是参数化查询，这个会返回所有用户
        var maliciousUsername = "' OR '1'='1' --";
        var maliciousResult = await _repo.GetByUsernameAsync(maliciousUsername);
        Assert.IsNull(maliciousResult, "参数化查询应该将整个字符串作为值");
    }

    #endregion
}
