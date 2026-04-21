using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Generator.Tests;

[TestClass]
public class GeneratorRuntimeCoverageTests
{
    [TestMethod]
    public void RuntimeDialectCodeGen_GenerateDialectField_CoversAllBranches()
    {
        var withPrimaryOnly = new IndentedStringBuilder(null);
        RuntimeDialectCodeGen.GenerateDialectField(withPrimaryOnly, "dialect");
        var code1 = withPrimaryOnly.ToString();
        StringAssert.Contains(code1, "_dialect = dialect ?? throw new global::System.ArgumentNullException(nameof(dialect));");
        StringAssert.Contains(code1, "public global::Sqlx.SqlDialect Dialect => _dialect;");

        var withoutDefaults = new IndentedStringBuilder(null);
        RuntimeDialectCodeGen.GenerateDialectField(withoutDefaults, null);
        StringAssert.Contains(withoutDefaults.ToString(), "private readonly global::Sqlx.SqlDialect _dialect;");
    }

    [TestMethod]
    public void RuntimeDialectCodeGen_GenerateConstructorAndRuntimeHelpers_CoversBranches()
    {
        var userCtor = new IndentedStringBuilder(null);
        RuntimeDialectCodeGen.GenerateConstructorIfNeeded(userCtor, "Repo", true, "_connection");
        StringAssert.Contains(userCtor.ToString(), "User-defined constructor should initialize _dialect field");

        var requiredCtor = new IndentedStringBuilder(null);
        RuntimeDialectCodeGen.GenerateConstructorIfNeeded(requiredCtor, "Repo", false, null);
        var requiredCode = requiredCtor.ToString();
        StringAssert.Contains(requiredCode, "public Repo(global::System.Data.Common.DbConnection connection, global::Sqlx.SqlDialect dialect)");
        StringAssert.Contains(requiredCode, "_dialect = dialect ?? throw new global::System.ArgumentNullException(nameof(dialect));");

        var contextWithEntity = new IndentedStringBuilder(null);
        RuntimeDialectCodeGen.GenerateRuntimeContext(contextWithEntity, "User", "users", true);
        var entityContextCode = contextWithEntity.ToString();
        StringAssert.Contains(entityContextCode, "columns: UserEntityProvider.Default.Columns);");
        StringAssert.Contains(entityContextCode, "private string ParamPrefix => _dialect.ParameterPrefix;");

        var contextWithoutEntity = new IndentedStringBuilder(null);
        RuntimeDialectCodeGen.GenerateRuntimeContext(contextWithoutEntity, "User", "users", false);
        StringAssert.Contains(contextWithoutEntity.ToString(), "columns: global::System.Array.Empty<global::Sqlx.ColumnMeta>());");

        var templateFields = new IndentedStringBuilder(null);
        RuntimeDialectCodeGen.GenerateRuntimeTemplateField(templateFields, "_t1", "SELECT 1", true);
        RuntimeDialectCodeGen.GenerateRuntimeTemplateField(templateFields, "_t2", "SELECT 2", false);
        var fieldsCode = templateFields.ToString();
        StringAssert.Contains(fieldsCode, "private global::Sqlx.SqlTemplate? _t1;");
        StringAssert.Contains(fieldsCode, "ConcurrentDictionary<global::Sqlx.SqlDialect, global::Sqlx.SqlTemplate> _t2Cache");

        var getTemplate = new IndentedStringBuilder(null);
        RuntimeDialectCodeGen.GenerateGetTemplate(getTemplate, "_t1", "SELECT \"x\"\r\nFROM t", true);
        RuntimeDialectCodeGen.GenerateGetTemplate(getTemplate, "_t2", "SELECT {{columns}}", false);
        var getTemplateCode = getTemplate.ToString();
        StringAssert.Contains(getTemplateCode, "GetDynamicContext());");
        StringAssert.Contains(getTemplateCode, "_t2Cache.GetOrAdd(_dialect");
        StringAssert.Contains(getTemplateCode, "Context));");
        StringAssert.Contains(getTemplateCode, "\\r\\n");
    }

