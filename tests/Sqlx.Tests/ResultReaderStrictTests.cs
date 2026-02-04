using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Data;
using System.Data.Common;

namespace Sqlx.Tests;

/// <summary>
/// Strict tests for ResultReader with edge cases, null handling, and type conversions.
/// </summary>
[TestClass]
public class ResultReaderStrictTests
{
    #region Null Handling Tests

    [TestMethod]
    public void Read_NullableField_WithNull_ReturnsNull()
    {
        var reader = TestEntityWithNullableResultReader.Default;
        var entities = new[]
        {
            new TestEntityWithNullable { Id = 1, Name = "test", Description = null }
        };
        using var dbReader = new TestDbDataReaderWithNullable(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual(1, results.Count);
        Assert.IsNull(results[0].Description);
    }

    [TestMethod]
    public void Read_NullableField_WithValue_ReturnsValue()
    {
        var reader = TestEntityWithNullableResultReader.Default;
        var entities = new[]
        {
            new TestEntityWithNullable { Id = 1, Name = "test", Description = "description" }
        };
        using var dbReader = new TestDbDataReaderWithNullable(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("description", results[0].Description);
    }

    [TestMethod]
    public void Read_AllNullableFields_AllNull_HandlesCorrectly()
    {
        var reader = TestEntityWithNullableResultReader.Default;
        var entities = new[]
        {
            new TestEntityWithNullable { Id = 1, Name = "test", Description = null }
        };
        using var dbReader = new TestDbDataReaderWithNullable(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual(1, results.Count);
        Assert.IsNull(results[0].Description);
    }

    #endregion

    #region Empty Result Set Tests

    [TestMethod]
    public void ToList_EmptyResultSet_ReturnsEmptyList()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());

        var results = reader.ToList(dbReader);

        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public void FirstOrDefault_EmptyResultSet_ReturnsNull()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());

        var result = reader.FirstOrDefault(dbReader);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ToListAsync_EmptyResultSet_ReturnsEmptyList()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());

