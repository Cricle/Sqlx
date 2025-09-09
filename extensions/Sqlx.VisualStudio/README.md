# 🔌 Sqlx Visual Studio Extension

**Enhanced IntelliSense support for Sqlx - High-performance .NET ORM with source generators**

## 🚀 Features

### 🎯 SQL IntelliSense Support
- **Smart Code Completion**: Intelligent suggestions for SQL keywords, table names, and column names
- **Syntax Highlighting**: Beautiful syntax highlighting for SQL code within `[Sqlx]` attributes
- **Real-time Validation**: Instant error detection and syntax validation as you type
- **Multi-Database Support**: Dialect-aware completion for MySQL, SQL Server, PostgreSQL, Oracle, DB2, and SQLite

### 🛡️ Advanced Diagnostics
- **SQLX001**: SQL syntax error detection
- **SQLX002**: Parameter mismatch warnings
- **SQLX003**: Unused parameter detection
- **SQLX004**: Performance optimization suggestions
- **SQLX005**: Security vulnerability warnings (SQL injection prevention)
- **SQLX006**: Database dialect compatibility checks

### ⚡ Code Generation Tools
- **Repository Scaffolding**: Generate complete repository interfaces and implementations
- **Entity Class Generation**: Auto-generate entity classes from database schema
- **SQL Snippet Templates**: Quick insertion of common SQL patterns
- **Batch Operation Support**: Generate high-performance batch operations

### 🔧 Productivity Tools
- **Validate Sqlx SQL**: Validate all SQL statements in your project
- **Performance Optimizer**: Analyze and suggest SQL optimizations
- **Repository Generator**: Interactive wizard for creating new repositories

## 📦 Installation

