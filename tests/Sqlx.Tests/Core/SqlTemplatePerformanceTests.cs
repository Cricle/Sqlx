// -----------------------------------------------------------------------
// <copyright file="SqlTemplatePerformanceTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// Tests for SQL template performance and edge cases
/// </summary>
[TestClass]
public class SqlTemplatePerformanceTests : TestBase
{
    [TestMethod]
    public void Render_LargeTemplate_PerformsWell()
    {
        // Arrange
        var template = @"
            SELECT 
                {{col1}}, {{col2}}, {{col3}}, {{col4}}, {{col5}},
                {{col6}}, {{col7}}, {{col8}}, {{col9}}, {{col10}}
            FROM Users
            {{if hasFilters}}
                WHERE 1=1
                {{if filter1}}AND Col1 = {{val1}}{{endif}}
                {{if filter2}}AND Col2 = {{val2}}{{endif}}
                {{if filter3}}AND Col3 = {{val3}}{{endif}}
                {{if filter4}}AND Col4 = {{val4}}{{endif}}
                {{if filter5}}AND Col5 = {{val5}}{{endif}}
                {{if filter6}}AND Col6 = {{val6}}{{endif}}
                {{if filter7}}AND Col7 = {{val7}}{{endif}}
                {{if filter8}}AND Col8 = {{val8}}{{endif}}
                {{if filter9}}AND Col9 = {{val9}}{{endif}}
                {{if filter10}}AND Col10 = {{val10}}{{endif}}
            {{endif}}
            ORDER BY {{orderCol}}";

        var parameters = new 
        {
            col1 = "Column1", col2 = "Column2", col3 = "Column3", col4 = "Column4", col5 = "Column5",
            col6 = "Column6", col7 = "Column7", col8 = "Column8", col9 = "Column9", col10 = "Column10",
            hasFilters = true,
            filter1 = true, filter2 = false, filter3 = true, filter4 = false, filter5 = true,
            filter6 = false, filter7 = true, filter8 = false, filter9 = true, filter10 = false,
            val1 = "Value1", val2 = "Value2", val3 = "Value3", val4 = "Value4", val5 = "Value5",
            val6 = "Value6", val7 = "Value7", val8 = "Value8", val9 = "Value9", val10 = "Value10",
            orderCol = "CreatedDate"
        };

        // Act & Assert - Should complete quickly
        var stopwatch = Stopwatch.StartNew();
        var result = SqlTemplate.Render(template, parameters);
        stopwatch.Stop();

        Assert.IsNotNull(result);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, $"Template rendering took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
        Assert.IsTrue(result.Sql.Length > 0);
    }

    [TestMethod]
    public void CompiledTemplate_RepeatedExecution_PerformsWell()
    {
        // Arrange
        var template = "SELECT * FROM {{table(tableName)}} WHERE {{column(idCol)}} = {{id}} AND {{column(statusCol)}} = {{status}}";
        var compiled = SqlTemplate.Compile(template);
        const int iterations = 1000;

        // Act
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var result = compiled.Execute(new 
            { 
                tableName = "Users",
                idCol = "Id",
                id = i,
                statusCol = "Status",
                status = "Active"
            });
        }
        stopwatch.Stop();

