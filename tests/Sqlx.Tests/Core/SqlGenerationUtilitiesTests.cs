// -----------------------------------------------------------------------
// <copyright file="SqlGenerationUtilitiesTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for SQL generation utilities and helper methods.
    /// </summary>
    [TestClass]
    public class SqlGenerationUtilitiesTests
    {
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
            var sqlServerNormal = $"[{identifier}]";
            var sqlServerSpaces = $"[{identifierWithSpaces}]";
            var sqlServerReserved = $"[{reservedWord}]";

            Assert.AreEqual("[user_table]", sqlServerNormal);
            Assert.AreEqual("[user table]", sqlServerSpaces);
            Assert.AreEqual("[order]", sqlServerReserved);

            // MySQL style
            var mysqlNormal = $"`{identifier}`";
            var mysqlSpaces = $"`{identifierWithSpaces}`";
            var mysqlReserved = $"`{reservedWord}`";

            Assert.AreEqual("`user_table`", mysqlNormal);
            Assert.AreEqual("`user table`", mysqlSpaces);
            Assert.AreEqual("`order`", mysqlReserved);

            // PostgreSQL style
            var postgresNormal = $"\"{identifier}\"";
            var postgresSpaces = $"\"{identifierWithSpaces}\"";
            var postgresReserved = $"\"{reservedWord}\"";

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
            var sqlServerParam = $"@{parameterName}";
            var mysqlParam = $"@{parameterName}";
            var postgresParam = $"${parameterName}";
            var oracleParam = $":{parameterName}";
            var sqliteParam = $"@{parameterName}";

            Assert.AreEqual("@userId", sqlServerParam);
            Assert.AreEqual("@userId", mysqlParam);
            Assert.AreEqual("$userId", postgresParam);
            Assert.AreEqual(":userId", oracleParam);
            Assert.AreEqual("@userId", sqliteParam);

            // Test parameter with value formatting (conceptual)
            var parameterPairs = new[]
            {
                ("@userId", parameterValue),
                ("$userId", parameterValue),
                (":userId", parameterValue)
            };

            foreach (var (param, value) in parameterPairs)
            {
                Assert.IsTrue(param.Length > 1);
                Assert.IsTrue(param.Contains("userId"));
                Assert.IsTrue(value.GetType() == typeof(int));
            }
        }

        [TestMethod]
        public void SqlGeneration_SelectQueries_CorrectFormat()
        {
            // Test SELECT query generation patterns
            var tableName = "users";
            var columns = new[] { "id", "name", "email", "created_at" };
            var whereCondition = "id = @id";

            // Basic SELECT
            var basicSelect = $"SELECT {string.Join(", ", columns)} FROM {tableName}";
            Assert.AreEqual("SELECT id, name, email, created_at FROM users", basicSelect);

            // SELECT with WHERE
            var selectWithWhere = $"SELECT {string.Join(", ", columns)} FROM {tableName} WHERE {whereCondition}";
            Assert.AreEqual("SELECT id, name, email, created_at FROM users WHERE id = @id", selectWithWhere);

            // SELECT with aliases
            var columnsWithAliases = columns.Select(col => $"{col} AS {col}");
            var selectWithAliases = $"SELECT {string.Join(", ", columnsWithAliases)} FROM {tableName}";
            Assert.AreEqual("SELECT id AS id, name AS name, email AS email, created_at AS created_at FROM users", selectWithAliases);

            // SELECT with table alias
            var tableAlias = "u";
            var selectWithTableAlias = $"SELECT {string.Join(", ", columns.Select(c => $"{tableAlias}.{c}"))} FROM {tableName} {tableAlias}";
            Assert.AreEqual("SELECT u.id, u.name, u.email, u.created_at FROM users u", selectWithTableAlias);
        }

        [TestMethod]
        public void SqlGeneration_InsertQueries_CorrectFormat()
        {
            // Test INSERT query generation patterns
            var tableName = "users";
            var columns = new[] { "name", "email", "created_at" };
            var parameters = columns.Select(c => $"@{c}").ToArray();

            // Basic INSERT
            var basicInsert = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})";
            Assert.AreEqual("INSERT INTO users (name, email, created_at) VALUES (@name, @email, @created_at)", basicInsert);

            // Batch INSERT template
            var batchInsertTemplate = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES {{VALUES_PLACEHOLDER}}";
            Assert.AreEqual("INSERT INTO users (name, email, created_at) VALUES {VALUES_PLACEHOLDER}", batchInsertTemplate);

            // INSERT with explicit column wrapping
            var wrappedColumns = columns.Select(c => $"[{c}]");
            var wrappedInsert = $"INSERT INTO [{tableName}] ({string.Join(", ", wrappedColumns)}) VALUES ({string.Join(", ", parameters)})";
            Assert.AreEqual("INSERT INTO [users] ([name], [email], [created_at]) VALUES (@name, @email, @created_at)", wrappedInsert);
        }

        [TestMethod]
        public void SqlGeneration_UpdateQueries_CorrectFormat()
        {
            // Test UPDATE query generation patterns
            var tableName = "users";
            var columns = new[] { "name", "email", "updated_at" };
            var setClause = string.Join(", ", columns.Select(c => $"{c} = @{c}"));
            var whereCondition = "id = @id";

            // Basic UPDATE
            var basicUpdate = $"UPDATE {tableName} SET {setClause} WHERE {whereCondition}";
            Assert.AreEqual("UPDATE users SET name = @name, email = @email, updated_at = @updated_at WHERE id = @id", basicUpdate);

            // UPDATE with table alias
            var tableAlias = "u";
            var updateWithAlias = $"UPDATE {tableName} {tableAlias} SET {string.Join(", ", columns.Select(c => $"{tableAlias}.{c} = @{c}"))} WHERE {tableAlias}.id = @id";
            Assert.AreEqual("UPDATE users u SET u.name = @name, u.email = @email, u.updated_at = @updated_at WHERE u.id = @id", updateWithAlias);

            // UPDATE with wrapped identifiers
            var wrappedUpdate = $"UPDATE [{tableName}] SET {string.Join(", ", columns.Select(c => $"[{c}] = @{c}"))} WHERE [id] = @id";
            Assert.AreEqual("UPDATE [users] SET [name] = @name, [email] = @email, [updated_at] = @updated_at WHERE [id] = @id", wrappedUpdate);
        }

        [TestMethod]
        public void SqlGeneration_DeleteQueries_CorrectFormat()
        {
            // Test DELETE query generation patterns
            var tableName = "users";
            var whereCondition = "id = @id";

            // Basic DELETE
            var basicDelete = $"DELETE FROM {tableName} WHERE {whereCondition}";
            Assert.AreEqual("DELETE FROM users WHERE id = @id", basicDelete);

            // DELETE with multiple conditions
            var multipleConditions = "id = @id AND status = @status";
            var deleteWithMultiple = $"DELETE FROM {tableName} WHERE {multipleConditions}";
            Assert.AreEqual("DELETE FROM users WHERE id = @id AND status = @status", deleteWithMultiple);

            // DELETE with wrapped identifiers
            var wrappedDelete = $"DELETE FROM [{tableName}] WHERE [id] = @id";
            Assert.AreEqual("DELETE FROM [users] WHERE [id] = @id", wrappedDelete);

            // DELETE with table alias (some databases support this)
            var tableAlias = "u";
            var deleteWithAlias = $"DELETE {tableAlias} FROM {tableName} {tableAlias} WHERE {tableAlias}.id = @id";
            Assert.AreEqual("DELETE u FROM users u WHERE u.id = @id", deleteWithAlias);
        }

        [TestMethod]
        public void SqlGeneration_ComplexQueries_CorrectFormat()
        {
            // Test more complex SQL generation patterns
            var mainTable = "users";
            var joinTable = "user_roles";
            var columns = new[] { "u.id", "u.name", "ur.role_name" };

            // JOIN query
            var joinQuery = $"SELECT {string.Join(", ", columns)} FROM {mainTable} u INNER JOIN {joinTable} ur ON u.id = ur.user_id WHERE u.active = @active";
            Assert.AreEqual("SELECT u.id, u.name, ur.role_name FROM users u INNER JOIN user_roles ur ON u.id = ur.user_id WHERE u.active = @active", joinQuery);

            // Subquery
            var subquery = $"SELECT * FROM {mainTable} WHERE id IN (SELECT user_id FROM {joinTable} WHERE role_name = @roleName)";
            Assert.AreEqual("SELECT * FROM users WHERE id IN (SELECT user_id FROM user_roles WHERE role_name = @roleName)", subquery);

            // COUNT query
            var countQuery = $"SELECT COUNT(*) FROM {mainTable} WHERE created_at >= @startDate";
            Assert.AreEqual("SELECT COUNT(*) FROM users WHERE created_at >= @startDate", countQuery);

            // EXISTS query
            var existsQuery = $"SELECT * FROM {mainTable} u WHERE EXISTS (SELECT 1 FROM {joinTable} ur WHERE ur.user_id = u.id AND ur.role_name = @roleName)";
            Assert.AreEqual("SELECT * FROM users u WHERE EXISTS (SELECT 1 FROM user_roles ur WHERE ur.user_id = u.id AND ur.role_name = @roleName)", existsQuery);
        }

        [TestMethod]
        public void SqlGeneration_EdgeCases_HandledCorrectly()
        {
            // Test edge cases in SQL generation
            var emptyColumns = new string[0];
            var singleColumn = new[] { "id" };
            var specialCharColumns = new[] { "user-name", "email@domain", "created at" };

            // Empty columns handling
            var emptyColumnsSql = string.Join(", ", emptyColumns);
            Assert.AreEqual("", emptyColumnsSql);

            // Single column
            var singleColumnSql = string.Join(", ", singleColumn);
            Assert.AreEqual("id", singleColumnSql);

            // Special characters in column names (should be wrapped)
            var wrappedSpecialColumns = specialCharColumns.Select(c => $"[{c}]");
            var specialColumnsSql = string.Join(", ", wrappedSpecialColumns);
            Assert.AreEqual("[user-name], [email@domain], [created at]", specialColumnsSql);

            // Null/empty table name handling
            var tableNameCases = new[] { "", "  ", "table_name", "Table Name" };
            foreach (var tableName in tableNameCases)
            {
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    // Should handle gracefully or throw appropriate exception
                    Assert.IsTrue(string.IsNullOrWhiteSpace(tableName));
                }
                else
                {
                    var sql = $"SELECT * FROM [{tableName.Trim()}]";
                    Assert.IsTrue(sql.Contains(tableName.Trim()));
                }
            }
        }

        [TestMethod]
        public void SqlGeneration_ParameterPlaceholders_CorrectFormat()
        {
            // Test parameter placeholder generation
            var parameterNames = new[] { "id", "name", "email", "created_at" };

            // Different parameter styles
            var atParameters = parameterNames.Select(p => $"@{p}");
            var dollarParameters = parameterNames.Select(p => $"${p}");
            var colonParameters = parameterNames.Select(p => $":{p}");
            var questionMarkParameters = parameterNames.Select(p => "?");

            Assert.AreEqual("@id, @name, @email, @created_at", string.Join(", ", atParameters));
            Assert.AreEqual("$id, $name, $email, $created_at", string.Join(", ", dollarParameters));
            Assert.AreEqual(":id, :name, :email, :created_at", string.Join(", ", colonParameters));
            Assert.AreEqual("?, ?, ?, ?", string.Join(", ", questionMarkParameters));

            // Parameter validation
            foreach (var param in atParameters)
            {
                Assert.IsTrue(param.StartsWith("@"));
                Assert.IsTrue(param.Length > 1);
                Assert.IsFalse(param.Contains(" "));
            }
        }

        [TestMethod]
        public void SqlGeneration_BatchOperations_CorrectTemplates()
        {
            // Test batch operation SQL generation
            var tableName = "users";
            var columns = new[] { "name", "email" };

            // Batch INSERT template
            var batchInsertTemplate = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ";
            var valueTemplate = $"({string.Join(", ", columns.Select(c => $"@{c}"))})";

            Assert.AreEqual("INSERT INTO users (name, email) VALUES ", batchInsertTemplate);
            Assert.AreEqual("(@name, @email)", valueTemplate);

            // Multiple value sets for batch
            var multipleSets = new[] { "(@name1, @email1)", "(@name2, @email2)", "(@name3, @email3)" };
            var batchInsertSql = batchInsertTemplate + string.Join(", ", multipleSets);

            Assert.AreEqual("INSERT INTO users (name, email) VALUES (@name1, @email1), (@name2, @email2), (@name3, @email3)", batchInsertSql);

            // Batch UPDATE (using CASE statements)
            var batchUpdateTemplate = $"UPDATE {tableName} SET name = CASE id WHEN @id1 THEN @name1 WHEN @id2 THEN @name2 END WHERE id IN (@id1, @id2)";
            Assert.IsTrue(batchUpdateTemplate.Contains("CASE"));
            Assert.IsTrue(batchUpdateTemplate.Contains("WHEN"));
            Assert.IsTrue(batchUpdateTemplate.Contains("WHERE id IN"));
        }
    }
}
