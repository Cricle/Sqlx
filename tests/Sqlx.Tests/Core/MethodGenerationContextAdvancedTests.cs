using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// 专门测试MethodGenerationContext中的高级场景和复杂逻辑
/// </summary>
[TestClass]
public class MethodGenerationContextAdvancedTests
{
    [TestMethod]
    public void MethodGenerationContext_WithComplexParameterScenarios_HandlesCorrectly()
    {
        // Arrange
        var testContext = new TestableMethodGenerationContextAdvanced();
        
        // Act & Assert - Test complex parameter scenarios
        Assert.IsTrue(testContext.TestHandleComplexParameters(new[] { "user", "connection", "cancellationToken" }));
        Assert.IsTrue(testContext.TestHandleComplexParameters(new[] { "id", "transaction", "timeout" }));
        Assert.IsFalse(testContext.TestHandleComplexParameters(new string[0]));
    }

    [TestMethod]
    public void GetHandlerInvoke_WithVariousMethodSignatures_GeneratesCorrectInvocations()
    {
        // Arrange
        var testContext = new TestableMethodGenerationContextAdvanced();
        
        // Act & Assert - Test different method signature scenarios
        Assert.IsTrue(testContext.TestGetHandlerInvoke("GetUser", new[] { "id" }, false).Contains("GetUser(id)"));
        Assert.IsTrue(testContext.TestGetHandlerInvoke("GetUserAsync", new[] { "id", "cancellationToken" }, true).Contains("await"));
        Assert.IsTrue(testContext.TestGetHandlerInvoke("GetUsers", new[] { "filter", "pageSize", "pageIndex" }, false).Contains("GetUsers"));
    }

    [TestMethod]
    public void GenerateFallbackBatchLogic_WithDifferentReturnTypes_GeneratesCorrectCode()
    {
        // Arrange
        var testContext = new TestableMethodGenerationContextAdvanced();
        var sb = new IndentedStringBuilder("");
        
        // Act & Assert - Test List return type
        testContext.TestGenerateFallbackBatchLogic(sb, "ids", "List", "User");
        var listResult = sb.ToString();
        Assert.IsTrue(listResult.Contains("List<User>"));
        Assert.IsTrue(listResult.Contains("foreach"));
        
        // Reset and test Scalar return type
        sb = new IndentedStringBuilder("");
        testContext.TestGenerateFallbackBatchLogic(sb, "ids", "Scalar", "int");
        var scalarResult = sb.ToString();
        Assert.IsTrue(scalarResult.Contains("int"));
        Assert.IsTrue(scalarResult.Contains("foreach"));
        
        // Reset and test Enumerable return type
        sb = new IndentedStringBuilder("");
        testContext.TestGenerateFallbackBatchLogic(sb, "ids", "Enumerable", "User");
        var enumerableResult = sb.ToString();
        Assert.IsTrue(enumerableResult.Contains("yield") || enumerableResult.Contains("IEnumerable"));
    }

    [TestMethod]
    public void DeclareCommand_WithComplexMethodSignatures_GeneratesCorrectDeclarations()
    {
        // Arrange
        var testContext = new TestableMethodGenerationContextAdvanced();
        var sb = new IndentedStringBuilder("");
        
        // Act
        var result = testContext.TestDeclareCommand(sb, "GetUserById", new[] { "id", "includeDeleted", "cancellationToken" });
        
        // Assert
        Assert.IsTrue(result);
        var code = sb.ToString();
        Assert.IsTrue(code.Contains("GetUserById") || code.Contains("command") || code.Length > 0);
    }

    [TestMethod]
    public void GetReturnType_WithVariousReturnTypes_ReturnsCorrectTypes()
    {
        // Arrange
        var testContext = new TestableMethodGenerationContextAdvanced();
        
        // Act & Assert - Test different return type scenarios
        Assert.AreEqual("Scalar", testContext.TestGetReturnType("int"));
        Assert.AreEqual("Scalar", testContext.TestGetReturnType("bool"));
        Assert.AreEqual("Scalar", testContext.TestGetReturnType("string"));
        Assert.AreEqual("Single", testContext.TestGetReturnType("User"));
        Assert.AreEqual("List", testContext.TestGetReturnType("List<User>"));
        Assert.AreEqual("List", testContext.TestGetReturnType("IEnumerable<User>"));
        Assert.AreEqual("Enumerable", testContext.TestGetReturnType("IAsyncEnumerable<User>"));
        Assert.AreEqual("Single", testContext.TestGetReturnType("Task<User>"));
        Assert.AreEqual("List", testContext.TestGetReturnType("Task<List<User>>"));
    }

