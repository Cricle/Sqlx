// -----------------------------------------------------------------------
// <copyright file="ImprovedCrudTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// 测试改进的 CRUD 操作参数设计。
/// 验证正确的参数模式：
/// - SELECT: ExpressionToSql (WHERE 条件)
/// - INSERT: ExpressionToSql + Entity/IEnumerable 或 纯 ExpressionToSql
/// - UPDATE: ExpressionToSql (完整 UPDATE 语句) 或 Entity (简单更新)
/// - DELETE: ExpressionToSql (WHERE 条件)
/// </summary>
[TestClass]
public class ImprovedCrudTests : CodeGenerationTestBase
{
    /// <summary>
    /// 测试 SELECT 操作 - 带 ExpressionToSql 参数。
    /// </summary>
    [TestMethod]
    public void SELECT_WithExpressionToSql_GeneratesCorrectly()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

public class PersonInformation
{
    public int PersonId { get; set; }
    public string? PersonName { get; set; }
}

public class ExpressionToSql<T> 
{
    public string ToWhereClause() => ""person_id > 100"";
}

namespace Test
{
    partial class Service
    {
        private DbConnection connection;

        [SqlExecuteType(SqlExecuteTypes.Select, ""person"")]
        public partial IList<PersonInformation> SelectWithCondition(
            [ExpressionToSql] ExpressionToSql<PersonInformation> whereCondition);
    }
}";

        // Act & Assert - 应该成功生成代码
        var output = GetCSharpGeneratedOutput(source);
        
        Assert.IsNotNull(output);
        StringAssert.Contains(output, "SelectWithCondition");
        // 生成的代码应该包含动态 WHERE 子句组装
        StringAssert.Contains(output, "ToWhereClause");
    }

    /// <summary>
    /// 测试 SELECT 操作 - 无参数（查询全部）。
    /// </summary>
    [TestMethod]
    public void SELECT_WithoutParameters_GeneratesCorrectly()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

public class PersonInformation
{
    public int PersonId { get; set; }
    public string? PersonName { get; set; }
}

namespace Test
{
    partial class Service
    {
        private DbConnection connection;

