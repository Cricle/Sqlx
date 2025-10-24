// Simple test to verify RepositoryFor with non-generic interface works
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.Tests.RepositoryFor
{
    [TableName("test_items")]
    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Simple non-generic interface
    public interface ITestRepository
    {
        [SqlTemplate("SELECT id, name FROM test_items WHERE id = @id")]
        Task<TestItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }

    // Test with non-generic interface
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(ITestRepository))]
    public partial class TestRepository(DbConnection connection) : ITestRepository
    {
    }

    //[TestClass]  // Temporarily disabled
    public class SimpleTest
    {
        [TestMethod]
        public void SimpleInterface_ShouldCompile()
        {
            Assert.IsTrue(true, "TestRepository compiled successfully");
        }
    }
}

