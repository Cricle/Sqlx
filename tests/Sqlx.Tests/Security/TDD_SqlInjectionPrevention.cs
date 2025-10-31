using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Security;

/// <summary>
/// TDD: SQL注入防护测试
/// 验证Sqlx的参数化查询能够有效防止SQL注入攻击
/// </summary>
[TestClass]
public class TDD_SqlInjectionPrevention
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                username TEXT NOT NULL,
                password TEXT NOT NULL,
                email TEXT
            )
        ");

        _connection.Execute("INSERT INTO users (id, username, password, email) VALUES (1, 'admin', 'secret123', 'admin@test.com')");
        _connection.Execute("INSERT INTO users (id, username, password, email) VALUES (2, 'user1', 'pass456', 'user1@test.com')");
        _connection.Execute("INSERT INTO users (id, username, password, email) VALUES (3, 'user2', 'pass789', 'user2@test.com')");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("经典OR 1=1注入攻击应被阻止")]
    public async Task SqlInjection_Classic_OR_1_Equals_1_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "admin' OR '1'='1";

        // Act
        var result = await repo.GetUserByUsernameAsync(maliciousInput);

        // Assert - 应该返回null，而不是返回所有用户
        Assert.IsNull(result, "SQL注入攻击应被阻止，不应返回任何结果");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("DROP TABLE注入攻击应被阻止")]
    public async Task SqlInjection_DROP_TABLE_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "admin'; DROP TABLE users; --";

        // Act
        var result = await repo.GetUserByUsernameAsync(maliciousInput);

        // Assert - 表应该仍然存在
        Assert.IsNull(result);

        // 验证表仍然存在
        var users = await repo.GetAllUsersAsync();
        Assert.AreEqual(3, users.Count, "表不应被删除");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("UNION注入攻击应被阻止")]
    public async Task SqlInjection_UNION_Attack_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "admin' UNION SELECT id, password, email, 'hacked' FROM users WHERE '1'='1";

        // Act
        var result = await repo.GetUserByUsernameAsync(maliciousInput);

        // Assert
        Assert.IsNull(result, "UNION注入应被阻止");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("注释符注入应被阻止")]
    public async Task SqlInjection_Comment_Attack_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "admin' --";

        // Act
        var result = await repo.GetUserByUsernameAsync(maliciousInput);

        // Assert
        Assert.IsNull(result, "注释符注入应被阻止");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("多语句注入应被阻止")]
    public async Task SqlInjection_MultiStatement_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "admin'; DELETE FROM users; --";

        // Act
        var result = await repo.GetUserByUsernameAsync(maliciousInput);

        // Assert
        Assert.IsNull(result);

        // 验证数据未被删除
        var users = await repo.GetAllUsersAsync();
        Assert.AreEqual(3, users.Count, "数据不应被删除");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("十六进制编码注入应被阻止")]
    public async Task SqlInjection_HexEncoded_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "0x61646D696E"; // 'admin' in hex

        // Act
        var result = await repo.GetUserByUsernameAsync(maliciousInput);

        // Assert
        Assert.IsNull(result, "十六进制编码注入应被阻止");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("时间盲注应被阻止")]
    public async Task SqlInjection_TimeBasedBlind_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "admin' AND SLEEP(5) --";

        // Act
        var startTime = DateTime.Now;
        var result = await repo.GetUserByUsernameAsync(maliciousInput);
        var duration = DateTime.Now - startTime;

        // Assert
        Assert.IsNull(result);
        Assert.IsTrue(duration.TotalSeconds < 1, "不应触发延迟执行");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("堆叠查询注入应被阻止")]
    public async Task SqlInjection_StackedQueries_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "admin'; INSERT INTO users VALUES (999, 'hacker', 'hacked', 'hack@evil.com'); --";

        // Act
        var result = await repo.GetUserByUsernameAsync(maliciousInput);

        // Assert
        Assert.IsNull(result);

        // 验证未插入新数据
        var users = await repo.GetAllUsersAsync();
        Assert.AreEqual(3, users.Count, "不应插入新的恶意数据");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("UPDATE注入应被阻止")]
    public async Task SqlInjection_UPDATE_Attack_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "admin', password='hacked' WHERE '1'='1' --";

        // Act
        var updated = await repo.UpdateEmailAsync(maliciousInput, "newemail@test.com");

        // Assert
        Assert.AreEqual(0, updated, "UPDATE注入应被阻止");

        // 验证密码未被修改
        var admin = await repo.GetUserByIdAsync(1);
        Assert.AreEqual("secret123", admin?.Password, "密码不应被修改");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("带引号闭合的注入应被阻止")]
    public async Task SqlInjection_QuotedClosure_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "admin\" OR \"1\"=\"1";

        // Act
        var result = await repo.GetUserByUsernameAsync(maliciousInput);

        // Assert
        Assert.IsNull(result, "引号闭合注入应被阻止");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("特殊字符组合注入应被正确转义")]
    public async Task SqlInjection_SpecialCharactersCombination_ShouldBeEscaped()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var specialChars = new[] { "'", "\"", "\\", ";", "--", "/*", "*/", "xp_", "sp_", "0x" };

        foreach (var specialChar in specialChars)
        {
            var maliciousInput = $"admin{specialChar}";

            // Act
            var result = await repo.GetUserByUsernameAsync(maliciousInput);

            // Assert
            Assert.IsNull(result, $"特殊字符 '{specialChar}' 注入应被阻止");
        }
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("整数参数注入应被阻止")]
    public async Task SqlInjection_IntegerParameter_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);

        // Act - 传入一个超大整数尝试溢出
        var result = await repo.GetUserByIdAsync(long.MaxValue);

        // Assert
        Assert.IsNull(result, "整数溢出注入应被阻止");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("NULL字节注入应被阻止")]
    public async Task SqlInjection_NullByte_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "admin\0' OR '1'='1";

        // Act
        var result = await repo.GetUserByUsernameAsync(maliciousInput);

        // Assert
        Assert.IsNull(result, "NULL字节注入应被阻止");
    }

    [TestMethod]
    [TestCategory("Security")]
    [TestCategory("SqlInjection")]
    [Description("路径遍历注入应被阻止")]
    public async Task SqlInjection_PathTraversal_ShouldBePrevented()
    {
        // Arrange
        var repo = new SecurityTestRepository(_connection!);
        var maliciousInput = "../../../etc/passwd";

        // Act
        var result = await repo.GetUserByUsernameAsync(maliciousInput);

        // Assert
        Assert.IsNull(result, "路径遍历注入应被阻止");
    }
}

// 测试模型
public class SecurityUser
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
}

// 测试仓储接口
public interface ISecurityTestRepository
{
    [SqlTemplate("SELECT * FROM users WHERE username = @username")]
    Task<SecurityUser?> GetUserByUsernameAsync(string username);

    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<SecurityUser?> GetUserByIdAsync(long id);

    [SqlTemplate("SELECT * FROM users")]
    Task<List<SecurityUser>> GetAllUsersAsync();

    [SqlTemplate("UPDATE users SET email = @email WHERE username = @username")]
    Task<int> UpdateEmailAsync(string username, string email);

    [BatchOperation(MaxBatchSize = 100)]
    [SqlTemplate("INSERT INTO users (id, username, password, email) VALUES {{batch_values}}")]
    Task<int> BatchInsertUsersAsync(IEnumerable<SecurityUser> users);
}

// 测试仓储实现
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISecurityTestRepository))]
public partial class SecurityTestRepository(IDbConnection connection) : ISecurityTestRepository { }

// 扩展方法
public static class SecurityTestExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

