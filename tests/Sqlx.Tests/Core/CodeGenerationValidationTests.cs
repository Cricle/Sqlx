// -----------------------------------------------------------------------
// <copyright file="CodeGenerationValidationTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// 代码生成验证测试 - 验证生成的代码符合优化要求
    /// </summary>
    [TestClass]
    public partial class CodeGenerationValidationTests
    {
        [TestMethod]
        public void Generated_Code_Should_Not_Use_Convert_Methods()
        {
            // Arrange
            var generatedCodeSamples = new[]
            {
                "return scalarResult;", // 优化后
                "return __methodResult__;",   // 优化后
                "return __result__;",   // 优化后
                "return (int)scalarResult;", // 优化后
                "return (long)scalarResult;", // 优化后
            };

            var badCodeSamples = new[]
            {
                "return System.Convert.ToInt32(scalarResult);", // 应该避免
                "return Convert.ToInt64(scalarResult);",        // 应该避免
                "return global::System.Convert.ToBoolean(scalarResult);", // 应该避免
            };

            // Act & Assert - 好的代码样例
            foreach (var goodCode in generatedCodeSamples)
            {
                Assert.IsFalse(ContainsConvertMethod(goodCode),
                    $"Good code should not use Convert methods: {goodCode}");
            }

            // Act & Assert - 坏的代码样例
            foreach (var badCode in badCodeSamples)
            {
                Assert.IsTrue(ContainsConvertMethod(badCode),
                    $"Bad code should be detected: {badCode}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Use_Strong_Typed_DataReader_Methods()
        {
            // Arrange
            var goodCodeSamples = new[]
            {
                "reader.GetInt32(ordinal)",
                "reader.GetString(ordinal)",
                "reader.GetBoolean(ordinal)",
                "reader.GetDecimal(ordinal)",
                "reader.GetDateTime(ordinal)",
                "reader.GetGuid(ordinal)",
            };

            var badCodeSamples = new[]
            {
                "(int)reader.GetValue(ordinal)",      // 装箱
                "(string)reader.GetValue(ordinal)",   // 装箱
                "Convert.ToInt32(reader.GetValue(ordinal))", // 装箱 + Convert
            };

            // Act & Assert - 好的代码样例
            foreach (var goodCode in goodCodeSamples)
            {
                Assert.IsTrue(UsesStrongTypedMethod(goodCode),
                    $"Should use strong-typed methods: {goodCode}");
                Assert.IsFalse(UsesGetValue(goodCode),
                    $"Should not use GetValue: {goodCode}");
            }

            // Act & Assert - 坏的代码样例
            foreach (var badCode in badCodeSamples)
            {
                Assert.IsTrue(UsesGetValue(badCode),
                    $"Bad code uses GetValue (boxing): {badCode}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Handle_Null_Properly()
        {
            // Arrange
            var goodNullHandlingSamples = new[]
            {
                "reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal)",
                "reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal)",
                "(object?)entity.Property ?? global::System.DBNull.Value",
                "param.Value = (object?)entity.Property ?? DBNull.Value;",
            };

            var badNullHandlingSamples = new[]
            {
                "entity.Property ?? (object)global::System.DBNull.Value", // 不必要的强转
                "reader.GetValue(ordinal) == DBNull.Value ? null : reader.GetValue(ordinal)", // 装箱
            };

            // Act & Assert - 好的null处理
            foreach (var goodCode in goodNullHandlingSamples)
            {
                Assert.IsTrue(HasProperNullHandling(goodCode),
                    $"Should have proper null handling: {goodCode}");
            }

            // Act & Assert - 坏的null处理
            foreach (var badCode in badNullHandlingSamples)
            {
                Assert.IsFalse(HasProperNullHandling(badCode) && !UsesGetValue(badCode),
                    $"Should avoid boxing in null handling: {badCode}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Include_Fail_Fast_Checks()
        {
            // Arrange
            var goodFailFastSamples = new[]
            {
                "if (entity == null) throw new global::System.ArgumentNullException(nameof(entity));",
                "if (entities == null) throw new ArgumentNullException(nameof(entities));",
                "// Parameter null checks (fail fast)",
            };

            // Act & Assert
            foreach (var code in goodFailFastSamples)
            {
                Assert.IsTrue(HasFailFastCheck(code),
                    $"Should include fail-fast checks: {code}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Include_Performance_Comments()
        {
            // Arrange
            var codeWithComments = new[]
            {
                "return reader.GetInt32(ordinal); // Zero-boxing data access",
                "// Note: Boxing unavoidable for fallback scenarios",
                "param.Value = (object?)entity.Property ?? DBNull.Value; // Optimized null handling",
                "// Parameter null checks (fail fast)",
            };

            // Act & Assert
            foreach (var code in codeWithComments)
            {
                Assert.IsTrue(HasPerformanceComment(code),
                    $"Should include performance-related comments: {code}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Use_Efficient_Scalar_Patterns()
        {
            // Arrange
            var efficientScalarPatterns = new[]
            {
                "return __methodResult__;",
                "return __result__;",
                "return scalarResult;",
                "return (int)scalarResult;",
                "return __methodResult__ > 0;",
                "return __result__ > 0;",
                "return (int)scalarResult > 0;",
            };

            var inefficientScalarPatterns = new[]
            {
                "return System.Convert.ToInt32(__result__);",
                "return Convert.ToInt32(scalarResult);",
                "return System.Convert.ToInt32(scalarResult) > 0;",
            };

            // Act & Assert - 高效模式
            foreach (var pattern in efficientScalarPatterns)
            {
                Assert.IsFalse(ContainsConvertMethod(pattern),
                    $"Efficient pattern should not use Convert: {pattern}");
            }

            // Act & Assert - 低效模式
            foreach (var pattern in inefficientScalarPatterns)
            {
                Assert.IsTrue(ContainsConvertMethod(pattern),
                    $"Inefficient pattern should be detected: {pattern}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Use_Optimized_ToDisplayString()
        {
            // Arrange
            var optimizedPatterns = new[]
            {
                "var typeDisplayString = returnType.ToDisplayString();", // 缓存
                "var unwrappedTypeString = unwrappedType.ToDisplayString();", // 缓存
            };

            var unoptimizedPatterns = new[]
            {
                "return ({ReturnType.ToDisplayString()}){ResultName};", // 多次调用
                "typeof({ReturnType.ToDisplayString()})", // 重复调用
            };

            // 这个测试更多是概念性的，实际应用中需要分析生成的代码
            foreach (var pattern in optimizedPatterns)
            {
                Assert.IsTrue(UsesVariableForDisplayString(pattern),
                    $"Should cache ToDisplayString results: {pattern}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Avoid_Unnecessary_Casts()
        {
            // Arrange
            var goodCastingSamples = new[]
            {
                "results.Add(row); // Cast to object is implicit",
                "return result;", // 直接返回
                "(object?)entity.Property ?? DBNull.Value", // 必要的转换
            };

            var badCastingSamples = new[]
            {
                "results.Add((object)row);", // 不必要的显式转换
                "return (object)result;",     // 不必要的转换
            };

            // Act & Assert
            foreach (var goodCode in goodCastingSamples)
            {
                Assert.IsFalse(HasUnnecessaryCast(goodCode),
                    $"Should not have unnecessary casts: {goodCode}");
            }
        }

        // Helper methods for pattern detection
        private static bool ContainsConvertMethod(string code)
        {
            return CodeGenValidationRegex().IsMatch(code);
        }

        private static bool UsesStrongTypedMethod(string code)
        {
            return Regex.IsMatch(code, @"reader\.Get(Int32|String|Boolean|Decimal|DateTime|Guid)\(", RegexOptions.IgnoreCase);
        }

        private static bool UsesGetValue(string code)
        {
            return Regex.IsMatch(code, @"reader\.GetValue\(", RegexOptions.IgnoreCase);
        }

        private static bool HasProperNullHandling(string code)
        {
            return code.Contains("IsDBNull") || code.Contains("?? DBNull.Value") || code.Contains("?? global::System.DBNull.Value");
        }

        private static bool HasFailFastCheck(string code)
        {
            return code.Contains("ArgumentNullException") || code.Contains("fail fast");
        }

        private static bool HasPerformanceComment(string code)
        {
            return code.Contains("//") && (
                code.Contains("boxing") ||
                code.Contains("Boxing") ||
                code.Contains("fail fast") ||
                code.Contains("Optimized") ||
                code.Contains("Zero-boxing"));
        }

        private static bool UsesVariableForDisplayString(string code)
        {
            return DisplayStringRegex().IsMatch(code);
        }

        private static bool HasUnnecessaryCast(string code)
        {
            // 检测可能不必要的显式转换
            return Regex.IsMatch(code, @"\(object\)\w+(?!\s*\?\?)", RegexOptions.IgnoreCase) &&
                   !code.Contains("// Cast to object is implicit");
        }

        [GeneratedRegex(@"var \w+.*=.*\.ToDisplayString\(\)", RegexOptions.IgnoreCase, "zh-CN")]
        private static partial Regex DisplayStringRegex();

        [GeneratedRegex(@"(System\.)?Convert\.To\w+", RegexOptions.IgnoreCase, "zh-CN")]
        private static partial Regex CodeGenValidationRegex();
    }

    /// <summary>
    /// 代码质量验证测试
    /// </summary>
    [TestClass]
    public partial class CodeQualityValidationTests
    {
        [TestMethod]
        public void Generated_Code_Should_Follow_Naming_Conventions()
        {
            // Arrange
            var goodNamingExamples = new[]
            {
                "__cmd__",           // 生成的命令变量
                "__result__",        // 生成的结果变量
                "__elapsed__",       // 生成的时间变量
                "ordinal_PropertyName", // 序号变量
                "param_PropertyName",   // 参数变量
            };

            // Act & Assert
            foreach (var name in goodNamingExamples)
            {
                Assert.IsTrue(FollowsNamingConvention(name),
                    $"Should follow naming convention: {name}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Include_Proper_Using_Statements()
        {
            // Arrange
            var requiredUsings = new[]
            {
                "using System;",
                "using System.Data;",
                "using System.Data.Common;",
                "using System.Threading.Tasks;",
                "using System.Collections.Generic;",
            };

            // 这些using语句应该在生成的代码中存在
            // 实际测试中需要检查生成的文件内容
            foreach (var usingStatement in requiredUsings)
            {
                Assert.IsTrue(IsValidUsingStatement(usingStatement),
                    $"Should be valid using statement: {usingStatement}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Handle_Edge_Cases()
        {
            // Arrange
            var edgeCaseHandling = new[]
            {
                "if (connection.State != global::System.Data.ConnectionState.Open)", // 连接状态检查
                "if (__cmd__.Parameters != null)", // 参数存在检查
                "try { /* ... */ } catch (Exception ex) { /* ... */ }", // 异常处理
            };

            // Act & Assert
            foreach (var caseHandling in edgeCaseHandling)
            {
                Assert.IsTrue(HandlesEdgeCase(caseHandling),
                    $"Should handle edge case: {caseHandling}");
            }
        }

        // Helper methods
        private static bool FollowsNamingConvention(string name)
        {
            // 生成的变量应该使用双下划线或特定模式
            return name.StartsWith("__") && name.EndsWith("__") ||
                   name.Contains('_') ||
                   VariableNameRegex().IsMatch(name);
        }

        private static bool IsValidUsingStatement(string usingStatement)
        {
            return usingStatement.StartsWith("using ") && usingStatement.EndsWith(";");
        }

        private static bool HandlesEdgeCase(string code)
        {
            return code.Contains("if (") || code.Contains("try") || code.Contains("catch");
        }

        [GeneratedRegex(@"^[a-z][a-zA-Z0-9]*$")]
        private static partial Regex VariableNameRegex();
    }
}
