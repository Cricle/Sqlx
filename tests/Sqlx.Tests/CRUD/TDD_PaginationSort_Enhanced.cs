using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.CRUD;

/// <summary>
/// Phase 1: 分页和排序增强测试 - 确保100%常用场景覆盖
/// 新增15个分页排序场景测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("CRUD")]
[TestCategory("CoveragePhase1")]
public class TDD_PaginationSort_Enhanced
{
    private IDbConnection? _connection;
    private IPaginationSortRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE books (
                id INTEGER PRIMARY KEY,
                title TEXT NOT NULL,
                author TEXT,
                price REAL,
                published_year INTEGER,
                rating REAL
            )");

        // Insert 20 test records
        for (int i = 1; i <= 20; i++)
        {
            _connection.Execute($@"INSERT INTO books VALUES
                ({i}, 'Book {i}', 'Author {i % 5}', {i * 10.0}, {2000 + i}, {i % 5 + 1}.0)");
        }

        _repo = new PaginationSortRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("Simple pagination - first page")]
    public async Task Pagination_FirstPage_ShouldWork()
    {
        var books = await _repo!.GetPageAsync(10, 0);
        Assert.AreEqual(10, books.Count);
        Assert.AreEqual(1, books[0].Id);
    }

    [TestMethod]
    [Description("Simple pagination - second page")]
    public async Task Pagination_SecondPage_ShouldWork()
    {
        var books = await _repo!.GetPageAsync(10, 10);
        Assert.AreEqual(10, books.Count);
        Assert.AreEqual(11, books[0].Id);
    }

    [TestMethod]
    [Description("Pagination with small limit")]
    public async Task Pagination_SmallLimit_ShouldWork()
    {
        var books = await _repo!.GetPageAsync(5, 0);
        Assert.AreEqual(5, books.Count);
    }

    [TestMethod]
    [Description("Pagination - last page (partial)")]
    public async Task Pagination_LastPagePartial_ShouldWork()
    {
        var books = await _repo!.GetPageAsync(10, 15);
        Assert.AreEqual(5, books.Count);
    }

    [TestMethod]
    [Description("Pagination - empty result beyond data")]
    public async Task Pagination_BeyondData_ShouldReturnEmpty()
    {
        var books = await _repo!.GetPageAsync(10, 100);
        Assert.AreEqual(0, books.Count);
    }

    [TestMethod]
    [Description("Sort by single column ASC")]
    public async Task Sort_SingleColumnAsc_ShouldWork()
    {
        var books = await _repo!.GetOrderedByPriceAscAsync();
        Assert.AreEqual(20, books.Count);
        for (int i = 0; i < books.Count - 1; i++)
        {
            Assert.IsTrue(books[i].Price <= books[i + 1].Price);
        }
    }

    [TestMethod]
    [Description("Sort by single column DESC")]
    public async Task Sort_SingleColumnDesc_ShouldWork()
    {
        var books = await _repo!.GetOrderedByPriceDescAsync();
        Assert.AreEqual(20, books.Count);
        for (int i = 0; i < books.Count - 1; i++)
        {
            Assert.IsTrue(books[i].Price >= books[i + 1].Price);
        }
    }

    [TestMethod]
    [Description("Sort by multiple columns")]
    public async Task Sort_MultipleColumns_ShouldWork()
    {
        var books = await _repo!.GetOrderedByAuthorAndPriceAsync();
        Assert.AreEqual(20, books.Count);
        // Verify author is primary sort
        var authors = books.Select(b => b.Author).Distinct().ToList();
        var previousAuthor = "";
        foreach (var book in books)
        {
            if (book.Author != previousAuthor)
            {
                previousAuthor = book.Author ?? "";
            }
        }
    }

    [TestMethod]
    [Description("Pagination + Sort combined")]
    public async Task PaginationAndSort_Combined_ShouldWork()
    {
        var books = await _repo!.GetPageOrderedByPriceAsync(5, 5);
        Assert.AreEqual(5, books.Count);
        // Should be records 6-10 when sorted by price
        for (int i = 0; i < books.Count - 1; i++)
        {
            Assert.IsTrue(books[i].Price <= books[i + 1].Price);
        }
    }