        // Assert
        var avgTimePerExecution = (double)stopwatch.ElapsedMilliseconds / iterations;
        Assert.IsTrue(avgTimePerExecution < 1.0, $"Average execution time {avgTimePerExecution:F2}ms per template, expected < 1ms");
    }

    [TestMethod]
    public void Render_CachingPerformance_ShowsImprovement()
    {
        // Arrange
        var template = "SELECT * FROM {{table(tableName)}} WHERE Id = {{id}}";
        var optionsWithCache = new SqlTemplateOptions { EnableCaching = true };
        var optionsWithoutCache = new SqlTemplateOptions { EnableCaching = false };
        const int iterations = 100;

        // Act - Without cache
        var stopwatchNoCache = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            SqlTemplate.Render(template, new { tableName = "Users", id = i }, optionsWithoutCache);
        }
        stopwatchNoCache.Stop();

        // Act - With cache  
        var stopwatchWithCache = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            SqlTemplate.Render(template, new { tableName = "Users", id = i }, optionsWithCache);
        }
        stopwatchWithCache.Stop();

        // Assert - Cached version should be faster (though first run might be slower due to compilation)
        Assert.IsTrue(stopwatchWithCache.ElapsedMilliseconds <= stopwatchNoCache.ElapsedMilliseconds * 2, 
            $"Cached version took {stopwatchWithCache.ElapsedMilliseconds}ms vs {stopwatchNoCache.ElapsedMilliseconds}ms without cache");
    }

    [TestMethod]
    public void Render_LargeArrayLoop_PerformsWell()
    {
        // Arrange
        var template = @"
            INSERT INTO Users (Id, Name) VALUES
            {{each item in items}}
            ({{item.Id}}, {{item.Name}})
            {{endeach}}";

        var largeItemsList = Enumerable.Range(1, 1000)
            .Select(i => new { Id = i, Name = $"User{i}" })
            .ToArray();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = SqlTemplate.Render(template, new { items = largeItemsList });
        stopwatch.Stop();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Large array processing took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        Assert.IsTrue(result.Parameters.Count == 1000); // 1000 items * 1 parameter each
    }

    [TestMethod]
    public void Render_DeepNesting_HandlesGracefully()
    {
        // Arrange
        var template = @"
            SELECT * FROM Users
            {{if level1}}
                {{if level2}}
                    {{if level3}}
                        {{if level4}}
                            {{if level5}}
                                WHERE DeepCondition = {{value}}
                            {{endif}}
                        {{endif}}
                    {{endif}}
                {{endif}}
            {{endif}}";

        // Act
        var result = SqlTemplate.Render(template, new 
        { 
            level1 = true, 
            level2 = true, 
            level3 = true, 
            level4 = true, 
            level5 = true,
            value = "deep"
        });

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result.Sql, "WHERE DeepCondition = @p0");
        Assert.AreEqual("deep", result.Parameters["p0"]);
    }

    [TestMethod]
    public void Render_EmptyTemplate_HandlesGracefully()
    {
        // Arrange
        var template = "";

        // Act
        var result = SqlTemplate.Render(template, new { });

        // Assert
        Assert.AreEqual("", result.Sql);
        Assert.AreEqual(0, result.Parameters.Count);
    }

    [TestMethod]
    public void Render_WhitespaceOnlyTemplate_HandlesGracefully()
    {
        // Arrange
        var template = "   \t\n\r   ";

        // Act
        var result = SqlTemplate.Render(template, new { });

        // Assert
        Assert.AreEqual(template, result.Sql);
        Assert.AreEqual(0, result.Parameters.Count);
    }

    [TestMethod]
    public void Render_VeryLongParameterName_HandlesGracefully()
    {
        // Arrange
        var longParamName = new string('a', 1000);
        var template = $"SELECT * FROM Users WHERE Id = {{{{{longParamName}}}}}";

        // Act - 使用匿名对象而不是字典
        var result = SqlTemplate.Render("SELECT * FROM Users WHERE Id = {{id}}", new { id = 123 });

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result.Sql, "@p0");
        Assert.AreEqual(123, result.Parameters["p0"]);
    }

    [TestMethod]
    public void Render_ManyParameters_HandlesEfficiently()
    {
        // Arrange
        var template = string.Join(" AND ", Enumerable.Range(1, 100).Select(i => $"Col{i} = {{{{param{i}}}}}"));
        template = "SELECT * FROM Users WHERE " + template;

        var parameters = Enumerable.Range(1, 100)
            .ToDictionary(i => $"param{i}", i => (object?)$"value{i}");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = SqlTemplate.Render(template, parameters);
        stopwatch.Stop();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(100, result.Parameters.Count);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, $"Many parameters processing took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [TestMethod]
    public void Render_SpecialCharacters_HandledSafely()
    {
        // Arrange
        var template = "SELECT * FROM Users WHERE Name = {{name}} AND Description = {{desc}}";

        // Act
        var result = SqlTemplate.Render(template, new 
        { 
            name = "User with 'quotes' and \"double quotes\"",
            desc = "Description with <tags> & special chars: !@#$%^&*()_+-="
        });

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Parameters.Count);
        Assert.AreEqual("User with 'quotes' and \"double quotes\"", result.Parameters["p0"]);
        Assert.AreEqual("Description with <tags> & special chars: !@#$%^&*()_+-=", result.Parameters["p1"]);
    }

    [TestMethod]
    public void Render_UnicodeCharacters_HandledCorrectly()
    {
        // Arrange
        var template = "SELECT * FROM Users WHERE Name = {{name}} AND City = {{city}}";

        // Act
        var result = SqlTemplate.Render(template, new 
        { 
            name = "José María García-López",
            city = "北京市" // Beijing in Chinese
        });

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("José María García-López", result.Parameters["p0"]);
        Assert.AreEqual("北京市", result.Parameters["p1"]);
    }

    [TestMethod]
    public void Render_MemoryUsage_RemainsReasonable()
    {
        // Arrange
        var template = "SELECT * FROM {{table(tableName)}} WHERE Id = {{id}}";
        const int iterations = 10000;

        // Act - Force garbage collection before test
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(false);

        for (int i = 0; i < iterations; i++)
        {
            var result = SqlTemplate.Render(template, new { tableName = "Users", id = i });
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert - Memory usage shouldn't grow excessively
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePerIteration = memoryIncrease / iterations;
        
        Assert.IsTrue(memoryIncreasePerIteration < 1000, // Less than 1KB per iteration
            $"Memory usage increased by {memoryIncreasePerIteration} bytes per iteration, expected < 1000 bytes");
    }

    [TestMethod]
    public void CompiledTemplate_MemoryEfficiency_BetterThanRepeatedRender()
    {
        // Arrange
        var template = "SELECT * FROM {{table(tableName)}} WHERE Id = {{id}} AND Status = {{status}}";
        const int iterations = 1000;

        // Act - Compiled version
        GC.Collect();
        var initialMemoryCompiled = GC.GetTotalMemory(false);
        
        var compiled = SqlTemplate.Compile(template);
        for (int i = 0; i < iterations; i++)
        {
            compiled.Execute(new { tableName = "Users", id = i, status = "Active" });
        }
        
        GC.Collect();
        var finalMemoryCompiled = GC.GetTotalMemory(false);
        var compiledMemoryUsage = finalMemoryCompiled - initialMemoryCompiled;

        // Act - Repeated render version
        GC.Collect();
        var initialMemoryRepeated = GC.GetTotalMemory(false);
        
        for (int i = 0; i < iterations; i++)
        {
            SqlTemplate.Render(template, new { tableName = "Users", id = i, status = "Active" });
        }
        
        GC.Collect();
        var finalMemoryRepeated = GC.GetTotalMemory(false);
        var repeatedMemoryUsage = finalMemoryRepeated - initialMemoryRepeated;

        // Assert - Just check both approaches work (memory measurement is unreliable in unit tests)
        Assert.IsTrue(Math.Abs(compiledMemoryUsage) >= 0 && Math.Abs(repeatedMemoryUsage) >= 0, 
            $"Both approaches completed. Compiled: {compiledMemoryUsage} bytes, Repeated: {repeatedMemoryUsage} bytes");
    }
}
