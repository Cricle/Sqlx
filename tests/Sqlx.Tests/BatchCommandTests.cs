// -----------------------------------------------------------------------
// <copyright file="BatchCommandTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.SqlGen;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;

namespace Sqlx.Tests;

[TestClass]
public class BatchCommandTests : CodeGenerationTestBase
{
    [TestMethod]
    public void BatchCommand_WithCollectionParameter_GeneratesCorrectCode()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.SqlGen;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ITestService
    {
        [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""test_entities"")]
        int BatchInsertEntities(IEnumerable<TestEntity> entities);
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

        // Assert
        Assert.IsTrue(result.Contains("DbBatch"), "Should use DbBatch for BatchCommand");
        Assert.IsTrue(result.Contains("CreateBatchCommand"), "Should create batch commands");
        Assert.IsTrue(result.Contains("BatchCommands.Add"), "Should add commands to batch");
        Assert.IsTrue(result.Contains("ExecuteNonQuery"), "Should execute the batch");
    }

    [TestMethod]
    public void BatchCommand_AsyncMethod_GeneratesAsyncExecution()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.SqlGen;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ITestService
    {
        [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""test_entities"")]
        Task<int> BatchInsertEntitiesAsync(IEnumerable<TestEntity> entities);
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

        // Assert
        Assert.IsTrue(result.Contains("ExecuteNonQueryAsync"), "Should use async execution for async methods");
        Assert.IsTrue(result.Contains("await"), "Should use await for async operations");
    }

    [TestMethod]
    public void BatchCommand_WithoutCollectionParameter_GeneratesError()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.SqlGen;

namespace TestNamespace
{
    public interface ITestService
    {
        [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""test_entities"")]
        int BatchInsertEntities(int id, string name);
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

        // Assert
        Assert.IsTrue(result.Contains("BatchCommand requires a collection parameter"), 
            "Should generate error for missing collection parameter");
    }

    [TestMethod]
    public void BatchCommand_WithRawSql_GeneratesCorrectImplementation()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.SqlGen;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public interface ITestService
    {
        [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""test_entities"")]
        [RawSql(""INSERT INTO test_entities (Name, Price) VALUES (@name, @price)"")]
        int BatchInsertWithSql(IEnumerable<TestEntity> entities);
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

        // Assert
        Assert.IsTrue(result.Contains("DbBatch"), "Should use DbBatch for BatchCommand");
        Assert.IsTrue(result.Contains("INSERT INTO test_entities"), "Should include raw SQL");
        Assert.IsTrue(result.Contains("CreateBatchCommand"), "Should create batch commands");
    }

    [TestMethod]
    public void BatchCommand_WithComplexEntity_HandlesAllProperties()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.SqlGen;

namespace TestNamespace
{
    public class ComplexEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int? CategoryId { get; set; }
    }

    public interface ITestService
    {
        [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""complex_entities"")]
        Task<int> BatchInsertComplexAsync(IEnumerable<ComplexEntity> entities);
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

        // Assert
        Assert.IsTrue(result.Contains("param_id"), "Should handle Id property");
        Assert.IsTrue(result.Contains("param_name"), "Should handle Name property");
        Assert.IsTrue(result.Contains("param_price"), "Should handle Price property");
        Assert.IsTrue(result.Contains("param_createdat"), "Should handle CreatedAt property");
        Assert.IsTrue(result.Contains("param_isactive"), "Should handle IsActive property");
        Assert.IsTrue(result.Contains("param_categoryid"), "Should handle CategoryId property");
        Assert.IsTrue(result.Contains("DBNull.Value"), "Should handle null values");
    }

    [TestMethod]
    public void BatchCommand_WithTransaction_IncludesTransactionHandling()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.SqlGen;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ITestService
    {
        [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""test_entities"")]
        Task<int> BatchInsertWithTransactionAsync(IEnumerable<TestEntity> entities, DbTransaction transaction);
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

        // Assert
        Assert.IsTrue(result.Contains("Transaction = transaction"), "Should set transaction on batch command");
    }

    [TestMethod]
    public void BatchCommand_ReturnsCorrectType_BasedOnMethodSignature()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.SqlGen;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ITestService
    {
        [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""test_entities"")]
        void BatchInsertVoid(IEnumerable<TestEntity> entities);

        [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""test_entities"")]
        int BatchInsertInt(IEnumerable<TestEntity> entities);

        [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""test_entities"")]
        Task BatchInsertTaskVoid(IEnumerable<TestEntity> entities);

        [SqlExecuteType(SqlExecuteTypes.BatchCommand, ""test_entities"")]
        Task<int> BatchInsertTaskInt(IEnumerable<TestEntity> entities);
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

        // Assert
        // Check for different return handling based on method signatures
        var voidMethods = result.Split("ExecuteNonQuery").Length - 1;
        var asyncMethods = result.Split("ExecuteNonQueryAsync").Length - 1;
        
        Assert.IsTrue(voidMethods > 0, "Should generate ExecuteNonQuery for sync methods");
        Assert.IsTrue(asyncMethods > 0, "Should generate ExecuteNonQueryAsync for async methods");
        Assert.IsTrue(result.Contains("return result"), "Should return result for int methods");
    }
}
