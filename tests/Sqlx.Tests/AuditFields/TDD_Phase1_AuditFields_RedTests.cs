// -----------------------------------------------------------------------
// <copyright file="TDD_Phase1_AuditFields_RedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.AuditFields;

/// <summary>
/// TDD Phase 1: Red Tests for AuditFields feature.
/// Goal: Auto-populate CreatedAt/UpdatedAt on INSERT/UPDATE operations.
/// </summary>
[TestClass]
public class TDD_Phase1_AuditFields_RedTests : CodeGenerationTestBase
{
    /// <summary>
    /// INSERT should automatically add created_at = NOW()
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("AuditFields")]
    [TestCategory("INSERT")]
    public void AuditFields_INSERT_Should_Set_CreatedAt()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[AuditFields]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();  // 帮助推断实体类型
    
    [SqlTemplate(""INSERT INTO {{table}} (name) VALUES (@name)"")]
    Task<int> InsertAsync(string name);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var insertMethodIndex = generatedCode.IndexOf("public System.Threading.Tasks.Task<int> InsertAsync");
        Assert.IsTrue(insertMethodIndex > 0, "应该找到InsertAsync方法");
        
        var commandTextIndex = generatedCode.IndexOf("CommandText =", insertMethodIndex);
        Assert.IsTrue(commandTextIndex > 0, "应该找到CommandText");
        
        var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(500, generatedCode.Length - commandTextIndex));
        
        // 应该包含created_at字段
        Assert.IsTrue(
            sqlPart.Contains("created_at") || sqlPart.Contains("CreatedAt"),
            "应该包含created_at字段");
        
        // 应该使用数据库时间函数（PostgreSQL使用NOW()）
        Assert.IsTrue(
            sqlPart.Contains("NOW()") || sqlPart.Contains("CURRENT_TIMESTAMP") ||
            sqlPart.Contains("GETDATE") || sqlPart.Contains("datetime"),
            "应该使用数据库时间函数");
    }

    /// <summary>
    /// INSERT with CreatedBy should add created_by parameter
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("AuditFields")]
    [TestCategory("INSERT")]
    public void AuditFields_INSERT_Should_Set_CreatedBy_From_Parameter()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[AuditFields(CreatedByColumn = ""CreatedBy"")]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();  // 帮助推断实体类型
    
    [SqlTemplate(""INSERT INTO {{table}} (name) VALUES (@name)"")]
    Task<int> InsertAsync(string name, string createdBy);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var insertMethodIndex = generatedCode.IndexOf("public System.Threading.Tasks.Task<int> InsertAsync");
        Assert.IsTrue(insertMethodIndex > 0, "应该找到InsertAsync方法");
        
        var commandTextIndex = generatedCode.IndexOf("CommandText =", insertMethodIndex);
        var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(500, generatedCode.Length - commandTextIndex));
        
        // 应该包含created_by字段
        Assert.IsTrue(
            sqlPart.Contains("created_by") || sqlPart.Contains("CreatedBy"),
            "应该包含created_by字段");
        
        // 应该有@createdBy参数
        Assert.IsTrue(
            sqlPart.Contains("@createdBy") || sqlPart.Contains("@created_by"),
            "应该包含@createdBy参数");
    }

    /// <summary>
    /// UPDATE should automatically add updated_at = NOW()
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("AuditFields")]
    [TestCategory("UPDATE")]
    public void AuditFields_UPDATE_Should_Set_UpdatedAt()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[AuditFields]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();  // 帮助推断实体类型
    
    [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
    Task<int> UpdateAsync(long id, string name);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var updateMethodIndex = generatedCode.IndexOf("public System.Threading.Tasks.Task<int> UpdateAsync");
        Assert.IsTrue(updateMethodIndex > 0, "应该找到UpdateAsync方法");
        
        var commandTextIndex = generatedCode.IndexOf("CommandText =", updateMethodIndex);
        var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(500, generatedCode.Length - commandTextIndex));
        
        // 应该在SET子句中包含updated_at
        Assert.IsTrue(
            sqlPart.Contains("updated_at") || sqlPart.Contains("UpdatedAt"),
            "应该包含updated_at字段");
        
        // 应该使用数据库时间函数
        Assert.IsTrue(
            sqlPart.Contains("NOW()") || sqlPart.Contains("CURRENT_TIMESTAMP") ||
            sqlPart.Contains("GETDATE") || sqlPart.Contains("datetime"),
            "应该使用数据库时间函数");
    }

    /// <summary>
    /// UPDATE with UpdatedBy should add updated_by parameter
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("AuditFields")]
    [TestCategory("UPDATE")]
    public void AuditFields_UPDATE_Should_Set_UpdatedBy_From_Parameter()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[AuditFields(UpdatedByColumn = ""UpdatedBy"")]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();  // 帮助推断实体类型
    
    [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
    Task<int> UpdateAsync(long id, string name, string updatedBy);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var updateMethodIndex = generatedCode.IndexOf("public System.Threading.Tasks.Task<int> UpdateAsync");
        Assert.IsTrue(updateMethodIndex > 0, "应该找到UpdateAsync方法");
        
        var commandTextIndex = generatedCode.IndexOf("CommandText =", updateMethodIndex);
        var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(500, generatedCode.Length - commandTextIndex));
        
        // 应该包含updated_by字段
        Assert.IsTrue(
            sqlPart.Contains("updated_by") || sqlPart.Contains("UpdatedBy"),
            "应该包含updated_by字段");
        
        // 应该有@updatedBy参数
        Assert.IsTrue(
            sqlPart.Contains("@updatedBy") || sqlPart.Contains("@updated_by"),
            "应该包含@updatedBy参数");
    }

    /// <summary>
    /// Different databases should use appropriate timestamp functions
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("AuditFields")]
    [TestCategory("MultiDatabase")]
    public void AuditFields_Should_Use_Database_Specific_Timestamp_Functions()
    {
        // Test SQL Server - GETDATE()
        var sqlServerSource = @"
using System;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[AuditFields]
public class User
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();  // 帮助推断实体类型
    
    [SqlTemplate(""INSERT INTO {{table}} (name) VALUES (@name)"")]
    Task<int> InsertAsync(string name);
}
";

        var sqlServerCode = GetCSharpGeneratedOutput(sqlServerSource);
        
        // SQL Server应该使用GETDATE()
        Assert.IsTrue(
            sqlServerCode.Contains("GETDATE()") || sqlServerCode.Contains("CURRENT_TIMESTAMP"),
            "SQL Server应该使用GETDATE()或CURRENT_TIMESTAMP");
    }

    /// <summary>
    /// Audit fields should work together with soft delete
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Red")]
    [TestCategory("AuditFields")]
    [TestCategory("Integration")]
    public void AuditFields_Should_Work_With_SoftDelete()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

[AuditFields]
[SoftDelete]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}}"")]
    Task<List<User>> GetAllAsync();
    
    [SqlTemplate(""DELETE FROM {{table}} WHERE id = @id"")]
    Task<int> DeleteAsync(long id);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var deleteMethodIndex = generatedCode.IndexOf("public System.Threading.Tasks.Task<int> DeleteAsync");
        Assert.IsTrue(deleteMethodIndex > 0, "应该找到DeleteAsync方法");
        
        var commandTextIndex = generatedCode.IndexOf("CommandText =", deleteMethodIndex);
        var sqlPart = generatedCode.Substring(commandTextIndex, Math.Min(500, generatedCode.Length - commandTextIndex));
        
        // DELETE转UPDATE时，应该同时设置updated_at
        Assert.IsTrue(
            sqlPart.Contains("UPDATE"),
            "SoftDelete应该将DELETE转为UPDATE");
        Assert.IsTrue(
            sqlPart.Contains("is_deleted"),
            "应该设置is_deleted");
        Assert.IsTrue(
            sqlPart.Contains("updated_at") || sqlPart.Contains("UpdatedAt"),
            "软删除时应该同时设置updated_at");
    }
}

