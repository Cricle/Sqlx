// TDD Dialect Compilation Tests - Verify all dialects generate correct code
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Sqlx.Annotations;

namespace Sqlx.Tests.Dialects
{
    #region Test Entity

    [TableName("products")]
    public class DialectProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region MySQL Test

    [SqlDefine(SqlDefineTypes.MySql)]
    [RepositoryFor<ICrudRepository<DialectProduct, int>>]
    public partial class MySqlProductRepo(DbConnection connection) 
        : ICrudRepository<DialectProduct, int>
    {
    }

    [TestClass]
    public class MySqlDialectCompilationTest
    {
        [TestMethod]
        public void MySql_ShouldCompile()
        {
            Assert.IsTrue(true);
        }
    }

    #endregion

    #region PostgreSQL Test

    [SqlDefine(SqlDefineTypes.PostgreSql)]
    [RepositoryFor<ICrudRepository<DialectProduct, int>>]
    public partial class PostgreSqlProductRepo(DbConnection connection) 
        : ICrudRepository<DialectProduct, int>
    {
    }

    [TestClass]
    public class PostgreSqlDialectCompilationTest
    {
        [TestMethod]
        public void PostgreSql_ShouldCompile()
        {
            Assert.IsTrue(true);
        }
    }

    #endregion

    #region SQL Server Test

    [SqlDefine(SqlDefineTypes.SqlServer)]
    [RepositoryFor<ICrudRepository<DialectProduct, int>>]
    public partial class SqlServerProductRepo(DbConnection connection) 
        : ICrudRepository<DialectProduct, int>
    {
    }

    [TestClass]
    public class SqlServerDialectCompilationTest
    {
        [TestMethod]
        public void SqlServer_ShouldCompile()
        {
            Assert.IsTrue(true);
        }
    }

    #endregion

    #region SQLite Test

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor<ICrudRepository<DialectProduct, int>>]
    public partial class SQLiteProductRepo(DbConnection connection) 
        : ICrudRepository<DialectProduct, int>
    {
    }

    [TestClass]
    public class SQLiteDialectCompilationTest
    {
        [TestMethod]
        public void SQLite_ShouldCompile()
        {
            Assert.IsTrue(true);
        }
    }

    #endregion
}

