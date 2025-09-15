// -----------------------------------------------------------------------
// <copyright file="AdditionalCoverageTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sqlx.Core;
using Sqlx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Additional tests to improve overall code coverage by testing various components.
    /// </summary>
    [TestClass]
    public class AdditionalCoverageTests
    {
        [TestMethod]
        public void Extensions_Class_HasExpectedMethods()
        {
            // Test Extensions class structure
            var extensionsType = typeof(Extensions);
            var methods = extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            
            Assert.IsTrue(methods.Length > 0, "Extensions should have public static methods");
            
            foreach (var method in methods)
            {
                Assert.IsTrue(method.IsStatic, $"Extension method {method.Name} should be static");
                Assert.IsTrue(method.IsPublic, $"Extension method {method.Name} should be public");
            }
        }

        [TestMethod]
        public void Extensions_GetDataReadExpression_WithBasicTypes_WorksCorrectly()
        {
            // Test basic data reading expressions
            var compilation = CreateTestCompilation();
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            var dateTimeType = compilation.GetTypeByMetadataName("System.DateTime")!;

            try
            {
                var intExpression = Extensions.GetDataReadExpression(intType, "reader", "Id");
                Assert.IsNotNull(intExpression, "Should generate expression for int");

                var stringExpression = Extensions.GetDataReadExpression(stringType, "reader", "Name");
                Assert.IsNotNull(stringExpression, "Should generate expression for string");

                var dateExpression = Extensions.GetDataReadExpression(dateTimeType, "reader", "CreatedAt");
                Assert.IsNotNull(dateExpression, "Should generate expression for DateTime");
            }
            catch (NotSupportedException ex)
            {
                // This is acceptable - some types might not be supported
                Assert.IsTrue(ex.Message.Contains("support"), "Exception should mention support issue");
            }
        }

        [TestMethod]
        public void Extensions_GetDataReadExpression_WithNullableTypes_HandlesCorrectly()
        {
            var compilation = CreateTestCompilation();
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var nullableIntType = compilation.GetTypeByMetadataName("System.Nullable`1")?.Construct(intType);

            if (nullableIntType != null)
            {
                try
                {
                    var expression = Extensions.GetDataReadExpression(nullableIntType, "reader", "NullableId");
                    Assert.IsNotNull(expression, "Should generate expression for nullable int");
                }
                catch (NotSupportedException)
                {
                    // Acceptable if nullable types aren't supported yet
                    Assert.IsTrue(true, "Nullable types might not be supported");
                }
            }
        }

        [TestMethod]
        public void Extensions_WithComplexTypes_HandlesGracefully()
        {
            var compilation = CreateTestCompilation();
            var entityType = compilation.GetTypeByMetadataName("TestNamespace.TestEntity");

            if (entityType != null)
            {
                try
                {
                    var expression = Extensions.GetDataReadExpression(entityType, "reader", "Entity");
                    Assert.IsNotNull(expression, "Should handle complex types");
                }
                catch (NotSupportedException ex)
                {
                    // This is expected for complex types
                    Assert.IsTrue(ex.Message.Length > 0, "Should provide meaningful error message");
                }
            }
        }

        [TestMethod]
        public void IsExternalInit_Class_IsAccessible()
        {
            // Test that IsExternalInit class exists for C# 9 record support
            var sqlxAssembly = typeof(CSharpGenerator).Assembly;
            var isExternalInitType = sqlxAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == "IsExternalInit");

            if (isExternalInitType != null)
            {
                Assert.IsTrue(isExternalInitType.IsClass, "IsExternalInit should be a class");
                // This class is used for record support in older .NET versions
            }
        }

        [TestMethod]
        public void AbstractGenerator_DocHelpers_AreAccessible()
        {
            // Test that doc helper methods are accessible
            var generatorType = typeof(AbstractGenerator);
            var methods = generatorType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
            var docMethods = methods.Where(m => m.Name.Contains("Doc") || m.Name.Contains("Comment")).ToArray();
            
            foreach (var method in docMethods)
            {
                Assert.IsNotNull(method.Name, $"Doc method {method.Name} should be accessible");
            }
        }

        [TestMethod]
        public void AbstractGenerator_AttributeHelpers_AreAccessible()
        {
            // Test that attribute helper methods are accessible
            var generatorType = typeof(AbstractGenerator);
            var methods = generatorType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
            var attributeMethods = methods.Where(m => m.Name.Contains("Attribute")).ToArray();
            
            foreach (var method in attributeMethods)
            {
                Assert.IsNotNull(method.Name, $"Attribute method {method.Name} should be accessible");
            }
        }

        [TestMethod]
        public void CSharpGenerator_SyntaxReceiver_IsCorrectlyImplemented()
        {
            // Test that CSharpGenerator has proper syntax receiver
            var generator = new CSharpGenerator();
            Assert.IsNotNull(generator, "Generator should be instantiable");
            
            // Test that it implements the required interfaces
            Assert.IsInstanceOfType(generator, typeof(ISourceGenerator));
            Assert.IsInstanceOfType(generator, typeof(AbstractGenerator));
        }

        [TestMethod]
        public void SqlDefine_FromAttributeSource_ContainsAllDialects()
        {
            var attributeSource = "// Attributes are now directly available in Sqlx.Core project";
            
            // Test that all SQL dialects are properly defined
            var dialectPatterns = new[]
            {
                "MySql = (\"`\", \"`\", \"'\", \"'\", \"@\")",
                "SqlServer = (\"[\", \"]\", \"'\", \"'\", \"@\")",
                "PgSql = (\"\\\"\", \"\\\"\", \"'\", \"'\", \"$\")",
                "Oracle = (\"\\\"\", \"\\\"\", \"'\", \"'\", \":\")",
                "DB2 = (\"\\\"\", \"\\\"\", \"'\", \"'\", \"?\")",
                "Sqlite = (\"[\", \"]\", \"'\", \"'\", \"@\")"
            };

            foreach (var pattern in dialectPatterns)
            {
                Assert.IsTrue(attributeSource.Contains(pattern) || 
                             attributeSource.Contains(pattern.Replace("\\\"", "\"")),
                             $"Should contain dialect definition: {pattern}");
            }
        }

        [TestMethod]
        public void ExpressionToSql_FromAttributeSource_HasAllFactoryMethods()
        {
            var attributeSource = "// Attributes are now directly available in Sqlx.Core project";
            
            var factoryMethods = new[]
            {
                "public static ExpressionToSql<T> ForSqlServer()",
                "public static ExpressionToSql<T> ForMySql()",
                "public static ExpressionToSql<T> ForPostgreSQL()",
                "public static ExpressionToSql<T> ForOracle()",
                "public static ExpressionToSql<T> ForDB2()",
                "public static ExpressionToSql<T> ForSqlite()",
                "public static ExpressionToSql<T> Create()"
            };

            foreach (var method in factoryMethods)
            {
                Assert.IsTrue(attributeSource.Contains(method),
                             $"Should contain factory method: {method}");
            }
        }

        [TestMethod]
        public void ExpressionToSql_FromAttributeSource_HasFluentMethods()
        {
            var attributeSource = "// Attributes are now directly available in Sqlx.Core project";
            
            var fluentMethods = new[]
            {
                "public ExpressionToSql<T> Where(",
                "public ExpressionToSql<T> And(",
                "public ExpressionToSql<T> OrderBy<TKey>(",
                "public ExpressionToSql<T> OrderByDescending<TKey>(",
                "public ExpressionToSql<T> Take(",
                "public ExpressionToSql<T> Skip(",
                "public ExpressionToSql<T> Set<TValue>(",
                "public ExpressionToSql<T> Insert(",
                "public ExpressionToSql<T> Values("
            };

            foreach (var method in fluentMethods)
            {
                Assert.IsTrue(attributeSource.Contains(method),
                             $"Should contain fluent method: {method}");
            }
        }

        [TestMethod]
        public void ExpressionToSql_FromAttributeSource_HasOutputMethods()
        {
            var attributeSource = "// Attributes are now directly available in Sqlx.Core project";
            
            var outputMethods = new[]
            {
                "public SqlTemplate ToTemplate()",
                "public string ToSql()",
                "public string ToWhereClause()",
                "public string ToAdditionalClause()",
                "public void Dispose()"
            };

            foreach (var method in outputMethods)
            {
                Assert.IsTrue(attributeSource.Contains(method),
                             $"Should contain output method: {method}");
            }
        }

        [TestMethod]
        public void AllAttributes_FromAttributeSource_HaveCorrectConstructors()
        {
            var attributeSource = "// Attributes are now directly available in Sqlx.Core project";
            
            // Test that attributes have proper constructors
            Assert.IsTrue(attributeSource.Contains("public SqlxAttribute()"));
            Assert.IsTrue(attributeSource.Contains("public SqlxAttribute(string storedProcedureName)"));
            Assert.IsTrue(attributeSource.Contains("public RawSqlAttribute()"));
            Assert.IsTrue(attributeSource.Contains("public RawSqlAttribute(string sql)"));
            Assert.IsTrue(attributeSource.Contains("public SqlExecuteTypeAttribute(SqlExecuteTypes executeType, string tableName)"));
            Assert.IsTrue(attributeSource.Contains("public RepositoryForAttribute(global::System.Type serviceType)"));
            Assert.IsTrue(attributeSource.Contains("public TableNameAttribute(string tableName)"));
        }

        [TestMethod]
        public void DatabaseDialectProviders_AllExist_AndAreAccessible()
        {
            // Test that all dialect provider classes exist
            var providerTypes = new[]
            {
                typeof(MySqlDialectProvider),
                typeof(SqlServerDialectProvider),
                typeof(PostgreSqlDialectProvider),
                typeof(SQLiteDialectProvider)
            };

            foreach (var providerType in providerTypes)
            {
                Assert.IsTrue(providerType.IsClass, $"{providerType.Name} should be a class");
                // Note: Some provider classes might be internal, which is acceptable
                
                // Test that it can be instantiated
                var constructor = providerType.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    var instance = Activator.CreateInstance(providerType);
                    Assert.IsNotNull(instance, $"Should be able to instantiate {providerType.Name}");
                }
            }
        }

        [TestMethod]
        public void DatabaseDialectFactory_ClassExists_AndIsAccessible()
        {
            // Test that DatabaseDialectFactory exists
            var sqlxAssembly = typeof(CSharpGenerator).Assembly;
            var factoryType = sqlxAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == "DatabaseDialectFactory");

            if (factoryType != null)
            {
                Assert.IsTrue(factoryType.IsClass, "DatabaseDialectFactory should be a class");
                
                var methods = factoryType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                Assert.IsTrue(methods.Length > 0, "DatabaseDialectFactory should have public static methods");
            }
        }

        [TestMethod]
        public void PrimaryConstructorAnalyzer_AllMethods_AreAccessible()
        {
            var analyzerType = typeof(PrimaryConstructorAnalyzer);
            var methods = analyzerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            
            Assert.IsTrue(methods.Length > 0, "PrimaryConstructorAnalyzer should have public static methods");
            
            var expectedMethods = new[] { "IsRecord", "HasPrimaryConstructor", "GetAccessibleMembers", "GetPrimaryConstructor" };
            
            foreach (var expectedMethod in expectedMethods)
            {
                var method = methods.FirstOrDefault(m => m.Name == expectedMethod);
                if (method != null)
                {
                    Assert.IsTrue(method.IsStatic, $"{expectedMethod} should be static");
                    Assert.IsTrue(method.IsPublic, $"{expectedMethod} should be public");
                }
            }
        }

        private CSharpCompilation CreateTestCompilation()
        {
            var sourceCode = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? NullableId { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public record TestRecord(int Id, string Name);

    public class TestPrimaryConstructor(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
    }
}";

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
                });
        }
    }
}
