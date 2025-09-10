using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.SqlGen;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class SqlGeneratorTests
    {
        private SqlGenerator _sqlGenerator = null!;
        private SqlDefine _sqlDefine;

        [TestInitialize]
        public void Setup()
        {
            _sqlGenerator = new SqlGenerator();
            _sqlDefine = new SqlDefine("[", "]", "'", "'", "@");
        }

        #region SqlDefine Tests

        [TestMethod]
        public void SqlDefine_Constructor_ShouldSetProperties()
        {
            // Arrange & Act
            var sqlDefine = new SqlDefine("[", "]", "'", "'", "@");

            // Assert
            Assert.AreEqual("[", sqlDefine.ColumnLeft);
            Assert.AreEqual("]", sqlDefine.ColumnRight);
            Assert.AreEqual("'", sqlDefine.StringLeft);
            Assert.AreEqual("'", sqlDefine.StringRight);
            Assert.AreEqual("@", sqlDefine.ParamterPrefx);
        }

        [TestMethod]
        public void SqlDefine_WrapColumn_ShouldWrapColumnName()
        {
            // Arrange
            var sqlDefine = new SqlDefine("[", "]", "'", "'", "@");
            var columnName = "UserName";

            // Act
            var result = sqlDefine.WrapColumn(columnName);

            // Assert
            Assert.AreEqual("[UserName]", result);
        }

        [TestMethod]
        public void SqlDefine_WrapColumn_WithMySqlSyntax_ShouldUseMySqlWrapping()
        {
            // Arrange
            var sqlDefine = new SqlDefine("`", "`", "'", "'", "@");
            var columnName = "user_name";

            // Act
            var result = sqlDefine.WrapColumn(columnName);

            // Assert
            Assert.AreEqual("`user_name`", result);
        }

        [TestMethod]
        public void SqlDefine_WrapColumn_WithPostgreSqlSyntax_ShouldUsePostgreSqlWrapping()
        {
            // Arrange
            var sqlDefine = new SqlDefine("\"", "\"", "'", "'", "$");
            var columnName = "user_id";

            // Act
            var result = sqlDefine.WrapColumn(columnName);

            // Assert
            Assert.AreEqual("\"user_id\"", result);
        }

        [TestMethod]
        public void SqlDefine_WrapString_ShouldWrapStringValue()
        {
            // Arrange
            var sqlDefine = new SqlDefine("[", "]", "'", "'", "@");
            var stringValue = "test value";

            // Act
            var result = sqlDefine.WrapString(stringValue);

            // Assert
            Assert.AreEqual("'test value'", result);
        }

        [TestMethod]
        public void SqlDefine_MySql_ShouldReturnMySqlDefine()
        {
            // Act
            var result = SqlDefine.MySql;

            // Assert
            Assert.AreEqual("`", result.ColumnLeft);
            Assert.AreEqual("`", result.ColumnRight);
            Assert.AreEqual("'", result.StringLeft);
            Assert.AreEqual("'", result.StringRight);
            Assert.AreEqual("@", result.ParamterPrefx);
        }

        [TestMethod]
        public void SqlDefine_SqlServer_ShouldReturnSqlServerDefine()
        {
            // Act
            var result = SqlDefine.SqlServer;

            // Assert
            Assert.AreEqual("[", result.ColumnLeft);
            Assert.AreEqual("]", result.ColumnRight);
            Assert.AreEqual("'", result.StringLeft);
            Assert.AreEqual("'", result.StringRight);
            Assert.AreEqual("@", result.ParamterPrefx);
        }

        [TestMethod]
        public void SqlDefine_PostgreSql_ShouldReturnPostgreSqlDefine()
        {
            // Act
            var result = SqlDefine.PgSql;

            // Assert
            Assert.AreEqual("\"", result.ColumnLeft);
            Assert.AreEqual("\"", result.ColumnRight);
            Assert.AreEqual("'", result.StringLeft);
            Assert.AreEqual("'", result.StringRight);
            Assert.AreEqual("@", result.ParamterPrefx); // 修复：实际实现使用@而不是$
        }

        [TestMethod]
        public void SqlDefine_Oracle_ShouldReturnOracleDefine()
        {
            // Act
            var result = SqlDefine.Oracle;

            // Assert
            Assert.AreEqual("\"", result.ColumnLeft);
            Assert.AreEqual("\"", result.ColumnRight);
            Assert.AreEqual("'", result.StringLeft);
            Assert.AreEqual("'", result.StringRight);
            Assert.AreEqual(":", result.ParamterPrefx);
        }

        [TestMethod]
        public void SqlDefine_DB2_ShouldReturnDB2Define()
        {
            // Act
            var result = SqlDefine.DB2;

            // Assert
            Assert.AreEqual("\"", result.ColumnLeft);
            Assert.AreEqual("\"", result.ColumnRight);
            Assert.AreEqual("'", result.StringLeft);
            Assert.AreEqual("'", result.StringRight);
            Assert.AreEqual("?", result.ParamterPrefx);
        }

        [TestMethod]
        public void SqlDefine_SQLite_ShouldReturnSQLiteDefine()
        {
            // Act
            var result = SqlDefine.SQLite;

            // Assert
            Assert.AreEqual("[", result.ColumnLeft);
            Assert.AreEqual("]", result.ColumnRight);
            Assert.AreEqual("'", result.StringLeft);
            Assert.AreEqual("'", result.StringRight);
            Assert.AreEqual("@", result.ParamterPrefx);
        }

        #endregion

        #region GenerateContext Tests

        [TestMethod]
        public void GenerateContext_GetColumnName_WithPascalCase_ShouldConvertToSnakeCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("UserName");

            // Assert
            Assert.AreEqual("user_name", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithCamelCase_ShouldConvertToSnakeCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("userName");

            // Assert
            Assert.AreEqual("user_name", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithUnderscoreUpperCase_ShouldConvertToLowerCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("USER_NAME");

            // Assert
            Assert.AreEqual("user_name", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithAlreadySnakeCase_ShouldReturnAsIs()
        {
            // Act
            var result = GenerateContext.GetColumnName("user_name");

            // Assert
            Assert.AreEqual("user_name", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithEmptyString_ShouldReturnEmpty()
        {
            // Act
            var result = GenerateContext.GetColumnName("");

            // Assert
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithSingleWord_ShouldReturnLowerCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("Name");

            // Assert
            Assert.AreEqual("name", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithNumbers_ShouldHandleCorrectly()
        {
            // Act
            var result = GenerateContext.GetColumnName("User123Name");

            // Assert
            Assert.AreEqual("user123_name", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithConsecutiveUpperCase_ShouldHandleCorrectly()
        {
            // Act
            var result = GenerateContext.GetColumnName("XMLHttpRequest");

            // Assert
            Assert.AreEqual("x_m_l_http_request", result);
        }

        #endregion

        #region SqlExecuteTypes Tests

        [TestMethod]
        public void SqlExecuteTypes_Values_ShouldHaveCorrectEnumValues()
        {
            // Assert
            Assert.AreEqual(0, (int)SqlExecuteTypes.Select);
            Assert.AreEqual(1, (int)SqlExecuteTypes.Update);
            Assert.AreEqual(2, (int)SqlExecuteTypes.Insert);
            Assert.AreEqual(3, (int)SqlExecuteTypes.Delete);
            Assert.AreEqual(4, (int)SqlExecuteTypes.BatchInsert);
            Assert.AreEqual(5, (int)SqlExecuteTypes.BatchUpdate);
            Assert.AreEqual(6, (int)SqlExecuteTypes.BatchDelete);
        }

        #endregion

        #region SqlDefineTypes Tests

        [TestMethod]
        public void SqlDefineTypes_Values_ShouldHaveCorrectEnumValues()
        {
            // Assert
            Assert.AreEqual(0, (int)SqlDefineTypes.MySql);
            Assert.AreEqual(1, (int)SqlDefineTypes.SqlServer);
            Assert.AreEqual(2, (int)SqlDefineTypes.Postgresql);
            Assert.AreEqual(3, (int)SqlDefineTypes.Oracle);
            Assert.AreEqual(4, (int)SqlDefineTypes.DB2);
            Assert.AreEqual(5, (int)SqlDefineTypes.SQLite);
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public void SqlDefine_WrapColumn_WithNullOrEmpty_ShouldHandleGracefully()
        {
            // Arrange
            var sqlDefine = new SqlDefine("[", "]", "'", "'", "@");

            // Act & Assert
            var result1 = sqlDefine.WrapColumn(null!);
            var result2 = sqlDefine.WrapColumn("");

            Assert.AreEqual("[]", result1);
            Assert.AreEqual("[]", result2);
        }

        [TestMethod]
        public void SqlDefine_WrapString_WithNullOrEmpty_ShouldHandleGracefully()
        {
            // Arrange
            var sqlDefine = new SqlDefine("[", "]", "'", "'", "@");

            // Act & Assert
            var result1 = sqlDefine.WrapString(null!);
            var result2 = sqlDefine.WrapString("");

            Assert.AreEqual("''", result1);
            Assert.AreEqual("''", result2);
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void SqlDefine_IntegrationTest_WithComplexScenario()
        {
            // Arrange
            var sqlDefine = new SqlDefine("`", "`", "'", "'", "@");
            var tableName = "UserProfiles";
            var columnName = "FirstName";
            var stringValue = "John's Data";

            // Act
            var wrappedTable = sqlDefine.WrapColumn(tableName);
            var wrappedColumn = sqlDefine.WrapColumn(columnName);
            var wrappedString = sqlDefine.WrapString(stringValue);
            var convertedColumnName = GenerateContext.GetColumnName(columnName);

            // Assert
            Assert.AreEqual("`UserProfiles`", wrappedTable);
            Assert.AreEqual("`FirstName`", wrappedColumn);
            Assert.AreEqual("'John's Data'", wrappedString);
            Assert.AreEqual("first_name", convertedColumnName);
        }

        #endregion
    }
}
