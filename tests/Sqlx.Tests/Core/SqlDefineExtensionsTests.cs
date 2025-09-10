// -----------------------------------------------------------------------
// <copyright file="SqlDefineExtensionsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Core;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class SqlDefineExtensionsTests
{
    [TestMethod]
    public void WrapColumns_WithMultipleColumns_ReturnsWrappedArray()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;
        var columns = new[] { "id", "name", "email" };

        // Act
        var result = sqlDefine.WrapColumns(columns);

        // Assert
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual("`id`", result[0]);
        Assert.AreEqual("`name`", result[1]);
        Assert.AreEqual("`email`", result[2]);
    }

    [TestMethod]
    public void WrapColumns_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var sqlDefine = SqlDefine.SqlServer;

        // Act
        var result = sqlDefine.WrapColumns();

        // Assert
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void WrapColumns_WithNullArray_ReturnsEmptyArray()
    {
        // Arrange
        var sqlDefine = SqlDefine.PgSql;

        // Act
        var result = sqlDefine.WrapColumns(null!);

        // Assert
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void WrapAndJoinColumns_WithMultipleColumns_ReturnsCommaSeparated()
    {
        // Arrange
        var sqlDefine = SqlDefine.SqlServer;
        var columns = new[] { "id", "name", "email" };

        // Act
        var result = sqlDefine.WrapAndJoinColumns(columns);

        // Assert
        Assert.AreEqual("[id], [name], [email]", result);
    }

    [TestMethod]
    public void WrapAndJoinColumns_WithCollection_ReturnsCommaSeparated()
    {
        // Arrange
        var sqlDefine = SqlDefine.PgSql;
        var columns = new[] { "user_id", "first_name" }.AsEnumerable();

        // Act
        var result = sqlDefine.WrapAndJoinColumns(columns);

        // Assert
        Assert.AreEqual("\"user_id\", \"first_name\"", result);
    }

    [TestMethod]
    public void WrapAndJoinColumns_WithNullCollection_ReturnsEmpty()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;

        // Act
        var result = sqlDefine.WrapAndJoinColumns((IEnumerable<string>?)null);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void CreateParameter_WithParameterName_ReturnsWithPrefix()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;

        // Act
        var result = sqlDefine.CreateParameter("userId");

        // Assert
        Assert.AreEqual("@userId", result);
    }

    [TestMethod]
    public void CreateParameter_WithSQLite_HandlesSpecialCase()
    {
        // Arrange
        var sqlDefine = SqlDefine.SQLite;

        // Act
        var result = sqlDefine.CreateParameter("userId");

        // Assert
        Assert.AreEqual("@userId", result); // Should use @ not @sqlite
    }

    [TestMethod]
    public void CreateParameter_WithEmptyName_ReturnsOnlyPrefix()
    {
        // Arrange
        var sqlDefine = SqlDefine.PgSql;

        // Act
        var result = sqlDefine.CreateParameter(string.Empty);

        // Assert
        Assert.AreEqual("$", result);
    }

    [TestMethod]
    public void CreateParameters_WithMultipleNames_ReturnsArray()
    {
        // Arrange
        var sqlDefine = SqlDefine.SqlServer;
        var parameters = new[] { "id", "name", "email" };

        // Act
        var result = sqlDefine.CreateParameters(parameters);

        // Assert
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual("@id", result[0]);
        Assert.AreEqual("@name", result[1]);
        Assert.AreEqual("@email", result[2]);
    }

    [TestMethod]
    public void CreateAndJoinParameters_WithMultipleNames_ReturnsCommaSeparated()
    {
        // Arrange
        var sqlDefine = SqlDefine.PgSql;
        var parameters = new[] { "id", "name" };

        // Act
        var result = sqlDefine.CreateAndJoinParameters(parameters);

        // Assert
        Assert.AreEqual("$id, $name", result);
    }

    [TestMethod]
    public void CreateAndJoinParameters_WithCollection_ReturnsCommaSeparated()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;
        var parameters = new[] { "user_id", "email" }.AsEnumerable();

        // Act
        var result = sqlDefine.CreateAndJoinParameters(parameters);

        // Assert
        Assert.AreEqual("@user_id, @email", result);
    }

    [TestMethod]
    public void CreateSetClauses_WithColumns_ReturnsSetStatements()
    {
        // Arrange
        var sqlDefine = SqlDefine.SqlServer;
        var columns = new[] { "name", "email" };

        // Act
        var result = sqlDefine.CreateSetClauses(columns);

        // Assert
        Assert.AreEqual("[name] = @name, [email] = @email", result);
    }

    [TestMethod]
    public void CreateSetClauses_WithCollection_ReturnsSetStatements()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;
        var columns = new[] { "first_name", "last_name" }.AsEnumerable();

        // Act
        var result = sqlDefine.CreateSetClauses(columns);

        // Assert
        Assert.AreEqual("`first_name` = @first_name, `last_name` = @last_name", result);
    }

    [TestMethod]
    public void CreateSetClauses_WithEmptyArray_ReturnsEmpty()
    {
        // Arrange
        var sqlDefine = SqlDefine.PgSql;

        // Act
        var result = sqlDefine.CreateSetClauses();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void CreateWhereConditions_WithColumns_ReturnsConditions()
    {
        // Arrange
        var sqlDefine = SqlDefine.SqlServer;
        var columns = new[] { "id", "status" };

        // Act
        var result = sqlDefine.CreateWhereConditions(columns);

        // Assert
        Assert.AreEqual("[id] = @id AND [status] = @status", result);
    }

    [TestMethod]
    public void CreateWhereConditions_WithCollection_ReturnsConditions()
    {
        // Arrange
        var sqlDefine = SqlDefine.PgSql;
        var columns = new[] { "user_id", "is_active" }.AsEnumerable();

        // Act
        var result = sqlDefine.CreateWhereConditions(columns);

        // Assert
        Assert.AreEqual("\"user_id\" = $user_id AND \"is_active\" = $is_active", result);
    }

    [TestMethod]
    public void CreateWhereConditions_WithEmptyArray_ReturnsEmpty()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;

        // Act
        var result = sqlDefine.CreateWhereConditions();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void UsesParameterPrefix_WithMatchingPrefix_ReturnsTrue()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;

        // Act
        var result = sqlDefine.UsesParameterPrefix("@");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void UsesParameterPrefix_WithNonMatchingPrefix_ReturnsFalse()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;

        // Act
        var result = sqlDefine.UsesParameterPrefix("$");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void UsesParameterPrefix_WithSQLiteSpecialCase_ReturnsTrue()
    {
        // Arrange
        var sqlDefine = SqlDefine.SQLite;

        // Act
        var result1 = sqlDefine.UsesParameterPrefix("@");
        var result2 = sqlDefine.UsesParameterPrefix("@sqlite");

        // Assert
        Assert.IsTrue(result1, "SQLite should match @ prefix");
        Assert.IsTrue(result2, "SQLite should match @sqlite prefix");
    }

    [TestMethod]
    public void GetEffectiveParameterPrefix_WithNormalPrefix_ReturnsSame()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;

        // Act
        var result = sqlDefine.GetEffectiveParameterPrefix();

        // Assert
        Assert.AreEqual("@", result);
    }

    [TestMethod]
    public void GetEffectiveParameterPrefix_WithSQLiteSpecialCase_ReturnsAt()
    {
        // Arrange
        var sqlDefine = SqlDefine.SQLite;

        // Act
        var result = sqlDefine.GetEffectiveParameterPrefix();

        // Assert
        Assert.AreEqual("@", result); // Should return @ not @sqlite
    }

    [TestMethod]
    public void GetEffectiveParameterPrefix_WithPostgreSQL_ReturnsDollar()
    {
        // Arrange
        var sqlDefine = SqlDefine.PgSql;

        // Act
        var result = sqlDefine.GetEffectiveParameterPrefix();

        // Assert
        Assert.AreEqual("$", result);
    }

    [TestMethod]
    public void ExtensionMethods_WorkWithAllDialects()
    {
        // Arrange
        var dialects = new[] { SqlDefine.MySql, SqlDefine.SqlServer, SqlDefine.PgSql, SqlDefine.SQLite };
        var columns = new[] { "id", "name" };

        // Act & Assert
        foreach (var dialect in dialects)
        {
            var wrappedColumns = dialect.WrapColumns(columns);
            var joinedColumns = dialect.WrapAndJoinColumns(columns);
            var parameters = dialect.CreateParameters(columns);
            var joinedParams = dialect.CreateAndJoinParameters(columns);
            var setClauses = dialect.CreateSetClauses(columns);
            var whereConditions = dialect.CreateWhereConditions(columns);

            Assert.AreEqual(2, wrappedColumns.Length);
            Assert.IsTrue(joinedColumns.Contains(","));
            Assert.AreEqual(2, parameters.Length);
            Assert.IsTrue(joinedParams.Contains(","));
            Assert.IsTrue(setClauses.Contains("="));
            Assert.IsTrue(whereConditions.Contains("AND"));
        }
    }
}