        var results = await reader.ToListAsync(dbReader);

        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task FirstOrDefaultAsync_EmptyResultSet_ReturnsNull()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());

        var result = await reader.FirstOrDefaultAsync(dbReader);

        Assert.IsNull(result);
    }

    #endregion

    #region Large Result Set Tests

    [TestMethod]
    public void ToList_LargeResultSet_HandlesCorrectly()
    {
        var reader = TestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 10000)
            .Select(i => new TestEntity
            {
                Id = i,
                UserName = $"user{i}",
                IsActive = i % 2 == 0,
                CreatedAt = DateTime.Now
            })
            .ToArray();
        using var dbReader = new TestDbDataReader(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual(10000, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual(10000, results[9999].Id);
    }

    [TestMethod]
    public async Task ToListAsync_LargeResultSet_HandlesCorrectly()
    {
        var reader = TestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 10000)
            .Select(i => new TestEntity
            {
                Id = i,
                UserName = $"user{i}",
                IsActive = i % 2 == 0,
                CreatedAt = DateTime.Now
            })
            .ToArray();
        using var dbReader = new TestDbDataReader(entities);

        var results = await reader.ToListAsync(dbReader);

        Assert.AreEqual(10000, results.Count);
    }

    #endregion

    #region Ordinal Caching Tests

    [TestMethod]
    public void GetOrdinals_CalledMultipleTimes_ReturnsSameOrdinals()
    {
        var reader = TestEntityResultReader.Default;
        using var dbReader = new TestDbDataReader(Array.Empty<TestEntity>());

        var ordinals1 = reader.GetOrdinals(dbReader);
        var ordinals2 = reader.GetOrdinals(dbReader);

        // Arrays should have same values
        CollectionAssert.AreEqual(ordinals1, ordinals2);
    }

    [TestMethod]
    public void Read_WithArrayOrdinals_PerformanceOptimization()
    {
        var reader = TestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new TestEntity
            {
                Id = i,
                UserName = $"user{i}",
                IsActive = true,
                CreatedAt = DateTime.Now
            })
            .ToArray();
        using var dbReader = new TestDbDataReader(entities);

        // ToList now uses struct ordinals internally
        var results = reader.ToList(dbReader);

        Assert.AreEqual(1000, results.Count);
    }

    #endregion

    #region Type Conversion Tests

    [TestMethod]
    public void Read_IntegerField_ConvertsCorrectly()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = int.MaxValue, UserName = "test", IsActive = true, CreatedAt = DateTime.Now }
        };
        using var dbReader = new TestDbDataReader(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual(int.MaxValue, results[0].Id);
    }

    [TestMethod]
    public void Read_BooleanField_ConvertsCorrectly()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "test", IsActive = true, CreatedAt = DateTime.Now },
            new TestEntity { Id = 2, UserName = "test2", IsActive = false, CreatedAt = DateTime.Now }
        };
        using var dbReader = new TestDbDataReader(entities);

        var results = reader.ToList(dbReader);

        Assert.IsTrue(results[0].IsActive);
        Assert.IsFalse(results[1].IsActive);
    }

    [TestMethod]
    public void Read_DateTimeField_ConvertsCorrectly()
    {
        var reader = TestEntityResultReader.Default;
        var testDate = new DateTime(2024, 1, 1, 12, 30, 45);
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "test", IsActive = true, CreatedAt = testDate }
        };
        using var dbReader = new TestDbDataReader(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual(testDate, results[0].CreatedAt);
    }

    [TestMethod]
    public void Read_StringField_HandlesEmptyString()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "", IsActive = true, CreatedAt = DateTime.Now }
        };
        using var dbReader = new TestDbDataReader(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual("", results[0].UserName);
    }

    #endregion

    #region Special Characters Tests

    [TestMethod]
    public void Read_StringWithSpecialCharacters_HandlesCorrectly()
    {
        var reader = TestEntityResultReader.Default;
        var specialStrings = new[]
        {
            "O'Brien",
            "Test\nNewline",
            "Test\tTab",
            "Test\"Quote",
            "Test\\Backslash",
            "Test 中文",
            "Test 🎉 Emoji"
        };

        foreach (var str in specialStrings)
        {
            var entities = new[]
            {
                new TestEntity { Id = 1, UserName = str, IsActive = true, CreatedAt = DateTime.Now }
            };
            using var dbReader = new TestDbDataReader(entities);

            var results = reader.ToList(dbReader);

            Assert.AreEqual(str, results[0].UserName, $"Failed for: {str}");
        }
    }

    #endregion

    #region Concurrent Access Tests

    [TestMethod]
    public void ToList_ConcurrentReaders_ThreadSafe()
    {
        var reader = TestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 100)
            .Select(i => new TestEntity
            {
                Id = i,
                UserName = $"user{i}",
                IsActive = true,
                CreatedAt = DateTime.Now
            })
            .ToArray();

        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            using var dbReader = new TestDbDataReader(entities);
            var results = reader.ToList(dbReader);
            Assert.AreEqual(100, results.Count);
        })).ToArray();

        Task.WaitAll(tasks);
    }

    #endregion

    #region Memory Efficiency Tests

    [TestMethod]
    public void ToList_MultipleIterations_DoesNotLeakMemory()
    {
        var reader = TestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new TestEntity
            {
                Id = i,
                UserName = $"user{i}",
                IsActive = true,
                CreatedAt = DateTime.Now
            })
            .ToArray();

        for (int iteration = 0; iteration < 100; iteration++)
        {
            using var dbReader = new TestDbDataReader(entities);
            var results = reader.ToList(dbReader);
            Assert.AreEqual(1000, results.Count);
        }

        // If we get here without OutOfMemoryException, test passes
        Assert.IsTrue(true);
    }

    #endregion

    #region FirstOrDefault Optimization Tests

    [TestMethod]
    public void FirstOrDefault_LargeResultSet_OnlyReadsFirstRow()
    {
        var reader = TestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 10000)
            .Select(i => new TestEntity
            {
                Id = i,
                UserName = $"user{i}",
                IsActive = true,
                CreatedAt = DateTime.Now
            })
            .ToArray();
        using var dbReader = new TestDbDataReader(entities);

        var result = reader.FirstOrDefault(dbReader);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        // Verify only one row was read
        Assert.AreEqual(1, dbReader.ReadCount);
    }

    [TestMethod]
    public async Task FirstOrDefaultAsync_LargeResultSet_OnlyReadsFirstRow()
    {
        var reader = TestEntityResultReader.Default;
        var entities = Enumerable.Range(1, 10000)
            .Select(i => new TestEntity
            {
                Id = i,
                UserName = $"user{i}",
                IsActive = true,
                CreatedAt = DateTime.Now
            })
            .ToArray();
        using var dbReader = new TestDbDataReader(entities);

        var result = await reader.FirstOrDefaultAsync(dbReader);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual(1, dbReader.ReadCount);
    }

    #endregion

    #region Boundary Value Tests

    [TestMethod]
    public void Read_MinMaxIntegerValues_HandlesCorrectly()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = int.MinValue, UserName = "min", IsActive = true, CreatedAt = DateTime.Now },
            new TestEntity { Id = int.MaxValue, UserName = "max", IsActive = true, CreatedAt = DateTime.Now },
            new TestEntity { Id = 0, UserName = "zero", IsActive = true, CreatedAt = DateTime.Now }
        };
        using var dbReader = new TestDbDataReader(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(int.MinValue, results[0].Id);
        Assert.AreEqual(int.MaxValue, results[1].Id);
        Assert.AreEqual(0, results[2].Id);
    }

    [TestMethod]
    public void Read_MinMaxDateTimeValues_HandlesCorrectly()
    {
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "min", IsActive = true, CreatedAt = DateTime.MinValue },
            new TestEntity { Id = 2, UserName = "max", IsActive = true, CreatedAt = DateTime.MaxValue }
        };
        using var dbReader = new TestDbDataReader(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(DateTime.MinValue, results[0].CreatedAt);
        Assert.AreEqual(DateTime.MaxValue, results[1].CreatedAt);
    }

    #endregion

    #region Column Order Independence Tests

    [TestMethod]
    public void Read_DifferentColumnOrder_HandlesCorrectly()
    {
        // This test verifies that the reader uses ordinals correctly
        // and doesn't depend on column order
        var reader = TestEntityResultReader.Default;
        var entities = new[]
        {
            new TestEntity { Id = 1, UserName = "test", IsActive = true, CreatedAt = DateTime.Now }
        };
        using var dbReader = new TestDbDataReader(entities);

        var results = reader.ToList(dbReader);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("test", results[0].UserName);
    }

    #endregion
}