    [TestMethod]
    public void RepositoryGeneratorRuntimeExtensions_CoverDialectDetectionBranches()
    {
        var compilation = CreateCompilation("""
using System.Data.Common;
using Sqlx;

namespace Test;

public partial class ExplicitCtorRepo
{
    public ExplicitCtorRepo(DbConnection connection, SqlDialect dialect) { }
}

public partial class ExplicitCtorWithoutDialect
{
    public ExplicitCtorWithoutDialect(DbConnection connection) { }
}

public partial class PrimaryCtorRepo(DbConnection connection, SqlDialect dialect) { }
public partial class PrimaryCtorWithoutDialect(DbConnection connection) { }
""");

        var explicitCtorRepo = GetType(compilation, "Test.ExplicitCtorRepo");
        var explicitNoDialect = GetType(compilation, "Test.ExplicitCtorWithoutDialect");
        var primaryCtorRepo = GetType(compilation, "Test.PrimaryCtorRepo");
        var primaryCtorWithoutDialect = GetType(compilation, "Test.PrimaryCtorWithoutDialect");

        Assert.IsTrue(explicitCtorRepo.HasDialectConstructor(out var ctor));
        Assert.IsNotNull(ctor);
        Assert.IsFalse(explicitNoDialect.HasDialectConstructor(out _));

        Assert.AreEqual("dialect", primaryCtorRepo.GetPrimaryConstructorDialectParameterName());
        Assert.IsNull(explicitCtorRepo.GetPrimaryConstructorDialectParameterName());
        Assert.IsNull(primaryCtorWithoutDialect.GetPrimaryConstructorDialectParameterName());
    }

