// -----------------------------------------------------------------------
// <copyright file="RepositoryForEdgeCasesTests.cs" company="Cricle">
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
/// Unit tests for edge cases and error handling in RepositoryFor generator.
/// Tests cover error conditions, malformed input, and boundary scenarios.
/// </summary>
[TestClass]
public class RepositoryForEdgeCasesTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that generator handles missing service type parameter gracefully.
    /// </summary>
    [TestMethod]
    public void EdgeCase_MissingServiceTypeParameter_HandlesGracefully()
    {
        string sourceCode = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    // Invalid: RepositoryFor without type parameter
    public partial class InvalidRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Generator should not crash
        Assert.IsNotNull(compilation, "Generator should handle invalid input without crashing");

        var generatedSources = GetGeneratedSources(compilation);
        // Should not generate repository implementation for invalid attribute
        var generatedCode = string.Join("\n", generatedSources);
        Assert.IsFalse(generatedCode.Contains("partial class InvalidRepository :"),
            "Should not generate implementation for invalid RepositoryFor usage");
    }

    /// <summary>
    /// Tests that generator handles null service type gracefully.
    /// </summary>
    [TestMethod]
    public void EdgeCase_NullServiceType_HandlesGracefully()
    {
        string sourceCode = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService
    {
        void DoSomething();
    }

    // This will compile but attribute has null argument
    [RepositoryFor(null)]
    public partial class NullServiceRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Generator should handle null service type gracefully
        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should not generate implementation for null service type
        Assert.IsFalse(generatedCode.Contains("partial class NullServiceRepository :"),
            "Should not generate implementation for null service type");
    }

    /// <summary>
    /// Tests that generator handles abstract class service types (not interfaces).
    /// </summary>
    [TestMethod]
    public void EdgeCase_AbstractClassServiceType_IsSkipped()
    {
        string sourceCode = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    public abstract class AbstractService
    {
        public abstract void DoSomething();
    }

    [RepositoryFor(typeof(AbstractService))]
    public partial class AbstractServiceRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should not generate implementation for abstract class (only interfaces)
        Assert.IsFalse(generatedCode.Contains("partial class AbstractServiceRepository : TestNamespace.AbstractService"),
            "Should not generate implementation for abstract class service types");
    }

    /// <summary>
    /// Tests that generator handles concrete class service types (not interfaces).
    /// </summary>
    [TestMethod]
    public void EdgeCase_ConcreteClassServiceType_IsSkipped()
    {
        string sourceCode = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    public class ConcreteService
    {
        public virtual void DoSomething() { }
    }

    [RepositoryFor(typeof(ConcreteService))]
    public partial class ConcreteServiceRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should not generate implementation for concrete class
        Assert.IsFalse(generatedCode.Contains("partial class ConcreteServiceRepository : TestNamespace.ConcreteService"),
            "Should not generate implementation for concrete class service types");
    }

    /// <summary>
    /// Tests that generator handles generic interfaces correctly.
    /// </summary>
    [TestMethod]
    public void EdgeCase_GenericInterface_HandlesCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IGenericService<T>
    {
        IList<T> GetAll();
        T? GetById(int id);
        int Create(T entity);
    }

    [RepositoryFor(typeof(IGenericService<User>))]
    public partial class GenericUserRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Should handle generic interfaces: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should generate implementation for generic interface
        Assert.IsTrue(generatedCode.Contains("partial class GenericUserRepository : TestNamespace.IGenericService<TestNamespace.User>"),
            "Should generate implementation for generic interface");

        // Should handle generic type parameters correctly
        Assert.IsTrue(generatedCode.Contains("IList<TestNamespace.User> GetAll()"),
            "Should handle generic type parameters in method signatures");
    }

    /// <summary>
    /// Tests that generator handles interfaces with no entity-related methods.
    /// </summary>
    [TestMethod]
    public void EdgeCase_NonEntityInterface_HandlesGracefully()
    {
        string sourceCode = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface IUtilityService
    {
        string GenerateId();
        void LogMessage(string message);
        int CalculateHash(string input);
        bool ValidateInput(string input);
    }

    [RepositoryFor(typeof(IUtilityService))]
    public partial class UtilityRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Should handle non-entity interfaces: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should still generate implementation even without clear entity types
        Assert.IsTrue(generatedCode.Contains("partial class UtilityRepository : TestNamespace.IUtilityService"),
            "Should generate implementation for non-entity interfaces");

        // Should generate all methods
        var methods = new[] { "GenerateId", "LogMessage", "CalculateHash", "ValidateInput" };
        foreach (var method in methods)
        {
            Assert.IsTrue(generatedCode.Contains(method),
                $"Should generate method {method}");
        }
    }

    /// <summary>
    /// Tests that generator handles interfaces with complex inheritance.
    /// </summary>
    [TestMethod]
    public void EdgeCase_InheritedInterface_HandlesCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public interface IReadOnlyService<T>
    {
        IList<T> GetAll();
        T? GetById(int id);
    }

    public interface IDocumentService : IReadOnlyService<Document>
    {
        int Create(Document document);
        int Update(Document document);
        int Delete(int id);
    }

    [RepositoryFor(typeof(IDocumentService))]
    public partial class DocumentRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Should handle inherited interfaces: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should generate implementation for the main interface
        Assert.IsTrue(generatedCode.Contains("partial class DocumentRepository : TestNamespace.IDocumentService"),
            "Should generate implementation for inherited interface");

        // Should include methods from both base and derived interfaces
        var allMethods = new[] { "GetAll", "GetById", "Create", "Update", "Delete" };
        foreach (var method in allMethods)
        {
            Assert.IsTrue(generatedCode.Contains(method),
                $"Should generate method {method} from inheritance hierarchy");
        }
    }

    /// <summary>
    /// Tests that generator handles very long method names and parameter lists.
    /// </summary>
    [TestMethod]
    public void EdgeCase_LongMethodNamesAndParameters_HandlesCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class LongNamedEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IVeryLongInterfaceNameForTestingPurposes
    {
        IList<LongNamedEntity> GetAllLongNamedEntitiesWithVerySpecificFilteringCriteria();
        LongNamedEntity? FindLongNamedEntityByIdAndNameAndStatusWithComplexLogic(
            int id, 
            string name, 
            bool isActive, 
            DateTime createdDate, 
            string category, 
            decimal? price);
        int CreateLongNamedEntityWithComplexValidationAndLogging(
            LongNamedEntity entity, 
            bool validateInput, 
            bool enableLogging, 
            string userContext);
    }

    [RepositoryFor(typeof(IVeryLongInterfaceNameForTestingPurposes))]
    public partial class VeryLongRepositoryNameForTestingPurposes
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Should handle long names: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should generate implementation despite long names
        Assert.IsTrue(generatedCode.Contains("partial class VeryLongRepositoryNameForTestingPurposes : TestNamespace.IVeryLongInterfaceNameForTestingPurposes"),
            "Should handle very long class and interface names");

        // Should handle long method names
        Assert.IsTrue(generatedCode.Contains("GetAllLongNamedEntitiesWithVerySpecificFilteringCriteria"),
            "Should handle very long method names");

        // Should handle complex parameter lists
        Assert.IsTrue(generatedCode.Contains("FindLongNamedEntityByIdAndNameAndStatusWithComplexLogic"),
            "Should handle methods with many parameters");
    }

    /// <summary>
    /// Tests that generator handles special characters in names.
    /// </summary>
    [TestMethod]
    public void EdgeCase_SpecialCharactersInNames_HandlesCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class EntityWith_Underscores
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IService_With_Underscores
    {
        IList<EntityWith_Underscores> Get_All_Entities();
        EntityWith_Underscores? Get_Entity_By_Id(int id);
        int Create_New_Entity(EntityWith_Underscores entity);
    }

    [RepositoryFor(typeof(IService_With_Underscores))]
    public partial class Repository_With_Underscores
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Should handle underscores in names: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should handle names with underscores
        Assert.IsTrue(generatedCode.Contains("partial class Repository_With_Underscores : TestNamespace.IService_With_Underscores"),
            "Should handle names with underscores");

        // Should handle method names with underscores
        var methodsWithUnderscores = new[] { "Get_All_Entities", "Get_Entity_By_Id", "Create_New_Entity" };
        foreach (var method in methodsWithUnderscores)
        {
            Assert.IsTrue(generatedCode.Contains(method),
                $"Should handle method name with underscores: {method}");
        }
    }

    /// <summary>
    /// Tests that generator handles empty namespace correctly.
    /// </summary>
    [TestMethod]
    public void EdgeCase_GlobalNamespace_HandlesCorrectly()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

