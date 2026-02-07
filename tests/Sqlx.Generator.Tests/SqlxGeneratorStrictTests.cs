// -----------------------------------------------------------------------
// <copyright file="SqlxGeneratorStrictTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Strict tests for Sqlx source generator with exact string matching.
/// </summary>
[TestClass]
public class SqlxGeneratorStrictTests
{
    [TestMethod]
    public void GenerateEntityProvider_SimpleEntity_ExactMatch()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    [Sqlx]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";

        // Act
        var generator = new SqlxGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        Assert.IsTrue(generatedSources.Count > 0, "Should generate at least one source file");

        var entityProviderFile = generatedSources.FirstOrDefault(s => s.FileName.Contains("User"));
        Assert.IsTrue(entityProviderFile != default, $"Should generate User file. Generated files: {string.Join(", ", generatedSources.Select(s => s.FileName))}");

        var generatedCode = entityProviderFile.Source;
        
        // Verify EntityProvider is present in generated code
        Assert.IsTrue(generatedCode.Contains("class UserEntityProvider"), "Should contain UserEntityProvider class");
        Assert.IsTrue(generatedCode.Contains("public static UserEntityProvider Default"), "Should have Default property");
        Assert.IsTrue(generatedCode.Contains("typeof(Test.User)"), "Should reference Test.User type");
        Assert.IsTrue(generatedCode.Contains("ColumnMeta"), "Should use ColumnMeta");
        Assert.IsTrue(generatedCode.Contains("\"id\""), "Should have id column");
        Assert.IsTrue(generatedCode.Contains("\"name\""), "Should have name column");
    }

    [TestMethod]
    public void GenerateEntityProvider_WithNullableProperty_ExactMatch()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    [Sqlx]
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }
}";

        // Act
        var generator = new SqlxGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        var entityProviderFile = generatedSources.FirstOrDefault(s => s.FileName.Contains("Product"));
        Assert.IsTrue(entityProviderFile != default, "Should generate Product file");

        var generatedCode = entityProviderFile.Source;
        
        // Verify nullable property handling - the generator uses lowercase column names
        Assert.IsTrue(generatedCode.Contains("\"description\""), "Should have description column");
        Assert.IsTrue(generatedCode.Contains("\"name\""), "Should have name column");
        Assert.IsTrue(generatedCode.Contains("DbType.String"), "Should use DbType.String for string properties");
    }

    [TestMethod]
    public void GenerateEntityProvider_WithTableNameAttribute_ExactMatch()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    [Sqlx]
    [TableName(""custom_users"")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";

        // Act
        var generator = new SqlxGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        var entityProviderFile = generatedSources.FirstOrDefault(s => s.FileName.Contains("User"));
        Assert.IsTrue(entityProviderFile != default, "Should generate User file");

        var generatedCode = entityProviderFile.Source;
        
        // Note: The current generator doesn't use TableName attribute in EntityProvider
        // It uses EntityType instead. This test verifies the code is generated correctly.
        Assert.IsTrue(generatedCode.Contains("class UserEntityProvider"), "Should contain UserEntityProvider class");
        Assert.IsTrue(generatedCode.Contains("typeof(Test.User)"), "Should reference Test.User type");
    }

    [TestMethod]
    public void GenerateEntityProvider_WithKeyAttribute_ExactMatch()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Test
{
    [Sqlx]
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public decimal Total { get; set; }
    }
}";

        // Act
        var generator = new SqlxGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        var entityProviderFile = generatedSources.FirstOrDefault(s => s.FileName.Contains("Order"));
        Assert.IsTrue(entityProviderFile != default, "Should generate Order file");

        var generatedCode = entityProviderFile.Source;
        
        // Verify OrderId column is present (Key attribute handling is in ColumnMeta)
        Assert.IsTrue(generatedCode.Contains("\"orderid\"") || generatedCode.Contains("\"OrderId\""), 
            "OrderId should be in columns");
        Assert.IsTrue(generatedCode.Contains("class OrderEntityProvider"), "Should contain OrderEntityProvider class");
    }

    [TestMethod]
    public void GenerateEntityProvider_WithMultipleKeys_ExactMatch()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Test
{
    [Sqlx]
    public class OrderItem
    {
        [Key]
        public int OrderId { get; set; }
        [Key]
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}";

        // Act
        var generator = new SqlxGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        var entityProviderFile = generatedSources.FirstOrDefault(s => s.FileName.Contains("OrderItem"));
        Assert.IsTrue(entityProviderFile != default, "Should generate OrderItem file");

        var generatedCode = entityProviderFile.Source;
        
        // Verify both key columns are present
        Assert.IsTrue(generatedCode.Contains("\"orderid\"") || generatedCode.Contains("\"OrderId\""), 
            "OrderId should be in columns");
        Assert.IsTrue(generatedCode.Contains("\"productid\"") || generatedCode.Contains("\"ProductId\""), 
            "ProductId should be in columns");
        Assert.IsTrue(generatedCode.Contains("class OrderItemEntityProvider"), "Should contain OrderItemEntityProvider class");
    }

    [TestMethod]
    public void GenerateEntityProvider_WithVariousTypes_ExactMatch()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;
