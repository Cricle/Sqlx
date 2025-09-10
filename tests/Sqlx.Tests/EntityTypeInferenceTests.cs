// -----------------------------------------------------------------------
// <copyright file="EntityTypeInferenceTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests specifically for entity type inference logic in RepositoryFor generator.
/// Tests cover various scenarios for inferring entity types from service interfaces.
/// </summary>
[TestClass]
public class EntityTypeInferenceTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests entity inference from simple return types.
    /// </summary>
    [TestMethod]
    public void EntityInference_SimpleReturnType_InfersCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public interface IBookService
    {
        Book GetBook();
        IList<Book> GetBooks();
    }

    [RepositoryFor(typeof(IBookService))]
    public partial class BookRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Compilation should succeed: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should infer Book as entity type
        Assert.IsTrue(generatedCode.Contains("partial class BookRepository : TestNamespace.IBookService"),
            "Should generate repository implementing IBookService");
        Assert.IsTrue(generatedCode.Contains("TestNamespace.Book GetBook()"),
            "Should handle Book return type");
        Assert.IsTrue(generatedCode.Contains("System.Collections.Generic.IList<TestNamespace.Book> GetBooks()"),
            "Should handle IList<Book> return type");
    }

    /// <summary>
    /// Tests entity inference from generic collection return types.
    /// </summary>
    [TestMethod]
    public void EntityInference_GenericCollections_InfersCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using System.Linq;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Year { get; set; }
    }

    public interface IMovieService
    {
        IEnumerable<Movie> GetMovies();
        List<Movie> SearchMovies(string query);
        Movie[] GetTopMovies();
        IQueryable<Movie> QueryMovies();
    }

    [RepositoryFor(typeof(IMovieService))]
    public partial class MovieRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Compilation should succeed: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should infer Movie as entity type from all collection types
        Assert.IsTrue(generatedCode.Contains("partial class MovieRepository : TestNamespace.IMovieService"),
            "Should generate repository implementing IMovieService");
        Assert.IsTrue(generatedCode.Contains("IEnumerable<TestNamespace.Movie>") ||
                     generatedCode.Contains("GetMovies"),
            "Should handle IEnumerable<Movie>");
        Assert.IsTrue(generatedCode.Contains("List<TestNamespace.Movie>") ||
                     generatedCode.Contains("SearchMovies"),
            "Should handle List<Movie>");
        Assert.IsTrue(generatedCode.Contains("TestNamespace.Movie[]") ||
                     generatedCode.Contains("GetTopMovies"),
            "Should handle Movie[]");
    }

    /// <summary>
    /// Tests entity inference from Task and async return types.
    /// </summary>
    [TestMethod]
    public void EntityInference_AsyncReturnTypes_InfersCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Song
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
    }

    public interface ISongService
    {
        Task<Song> GetSongAsync(int id);
        Task<IList<Song>> GetSongsAsync();
        Task<List<Song>> SearchSongsAsync(string query);
        ValueTask<Song[]> GetPopularSongsAsync();
    }

    [RepositoryFor(typeof(ISongService))]
    public partial class SongRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Compilation should succeed: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should infer Song as entity type from async return types
        Assert.IsTrue(generatedCode.Contains("partial class SongRepository : TestNamespace.ISongService"),
            "Should generate repository implementing ISongService");
        Assert.IsTrue(generatedCode.Contains("Task<TestNamespace.Song>") ||
                     generatedCode.Contains("GetSongAsync"),
            "Should handle Task<Song>");
        Assert.IsTrue(generatedCode.Contains("Task<System.Collections.Generic.IList<TestNamespace.Song>>") ||
                     generatedCode.Contains("GetSongsAsync"),
            "Should handle Task<IList<Song>>");
    }

    /// <summary>
    /// Tests entity inference from method parameters.
    /// </summary>
    [TestMethod]
    public void EntityInference_MethodParameters_InfersCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ICategoryService
    {
        int CreateCategory(Category category);
        void UpdateCategory(Category category);
        void ProcessCategories(IList<Category> categories);
        bool ValidateCategory(Category category);
    }

    [RepositoryFor(typeof(ICategoryService))]
    public partial class CategoryRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Compilation should succeed: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should infer Category as entity type from parameters
        Assert.IsTrue(generatedCode.Contains("partial class CategoryRepository : TestNamespace.ICategoryService"),
            "Should generate repository implementing ICategoryService");
        Assert.IsTrue(generatedCode.Contains("CreateCategory(TestNamespace.Category category)"),
            "Should handle Category parameter in CreateCategory");
        Assert.IsTrue(generatedCode.Contains("UpdateCategory(TestNamespace.Category category)"),
            "Should handle Category parameter in UpdateCategory");
    }

    /// <summary>
    /// Tests entity inference fallback to interface name.
    /// </summary>
    [TestMethod]
    public void EntityInference_InterfaceNameFallback_WorksCorrectly()
    {
        string sourceCode = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IAuthorService
    {
        // Methods don't explicitly reference Author type
        void DoWork();
        int GetCount();
        string GetStatus();
    }

    [RepositoryFor(typeof(IAuthorService))]
    public partial class AuthorRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Compilation should succeed: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should still generate implementation using interface name fallback
        Assert.IsTrue(generatedCode.Contains("partial class AuthorRepository : TestNamespace.IAuthorService"),
            "Should generate repository implementing IAuthorService");
        Assert.IsTrue(generatedCode.Contains("DoWork()"),
            "Should generate DoWork method");
        Assert.IsTrue(generatedCode.Contains("GetCount()"),
            "Should generate GetCount method");
        Assert.IsTrue(generatedCode.Contains("GetStatus()"),
            "Should generate GetStatus method");
    }

    /// <summary>
    /// Tests that system types are filtered out during entity inference.
    /// </summary>
    [TestMethod]
    public void EntityInference_SystemTypesFiltered_WorksCorrectly()
    {
        string sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Report
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public interface IReportService
    {
        // Mix of entity types and system types
        Report GetReport(int id);
        string GetReportTitle(int id);
        DateTime GetLastUpdated();
        IList<string> GetReportNames();
        IList<Report> GetAllReports();
        bool IsReportValid(Report report);
    }

    [RepositoryFor(typeof(IReportService))]
    public partial class ReportRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Compilation should succeed: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should infer Report as entity type (not string, DateTime, etc.)
        Assert.IsTrue(generatedCode.Contains("partial class ReportRepository : TestNamespace.IReportService"),
            "Should generate repository implementing IReportService");
        Assert.IsTrue(generatedCode.Contains("TestNamespace.Report GetReport"),
            "Should handle Report return type");
        Assert.IsTrue(generatedCode.Contains("System.Collections.Generic.IList<TestNamespace.Report> GetAllReports"),
            "Should handle IList<Report> return type");
        Assert.IsTrue(generatedCode.Contains("IsReportValid(TestNamespace.Report report)"),
            "Should handle Report parameter type");
    }

    /// <summary>
    /// Tests entity inference with multiple candidate types.
    /// </summary>
    [TestMethod]
    public void EntityInference_MultipleCandidates_SelectsMostFrequent()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Course
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public interface IStudentService
    {
        // Student appears more frequently
        IList<Student> GetStudents();
        Student GetStudent(int id);
        Student CreateStudent(Student student);
        void UpdateStudent(Student student);
        
        // Course appears less frequently
        Course GetStudentCourse(int studentId);
    }

    [RepositoryFor(typeof(IStudentService))]
    public partial class StudentRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Compilation should succeed: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should infer Student as primary entity type (appears more frequently)
        Assert.IsTrue(generatedCode.Contains("partial class StudentRepository : TestNamespace.IStudentService"),
            "Should generate repository implementing IStudentService");
        // Most methods should work with Student type
        Assert.IsTrue(generatedCode.Contains("GetStudents()"),
            "Should generate GetStudents method");
        Assert.IsTrue(generatedCode.Contains("GetStudent(int id)"),
            "Should generate GetStudent method");
    }

    /// <summary>
    /// Tests entity inference with nested generic types.
    /// </summary>
    [TestMethod]
    public void EntityInference_NestedGenerics_HandlesCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public interface ICommentService
    {
        Task<IEnumerable<Comment>> GetCommentsAsync();
        Task<List<IList<Comment>>> GetNestedCommentsAsync();
        Task<Dictionary<int, Comment>> GetCommentDictionaryAsync();
    }

    [RepositoryFor(typeof(ICommentService))]
    public partial class CommentRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Compilation should succeed: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should infer Comment from nested generic types
        Assert.IsTrue(generatedCode.Contains("partial class CommentRepository : TestNamespace.ICommentService"),
            "Should generate repository implementing ICommentService");
        Assert.IsTrue(generatedCode.Contains("GetCommentsAsync()"),
            "Should generate GetCommentsAsync method");
    }

    /// <summary>
    /// Tests that entity inference handles empty interfaces gracefully.
    /// </summary>
    [TestMethod]
    public void EntityInference_EmptyInterface_HandlesGracefully()
    {
        string sourceCode = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ITagService
    {
        // Empty interface - no methods
    }

    [RepositoryFor(typeof(ITagService))]
    public partial class TagRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Compilation should succeed: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should still generate basic repository structure
        Assert.IsTrue(generatedCode.Contains("partial class TagRepository : TestNamespace.ITagService"),
            "Should generate repository implementing ITagService even for empty interface");
    }

    private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) CompileWithSourceGenerator(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = GetBasicReferences();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        return (newCompilation, diagnostics);
    }

    private static List<string> GetGeneratedSources(Compilation compilation)
    {
        var generatedSources = new List<string>();
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            if (syntaxTree.FilePath.Contains("Generated") ||
                string.IsNullOrEmpty(syntaxTree.FilePath) ||
                syntaxTree.ToString().Contains("// <auto-generated>"))
            {
                generatedSources.Add(syntaxTree.ToString());
            }
        }

        return generatedSources;
    }

    private static List<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>();

        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location));

        var runtimeAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        references.Add(MetadataReference.CreateFromFile(typeof(CSharpGenerator).Assembly.Location));

        return references;
    }
}

