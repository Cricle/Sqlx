// <copyright file="WherePlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// Tests for WherePlaceholderHandler covering all WHERE clause generation branches.
/// </summary>
[TestClass]
public class WherePlaceholderTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int32, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
    };

    /// <summary>
    /// Basic handler functionality tests.
    /// </summary>
    [TestClass]
    public class HandlerTests
    {
        [TestMethod]
        public void Name_ReturnsWhere()
        {
            var handler = WherePlaceholderHandler.Instance;
            var name = handler.Name;
            Assert.AreEqual("where", name);
        }

        [TestMethod]
        public void GetType_ReturnsDynamic()
        {
            var handler = WherePlaceholderHandler.Instance;
            var type = handler.GetType("--param predicate");
            Assert.AreEqual(PlaceholderType.Dynamic, type);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Process_ThrowsInvalidOperationException()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            handler.Process(context, "--param predicate");
        }
    }

    /// <summary>
    /// Tests for --param mode.
    /// </summary>
    [TestClass]
    public class ParamModeTests
    {
        [TestMethod]
        public void Render_WithParamOption_ReturnsParameterValue()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var parameters = new Dictionary<string, object?>
            {
                ["predicate"] = "age > 18 AND status = 'active'"
            };

            var result = handler.Render(context, "--param predicate", parameters);

            Assert.AreEqual("age > 18 AND status = 'active'", result);
        }

        [TestMethod]
        public void Render_WithParamOption_NullParameter_ReturnsEmptyString()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var parameters = new Dictionary<string, object?>
            {
                ["predicate"] = null
            };

            var result = handler.Render(context, "--param predicate", parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Render_WithParamOption_MissingParameter_ThrowsException()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var parameters = new Dictionary<string, object?>();

            handler.Render(context, "--param predicate", parameters);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Render_NoOption_ThrowsException()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var parameters = new Dictionary<string, object?>();

            handler.Render(context, "", parameters);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Render_NullOptions_ThrowsException()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var parameters = new Dictionary<string, object?>();

            handler.Render(context, null!, parameters);
        }
    }

    /// <summary>
    /// Tests for --object mode.
    /// </summary>
    [TestClass]
    public class ObjectModeTests
    {
        [TestMethod]
        public void Render_WithObjectOption_SingleProperty_GeneratesCondition()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var filter = new Dictionary<string, object?> { ["Name"] = "John" };
            var parameters = new Dictionary<string, object?> { ["filter"] = filter };

            var result = handler.Render(context, "--object filter", parameters);

            Assert.AreEqual("[name] = @name", result);
        }

        [TestMethod]
        public void Render_WithObjectOption_MultipleProperties_GeneratesAndCondition()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Email"] = "john@test.com" };
            var parameters = new Dictionary<string, object?> { ["filter"] = filter };

            var result = handler.Render(context, "--object filter", parameters);

            Assert.AreEqual("([name] = @name AND [email] = @email)", result);
        }

        [TestMethod]
        public void Render_WithObjectOption_NullObject_Returns1Equals1()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var parameters = new Dictionary<string, object?> { ["filter"] = null };

            var result = handler.Render(context, "--object filter", parameters);

            Assert.AreEqual("1=1", result);
        }

        [TestMethod]
        public void Render_WithObjectOption_EmptyDictionary_Returns1Equals1()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var filter = new Dictionary<string, object?>();
            var parameters = new Dictionary<string, object?> { ["filter"] = filter };

            var result = handler.Render(context, "--object filter", parameters);

            Assert.AreEqual("1=1", result);
        }

        [TestMethod]
        public void Render_WithObjectOption_AllNullValues_Returns1Equals1()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var filter = new Dictionary<string, object?> { ["Name"] = null, ["Email"] = null };
            var parameters = new Dictionary<string, object?> { ["filter"] = filter };

            var result = handler.Render(context, "--object filter", parameters);

            Assert.AreEqual("1=1", result);
        }

        [TestMethod]
        public void Render_WithObjectOption_UnknownProperty_Ignored()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var filter = new Dictionary<string, object?> { ["UnknownProp"] = "value", ["Name"] = "John" };
            var parameters = new Dictionary<string, object?> { ["filter"] = filter };

            var result = handler.Render(context, "--object filter", parameters);

            Assert.AreEqual("[name] = @name", result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Render_WithObjectOption_NonDictionaryObject_ThrowsException()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var parameters = new Dictionary<string, object?> { ["filter"] = "not a dictionary" };

            handler.Render(context, "--object filter", parameters);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Render_WithObjectOption_MissingParameter_ThrowsException()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var parameters = new Dictionary<string, object?>();

            handler.Render(context, "--object filter", parameters);
        }

        [TestMethod]
        public void Render_WithObjectOption_FewColumns_UsesLinearSearch()
        {
            // 3 columns (< 4, should use linear search)
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
            var filter = new Dictionary<string, object?> { ["Name"] = "John" };
            var parameters = new Dictionary<string, object?> { ["filter"] = filter };

            var result = handler.Render(context, "--object filter", parameters);

            Assert.AreEqual("[name] = @name", result);
        }

        [TestMethod]
        public void Render_WithObjectOption_ManyColumns_UsesHashLookup()
        {
            // 5 columns (> 4, should use hash lookup)
            var manyColumns = new List<ColumnMeta>
            {
                new ColumnMeta("id", "Id", DbType.Int32, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("email", "Email", DbType.String, true),
                new ColumnMeta("age", "Age", DbType.Int32, true),
                new ColumnMeta("is_active", "IsActive", DbType.Boolean, false)
            };
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new SqlServerDialect(), "users", manyColumns);
            var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 25 };
            var parameters = new Dictionary<string, object?> { ["filter"] = filter };

            var result = handler.Render(context, "--object filter", parameters);

            Assert.AreEqual("([name] = @name AND [age] = @age)", result);
        }
    }

    /// <summary>
    /// Dialect-specific tests.
    /// </summary>
    [TestClass]
    public class DialectTests
    {
        [TestMethod]
        public void Render_WithObjectOption_PostgreSQL_UsesDoubleQuotes()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new PostgreSqlDialect(), "users", TestColumns);
            var filter = new Dictionary<string, object?> { ["Name"] = "John" };
            var parameters = new Dictionary<string, object?> { ["filter"] = filter };

            var result = handler.Render(context, "--object filter", parameters);

            Assert.IsTrue(result.Contains("\"name\""));
        }

        [TestMethod]
        public void Render_WithObjectOption_MySQL_UsesBackticks()
        {
            var handler = WherePlaceholderHandler.Instance;
            var context = new PlaceholderContext(new MySqlDialect(), "users", TestColumns);
            var filter = new Dictionary<string, object?> { ["Name"] = "John" };
            var parameters = new Dictionary<string, object?> { ["filter"] = filter };

            var result = handler.Render(context, "--object filter", parameters);

            Assert.IsTrue(result.Contains("`name`"));
        }
    }
}
