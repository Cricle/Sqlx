# Contributing Guide

Thank you for your interest in the Sqlx ORM framework! We welcome all forms of contributions, including code contributions, documentation improvements, issue reports, and feature suggestions.

## 🤝 How to Contribute

### 1️⃣ Reporting Issues

If you've found a bug or have a feature suggestion, please report it through GitHub Issues:

1. **Search existing issues** - Make sure the issue hasn't been reported already
2. **Use issue templates** - Choose the appropriate issue template
3. **Provide detailed information** - Include reproduction steps, environment info, etc.
4. **Add labels** - Select appropriate labels (bug, enhancement, question, etc.)

#### Issue Report Template

```markdown
**Issue Description**
Brief description of the issue encountered

**Reproduction Steps**
1. First...
2. Then...
3. Next...
4. See error

**Expected Behavior**
Describe the expected correct behavior

**Environment Information**
- OS: [e.g., Windows 10]
- .NET Version: [e.g., .NET 9.0]
- Sqlx Version: [e.g., 3.0.0]
- Database: [e.g., SQL Server 2022]

**Additional Information**
Any other information that might help resolve the issue
```

### 2️⃣ Contributing Code

We welcome code contributions! Here's how to get started:

#### Development Setup

1. **Fork the repository**
```bash
git clone https://github.com/your-username/Sqlx.git
cd Sqlx
   ```

2. **Install dependencies**
   ```bash
dotnet restore
   ```

3. **Build the project**
   ```bash
dotnet build
   ```

4. **Run tests**
   ```bash
dotnet test
```

#### Development Environment

- **IDE**: Visual Studio 2022 or VS Code with C# extension
- **.NET SDK**: .NET 8.0 or later
- **Database**: SQL Server, MySQL, PostgreSQL, or SQLite for testing

### 3️⃣ Pull Request Process

1. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes**
   - Follow coding standards (see below)
   - Add tests for new functionality
   - Update documentation if needed

3. **Run quality checks**
   ```bash
   dotnet build
   dotnet test
   dotnet format
   ```

4. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat: add new feature description"
   ```

5. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Create Pull Request**
   - Use a clear, descriptive title
   - Fill out the PR template
   - Link related issues
   - Request review from maintainers

#### Pull Request Template

```markdown
**Description**
Brief description of the changes

**Changes Made**
- [ ] Added new feature X
- [ ] Fixed bug Y
- [ ] Updated documentation
- [ ] Added tests

**Related Issues**
Closes #123

**Testing**
- [ ] All existing tests pass
- [ ] New tests added for new functionality
- [ ] Manual testing completed

**Checklist**
- [ ] Code follows project coding standards
- [ ] Documentation updated (if applicable)
- [ ] Tests added/updated
- [ ] CHANGELOG.md updated (if applicable)
```

## 📋 Coding Standards

### Code Style

We use **StyleCop** for code style enforcement. Key guidelines:

```csharp
// ✅ Good: Proper naming and structure
public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;
    
    public UserRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    public async Task<User?> GetByIdAsync(int id)
    {
        var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
        var sql = template.Execute(new { id });
        return await _connection.QueryFirstOrDefaultAsync<User>(sql.Sql, sql.Parameters);
    }
}
```

### Naming Conventions

- **Classes**: PascalCase (`UserService`, `SqlTemplate`)
- **Methods**: PascalCase (`GetByIdAsync`, `ExecuteAsync`)
- **Properties**: PascalCase (`ConnectionString`, `IsValid`)
- **Fields**: camelCase with underscore (`_connection`, `_logger`)
- **Parameters**: camelCase (`userId`, `connectionString`)
- **Local variables**: camelCase (`user`, `sqlResult`)

### Documentation

#### XML Documentation
All public APIs must have XML documentation:

```csharp
/// <summary>
/// Creates a new SQL template from the specified SQL string.
/// </summary>
/// <param name="sql">The SQL template string containing parameter placeholders.</param>
/// <returns>A new SqlTemplate instance.</returns>
/// <exception cref="ArgumentException">Thrown when sql is null or empty.</exception>
public static SqlTemplate Parse(string sql)
{
    // Implementation...
}
```

#### Code Comments
Use comments for complex logic:

```csharp
// Convert LINQ expression to SQL WHERE clause
// Handle both simple comparisons and complex nested expressions
var whereClause = ExpressionTranslator.Translate(predicate, dialect);
```

### Testing Guidelines

#### Unit Tests
- Use **MSTest** framework
- Follow AAA pattern (Arrange, Act, Assert)
- Test both positive and negative scenarios
- Use descriptive test names

```csharp
    [TestMethod]