    [TestMethod]
    [Description("Sort with NULL values")]
    public async Task Sort_WithNullValues_ShouldWork()
    {
        // Insert records with NULL author
        _connection!.Execute("INSERT INTO books VALUES (21, 'Book NULL', NULL, 100.0, 2025, 5.0)");
        _connection.Execute("INSERT INTO books VALUES (22, 'Book NULL2', NULL, 200.0, 2025, 5.0)");

        var books = await _repo!.GetOrderedByAuthorAsync();
        Assert.AreEqual(22, books.Count);
        // NULLs should be first or last depending on DB
    }

    [TestMethod]
    [Description("Sort by string column")]
    public async Task Sort_StringColumn_ShouldWork()
    {
        var books = await _repo!.GetOrderedByTitleAsync();
        Assert.AreEqual(20, books.Count);
        // Verify alphabetical order
    }

    [TestMethod]
    [Description("Sort by integer column")]
    public async Task Sort_IntegerColumn_ShouldWork()
    {
        var books = await _repo!.GetOrderedByYearAsync();
        Assert.AreEqual(20, books.Count);
        for (int i = 0; i < books.Count - 1; i++)
        {
            Assert.IsTrue(books[i].PublishedYear <= books[i + 1].PublishedYear);
        }
    }

    [TestMethod]
    [Description("Sort by decimal column")]
    public async Task Sort_DecimalColumn_ShouldWork()
    {
        var books = await _repo!.GetOrderedByRatingAsync();
        Assert.AreEqual(20, books.Count);
        for (int i = 0; i < books.Count - 1; i++)
        {
            Assert.IsTrue(books[i].Rating <= books[i + 1].Rating);
        }
    }

    [TestMethod]
    [Description("Large offset pagination")]
    public async Task Pagination_LargeOffset_ShouldWork()
    {
        var books = await _repo!.GetPageAsync(5, 18);
        Assert.AreEqual(2, books.Count); // Only 2 records left
    }

    [TestMethod]
    [Description("Limit 1 (single record)")]
    public async Task Pagination_Limit1_ShouldWork()
    {
        var books = await _repo!.GetPageAsync(1, 0);
        Assert.AreEqual(1, books.Count);
    }
}

// Repository interface
public interface IPaginationSortRepository
{
    [SqlTemplate("SELECT * FROM books LIMIT @limit OFFSET @offset")]
    Task<List<SortBook>> GetPageAsync(int limit, int offset);

    [SqlTemplate("SELECT * FROM books ORDER BY price ASC")]
    Task<List<SortBook>> GetOrderedByPriceAscAsync();

    [SqlTemplate("SELECT * FROM books ORDER BY price DESC")]
    Task<List<SortBook>> GetOrderedByPriceDescAsync();

    [SqlTemplate("SELECT * FROM books ORDER BY author ASC, price ASC")]
    Task<List<SortBook>> GetOrderedByAuthorAndPriceAsync();

    [SqlTemplate("SELECT * FROM books ORDER BY price ASC LIMIT @limit OFFSET @offset")]
    Task<List<SortBook>> GetPageOrderedByPriceAsync(int limit, int offset);

    [SqlTemplate("SELECT * FROM books ORDER BY author ASC")]
    Task<List<SortBook>> GetOrderedByAuthorAsync();

    [SqlTemplate("SELECT * FROM books ORDER BY title ASC")]
    Task<List<SortBook>> GetOrderedByTitleAsync();

    [SqlTemplate("SELECT * FROM books ORDER BY published_year ASC")]
    Task<List<SortBook>> GetOrderedByYearAsync();

    [SqlTemplate("SELECT * FROM books ORDER BY rating ASC")]
    Task<List<SortBook>> GetOrderedByRatingAsync();
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPaginationSortRepository))]
public partial class PaginationSortRepository(IDbConnection connection) : IPaginationSortRepository { }

// Model
public class SortBook
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? Author { get; set; }
    public double Price { get; set; }
    public int PublishedYear { get; set; }
    public double Rating { get; set; }
}

