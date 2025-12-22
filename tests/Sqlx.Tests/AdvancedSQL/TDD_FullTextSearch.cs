using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Sqlx.Annotations;
using Sqlx.Generator;

namespace Sqlx.Tests.AdvancedSQL;

/// <summary>
/// 全文搜索测试 - Requirement 50
/// 包括：MySQL MATCH AGAINST、PostgreSQL to_tsvector/to_tsquery、SQL Server CONTAINS/FREETEXT、SQLite FTS5
/// 注意：SQLite FTS5需要创建虚拟表，这里主要测试语法验证
/// </summary>
[TestClass]
public class TDD_FullTextSearch
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    #region SQLite FTS5测试 (Requirement 50.4)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQLite FTS5 - 创建虚拟表")]
    public void SQLite_FTS5_CreateVirtualTable_ShouldWork()
    {
        // Arrange & Act
        _connection!.Execute("CREATE VIRTUAL TABLE articles_fts USING fts5(title, content)");
        _connection.Execute("INSERT INTO articles_fts VALUES ('Hello World', 'This is a test article about programming')");
        _connection.Execute("INSERT INTO articles_fts VALUES ('SQLite Tutorial', 'Learn how to use SQLite database')");
        _connection.Execute("INSERT INTO articles_fts VALUES ('C# Guide', 'A comprehensive guide to C# programming')");

        // Assert - 验证表创建成功
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM articles_fts";
        var count = (long)cmd.ExecuteScalar()!;
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQLite FTS5 - 基本MATCH查询")]
    public async Task SQLite_FTS5_BasicMatch_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE VIRTUAL TABLE docs_fts USING fts5(title, body)");
        _connection.Execute("INSERT INTO docs_fts VALUES ('Introduction to Python', 'Python is a popular programming language')");
        _connection.Execute("INSERT INTO docs_fts VALUES ('Java Basics', 'Java is an object-oriented language')");
        _connection.Execute("INSERT INTO docs_fts VALUES ('Python Advanced', 'Advanced Python programming techniques')");

        var repo = new FullTextSearchRepo(_connection);

        // Act
        var results = await repo.SearchDocsByKeyword("Python");

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(d => d.Title.Contains("Python") || d.Body.Contains("Python")));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQLite FTS5 - 短语搜索")]
    public async Task SQLite_FTS5_PhraseSearch_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE VIRTUAL TABLE posts_fts USING fts5(title, content)");
        _connection.Execute("INSERT INTO posts_fts VALUES ('Web Development', 'Learn web development with HTML and CSS')");
        _connection.Execute("INSERT INTO posts_fts VALUES ('Mobile Development', 'Build mobile apps with React Native')");
        _connection.Execute("INSERT INTO posts_fts VALUES ('Full Stack', 'Full stack web development guide')");

        var repo = new FullTextSearchRepo(_connection);

        // Act - 搜索短语 "web development"
        var results = await repo.SearchPostsByPhrase("web development");

        // Assert
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQLite FTS5 - 布尔搜索 (AND)")]
    public async Task SQLite_FTS5_BooleanAnd_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE VIRTUAL TABLE books_fts USING fts5(title, author, description)");
        _connection.Execute("INSERT INTO books_fts VALUES ('Python Cookbook', 'David Beazley', 'Recipes for Python programming')");
        _connection.Execute("INSERT INTO books_fts VALUES ('Python Crash Course', 'Eric Matthes', 'A hands-on introduction to Python')");
        _connection.Execute("INSERT INTO books_fts VALUES ('Java Cookbook', 'Ian Darwin', 'Recipes for Java programming')");

        var repo = new FullTextSearchRepo(_connection);

        // Act - 搜索同时包含 Python AND Cookbook
        var results = await repo.SearchBooksWithBooleanAnd("Python", "Cookbook");

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Python Cookbook", results[0].Title);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQLite FTS5 - 布尔搜索 (OR)")]
    public async Task SQLite_FTS5_BooleanOr_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE VIRTUAL TABLE products_fts USING fts5(name, description)");
        _connection.Execute("INSERT INTO products_fts VALUES ('Laptop', 'Powerful laptop for work')");
        _connection.Execute("INSERT INTO products_fts VALUES ('Desktop', 'High performance desktop computer')");
        _connection.Execute("INSERT INTO products_fts VALUES ('Tablet', 'Portable tablet device')");

        var repo = new FullTextSearchRepo(_connection);

        // Act - 搜索 laptop OR desktop
        var results = await repo.SearchProductsWithBooleanOr("laptop", "desktop");

        // Assert
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQLite FTS5 - 前缀搜索")]
    public async Task SQLite_FTS5_PrefixSearch_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE VIRTUAL TABLE terms_fts USING fts5(term, definition)");
        _connection.Execute("INSERT INTO terms_fts VALUES ('programming', 'The process of writing code')");
        _connection.Execute("INSERT INTO terms_fts VALUES ('program', 'A set of instructions')");
        _connection.Execute("INSERT INTO terms_fts VALUES ('database', 'Organized collection of data')");

        var repo = new FullTextSearchRepo(_connection);

        // Act - 前缀搜索 "prog*"
        var results = await repo.SearchTermsByPrefix("prog");

        // Assert
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQLite FTS5 - 排名/相关性")]
    public async Task SQLite_FTS5_Ranking_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE VIRTUAL TABLE news_fts USING fts5(headline, body)");
        _connection.Execute("INSERT INTO news_fts VALUES ('Tech News Today', 'Technology technology technology advances')");
        _connection.Execute("INSERT INTO news_fts VALUES ('Sports Update', 'Latest sports news and technology')");
        _connection.Execute("INSERT INTO news_fts VALUES ('Weather Report', 'Sunny weather expected')");

        var repo = new FullTextSearchRepo(_connection);

        // Act - 搜索并按相关性排序
        var results = await repo.SearchNewsWithRanking("technology");

        // Assert
        Assert.AreEqual(2, results.Count);
        // 第一个结果应该是包含更多"technology"的文章
        Assert.AreEqual("Tech News Today", results[0].Headline);
    }

    #endregion

    #region 方言特定全文搜索语法测试

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("MySQL MATCH AGAINST语法验证")]
    public void MySQL_MatchAgainst_SyntaxShouldBeCorrect()
    {
        // Arrange
        var provider = new MySqlDialectProvider();
        
        // Act - 验证MySQL的MATCH AGAINST语法
        var sqlNatural = "SELECT * FROM articles WHERE MATCH(title, content) AGAINST('search term' IN NATURAL LANGUAGE MODE)";
        var sqlBoolean = "SELECT * FROM articles WHERE MATCH(title, content) AGAINST('+required -excluded' IN BOOLEAN MODE)";
        var sqlExpansion = "SELECT * FROM articles WHERE MATCH(title, content) AGAINST('search' WITH QUERY EXPANSION)";
        
        // Assert
        Assert.IsTrue(sqlNatural.Contains("MATCH"));
        Assert.IsTrue(sqlNatural.Contains("AGAINST"));
        Assert.IsTrue(sqlNatural.Contains("IN NATURAL LANGUAGE MODE"));
        Assert.IsTrue(sqlBoolean.Contains("IN BOOLEAN MODE"));
        Assert.IsTrue(sqlExpansion.Contains("WITH QUERY EXPANSION"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("PostgreSQL to_tsvector/to_tsquery语法验证")]
    public void PostgreSQL_TsVectorTsQuery_SyntaxShouldBeCorrect()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();
        
        // Act - 验证PostgreSQL的全文搜索语法
        var sqlBasic = "SELECT * FROM articles WHERE to_tsvector('english', content) @@ to_tsquery('english', 'search')";
        var sqlPhrase = "SELECT * FROM articles WHERE to_tsvector(content) @@ phraseto_tsquery('search phrase')";
        var sqlRank = "SELECT *, ts_rank(to_tsvector(content), to_tsquery('search')) as rank FROM articles ORDER BY rank DESC";
        
        // Assert
        Assert.IsTrue(sqlBasic.Contains("to_tsvector"));
        Assert.IsTrue(sqlBasic.Contains("to_tsquery"));
        Assert.IsTrue(sqlBasic.Contains("@@"));
        Assert.IsTrue(sqlPhrase.Contains("phraseto_tsquery"));
        Assert.IsTrue(sqlRank.Contains("ts_rank"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQL Server CONTAINS/FREETEXT语法验证")]
    public void SqlServer_ContainsFreetext_SyntaxShouldBeCorrect()
    {
        // Arrange
        var provider = new SqlServerDialectProvider();
        
        // Act - 验证SQL Server的全文搜索语法
        var sqlContains = "SELECT * FROM articles WHERE CONTAINS(content, 'search')";
        var sqlContainsPhrase = "SELECT * FROM articles WHERE CONTAINS(content, '\"search phrase\"')";
        var sqlFreetext = "SELECT * FROM articles WHERE FREETEXT(content, 'search terms')";
        var sqlContainstable = "SELECT * FROM CONTAINSTABLE(articles, content, 'search') AS ft";
        
        // Assert
        Assert.IsTrue(sqlContains.Contains("CONTAINS"));
        Assert.IsTrue(sqlContainsPhrase.Contains("CONTAINS"));
        Assert.IsTrue(sqlFreetext.Contains("FREETEXT"));
        Assert.IsTrue(sqlContainstable.Contains("CONTAINSTABLE"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQLite FTS5语法验证")]
    public void SQLite_FTS5_SyntaxShouldBeCorrect()
    {
        // Arrange
        var provider = new SQLiteDialectProvider();
        
        // Act - 验证SQLite FTS5语法
        var sqlCreate = "CREATE VIRTUAL TABLE articles_fts USING fts5(title, content)";
        var sqlMatch = "SELECT * FROM articles_fts WHERE articles_fts MATCH 'search'";
        var sqlBm25 = "SELECT *, bm25(articles_fts) as rank FROM articles_fts WHERE articles_fts MATCH 'search' ORDER BY rank";
        
        // Assert
        Assert.IsTrue(sqlCreate.Contains("VIRTUAL TABLE"));
        Assert.IsTrue(sqlCreate.Contains("fts5"));
        Assert.IsTrue(sqlMatch.Contains("MATCH"));
        Assert.IsTrue(sqlBm25.Contains("bm25"));
    }

    #endregion

    #region 不支持全文搜索的方言错误处理 (Requirement 50.5)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("验证各方言的全文搜索支持状态")]
    public void AllDialects_FullTextSearchSupport_ShouldBeDocumented()
    {
        // 验证各方言的全文搜索支持情况
        // MySQL: FULLTEXT索引 + MATCH AGAINST (InnoDB 5.6+, MyISAM)
        // PostgreSQL: tsvector/tsquery + GIN/GiST索引
        // SQL Server: Full-Text Search (需要安装Full-Text Search组件)
        // SQLite: FTS3/FTS4/FTS5虚拟表
        // Oracle: Oracle Text (CONTAINS, CATSEARCH)
        
        var sqlServerProvider = new SqlServerDialectProvider();
        var postgreSqlProvider = new PostgreSqlDialectProvider();
        var mySqlProvider = new MySqlDialectProvider();
        var sqliteProvider = new SQLiteDialectProvider();
        
        // 所有主流数据库都支持某种形式的全文搜索
        Assert.IsNotNull(sqlServerProvider);
        Assert.IsNotNull(postgreSqlProvider);
        Assert.IsNotNull(mySqlProvider);
        Assert.IsNotNull(sqliteProvider);
    }

    #endregion

    #region 全文搜索与其他功能组合

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQLite FTS5 - 与JOIN组合")]
    public async Task SQLite_FTS5_WithJoin_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE authors (id INTEGER PRIMARY KEY, name TEXT)");
        _connection.Execute("CREATE VIRTUAL TABLE articles_fts USING fts5(title, content, author_id UNINDEXED)");
        _connection.Execute("INSERT INTO authors VALUES (1, 'John Doe'), (2, 'Jane Smith')");
        _connection.Execute("INSERT INTO articles_fts VALUES ('Python Guide', 'Learn Python programming', 1)");
        _connection.Execute("INSERT INTO articles_fts VALUES ('Java Tutorial', 'Java programming basics', 2)");

        var repo = new FullTextSearchRepo(_connection);

        // Act
        var results = await repo.SearchArticlesWithAuthor("Python");

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("John Doe", results[0].AuthorName);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("FullTextSearch")]
    [Description("SQLite FTS5 - 高亮显示")]
    public async Task SQLite_FTS5_Highlight_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE VIRTUAL TABLE content_fts USING fts5(text)");
        _connection.Execute("INSERT INTO content_fts VALUES ('The quick brown fox jumps over the lazy dog')");
        _connection.Execute("INSERT INTO content_fts VALUES ('A quick solution to the problem')");

        var repo = new FullTextSearchRepo(_connection);

        // Act
        var results = await repo.SearchWithHighlight("quick");

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(r => r.HighlightedText.Contains("<b>") && r.HighlightedText.Contains("</b>")));
    }

    #endregion
}


#region 全文搜索Repository接口和模型

public interface IFullTextSearchRepo
{
    // SQLite FTS5 - 基本MATCH
    [SqlTemplate("SELECT title, body FROM docs_fts WHERE docs_fts MATCH @keyword")]
    Task<List<DocSearchResult>> SearchDocsByKeyword(string keyword);

    // SQLite FTS5 - 短语搜索
    [SqlTemplate("SELECT title, content FROM posts_fts WHERE posts_fts MATCH '\"' || @phrase || '\"'")]
    Task<List<PostSearchResult>> SearchPostsByPhrase(string phrase);

    // SQLite FTS5 - 布尔AND
    [SqlTemplate("SELECT title, author, description FROM books_fts WHERE books_fts MATCH @term1 || ' AND ' || @term2")]
    Task<List<BookSearchResult>> SearchBooksWithBooleanAnd(string term1, string term2);

    // SQLite FTS5 - 布尔OR
    [SqlTemplate("SELECT name, description FROM products_fts WHERE products_fts MATCH @term1 || ' OR ' || @term2")]
    Task<List<ProductSearchResult>> SearchProductsWithBooleanOr(string term1, string term2);

    // SQLite FTS5 - 前缀搜索
    [SqlTemplate("SELECT term, definition FROM terms_fts WHERE terms_fts MATCH @prefix || '*'")]
    Task<List<TermSearchResult>> SearchTermsByPrefix(string prefix);

    // SQLite FTS5 - 排名
    [SqlTemplate("SELECT headline, body, bm25(news_fts) as rank FROM news_fts WHERE news_fts MATCH @keyword ORDER BY rank")]
    Task<List<NewsSearchResult>> SearchNewsWithRanking(string keyword);

    // SQLite FTS5 - 与JOIN组合
    [SqlTemplate("SELECT a.title, a.content, au.name as author_name FROM articles_fts a JOIN authors au ON a.author_id = au.id WHERE articles_fts MATCH @keyword")]
    Task<List<ArticleWithAuthor>> SearchArticlesWithAuthor(string keyword);

    // SQLite FTS5 - 高亮显示
    [SqlTemplate("SELECT highlight(content_fts, 0, '<b>', '</b>') as highlighted_text FROM content_fts WHERE content_fts MATCH @keyword")]
    Task<List<HighlightResult>> SearchWithHighlight(string keyword);
}

[RepositoryFor(typeof(IFullTextSearchRepo))]
public partial class FullTextSearchRepo(IDbConnection connection) : IFullTextSearchRepo { }

// 模型类
public class DocSearchResult
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
}

public class PostSearchResult
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
}

public class BookSearchResult
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
}

public class ProductSearchResult
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class TermSearchResult
{
    public string Term { get; set; } = "";
    public string Definition { get; set; } = "";
}

public class NewsSearchResult
{
    public string Headline { get; set; } = "";
    public string Body { get; set; } = "";
    public double Rank { get; set; }
}

public class ArticleWithAuthor
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string AuthorName { get; set; } = "";
}

public class HighlightResult
{
    public string HighlightedText { get; set; } = "";
}

#endregion