public void SqlTemplate_Parse_WithValidSql_ReturnsTemplate()
    {
        // Arrange
    var sql = "SELECT * FROM Users WHERE Id = @id";
        
        // Act
        var template = SqlTemplate.Parse(sql);
        
        // Assert
        Assert.IsNotNull(template);
        Assert.AreEqual(sql, template.Sql);
    }
    
    [TestMethod]
public void SqlTemplate_Parse_WithNullSql_ThrowsArgumentException()
{
    // Act & Assert
    Assert.ThrowsException<ArgumentException>(() => SqlTemplate.Parse(null));
}
```

#### Integration Tests
- Test real database scenarios
- Use test databases (not production)
- Clean up test data
- Test multiple database dialects

### Performance Guidelines

#### Memory Efficiency
```csharp
// ✅ Good: Use readonly structs for immutable data
public readonly struct ParameterizedSql
{
    public ParameterizedSql(string sql, IReadOnlyDictionary<string, object?> parameters)
    {
        Sql = sql;
        Parameters = parameters;
    }
    
    public string Sql { get; }
    public IReadOnlyDictionary<string, object?> Parameters { get; }
}
```

#### AOT Compatibility
```csharp
// ✅ AOT-friendly: Avoid reflection
public static string GetUserColumns()
{
    return "Id, Name, Email, Age";  // Explicit column list
}

// ❌ Avoid: Reflection-based approaches in AOT scenarios
public static string GetUserColumns<T>()
{
    return string.Join(", ", typeof(T).GetProperties().Select(p => p.Name));
}
```

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/Sqlx.Tests/

# Run tests with verbose output
dotnet test --verbosity normal
```

### Test Categories

1. **Unit Tests** - Fast, isolated tests
2. **Integration Tests** - Database interaction tests
3. **Performance Tests** - Benchmarking and optimization
4. **AOT Tests** - Native compilation verification

### Writing Good Tests

```csharp
[TestClass]
public class ExpressionToSqlTests
{
    [TestMethod]
    public void Where_WithSimpleCondition_GeneratesCorrectSql()
    {
        // Arrange
        var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer);
        
        // Act
        var result = query.Where(u => u.Age > 18).ToSql();
        
        // Assert
        StringAssert.Contains(result, "WHERE ([Age] > 18)");
    }
    
    [TestMethod]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSql")]
    public void Query_WithDifferentDialects_GeneratesCorrectSyntax(string dialectName)
    {
        // Arrange
        var dialect = GetDialectByName(dialectName);
        var query = ExpressionToSql<User>.Create(dialect);
        
        // Act
        var result = query.Where(u => u.Name == "John").ToSql();
        
        // Assert
        Assert.IsTrue(result.Contains("John"));
        // Additional dialect-specific assertions...
    }
}
```

## 📚 Documentation

### Documentation Types

1. **API Documentation** - XML docs in code
2. **User Guide** - docs/ folder
3. **Examples** - samples/ folder
4. **README** - Project overview

### Writing Documentation

- Use clear, concise language
- Provide practical examples
- Include both simple and advanced scenarios
- Keep documentation up-to-date with code changes

## 🏆 Recognition

### Contributors

We recognize all contributors in:
- README.md contributors section
- Release notes
- GitHub contributors page

### Contribution Types

We value all types of contributions:
- **Code** - New features, bug fixes, optimizations
- **Documentation** - Guides, examples, API docs
- **Testing** - Unit tests, integration tests, bug reports
- **Community** - Answering questions, discussions
- **Design** - UX improvements, architecture suggestions

## 📞 Communication

### Channels

- **GitHub Issues** - Bug reports, feature requests
- **GitHub Discussions** - Questions, ideas, showcase
- **Pull Requests** - Code review and collaboration

### Getting Help

1. **Check documentation** first
2. **Search existing issues**
3. **Ask in discussions** for questions
4. **Create issue** for bugs/features

### Code of Conduct

We are committed to providing a welcoming and inspiring community for all. Please be:

- **Respectful** - Value diverse opinions and experiences
- **Collaborative** - Work together towards common goals
- **Constructive** - Provide helpful feedback and suggestions
- **Professional** - Maintain high standards of communication

## 🎯 Project Goals

### Vision
Create the most efficient, type-safe, and developer-friendly ORM for modern .NET applications.

### Principles
- **Performance First** - Zero reflection, AOT compatible
- **Type Safety** - Compile-time validation
- **Simplicity** - Minimal learning curve
- **Compatibility** - Multi-database support

### Roadmap

See our [roadmap](docs/OPTIMIZATION_ROADMAP.md) for planned features and improvements.

## 🙏 Thank You

Thank you for contributing to Sqlx! Your contributions help make .NET data access better for everyone.

---

**Questions?** Feel free to reach out through GitHub Issues or Discussions. We're here to help!