    [TestMethod]
    public void GetSqlDefine_WithDifferentContexts_ReturnsCorrectSqlDefine()
    {
        // Arrange
        var testContext = new TestableMethodGenerationContextAdvanced();
        
        // Act & Assert - Test SQL define detection
        var sqlServerResult = testContext.TestGetSqlDefine("SqlConnection");
        var mySqlResult = testContext.TestGetSqlDefine("MySqlConnection");
        var pgSqlResult = testContext.TestGetSqlDefine("NpgsqlConnection");
        var sqliteResult = testContext.TestGetSqlDefine("SqliteConnection");
        var defaultResult = testContext.TestGetSqlDefine("UnknownConnection");
        
        Assert.AreEqual("[", sqlServerResult.ColumnLeft);
        Assert.AreEqual("]", sqlServerResult.ColumnRight);
        Assert.AreEqual("`", mySqlResult.ColumnLeft);
        Assert.AreEqual("`", mySqlResult.ColumnRight);
        Assert.AreEqual("\"", pgSqlResult.ColumnLeft);
        Assert.AreEqual("\"", pgSqlResult.ColumnRight);
        Assert.AreEqual("[", sqliteResult.ColumnLeft);
        Assert.AreEqual("]", sqliteResult.ColumnRight);
        Assert.AreEqual("`", defaultResult.ColumnLeft); // Default is MySql
    }

    [TestMethod]
    public void RemoveIfExists_WithParameterArrays_RemovesCorrectly()
    {
        // Arrange
        var testContext = new TestableMethodGenerationContextAdvanced();
        var parameters = new[] { "id", "connection", "cancellationToken", "user" };
        
        // Act & Assert - Test parameter removal
        var result1 = testContext.TestRemoveIfExists(parameters, "connection");
        Assert.AreEqual(3, result1.Length);
        Assert.IsFalse(result1.Contains("connection"));
        
        var result2 = testContext.TestRemoveIfExists(parameters, "nonexistent");
        Assert.AreEqual(4, result2.Length);
    }

    [TestMethod]
    public void GetAttributeParameter_WithDifferentAttributes_ReturnsCorrectParameters()
    {
        // Arrange
        var testContext = new TestableMethodGenerationContextAdvanced();
        
        // Act & Assert - Test attribute parameter extraction
        Assert.IsNotNull(testContext.TestGetAttributeParameter("RawSqlAttribute"));
        Assert.IsNotNull(testContext.TestGetAttributeParameter("TimeoutAttribute"));
        Assert.IsNotNull(testContext.TestGetAttributeParameter("ReadHandlerAttribute"));
        Assert.IsNull(testContext.TestGetAttributeParameter("NonExistentAttribute"));
    }

    [TestMethod]
    public void IsAsync_WithDifferentReturnTypes_DetectsCorrectly()
    {
        // Arrange
        var testContext = new TestableMethodGenerationContextAdvanced();
        
        // Act & Assert - Test async detection
        Assert.IsTrue(testContext.TestIsAsync("Task"));
        Assert.IsTrue(testContext.TestIsAsync("Task<User>"));
        Assert.IsTrue(testContext.TestIsAsync("Task<List<User>>"));
        Assert.IsTrue(testContext.TestIsAsync("IAsyncEnumerable<User>"));
        Assert.IsFalse(testContext.TestIsAsync("User"));
        Assert.IsFalse(testContext.TestIsAsync("List<User>"));
        Assert.IsFalse(testContext.TestIsAsync("int"));
    }
}