1. Download the `.vsix` file from the [Releases](https://github.com/Cricle/Sqlx/releases) page
2. Double-click the `.vsix` file to install, or use Visual Studio's Extension Manager
3. Restart Visual Studio
4. Enjoy enhanced Sqlx development experience!

## 🎨 IntelliSense Features

### SQL Keyword Completion
```csharp
[Sqlx("SE|")] // Type 'SE' and get 'SELECT' completion
public IList<User> GetUsers();
```

### Table and Column Suggestions
```csharp
[Sqlx("SELECT * FROM U|")] // Get 'Users', 'UserProfiles' suggestions
public IList<User> GetUsers();

[Sqlx("SELECT FirstN| FROM Users")] // Get 'FirstName' completion
public IList<User> GetUsers();
```

### Parameter Validation
```csharp
[Sqlx("SELECT * FROM Users WHERE Age > @age")]
public IList<User> GetUsersByAge(int age); // ✅ Correct

[Sqlx("SELECT * FROM Users WHERE Age > @wrongParam")]
public IList<User> GetUsersByAge(int age); // ❌ Warning: Parameter mismatch
```

## 🔍 Syntax Highlighting

The extension provides beautiful syntax highlighting for SQL within Sqlx attributes:

- **Blue Bold**: SQL Keywords (SELECT, FROM, WHERE, etc.)
- **Dark Red**: String literals ('value')
- **Dark Magenta**: Parameters (@param, :param, ?)
- **Dark Green**: Table names
- **Dark Cyan**: Column names

## ⚡ Code Generation

### Generate Repository Interface

Use **Tools > Generate Sqlx Repository** to create a complete repository:

```csharp
public interface IUserRepository
{
    [SqlExecuteType(SqlExecuteTypes.Select, "Users")]
    IList<User> GetAllUsers();
    
    [Sqlx("SELECT * FROM Users WHERE Id = @id")]
    User GetById(int id);
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "Users")]
    int Create(User user);
    
    [SqlExecuteType(SqlExecuteTypes.Update, "Users")]
    int Update(User user);
    
    [SqlExecuteType(SqlExecuteTypes.Delete, "Users")]
    int Delete(int id);
    
    [SqlExecuteType(SqlExecuteTypes.BatchInsert, "Users")]
    int BatchCreate(IEnumerable<User> users);
}
```

### Generate Repository Implementation

```csharp
[RepositoryFor(typeof(IUserRepository))]
[SqlDefine(SqlDefineTypes.SqlServer)]
public partial class UserRepository
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    // Methods are auto-implemented by Sqlx source generator
}
```

## 🛠️ Diagnostic Rules

| Rule ID | Description | Severity |
|---------|-------------|----------|
| SQLX001 | Invalid SQL syntax | Error |
| SQLX002 | Parameter mismatch | Warning |
| SQLX003 | Unused parameter | Info |
| SQLX004 | Performance issue | Warning |
| SQLX005 | Security vulnerability | Warning |
| SQLX006 | Dialect compatibility | Info |

### Example Diagnostics

```csharp
// SQLX001: SQL syntax error
[Sqlx("SELECT * FORM Users")] // ❌ 'FORM' should be 'FROM'
public IList<User> GetUsers();

// SQLX002: Parameter mismatch
[Sqlx("SELECT * FROM Users WHERE Age > @wrongParam")]
public IList<User> GetUsersByAge(int age); // ❌ @wrongParam != age

// SQLX004: Performance warning
[Sqlx("SELECT * FROM Users")] // ⚠️ Consider specifying columns
public IList<User> GetUsers();

// SQLX005: Security warning
[Sqlx("SELECT * FROM Users WHERE Name = '" + userName + "'")] // ❌ SQL injection risk
public IList<User> GetUsersByName(string userName);
```

## 🎯 Database Dialect Support

The extension is fully aware of different SQL dialects:

```csharp
// MySQL
[SqlDefine(SqlDefineTypes.MySql)]
[Sqlx("SELECT `FirstName` FROM `Users` WHERE `Age` > @age")]

// SQL Server
[SqlDefine(SqlDefineTypes.SqlServer)]
[Sqlx("SELECT [FirstName] FROM [Users] WHERE [Age] > @age")]

// PostgreSQL
[SqlDefine(SqlDefineTypes.Postgresql)]
[Sqlx("SELECT \"FirstName\" FROM \"Users\" WHERE \"Age\" > $1")]

// Oracle
[SqlDefine(SqlDefineTypes.Oracle)]
[Sqlx("SELECT \"FirstName\" FROM \"Users\" WHERE \"Age\" > :age")]

// DB2
[SqlDefine(SqlDefineTypes.DB2)]
[Sqlx("SELECT \"FirstName\" FROM \"Users\" WHERE \"Age\" > ?")]

// SQLite
[SqlDefine(SqlDefineTypes.SQLite)]
[Sqlx("SELECT [FirstName] FROM [Users] WHERE [Age] > @age")]
```

## 🔧 Menu Commands

Access Sqlx tools from **Tools** menu:

1. **Generate Sqlx Repository...**: Interactive repository scaffolding wizard
2. **Validate Sqlx SQL**: Validate all SQL statements in current project
3. **Optimize Sqlx Performance**: Analyze and suggest performance improvements

## 🎨 Customization

The extension integrates with Visual Studio's theme system:
- Syntax highlighting adapts to your current theme
- Colors can be customized in **Tools > Options > Environment > Fonts and Colors**
- Look for "Sqlx" entries in the display items list

## 📝 Requirements

- Visual Studio 2022 (17.0 or later)
- .NET Framework 4.7.2 or later
- Sqlx NuGet package in your project

## 🐛 Troubleshooting

### IntelliSense not working?
1. Ensure your project references the Sqlx NuGet package
2. Clean and rebuild your solution
3. Check that the extension is enabled in **Extensions > Manage Extensions**

### Syntax highlighting not showing?
1. Verify the file contains `[Sqlx]` attributes
2. Check your color theme settings
3. Restart Visual Studio if needed

### Code generation fails?
1. Ensure your project compiles successfully
2. Check that you have write permissions to the project folder
3. Verify the entity class exists and is accessible

## 🔗 Links

- [Sqlx GitHub Repository](https://github.com/Cricle/Sqlx)
- [Sqlx Documentation](https://github.com/Cricle/Sqlx/tree/main/docs)
- [Report Issues](https://github.com/Cricle/Sqlx/issues)
- [Feature Requests](https://github.com/Cricle/Sqlx/discussions)

## 📄 License

This extension is licensed under the same terms as the Sqlx project.

---

**Transform your data access development with intelligent SQL assistance! 🚀**