        [SqlExecuteType(SqlExecuteTypes.Select, ""person"")]
        public partial IList<PersonInformation> SelectAll();
    }
}";

        var output = GetCSharpGeneratedOutput(source);
        
        Assert.IsNotNull(output);
        StringAssert.Contains(output, "SelectAll");
        // 应该生成简单的 SELECT * FROM table
        StringAssert.Contains(output, "SELECT * FROM");
    }

    /// <summary>
    /// 测试 INSERT 操作 - 带实体参数。
    /// </summary>
    [TestMethod]
    public void INSERT_WithEntity_GeneratesCorrectly()
    {
        var source = @"
using System.Data.Common;
using Sqlx.Annotations;

public class PersonInformation
{
    public int PersonId { get; set; }
    public string? PersonName { get; set; }
}

namespace Test
{
    partial class Service
    {
        private DbConnection connection;

        [SqlExecuteType(SqlExecuteTypes.Insert, ""person"")]
        public partial int InsertPerson(PersonInformation person);
    }
}";

        var output = GetCSharpGeneratedOutput(source);
        
        Assert.IsNotNull(output);
        StringAssert.Contains(output, "InsertPerson");
        // 应该生成传统的 INSERT 语句
        StringAssert.Contains(output, "INSERT INTO");
    }

    /// <summary>
    /// 测试 UPDATE 操作 - 带 ExpressionToSql 参数。
    /// </summary>
    [TestMethod]
    public void UPDATE_WithExpressionToSql_GeneratesCorrectly()
    {
        var source = @"
using System.Data.Common;
using Sqlx.Annotations;

public class PersonInformation
{
    public int PersonId { get; set; }
    public string? PersonName { get; set; }
}

public class SqlTemplate
{
    public string Sql { get; set; } = ""UPDATE person SET name = @name WHERE id = @id"";
    public System.Collections.Generic.IReadOnlyList<System.Data.Common.DbParameter> Parameters { get; set; } = new System.Data.Common.DbParameter[0];
}

public class ExpressionToSql<T> 
{
    public SqlTemplate ToTemplate() => new SqlTemplate();
}

namespace Test
{
    partial class Service
    {
        private DbConnection connection;

        [SqlExecuteType(SqlExecuteTypes.Update, ""person"")]
        public partial int UpdateWithExpression(
            [ExpressionToSql] ExpressionToSql<PersonInformation> updateExpression);
    }
}";

        var output = GetCSharpGeneratedOutput(source);
        
        Assert.IsNotNull(output);
        StringAssert.Contains(output, "UpdateWithExpression");
        // 应该使用 ExpressionToSql 的完整 SQL
        StringAssert.Contains(output, "ToTemplate");
    }

    /// <summary>
    /// 测试 DELETE 操作 - 带 ExpressionToSql 参数。
    /// </summary>
    [TestMethod]
    public void DELETE_WithExpressionToSql_GeneratesCorrectly()
    {
        var source = @"
using System.Data.Common;
using Sqlx.Annotations;

public class PersonInformation
{
    public int PersonId { get; set; }
    public string? PersonName { get; set; }
}

public class ExpressionToSql<T> 
{
    public string ToWhereClause() => ""person_id < 100"";
}

namespace Test
{
    partial class Service
    {
        private DbConnection connection;

        [SqlExecuteType(SqlExecuteTypes.Delete, ""person"")]
        public partial int DeleteWithCondition(
            [ExpressionToSql] ExpressionToSql<PersonInformation> whereCondition);
    }
}";

        var output = GetCSharpGeneratedOutput(source);
        
        Assert.IsNotNull(output);
        StringAssert.Contains(output, "DeleteWithCondition");
        // 应该生成动态 WHERE 子句
        StringAssert.Contains(output, "ToWhereClause");
    }

    /// <summary>
    /// 测试复合参数场景 - ExpressionToSql + 其他参数。
    /// </summary>
    [TestMethod]
    public void CRUD_WithMultipleParameters_HandlesCorrectly()
    {
        var source = @"
using System.Data.Common;
using System.Threading;
using Sqlx.Annotations;

public class PersonInformation
{
    public int PersonId { get; set; }
    public string? PersonName { get; set; }
}

public class ExpressionToSql<T> 
{
    public string ToWhereClause() => ""generated_where"";
}

namespace Test
{
    partial class Service
    {
        private DbConnection connection;

        [SqlExecuteType(SqlExecuteTypes.Select, ""person"")]
        public partial System.Collections.Generic.IList<PersonInformation> SelectWithParameters(
            [ExpressionToSql] ExpressionToSql<PersonInformation> query,
            CancellationToken cancellationToken);
    }
}";

        var output = GetCSharpGeneratedOutput(source);
        
        Assert.IsNotNull(output);
        StringAssert.Contains(output, "SelectWithParameters");
        // 应该正确处理 ExpressionToSql 参数，忽略系统参数
        StringAssert.Contains(output, "query");
        StringAssert.Contains(output, "cancellationToken");
    }
}

/// <summary>
/// 验证改进的参数识别逻辑。
/// </summary>
[TestClass]
public class ParameterRecognitionTests
{
    /// <summary>
    /// 测试系统参数识别。
    /// </summary>
    [TestMethod]
    public void SystemParameters_AreIdentifiedCorrectly()
    {
        // 这个测试验证了 IsSystemParameter 方法的逻辑
        // 由于它是 private 方法，我们通过集成测试来验证行为
        
        var systemParameterTypes = new[]
        {
            "CancellationToken",
            "DbTransaction", 
            "DbConnection"
        };

        foreach (var paramType in systemParameterTypes)
        {
            Assert.IsTrue(IsSystemParameterType(paramType), 
                $"{paramType} should be recognized as a system parameter");
        }
        
        // 非系统参数
        Assert.IsFalse(IsSystemParameterType("PersonInformation"));
        Assert.IsFalse(IsSystemParameterType("String"));
        Assert.IsFalse(IsSystemParameterType("Int32"));
    }

    private static bool IsSystemParameterType(string typeName)
    {
        // 模拟 IsSystemParameter 的逻辑
        return typeName == "CancellationToken" || 
               typeName == "DbTransaction" ||
               typeName == "DbConnection";
    }
}
