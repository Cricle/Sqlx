// -----------------------------------------------------------------------
// <copyright file="Integration_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.InsertReturning;

/// <summary>
/// Integration tests for [ReturnInsertedId] and [ReturnInsertedEntity] with other features
/// </summary>
[TestClass]
public class Integration_Tests : CodeGenerationTestBase
{
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("InsertReturning")]
    public void ReturnInsertedId_WithAuditFields_Should_Work()
    {
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

// Note: Entity doesn't have CreatedAt/CreatedBy properties
// AuditFields will auto-inject them as NOW() and @currentUser
[AuditFields(CreatedAtColumn = ""CreatedAt"", CreatedByColumn = ""CreatedBy"")]
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    // CreatedAt and CreatedBy will be auto-added by AuditFields
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})"")]
    [ReturnInsertedId]
    Task<long> InsertAsync(Product product, string currentUser);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // 应该包含审计字段设置（PostgreSQL使用NOW()）
        Assert.IsTrue(
            generatedCode.Contains("NOW()") || generatedCode.Contains("CURRENT_TIMESTAMP") || generatedCode.Contains("GETDATE()"),
            "应该设置CreatedAt字段");
        
        // 应该包含RETURNING子句
        Assert.IsTrue(
            generatedCode.Contains("RETURNING") || generatedCode.Contains("OUTPUT"),
            "应该包含RETURNING或OUTPUT子句");
        
        // 应该使用ExecuteScalar
        Assert.IsTrue(
            generatedCode.Contains("ExecuteScalar"),
            "应该使用ExecuteScalar获取ID");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("InsertReturning")]
    public void ReturnInsertedEntity_WithAuditFields_Should_ReturnCompleteEntity()
    {
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

// Entity doesn't have CreatedAt property - will be auto-injected
[AuditFields(CreatedAtColumn = ""CreatedAt"")]
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})"")]
    [ReturnInsertedEntity]
    Task<Product> InsertAsync(Product product);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // 应该包含审计字段设置
        Assert.IsTrue(
            generatedCode.Contains("CURRENT_TIMESTAMP") || generatedCode.Contains("NOW()") ||
            generatedCode.Contains("GETDATE()"),
            "应该使用数据库时间函数设置CreatedAt");
        
        // 应该包含RETURNING子句
        Assert.IsTrue(
            (generatedCode.Contains("RETURNING") || generatedCode.Contains("OUTPUT")),
            "应该包含RETURNING或OUTPUT子句");
        
        // 应该使用ExecuteReader
        Assert.IsTrue(
            generatedCode.Contains("ExecuteReader"),
            "应该使用ExecuteReader读取完整实体");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("InsertReturning")]
    public void ReturnInsertedId_WithConcurrencyCheck_Should_NotAffectInsert()
    {
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    [ConcurrencyCheck]
    public int Version { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})"")]
    [ReturnInsertedId]
    Task<long> InsertAsync(Product product);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // ConcurrencyCheck只影响UPDATE，不影响INSERT
        Assert.IsTrue(
            generatedCode.Contains("RETURNING") || generatedCode.Contains("OUTPUT"),
            "应该包含RETURNING或OUTPUT子句");
        
        Assert.IsTrue(
            generatedCode.Contains("ExecuteScalar"),
            "应该使用ExecuteScalar");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("InsertReturning")]
    public void ReturnInsertedEntity_WithConcurrencyCheck_Should_ReturnVersion()
    {
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    [ConcurrencyCheck]
    public int Version { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})"")]
    [ReturnInsertedEntity]
    Task<Product> InsertAsync(Product product);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // 应该返回包含Version的完整实体
        Assert.IsTrue(
            (generatedCode.Contains("RETURNING") || generatedCode.Contains("OUTPUT")) &&
            generatedCode.Contains("Version"),
            "RETURNING应该包含Version列");
        
        Assert.IsTrue(
            generatedCode.Contains("Version ="),
            "应该映射Version属性");
    }

    [TestMethod]
    [Ignore("TODO: Fix RETURNING clause generation with all features combined")]
    [TestCategory("Integration")]
    [TestCategory("InsertReturning")]
    public void ReturnInsertedId_WithAllFeatures_Should_Work()
    {
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[SoftDelete(FlagColumn = ""IsDeleted"")]
[AuditFields(CreatedAtColumn = ""CreatedAt"", CreatedByColumn = ""CreatedBy"")]
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public bool IsDeleted { get; set; }
    [ConcurrencyCheck]
    public int Version { get; set; }
    // CreatedAt and CreatedBy will be auto-injected
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})"")]
    [ReturnInsertedId]
    Task<long> InsertAsync(Product product, string currentUser);
    
    Task<Product?> GetByIdAsync(long id);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // 应该包含审计字段设置
        Assert.IsTrue(
            generatedCode.Contains("NOW()") || generatedCode.Contains("CURRENT_TIMESTAMP") || generatedCode.Contains("GETDATE()"),
            "应该包含审计字段");
        
        // 应该包含RETURNING
        Assert.IsTrue(
            generatedCode.Contains("RETURNING") || generatedCode.Contains("OUTPUT"),
            "应该包含RETURNING或OUTPUT子句");
        
        // 软删除不应影响INSERT
        // ConcurrencyCheck不应影响INSERT
        Assert.IsTrue(
            generatedCode.Contains("ExecuteScalar"),
            "应该使用ExecuteScalar");
    }

    [TestMethod]
    [Ignore("TODO: Fix RETURNING clause generation with all features combined")]
    [TestCategory("Integration")]
    [TestCategory("InsertReturning")]
    public void ReturnInsertedEntity_WithAllFeatures_Should_ReturnComplete()
    {
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[SoftDelete(FlagColumn = ""IsDeleted"")]
[AuditFields(CreatedAtColumn = ""CreatedAt"")]
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public bool IsDeleted { get; set; }
    [ConcurrencyCheck]
    public int Version { get; set; }
    // CreatedAt will be auto-injected
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})"")]
    [ReturnInsertedEntity]
    Task<Product> InsertAsync(Product product);
    
    Task<Product?> GetByIdAsync(long id);
}
";

        var generatedCode = GetCSharpGeneratedOutput(source);
        
        // 应该包含审计字段设置
        Assert.IsTrue(
            generatedCode.Contains("CURRENT_TIMESTAMP") || generatedCode.Contains("NOW()") ||
            generatedCode.Contains("GETDATE()"),
            "应该使用数据库时间函数设置CreatedAt");
        
        // 应该包含RETURNING子句
        Assert.IsTrue(
            (generatedCode.Contains("RETURNING") || generatedCode.Contains("OUTPUT")),
            "应该包含RETURNING或OUTPUT子句");
        
        // 应该使用ExecuteReader
        Assert.IsTrue(
            generatedCode.Contains("ExecuteReader"),
            "应该使用ExecuteReader读取完整实体");
    }
}

