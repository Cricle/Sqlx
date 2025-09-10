// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlModOperatorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using System.Collections.Generic;

namespace Sqlx.Tests;

[TestClass]
public class ExpressionToSqlModOperatorTests : CodeGenerationTestBase
{
    [TestMethod]
    public void ExpressionToSql_ModuloOperator_GeneratesCorrectSql()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ITestService
    {
        [Sqlx]
        IList<TestEntity> GetEntities([ExpressionToSql] ExpressionToSql<TestEntity> filter);
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestRepository : ITestService
    {
        private readonly DbConnection connection;
        public TestRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert - Check that the generated ExpressionToSql class supports modulo
        Assert.IsTrue(result.Contains("ExpressionType.Modulo"), 
            "Generated ExpressionToSql should support Modulo operator");
        Assert.IsTrue(result.Contains("% {right}"), 
            "Generated code should produce modulo SQL syntax");
    }

    [TestMethod]
    public void ExpressionToSql_ArithmeticOperators_AllSupported()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ITestService
    {
        [Sqlx]
        IList<TestEntity> GetEntities([ExpressionToSql] ExpressionToSql<TestEntity> filter);
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestRepository : ITestService
    {
        private readonly DbConnection connection;
        public TestRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert - Check that all arithmetic operators are supported
        Assert.IsTrue(result.Contains("ExpressionType.Add"), "Should support Add operator");
        Assert.IsTrue(result.Contains("ExpressionType.Subtract"), "Should support Subtract operator");
        Assert.IsTrue(result.Contains("ExpressionType.Multiply"), "Should support Multiply operator");
        Assert.IsTrue(result.Contains("ExpressionType.Divide"), "Should support Divide operator");
        Assert.IsTrue(result.Contains("ExpressionType.Modulo"), "Should support Modulo operator");

        Assert.IsTrue(result.Contains("+ {right}"), "Should generate + SQL syntax");
        Assert.IsTrue(result.Contains("- {right}"), "Should generate - SQL syntax");
        Assert.IsTrue(result.Contains("* {right}"), "Should generate * SQL syntax");
        Assert.IsTrue(result.Contains("/ {right}"), "Should generate / SQL syntax");
        Assert.IsTrue(result.Contains("% {right}"), "Should generate % SQL syntax");
    }

    [TestMethod]
    public void ExpressionToSql_ModOperator_InComplexExpressions()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public int GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ITestService
    {
        [Sqlx]
        IList<TestEntity> GetEntitiesWithComplexMod([ExpressionToSql] ExpressionToSql<TestEntity> filter);
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestRepository : ITestService
    {
        private readonly DbConnection connection;
        public TestRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert - Test complex expressions with mod
        Assert.IsTrue(result.Contains("ExpressionType.Modulo"), "Should support Modulo in complex expressions");
        
        // Test that mod can be combined with other operators
        var modPattern = @"ExpressionType\.Modulo\s*=>\s*\$""\{left\}\s*%\s*\{right\}""";
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(result, modPattern), 
            "Should generate correct mod operator pattern");
    }

    [TestMethod]
    public void ExpressionToSql_AllArithmeticOperators_GenerateCorrectSql()
    {
        // Arrange  
        var source = @"
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Calculator
    {
        public int A { get; set; }
        public int B { get; set; }
        public decimal Price { get; set; }
        public double Rate { get; set; }
    }

    public interface ICalculatorService
    {
        [Sqlx]
        IList<Calculator> Calculate([ExpressionToSql] ExpressionToSql<Calculator> expr);
    }

    [RepositoryFor(typeof(ICalculatorService))]
    public partial class CalculatorRepository : ICalculatorService
    {
        private readonly DbConnection connection;
        public CalculatorRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert - Check all arithmetic operators are present
        var expectedOperators = new[]
        {
            ("Add", "+ {right}"),
            ("Subtract", "- {right}"),
            ("Multiply", "* {right}"),
            ("Divide", "/ {right}"),
            ("Modulo", "% {right}")
        };

        foreach (var (opName, sqlPattern) in expectedOperators)
        {
            Assert.IsTrue(result.Contains($"ExpressionType.{opName}"), 
                $"Should contain {opName} operator");
            Assert.IsTrue(result.Contains(sqlPattern), 
                $"Should generate correct SQL pattern for {opName}: {sqlPattern}");
        }
    }

    [TestMethod]
    public void ExpressionToSql_ModOperator_WithDifferentDataTypes()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class NumericEntity
    {
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public short ShortValue { get; set; }
        public byte ByteValue { get; set; }
        public uint UIntValue { get; set; }
    }

    public interface INumericService
    {
        [Sqlx]
        IList<NumericEntity> GetByModulo([ExpressionToSql] ExpressionToSql<NumericEntity> filter);
    }

    [RepositoryFor(typeof(INumericService))]
    public partial class NumericRepository : INumericService
    {
        private readonly DbConnection connection;
        public NumericRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert
        Assert.IsTrue(result.Contains("ExpressionType.Modulo"), 
            "Should support modulo with different numeric types");
        Assert.IsTrue(result.Contains("% {right}"), 
            "Should generate % operator for all numeric types");
    }

    [TestMethod]
    public void ExpressionToSql_ModOperator_InWhereAndOrderBy()
    {
        // Test that mod operator works in both WHERE and ORDER BY clauses
        var source = @"
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class PagedEntity
    {
        public int Id { get; set; }
        public int PageNumber { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public interface IPagedService
    {
        [Sqlx]
        IList<PagedEntity> GetPagedResults([ExpressionToSql] ExpressionToSql<PagedEntity> query);
    }

    [RepositoryFor(typeof(IPagedService))]
    public partial class PagedRepository : IPagedService
    {
        private readonly DbConnection connection;
        public PagedRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert
        Assert.IsTrue(result.Contains("ExpressionType.Modulo"), "Should support mod in expressions");
        
        // Verify the generated ExpressionToSql class can handle mod in different contexts
        Assert.IsTrue(result.Contains("ParseBinaryExpression"), "Should have binary expression parsing");
        Assert.IsTrue(result.Contains("% {right}"), "Should generate modulo SQL syntax");
    }

    [TestMethod]
    public void ExpressionToSql_ModOperator_WithConstants()
    {
        // Test mod operator with constant values
        var source = @"
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class ConstantTestEntity
    {
        public int Id { get; set; }
        public int Value { get; set; }
    }

    public interface IConstantTestService
    {
        [Sqlx]
        IList<ConstantTestEntity> GetWithConstantMod([ExpressionToSql] ExpressionToSql<ConstantTestEntity> filter);
    }

    [RepositoryFor(typeof(IConstantTestService))]
    public partial class ConstantTestRepository : IConstantTestService
    {
        private readonly DbConnection connection;
        public ConstantTestRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert
        Assert.IsTrue(result.Contains("ExpressionType.Modulo"), "Should handle mod with constants");
        Assert.IsTrue(result.Contains("GetConstantValue"), "Should have constant value handling");
        Assert.IsTrue(result.Contains("FormatConstantValue"), "Should format constant values properly");
    }
}