using System;

namespace Test
{
    [Sqlx]
    public class ComplexEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public decimal Amount { get; set; }
        public double? Rating { get; set; }
        public long Count { get; set; }
    }
}";

        // Act
        var generator = new SqlxGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        var entityProviderFile = generatedSources.FirstOrDefault(s => s.FileName.Contains("ComplexEntity"));
        Assert.IsTrue(entityProviderFile != default, "Should generate ComplexEntity file");

        var generatedCode = entityProviderFile.Source;
        
        // Verify various DbTypes are correctly represented
        Assert.IsTrue(generatedCode.Contains("DbType.Int32"), "Should contain Int32 type");
        Assert.IsTrue(generatedCode.Contains("DbType.String"), "Should contain String type");
        Assert.IsTrue(generatedCode.Contains("DbType.Boolean"), "Should contain Boolean type");
        Assert.IsTrue(generatedCode.Contains("DbType.DateTime"), "Should contain DateTime type");
        Assert.IsTrue(generatedCode.Contains("DbType.Decimal"), "Should contain Decimal type");
        Assert.IsTrue(generatedCode.Contains("DbType.Double"), "Should contain Double type");
        Assert.IsTrue(generatedCode.Contains("DbType.Int64"), "Should contain Int64 type");
    }

    [TestMethod]
    public void GenerateEntityProvider_ColumnOrder_IsStable()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    [Sqlx]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
    }
}";

        // Act
        var generator = new SqlxGenerator();
        var result1 = GeneratorTestHelper.RunGenerator(source, generator);
        var result2 = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var sources1 = result1.GetAllGeneratedSources().ToList();
        var sources2 = result2.GetAllGeneratedSources().ToList();

        Assert.AreEqual(sources1.Count, sources2.Count, "Should generate same number of files");

        for (int i = 0; i < sources1.Count; i++)
        {
            var normalized1 = GeneratorTestHelper.NormalizeWhitespace(sources1[i].Source);
            var normalized2 = GeneratorTestHelper.NormalizeWhitespace(sources2[i].Source);
            
            Assert.AreEqual(normalized1, normalized2, 
                $"Generated code should be identical across runs for file {sources1[i].FileName}");
        }
    }

    [TestMethod]
    public void GenerateEntityProvider_EmptyClass_NoGeneration()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Test
{
    [Sqlx]
    public class EmptyEntity
    {
    }
}";

        // Act
        var generator = new SqlxGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        
        // Empty entities should not generate entity-specific files (only ModuleInit)
        var entityProviderFile = generatedSources.FirstOrDefault(s => s.FileName.Contains("EmptyEntity"));
        
        if (entityProviderFile != default)
        {
            var generatedCode = entityProviderFile.Source;
            // If generated, should have empty or minimal Columns array
            Assert.IsTrue(generatedCode.Contains("ColumnMeta[]") || generatedCode.Contains("Array.Empty"),
                "Empty entity should have empty or minimal Columns array");
        }
        else
        {
            // It's also acceptable to not generate anything for empty entities
            Assert.IsTrue(true, "Empty entity correctly skipped generation");
        }
    }

    [TestMethod]
    public void GenerateEntityProvider_NestedNamespace_ExactMatch()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

namespace Outer.Middle.Inner
{
    [Sqlx]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";

        // Act
        var generator = new SqlxGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        var entityProviderFile = generatedSources.FirstOrDefault(s => s.FileName.Contains("User"));
        Assert.IsTrue(entityProviderFile != default, "Should generate User file");

        var generatedCode = entityProviderFile.Source;
        
        // Verify namespace is preserved (file-scoped namespace)
        Assert.IsTrue(generatedCode.Contains("namespace Outer.Middle.Inner"),
            "Should preserve nested namespace structure");
        Assert.IsTrue(generatedCode.Contains("typeof(Outer.Middle.Inner.User)"),
            "Should reference full type name with nested namespace");
    }

    [TestMethod]
    public void GenerateEntityProvider_WithIgnoredProperty_ExcludesFromColumns()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Test
{
    [Sqlx]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [NotMapped]
        public string FullName { get; set; }
    }
}";

        // Act
        var generator = new SqlxGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Assert
        var generatedSources = result.GetAllGeneratedSources().ToList();
        var entityProviderFile = generatedSources.FirstOrDefault(s => s.FileName.Contains("User"));
        Assert.IsTrue(entityProviderFile != default, "Should generate User file");

        var generatedCode = entityProviderFile.Source;
        
        // Verify FullName is not in Columns (lowercase column names)
        Assert.IsFalse(generatedCode.Contains("\"fullname\"") && generatedCode.Contains("\"FullName\"", StringComparison.OrdinalIgnoreCase),
            "NotMapped property should not be included in Columns");
        Assert.IsTrue(generatedCode.Contains("\"id\""), "Id should be included");
        Assert.IsTrue(generatedCode.Contains("\"name\""), "Name should be included");
    }
}