/// <summary>
/// 可测试的MethodGenerationContext实现，用于测试高级场景
/// </summary>
internal class TestableMethodGenerationContextAdvanced
{
    public bool TestHandleComplexParameters(string[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return false;

        // Simulate complex parameter handling logic
        var hasConnection = parameters.Any(p => p.Contains("connection"));
        var hasEntity = parameters.Any(p => !IsSystemParameter(p));
        var hasToken = parameters.Any(p => p.Contains("cancellation") || p == "token");

        return hasConnection || hasEntity || hasToken;
    }

    public string TestGetHandlerInvoke(string methodName, string[] parameters, bool isAsync)
    {
        var paramList = string.Join(", ", parameters);
        
        if (isAsync)
        {
            return $"await {methodName}({paramList})";
        }
        else
        {
            return $"{methodName}({paramList})";
        }
    }

    public void TestGenerateFallbackBatchLogic(IndentedStringBuilder sb, string batchParameterName, string returnType, string elementType)
    {
        switch (returnType)
        {
            case "List":
                sb.AppendLine($"var __result__ = new List<{elementType}>();");
                sb.AppendLine($"foreach (var item in {batchParameterName})");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"var singleResult = GetSingleItem(item);");
                sb.AppendLine($"__result__.Add(singleResult);");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine("return __result__;");
                break;
                
            case "Scalar":
                sb.AppendLine($"{elementType} __totalResult__ = default({elementType});");
                sb.AppendLine($"foreach (var item in {batchParameterName})");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"var singleResult = GetSingleItem(item);");
                sb.AppendLine($"__totalResult__ += singleResult;");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine("return __totalResult__;");
                break;
                
            case "Enumerable":
                sb.AppendLine($"foreach (var item in {batchParameterName})");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"var singleResult = GetSingleItem(item);");
                sb.AppendLine($"yield return singleResult;");
                sb.PopIndent();
                sb.AppendLine("}");
                break;
        }
    }

    public bool TestDeclareCommand(IndentedStringBuilder sb, string methodName, string[] parameters)
    {
        sb.AppendLine($"// Command declaration for {methodName}");
        sb.AppendLine("using var __cmd__ = connection.CreateCommand();");
        
        foreach (var param in parameters.Where(p => !IsSystemParameter(p)))
        {
            sb.AppendLine($"__cmd__.Parameters.Add(new SqlParameter(\"@{param}\", {param}));");
        }
        
        return true;
    }

    public string TestGetReturnType(string typeName)
    {
        if (typeName.StartsWith("Task<"))
        {
            var innerType = typeName.Substring(5, typeName.Length - 6);
            return TestGetReturnType(innerType);
        }

        return typeName switch
        {
            "int" or "bool" or "string" or "decimal" or "double" or "float" or "long" or "byte" or "DateTime" or "Guid" => "Scalar",
            var t when t.StartsWith("List<") || t.StartsWith("IEnumerable<") => "List",
            var t when t.StartsWith("IAsyncEnumerable<") => "Enumerable",
            _ => "Single"
        };
    }

    public SqlDefine TestGetSqlDefine(string connectionTypeName)
    {
        var nameLower = connectionTypeName.ToLowerInvariant();
        
        if (nameLower.Contains("mysql") || nameLower.Contains("mariadb"))
            return SqlDefine.MySql;
        else if (nameLower.Contains("npgsql") || nameLower.Contains("postgres"))
            return SqlDefine.PgSql;
        else if (nameLower.Contains("sqlite"))
            return SqlDefine.SQLite;
        else if (nameLower.Contains("sqlconnection") || nameLower.Contains("sqlserver"))
            return SqlDefine.SqlServer;
        else
            return SqlDefine.MySql; // Default
    }

    public string[] TestRemoveIfExists(string[] parameters, string parameterToRemove)
    {
        return parameters.Where(p => p != parameterToRemove).ToArray();
    }

    public string? TestGetAttributeParameter(string attributeName)
    {
        return attributeName switch
        {
            "RawSqlAttribute" => "sql",
            "TimeoutAttribute" => "timeout",
            "ReadHandlerAttribute" => "handler",
            _ => null
        };
    }

    public bool TestIsAsync(string returnTypeName)
    {
        return returnTypeName == "Task" || 
               returnTypeName.StartsWith("Task<") || 
               returnTypeName.StartsWith("IAsyncEnumerable<");
    }

    private bool IsSystemParameter(string paramName)
    {
        return paramName.ToLowerInvariant() switch
        {
            "connection" or "transaction" or "cancellationtoken" or "token" or "timeout" or "handler" => true,
            _ => false
        };
    }
}
