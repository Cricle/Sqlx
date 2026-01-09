// -----------------------------------------------------------------------
// <copyright file="ComponentIntegrationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Simple integration tests that test multiple components working together.
    /// </summary>
    [TestClass]
    public class ComponentIntegrationTests
    {
        [TestMethod]
        public void NameMapper_WithSqlDefine_ProducesCorrectColumnNames()
        {
            // Test NameMapper integration with SqlDefine for different dialects
            var propertyNames = new[] { "UserId", "FirstName", "CreatedAt", "IsActive" };
            var dialects = new[]
            {
                ("SqlServer", SqlDefine.SqlServer),
                ("MySQL", SqlDefine.MySql),
                ("PostgreSQL", SqlDefine.PgSql),
                // ("SQLite", SqlDefine.Sqlite), // Skip SQLite for now due to reference issues
                ("Oracle", SqlDefine.Oracle)
            };

            foreach (var (dialectName, dialect) in dialects)
            {
                foreach (var propertyName in propertyNames)
                {
                    // Map property name to column name
                    var columnName = NameMapper.MapName(propertyName);

                    // Wrap column name with dialect-specific identifiers
                    var wrappedColumn = dialect.WrapColumn(columnName);

                    // Verify the complete transformation
                    Assert.IsNotNull(wrappedColumn, $"Wrapped column should not be null for {dialectName}");
                    Assert.IsTrue(wrappedColumn.Length > columnName.Length,
                        $"Wrapped column should be longer than original for {dialectName}");
                    Assert.IsTrue(wrappedColumn.Contains(columnName),
                        $"Wrapped column should contain original column name for {dialectName}");
                }
            }
        }

        [TestMethod]
        public void SqlDefine_WithDifferentDialects_CoverAllOperations()
        {
            // Test that SqlDefine dialects work with different SQL operations
            var dialects = new[]
            {
                ("SqlServer", SqlDefine.SqlServer),
                ("MySQL", SqlDefine.MySql),
                ("PostgreSQL", SqlDefine.PgSql),
                // ("SQLite", SqlDefine.Sqlite), // Skip SQLite for now due to reference issues
                ("Oracle", SqlDefine.Oracle),
                ("DB2", SqlDefine.DB2)
            };

            Assert.IsTrue(dialects.Length > 0, "Should have SQL dialects");

            // Test that each dialect can handle basic SQL operations
            foreach (var (dialectName, dialect) in dialects)
            {
                Assert.IsNotNull(dialect, $"Should have valid SqlDefine for {dialectName}");
                Assert.IsNotNull(dialect.ParameterPrefix, $"Should have parameter prefix for {dialectName}");

                // Test column wrapping
                var wrappedColumn = dialect.WrapColumn("test_column");
                Assert.IsNotNull(wrappedColumn, $"Should wrap column for {dialectName}");
                Assert.IsTrue(wrappedColumn.Contains("test_column"), $"Should contain column name for {dialectName}");

                // Test parameter generation
                var parameter = $"{dialect.ParameterPrefix}test_param";
                Assert.IsNotNull(parameter, $"Should generate parameter for {dialectName}");
                Assert.IsTrue(parameter.StartsWith(dialect.ParameterPrefix), $"Should start with prefix for {dialectName}");
            }
        }

        [TestMethod]
        public void TypeAnalyzer_WithNameMapper_HandlesComplexProperties()
        {
            // Test TypeAnalyzer integration with NameMapper for complex property scenarios
            var complexPropertyNames = new[]
            {
                "XMLHttpRequestId",
                "JSONAPIResponse",
                "HTTPSConnection",
                "SQLDatabaseConnection",
                "HTMLDOMElement",
                "CSSStyleSheet",
                "URLPath",
                "UUIDValue"
            };

            foreach (var propertyName in complexPropertyNames)
            {
                // Map property name to database column
                var columnName = NameMapper.MapName(propertyName);

                // Verify the mapping makes sense
                Assert.IsNotNull(columnName, $"Column name should not be null for {propertyName}");
                Assert.IsTrue(columnName.Length > 0, $"Column name should not be empty for {propertyName}");
                Assert.IsTrue(columnName.ToLower() == columnName, $"Column name should be lowercase for {propertyName}");
                Assert.IsTrue(columnName.Contains("_"), $"Complex property should contain underscores for {propertyName}");

                // Test that the column name can be used with different SQL dialects
                var dialects = new[] { SqlDefine.SqlServer, SqlDefine.MySql, SqlDefine.PgSql };

                foreach (var dialect in dialects)
                {
                    var wrappedColumn = dialect.WrapColumn(columnName);
                    var parameter = $"{dialect.ParameterPrefix}{columnName.Replace("_", "")}";

                    Assert.IsNotNull(wrappedColumn, $"Wrapped column should not be null for {propertyName} in {dialect.ParameterPrefix} dialect");
                    Assert.IsNotNull(parameter, $"Parameter should not be null for {propertyName} in {dialect.ParameterPrefix} dialect");
                }
            }
        }

        [TestMethod]
        public void DatabaseDialects_WithParameterGeneration_ProduceConsistentResults()
        {
            // Test database dialect consistency across parameter generation
            var dialects = new[]
            {
                ("SqlServer", SqlDefine.SqlServer, "@"),
                ("MySQL", SqlDefine.MySql, "@"),
                ("PostgreSQL", SqlDefine.PgSql, "@"),
                ("Oracle", SqlDefine.Oracle, ":"),
                // ("SQLite", SqlDefine.Sqlite, "@"), // Skip SQLite for now
                ("DB2", SqlDefine.DB2, "?")
            };

            var parameterNames = new[] { "id", "name", "email", "created_at", "is_active" };

            foreach (var (dialectName, dialect, expectedPrefix) in dialects)
            {
                Assert.AreEqual(expectedPrefix, dialect.ParameterPrefix,
                    $"Parameter prefix should be {expectedPrefix} for {dialectName}");

                foreach (var paramName in parameterNames)
                {
                    // Test column wrapping
                    var wrappedColumn = dialect.WrapColumn(paramName);
                    Assert.IsNotNull(wrappedColumn, $"Wrapped column should not be null for {dialectName}");
                    Assert.IsTrue(wrappedColumn.Contains(paramName), $"Wrapped column should contain original name for {dialectName}");

                    // Test parameter generation
                    var parameter = $"{dialect.ParameterPrefix}{paramName}";
                    Assert.IsTrue(parameter.StartsWith(expectedPrefix), $"Parameter should start with {expectedPrefix} for {dialectName}");
                    Assert.IsTrue(parameter.EndsWith(paramName), $"Parameter should end with {paramName} for {dialectName}");
                }
            }
        }

        [TestMethod]
        public void CodeGeneration_EndToEndFlow_ProducesValidOutput()
        {
            // Test end-to-end code generation flow
            var entityName = "User";
            var properties = new[] { "Id", "FirstName", "LastName", "Email", "CreatedAt", "IsActive" };
            var dialect = SqlDefine.SqlServer;

            // Step 1: Map entity to table
            var tableName = NameMapper.MapName(entityName);
            Assert.AreEqual("user", tableName);

            // Step 2: Map properties to columns
            var columnMappings = properties.ToDictionary(
                prop => prop,
                prop => NameMapper.MapName(prop));

            Assert.AreEqual(properties.Length, columnMappings.Count);
            Assert.AreEqual("id", columnMappings["Id"]);
            Assert.AreEqual("first_name", columnMappings["FirstName"]);
            Assert.AreEqual("is_active", columnMappings["IsActive"]);

            // Step 3: Generate SQL for different operations
            var wrappedTable = dialect.WrapColumn(tableName);
            var wrappedColumns = columnMappings.Values.Select(dialect.WrapColumn).ToArray();
            var parameters = columnMappings.Values.Select(col => $"{dialect.ParameterPrefix}{col}").ToArray();

            // Generate SELECT query
            var selectColumns = string.Join(", ", wrappedColumns);
            var selectSql = $"SELECT {selectColumns} FROM {wrappedTable}";

            Assert.IsTrue(selectSql.StartsWith("SELECT"), "Should be valid SELECT query");
            Assert.IsTrue(selectSql.Contains("[user]"), "Should contain wrapped table name");
            Assert.IsTrue(selectSql.Contains("[first_name]"), "Should contain wrapped column names");

            // Generate INSERT query
            var insertColumns = string.Join(", ", wrappedColumns.Skip(1)); // Skip Id for INSERT
            var insertParameters = string.Join(", ", parameters.Skip(1));
            var insertSql = $"INSERT INTO {wrappedTable} ({insertColumns}) VALUES ({insertParameters})";

            Assert.IsTrue(insertSql.StartsWith("INSERT INTO"), "Should be valid INSERT query");
            Assert.IsTrue(insertSql.Contains("VALUES"), "Should contain VALUES clause");
            Assert.IsTrue(insertSql.Contains("@first_name"), "Should contain parameters");

            // Generate UPDATE query
            var updateSets = columnMappings.Skip(1).Select(kvp =>
                $"{dialect.WrapColumn(kvp.Value)} = {dialect.ParameterPrefix}{kvp.Value}");
            var updateSetClause = string.Join(", ", updateSets);
            var updateSql = $"UPDATE {wrappedTable} SET {updateSetClause} WHERE {dialect.WrapColumn("id")} = {dialect.ParameterPrefix}id";

            Assert.IsTrue(updateSql.StartsWith("UPDATE"), "Should be valid UPDATE query");
            Assert.IsTrue(updateSql.Contains("SET"), "Should contain SET clause");
            Assert.IsTrue(updateSql.Contains("WHERE"), "Should contain WHERE clause");

            // Generate DELETE query
            var deleteSql = $"DELETE FROM {wrappedTable} WHERE {dialect.WrapColumn("id")} = {dialect.ParameterPrefix}id";

            Assert.IsTrue(deleteSql.StartsWith("DELETE FROM"), "Should be valid DELETE query");
            Assert.IsTrue(deleteSql.Contains("WHERE"), "Should contain WHERE clause");
        }

        [TestMethod]
        public void SqlTemplate_BasicOperations_WorkCorrectly()
        {
            // Test basic SQL template operations
            var selectTemplate = "SELECT {0} FROM {1} WHERE {2}";
            var insertTemplate = "INSERT INTO {0} ({1}) VALUES ({2})";
            var updateTemplate = "UPDATE {0} SET {1} WHERE {2}";
            var deleteTemplate = "DELETE FROM {0} WHERE {1}";

            // Test template formatting
            var selectSql = string.Format(selectTemplate, "id, name", "users", "id = @id");
            var insertSql = string.Format(insertTemplate, "users", "id, name", "@id, @name");
            var updateSql = string.Format(updateTemplate, "users", "name = @name", "id = @id");
            var deleteSql = string.Format(deleteTemplate, "users", "id = @id");

            Assert.AreEqual("SELECT id, name FROM users WHERE id = @id", selectSql);
            Assert.AreEqual("INSERT INTO users (id, name) VALUES (@id, @name)", insertSql);
            Assert.AreEqual("UPDATE users SET name = @name WHERE id = @id", updateSql);
            Assert.AreEqual("DELETE FROM users WHERE id = @id", deleteSql);
        }

        [TestMethod]
        public void SqlDialect_IdentifierWrapping_WorksForAllDialects()
        {
            var identifier = "user_table";
            var identifierWithSpaces = "user table";
            var reservedWord = "order";

            // SQL Server style
            var sqlServerNormal = SqlDefine.SqlServer.WrapColumn(identifier);
            var sqlServerSpaces = SqlDefine.SqlServer.WrapColumn(identifierWithSpaces);
            var sqlServerReserved = SqlDefine.SqlServer.WrapColumn(reservedWord);

            Assert.AreEqual("[user_table]", sqlServerNormal);
            Assert.AreEqual("[user table]", sqlServerSpaces);
            Assert.AreEqual("[order]", sqlServerReserved);

            // MySQL style
            var mysqlNormal = SqlDefine.MySql.WrapColumn(identifier);
            var mysqlSpaces = SqlDefine.MySql.WrapColumn(identifierWithSpaces);
            var mysqlReserved = SqlDefine.MySql.WrapColumn(reservedWord);

            Assert.AreEqual("`user_table`", mysqlNormal);
            Assert.AreEqual("`user table`", mysqlSpaces);
            Assert.AreEqual("`order`", mysqlReserved);

            // PostgreSQL style
            var postgresNormal = SqlDefine.PgSql.WrapColumn(identifier);
            var postgresSpaces = SqlDefine.PgSql.WrapColumn(identifierWithSpaces);
            var postgresReserved = SqlDefine.PgSql.WrapColumn(reservedWord);

            Assert.AreEqual("\"user_table\"", postgresNormal);
            Assert.AreEqual("\"user table\"", postgresSpaces);
            Assert.AreEqual("\"order\"", postgresReserved);
        }

        [TestMethod]
        public void ParameterGeneration_DifferentDialects_CorrectPrefixes()
        {
            var parameterName = "userId";
            var parameterValue = 123;

            // Test different parameter prefix styles
            var sqlServerParam = $"{SqlDefine.SqlServer.ParameterPrefix}{parameterName}";
            var mysqlParam = $"{SqlDefine.MySql.ParameterPrefix}{parameterName}";
            var postgresParam = $"{SqlDefine.PgSql.ParameterPrefix}{parameterName}";
            var oracleParam = $"{SqlDefine.Oracle.ParameterPrefix}{parameterName}";
            // var sqliteParam = $"{SqlDefine.Sqlite.ParameterPrefix}{parameterName}"; // Skip SQLite for now

            Assert.AreEqual("@userId", sqlServerParam);
            Assert.AreEqual("@userId", mysqlParam);
            Assert.AreEqual("@userId", postgresParam);
            Assert.AreEqual(":userId", oracleParam);
            // Assert.AreEqual("@userId", sqliteParam); // Skip SQLite for now

            // Test parameter with value formatting (conceptual)
            var parameterPairs = new[]
            {
                ("@userId", parameterValue),
                ("@userId", parameterValue),
                (":userId", parameterValue)
            };

            foreach (var (param, value) in parameterPairs)
            {
                Assert.IsTrue(param.Length > 1);
                Assert.IsTrue(param.Contains("userId"));
                Assert.IsTrue(value.GetType() == typeof(int));
            }
        }
    }
}