public class GlobalEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface IGlobalService
{
    IList<GlobalEntity> GetAll();
    GlobalEntity? GetById(int id);
}

[RepositoryFor(typeof(IGlobalService))]
public partial class GlobalRepository
{
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Should handle global namespace: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should handle global namespace (no namespace declaration)
        Assert.IsTrue(generatedCode.Contains("partial class GlobalRepository : IGlobalService"),
            "Should handle types in global namespace");
    }

    /// <summary>
    /// Tests that generator handles multiple RepositoryFor attributes on same class.
    /// </summary>
    [TestMethod]
    public void EdgeCase_MultipleRepositoryForAttributes_HandlesGracefully()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Entity1
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Entity2
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public interface IService1
    {
        IList<Entity1> GetAll();
    }

    public interface IService2
    {
        IList<Entity2> GetAll();
    }

    // Multiple RepositoryFor attributes - should only process the first one
    [RepositoryFor(typeof(IService1))]
    [RepositoryFor(typeof(IService2))]
    public partial class MultiAttributeRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle multiple attributes gracefully (use first one)
        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should generate implementation (probably for the first service)
        Assert.IsTrue(generatedCode.Contains("partial class MultiAttributeRepository : TestNamespace.IService1") ||
                     generatedCode.Contains("partial class MultiAttributeRepository : TestNamespace.IService2"),
            "Should handle multiple RepositoryFor attributes by using one of them");
    }

    /// <summary>
    /// Tests that generator handles circular references gracefully.
    /// </summary>
    [TestMethod]
    public void EdgeCase_CircularReferences_HandlesGracefully()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Parent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public IList<Child>? Children { get; set; }
    }

    public class Child
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Parent? Parent { get; set; }
    }

    public interface IParentService
    {
        IList<Parent> GetAllParents();
        Parent? GetParentById(int id);
        IList<Child> GetChildrenForParent(int parentId);
    }

    [RepositoryFor(typeof(IParentService))]
    public partial class ParentRepository
    {
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Should handle circular references: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Should handle circular references without infinite loops
        Assert.IsTrue(generatedCode.Contains("partial class ParentRepository : TestNamespace.IParentService"),
            "Should handle circular references in entity types");
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
        references.Add(MetadataReference.CreateFromFile(typeof(DateTime).Assembly.Location));

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

