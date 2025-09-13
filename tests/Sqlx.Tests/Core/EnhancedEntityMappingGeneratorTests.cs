// -----------------------------------------------------------------------
// <copyright file="EnhancedEntityMappingGeneratorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Targeted tests to increase coverage for EnhancedEntityMappingGenerator paths and helpers.
    /// </summary>
    [TestClass]
    public class EnhancedEntityMappingGeneratorTests
    {
        [TestMethod]
        public void GenerateEntityMapping_NoAccessibleMembers_EmitsFallback()
        {
            var compilation = CreateCompilation(@"namespace TestNS { public class EmptyEntity { } }");
            var entityType = (INamedTypeSymbol)compilation.GetTypeByMetadataName("TestNS.EmptyEntity")!;

            var sb = new IndentedStringBuilder("    ");
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var code = sb.ToString();

            Assert.IsTrue(code.Contains("No accessible members"));
            Assert.IsTrue(code.Contains("new TestNS.EmptyEntity()"));
        }

        [TestMethod]
        public void GenerateEntityMapping_TraditionalClass_UsesObjectInitializer()
        {
            var source = @"namespace TestNS { public class PlainEntity { public int Id { get; set; } public string Name { get; set; } = string.Empty; } }";
            var compilation = CreateCompilation(source);
            var entityType = (INamedTypeSymbol)compilation.GetTypeByMetadataName("TestNS.PlainEntity")!;

            var sb = new IndentedStringBuilder("    ");
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var code = sb.ToString();

            Assert.IsTrue(code.Contains("new TestNS.PlainEntity"));
            Assert.IsTrue(code.Contains("Id = "));
            Assert.IsTrue(code.Contains("Name = "));
        }

        [TestMethod]
        public void GenerateEntityMapping_PrimaryConstructor_WithAdditionalWritableMembers_SetsProperties()
        {
            var source = @"namespace TestNS { public class User(int id, string name) { public int Id { get; } = id; public string Name { get; } = name; public string Description { get; set; } = string.Empty; } }";
            var compilation = CreateCompilation(source);
            var entityType = (INamedTypeSymbol)compilation.GetTypeByMetadataName("TestNS.User")!;

            var sb = new IndentedStringBuilder("    ");
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
            var code = sb.ToString();

            Assert.IsTrue(code.Contains("new TestNS.User("));
            Assert.IsTrue(code.Contains("entity.Description = "));
        }

        [TestMethod]
        public void Private_GetDataReadExpression_CoversValueTypes_Reference_Enum_AndFallback()
        {
            var source = @"namespace TestNS {
                public enum Status { A=0, B=1 }
                public struct CustomValue { public int Value { get; set; } }
            }";
            var compilation = CreateCompilation(source);
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var nullableInt = compilation.GetTypeByMetadataName("System.Nullable`1")!.Construct(intType);
            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            var enumType = (INamedTypeSymbol)compilation.GetTypeByMetadataName("TestNS.Status")!;
            var nullableEnum = compilation.GetTypeByMetadataName("System.Nullable`1")!.Construct(enumType);
            var dtoType = (INamedTypeSymbol)compilation.GetTypeByMetadataName("System.DateTimeOffset")!;
            var nullableDto = compilation.GetTypeByMetadataName("System.Nullable`1")!.Construct(dtoType);
            var guidType = (INamedTypeSymbol)compilation.GetTypeByMetadataName("System.Guid")!;
            var customValue = (INamedTypeSymbol)compilation.GetTypeByMetadataName("TestNS.CustomValue")!;

            var method = typeof(EnhancedEntityMappingGenerator).GetMethod(
                "GetDataReadExpression",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(ITypeSymbol), typeof(string), typeof(string), typeof(string) },
                modifiers: null
            );
            Assert.IsNotNull(method, "Expected private GetDataReadExpression method");

            string Invoke(ITypeSymbol t)
            {
                var result = method!.Invoke(null, new object[] { t, "reader", "Col", "ord" }) as string;
                return result ?? string.Empty;
            }

            // Nullable int -> null coalesce path
            var exprNullableInt = Invoke(nullableInt);
            Assert.IsTrue(exprNullableInt.Contains("IsDBNull(ord) ? null :"));

            // Non-nullable int -> default path
            var exprInt = Invoke(intType);
            Assert.IsTrue(exprInt.Contains("IsDBNull(ord) ? default :"));

            // Non-nullable string -> empty-string fallback path
            var exprString = Invoke(stringType);
            Assert.IsTrue(exprString.Contains("IsDBNull(ord) ? string.Empty :"));

            // Enum (non-nullable) -> ensure we generate some readable expression
            var exprEnum = Invoke(enumType);
            Assert.IsFalse(string.IsNullOrEmpty(exprEnum), $"Unexpected empty enum expr");

            // Nullable enum -> should include null handling of some form
            var exprNullableEnum = Invoke(nullableEnum);
            Assert.IsTrue(
                !string.IsNullOrEmpty(exprNullableEnum) &&
                exprNullableEnum.Contains("IsDBNull(") &&
                exprNullableEnum.Contains("null"),
                $"Unexpected nullable enum expr: {exprNullableEnum}");

            // DateTimeOffset: accept Parse or direct reader method
            var exprDto = Invoke(dtoType);
            Assert.IsTrue(
                !string.IsNullOrEmpty(exprDto) && (
                    exprDto.Contains("DateTimeOffset.Parse") ||
                    exprDto.Contains("GetValue(") ||
                    exprDto.Contains("GetDateTime(") ||
                    exprDto.Contains("GetDateTimeOffset(") ||
                    exprDto.Contains("GetFieldValue")
                ),
                $"Unexpected DateTimeOffset expr: {exprDto}");

            // Nullable DateTimeOffset -> null branch with Parse
            var exprNullableDto = Invoke(nullableDto);
            Assert.IsTrue(exprNullableDto.Contains("IsDBNull(ord) ? null :"));

            // Custom struct (no reader or convert) -> cast from GetValue fallback
            var exprCustom = Invoke(customValue);
            Assert.IsTrue(exprCustom.Contains("(global::TestNS.CustomValue)"));
        }

        [TestMethod]
        public void Private_GetPropertyNameFromParameter_CapitalizesFirstLetter()
        {
            var method = typeof(EnhancedEntityMappingGenerator).GetMethod(
                "GetPropertyNameFromParameter",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.IsNotNull(method);

            var result = method!.Invoke(null, new object[] { "id" }) as string;
            Assert.AreEqual("Id", result);
        }

        private CSharpCompilation CreateCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DateTimeOffset).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Guid).Assembly.Location)
            };

            return CSharpCompilation.Create(
                assemblyName: "EnhancedEntityMappingGeneratorTestsAsm",
                syntaxTrees: new[] { syntaxTree },
                references: references
            );
        }
    }
}