    [TestMethod]
    public void RepositoryGenerator_PrivateHelpers_GenerateSource_And_HelperBranches_AreCovered()
    {
        var compilation = CreateCompilation("""
using System;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace Test;

[Sqlx]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface IUserRepository : ICrudRepository<User, int>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}}")]
    Task<int> CountAsync();

    [SqlTemplate("SELECT {{table}}")]
    SqlTemplate GetTemplate();

    [SqlTemplate("UPDATE {{table}} SET {{set --param updateExpr}} WHERE {{where --param predicate1}} AND {{where --param predicate2}}")]
    Task<int> UpdateManyAsync(
        Expression<Func<User, User>> updateExpr,
        Expression<Func<User, bool>> predicate1,
        Expression<Func<User, bool>> predicate2);

    [SqlTemplate("EXEC test_output")]
    Task<int> RunAsync(OutputParameter<int> outputValue);
}

[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository
{
    internal DbConnection ExistingConnection { get; set; } = null!;
}

[RepositoryFor(typeof(IUserRepository))]
public partial class MinimalUserRepository
{
}

public interface IRawRepository
{
    [SqlTemplate("SELECT {{var --name tableName}}")]
    SqlTemplate RawTemplate();
}

[RepositoryFor(typeof(IRawRepository))]
public partial class RawRepository
{
    [SqlxVar("tableName")]
    private string GetTableName() => "raw_table";
}
""");

        var sqlTemplateAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlTemplateAttribute");
        var returnInsertedIdAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.ReturnInsertedIdAttribute");
        var returnInsertedEntityAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.ReturnInsertedEntityAttribute");
        var expressionToSqlAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.ExpressionToSqlAttribute");

        var userRepo = GetType(compilation, "Test.UserRepository");
        var minimalUserRepo = GetType(compilation, "Test.MinimalUserRepository");
        var userService = GetType(compilation, "Test.IUserRepository");
        var userEntity = GetType(compilation, "Test.User");
        var rawRepo = GetType(compilation, "Test.RawRepository");
        var rawService = GetType(compilation, "Test.IRawRepository");

        var generateSource = GetPrivateMethod(typeof(RepositoryGenerator), "GenerateSource");
        var generatedUserCode = (string)generateSource.Invoke(null, new object?[]
        {
            userRepo,
            userService,
            userEntity,
            "users",
            sqlTemplateAttr,
            returnInsertedIdAttr,
            returnInsertedEntityAttr,
            expressionToSqlAttr,
            compilation
        })!;

        var generatedMinimalUserCode = (string)generateSource.Invoke(null, new object?[]
        {
            minimalUserRepo,
            userService,
            userEntity,
            "users",
            sqlTemplateAttr,
            returnInsertedIdAttr,
            returnInsertedEntityAttr,
            expressionToSqlAttr,
            compilation
        })!;

        StringAssert.Contains(generatedUserCode, "private readonly global::Sqlx.SqlDialect _dialect;");
        StringAssert.Contains(generatedUserCode, "set => ExistingConnection = value!;");
        StringAssert.Contains(generatedUserCode, "private const string _tableName = \"users\";");
        StringAssert.Contains(generatedUserCode, "var __template = _getTemplateTemplate ?? throw new global::System.InvalidOperationException");
        StringAssert.Contains(generatedUserCode, "return __template;");
        StringAssert.Contains(generatedUserCode, "var dynamicParams = new Dictionary<string, object?>(3)");
        StringAssert.Contains(generatedUserCode, "[\"updateExpr\"] = global::Sqlx.SetExpressionExtensions.ToSetClause(updateExpr, Dialect),");
        StringAssert.Contains(generatedUserCode, "[\"predicate1\"] = global::Sqlx.ExpressionExtensions.ToWhereClause(predicate1, Dialect),");
        StringAssert.Contains(generatedMinimalUserCode, "public MinimalUserRepository(global::System.Data.Common.DbConnection connection, global::Sqlx.SqlDialect dialect)");
        StringAssert.Contains(generatedMinimalUserCode, "private global::System.Data.Common.DbConnection _connection = null!;");

        var generatedRawCode = (string)generateSource.Invoke(null, new object?[]
        {
            rawRepo,
            rawService,
            null,
            "raw_table",
            sqlTemplateAttr,
            returnInsertedIdAttr,
            returnInsertedEntityAttr,
            expressionToSqlAttr,
            compilation
        })!;

        StringAssert.Contains(generatedRawCode, "columns: global::System.Array.Empty<global::Sqlx.ColumnMeta>(),");
        StringAssert.Contains(generatedRawCode, "\"tableName\" => GetTableName(),");

        var countMethod = userService.GetMembers("CountAsync").OfType<IMethodSymbol>().Single();
        var outputMethod = userService.GetMembers("RunAsync").OfType<IMethodSymbol>().Single();
        var templateMethod = userService.GetMembers("GetTemplate").OfType<IMethodSymbol>().Single();
        var updateManyMethod = userService.GetMembers("UpdateManyAsync").OfType<IMethodSymbol>().Single();

        var generateParameterNames = GetPrivateMethod(typeof(RepositoryGenerator), "GenerateParameterNameFields");
        var emptySb = new IndentedStringBuilder(null);
        var emptyNames = (Dictionary<string, string>)generateParameterNames.Invoke(null, new object?[] { emptySb, new List<IMethodSymbol> { countMethod }, "User" })!;
        Assert.AreEqual(0, emptyNames.Count);

        var paramSb = new IndentedStringBuilder(null);
        var paramNames = (Dictionary<string, string>)generateParameterNames.Invoke(null, new object?[] { paramSb, new List<IMethodSymbol> { outputMethod }, "User" })!;
        StringAssert.Contains(paramSb.ToString(), "private string _param_outputValue => ParamPrefix + \"outputValue\";");

        var placeholderSb = new IndentedStringBuilder(null);
        var generatePlaceholderContext = GetPrivateMethod(typeof(RepositoryGenerator), "GeneratePlaceholderContext");
        generatePlaceholderContext.Invoke(null, new object?[] { placeholderSb, rawRepo, "Entity", false, new List<IMethodSymbol> { rawService.GetMembers("RawTemplate").OfType<IMethodSymbol>().Single() }, sqlTemplateAttr });
        var placeholderCode = placeholderSb.ToString();
        StringAssert.Contains(placeholderCode, "columns: global::System.Array.Empty<global::Sqlx.ColumnMeta>(),");
        StringAssert.Contains(placeholderCode, "return variableName switch");

        var dynamicSb = new IndentedStringBuilder(null);
        GetPrivateMethod(typeof(RepositoryGenerator), "GenerateDynamicParamsDeclaration").Invoke(null, new object?[] { dynamicSb, updateManyMethod });
        var dynamicCode = dynamicSb.ToString();
        StringAssert.Contains(dynamicCode, "var dynamicParams = new Dictionary<string, object?>(3)");
        StringAssert.Contains(dynamicCode, "ToSetClause(updateExpr, Dialect)");
        StringAssert.Contains(dynamicCode, "ToWhereClause(predicate2, Dialect)");

        var bindingSb = new IndentedStringBuilder(null);
        GetPrivateMethod(typeof(RepositoryGenerator), "GenerateParameterBinding").Invoke(null, new object?[]
        {
            bindingSb,
            outputMethod,
            "User",
            "Int32",
            "int",
            false,
            "EXEC test_output @outputValue",
            expressionToSqlAttr,
            new Dictionary<string, string>(),
            true
        });
        StringAssert.Contains(bindingSb.ToString(), "ParamPrefix + \"outputValue\"");

        var retrievalSb = new IndentedStringBuilder(null);
        GetPrivateMethod(typeof(RepositoryGenerator), "GenerateOutputParameterRetrieval").Invoke(null, new object?[] { retrievalSb, outputMethod, new Dictionary<string, string>() });
        StringAssert.Contains(retrievalSb.ToString(), "cmd.Parameters[ParamPrefix + \"outputValue\"].Value");

        var methodImplSb = new IndentedStringBuilder(null);
        var buildFieldNames = GetPrivateMethod(typeof(RepositoryGenerator), "BuildMethodFieldNames");
        var methodFieldNames = (Dictionary<string, string>)buildFieldNames.Invoke(null, new object?[] { new List<IMethodSymbol> { templateMethod } })!;
        var connectionInfo = CreateConnectionInfo("_connection", false);
        GetPrivateMethod(typeof(RepositoryGenerator), "GenerateMethodImplementation").Invoke(null, new object?[]
        {
            methodImplSb,
            templateMethod,
            "Test.UserRepository",
            "Test.User",
            "User",
            "int",
            sqlTemplateAttr,
            returnInsertedIdAttr,
            returnInsertedEntityAttr,
            expressionToSqlAttr,
            connectionInfo,
            methodFieldNames,
            new Dictionary<string, string>()
        });
        var methodImplCode = methodImplSb.ToString();
        StringAssert.Contains(methodImplCode, "public global::Sqlx.SqlTemplate GetTemplate()");
        StringAssert.Contains(methodImplCode, "return __template;");
    }

