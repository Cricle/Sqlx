// TDD Phase 1 - RED: 测试失败，因为功能尚未实现
// 目标：确认ReturnInsertedId特性能被源生成器识别并生成适当的代码

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.InsertReturning
{
    /// <summary>
    /// TDD红灯阶段 - Insert返回ID功能测试
    /// 这些测试当前会失败，因为源生成器尚未实现[ReturnInsertedId]特性的处理
    /// </summary>
    [TestClass]
    public class TDD_Phase1_ReturnInsertedId_RedTests : CodeGenerationTestBase
    {
        [TestMethod]
        [TestCategory("TDD-Red")]
        [TestCategory("Insert-ReturnId")]
        [TestCategory("PostgreSQL")]
        public void PostgreSQL_InsertAndGetId_Should_Generate_RETURNING_Clause()
        {
            // Arrange
            var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = """";
    }

    [TableName(""users"")]
    [SqlDefine(SqlDefineTypes.PostgreSql)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly IDbConnection _connection;
        public UserRepository(IDbConnection connection) => _connection = connection;
    }

    public interface IUserRepository
    {
        [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})"")]
        [ReturnInsertedId]
        Task<long> InsertAndGetIdAsync(User entity);
    }
}";

            // Act
            var generatedCode = GetCSharpGeneratedOutput(source);

            // Assert
            // PostgreSQL应该生成RETURNING id子句
            StringAssert.Contains(generatedCode, "RETURNING", "应该包含RETURNING子句");

            // 应该使用ExecuteScalarAsync获取返回值
            StringAssert.Contains(generatedCode, "ExecuteScalarAsync", "应该使用ExecuteScalarAsync");

            // 应该返回Task<long>
            StringAssert.Contains(generatedCode, "Task<long>", "方法签名应该返回Task<long>");
        }

        [TestMethod]
        [TestCategory("TDD-Red")]
        [TestCategory("Insert-ReturnId")]
        [TestCategory("SqlServer")]
        public void SqlServer_InsertAndGetId_Should_Generate_OUTPUT_Clause()
        {
            var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = """";
    }

    [TableName(""users"")]
    [SqlDefine(SqlDefineTypes.SqlServer)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly IDbConnection _connection;
        public UserRepository(IDbConnection connection) => _connection = connection;
    }

    public interface IUserRepository
    {
        [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})"")]
        [ReturnInsertedId]
        Task<long> InsertAndGetIdAsync(User entity);
    }
}";

            var generatedCode = GetCSharpGeneratedOutput(source);

            // SQL Server应该生成OUTPUT INSERTED.id
            StringAssert.Contains(generatedCode, "OUTPUT", "应该包含OUTPUT子句");
            StringAssert.Contains(generatedCode, "INSERTED", "应该包含INSERTED关键字");
        }

        [TestMethod]
        [TestCategory("TDD-Red")]
        [TestCategory("AOT")]
        public void ReturnInsertedId_Should_Be_AOT_Friendly_No_Reflection()
        {
            var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = """";
    }

    [TableName(""users"")]
    [SqlDefine(SqlDefineTypes.PostgreSql)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly IDbConnection _connection;
        public UserRepository(IDbConnection connection) => _connection = connection;
    }

    public interface IUserRepository
    {
        [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})"")]
        [ReturnInsertedId]
        Task<long> InsertAndGetIdAsync(User entity);
    }
}";

            var generatedCode = GetCSharpGeneratedOutput(source);

            // 应该不包含反射相关的代码（AOT友好）
            Assert.IsFalse(generatedCode.Contains("GetType()"), "不应使用GetType()");
            Assert.IsFalse(generatedCode.Contains("typeof(User)"), "不应使用typeof()进行反射");
            Assert.IsFalse(generatedCode.Contains("Activator.CreateInstance"), "不应使用Activator");
            Assert.IsFalse(generatedCode.Contains("PropertyInfo"), "不应使用PropertyInfo");

            // 应该直接访问属性
            StringAssert.Contains(generatedCode, "entity.Name", "应该直接访问entity.Name属性");
        }

        [TestMethod]
        [TestCategory("TDD-Red")]
        [TestCategory("GC-Optimization")]
        public void ReturnInsertedId_With_ValueTask_Should_Generate_ValueTask_Return()
        {
            var source = @"
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = """";
    }

    [TableName(""users"")]
    [SqlDefine(SqlDefineTypes.PostgreSql)]
    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly IDbConnection _connection;
        public UserRepository(IDbConnection connection) => _connection = connection;
    }

    public interface IUserRepository
    {
        [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})"")]
        [ReturnInsertedId(UseValueTask = true)]
        ValueTask<long> InsertAndGetIdAsync(User entity);
    }
}";

            var generatedCode = GetCSharpGeneratedOutput(source);

            // 应该返回ValueTask而不是Task（GC优化）
            StringAssert.Contains(generatedCode, "ValueTask<long>", "应该返回ValueTask<long>");
        }
    }
}