    [TestMethod]
    public void RepositoryGenerator_PrivateGetTableNameFromRepositoryFor_CoversMissingValueBranch()
    {
        var compilation = CreateCompilation("""
using Sqlx.Annotations;

namespace Test;

[RepositoryFor(typeof(object))]
public partial class RepoWithoutTableName { }
""");

        var repo = GetType(compilation, "Test.RepoWithoutTableName");
        var repoAttr = repo.GetAttributes().Single(a => a.AttributeClass?.Name == "RepositoryForAttribute");
        var getTableName = GetPrivateMethod(typeof(RepositoryGenerator), "GetTableNameFromRepositoryFor");
        Assert.IsNull(getTableName.Invoke(null, new object?[] { repoAttr }));
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(global::Sqlx.SqlDialect).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(RepositoryGenerator).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "GeneratorCoverageTests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            Assert.Fail("Compilation errors: " + string.Join(" | ", errors.Select(e => e.ToString())));
        }

        return compilation;
    }

    private static INamedTypeSymbol GetType(Compilation compilation, string metadataName) =>
        compilation.GetTypeByMetadataName(metadataName) ?? throw new AssertFailedException($"Type not found: {metadataName}");

    private static MethodInfo GetPrivateMethod(Type type, string name) =>
        type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .First(m => m.Name == name);

    private static object CreateConnectionInfo(string accessExpression, bool fromMethodParameter)
    {
        var type = typeof(RepositoryGenerator).GetNestedType("ConnectionInfo", BindingFlags.NonPublic)
            ?? throw new AssertFailedException("ConnectionInfo type not found.");
        return Activator.CreateInstance(type, accessExpression, fromMethodParameter)
            ?? throw new AssertFailedException("Failed to create ConnectionInfo.");
    }
}
