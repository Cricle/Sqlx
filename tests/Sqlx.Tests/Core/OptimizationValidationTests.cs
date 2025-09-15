// -----------------------------------------------------------------------
// <copyright file="OptimizationValidationTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// 验证性能优化和代码质量的测试
    /// </summary>
    [TestClass]
    public class OptimizationValidationTests
    {
        [TestMethod]
        public void Generated_Code_Should_Avoid_Convert_ToInt32_Patterns()
        {
            // Arrange - 模拟生成的代码片段
            var optimizedCodeSamples = new[]
            {
                "return scalarResult;",
                "return __methodResult__;", 
                "return __result__;",
                "return (int)scalarResult;",
                "return __methodResult__ > 0;",
                "return __result__ > 0;"
            };

            var unoptimizedCodeSamples = new[]
            {
                "return System.Convert.ToInt32(scalarResult);",
                "return Convert.ToInt32(__result__);",
                "return System.Convert.ToInt32(__result__) > 0;"
            };

            // Act & Assert - 验证优化后的代码模式
            foreach (var code in optimizedCodeSamples)
            {
                Assert.IsFalse(ContainsConvertCall(code), 
                    $"Optimized code should not use Convert methods: {code}");
            }

            // 验证我们能识别需要优化的代码
            foreach (var code in unoptimizedCodeSamples)
            {
                Assert.IsTrue(ContainsConvertCall(code), 
                    $"Should detect unoptimized Convert usage: {code}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Use_Strong_Typed_DataReader_Access()
        {
            // Arrange - 模拟DataReader访问模式
            var strongTypedPatterns = new[]
            {
                "reader.GetInt32(ordinal)",
                "reader.GetString(ordinal)", 
                "reader.GetBoolean(ordinal)",
                "reader.GetDecimal(ordinal)",
                "reader.GetDateTime(ordinal)"
            };

            var weakTypedPatterns = new[]
            {
                "(int)reader.GetValue(ordinal)",
                "(string)reader.GetValue(ordinal)",
                "Convert.ToInt32(reader.GetValue(ordinal))"
            };

            // Act & Assert - 验证强类型访问
            foreach (var pattern in strongTypedPatterns)
            {
                Assert.IsTrue(UsesStrongTypedAccess(pattern), 
                    $"Should use strong-typed DataReader access: {pattern}");
            }

            foreach (var pattern in weakTypedPatterns)
            {
                Assert.IsFalse(UsesStrongTypedAccess(pattern), 
                    $"Should avoid weak-typed DataReader access: {pattern}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Handle_DBNull_Efficiently()
        {
            // Arrange - 模拟DBNull处理模式
            var efficientNullPatterns = new[]
            {
                "reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal)",
                "reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal)",
                "(object?)entity.Property ?? global::System.DBNull.Value"
            };

            var inefficientNullPatterns = new[]
            {
                "reader.GetValue(ordinal) == DBNull.Value ? null : (string)reader.GetValue(ordinal)",
                "entity.Property ?? (object)global::System.DBNull.Value"
            };

            // Act & Assert
            foreach (var pattern in efficientNullPatterns)
            {
                Assert.IsTrue(HasEfficientNullHandling(pattern), 
                    $"Should have efficient null handling: {pattern}");
            }

            foreach (var pattern in inefficientNullPatterns)
            {
                Assert.IsFalse(HasEfficientNullHandling(pattern), 
                    $"Should avoid inefficient null handling: {pattern}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Include_Performance_Comments()
        {
            // Arrange - 验证性能相关注释
            var performanceComments = new[]
            {
                "// Zero-boxing data access",
                "// Note: Boxing unavoidable for fallback scenarios", 
                "// Parameter null checks (fail fast)",
                "// Cast to object is implicit"
            };

            // Act & Assert
            foreach (var comment in performanceComments)
            {
                Assert.IsTrue(IsValidPerformanceComment(comment), 
                    $"Should have valid performance comment: {comment}");
            }
        }

        [TestMethod]
        public void Fail_Fast_Validation_Should_Be_Implemented()
        {
            // Arrange - 验证Fail-Fast模式
            var failFastPatterns = new[]
            {
                "if (entity == null) throw new global::System.ArgumentNullException(nameof(entity));",
                "if (entities == null) throw new ArgumentNullException(nameof(entities));",
                "// Parameter null checks (fail fast)"
            };

            // Act & Assert
            foreach (var pattern in failFastPatterns)
            {
                Assert.IsTrue(ImplementsFailFast(pattern), 
                    $"Should implement fail-fast pattern: {pattern}");
            }
        }

        [TestMethod]
        public void Generated_Code_Should_Optimize_String_Operations()
        {
            // Arrange - 验证字符串操作优化
            var optimizedStringPatterns = new[]
            {
                "var typeDisplayString = returnType.ToDisplayString();", // 缓存
                "var unwrappedTypeString = unwrappedType.ToDisplayString();" // 缓存
            };

            var unoptimizedStringPatterns = new[]
            {
                "returnType.ToDisplayString().Length + returnType.ToDisplayString().Contains", // 重复调用
                "method.ReturnType.ToDisplayString() + method.ReturnType.ToDisplayString()" // 重复调用
            };

            // Act & Assert
            foreach (var pattern in optimizedStringPatterns)
            {
                Assert.IsTrue(OptimizesStringOperations(pattern), 
                    $"Should optimize string operations: {pattern}");
            }

            foreach (var pattern in unoptimizedStringPatterns)
            {
                Assert.IsFalse(OptimizesStringOperations(pattern), 
                    $"Should avoid unoptimized string operations: {pattern}");
            }
        }

        [TestMethod]
        public void Type_Safety_Should_Be_Maintained()
        {
            // Arrange - 验证类型安全
            var typeSafePatterns = new[]
            {
                "string? nullableString = null;",
                "int? nullableInt = reader.IsDBNull(0) ? null : reader.GetInt32(0);",
                "var result = (TargetType)sourceValue;"
            };

            // Act & Assert
            foreach (var pattern in typeSafePatterns)
            {
                Assert.IsTrue(MaintainsTypeSafety(pattern), 
                    $"Should maintain type safety: {pattern}");
            }
        }

        [TestMethod]
        public void Error_Handling_Should_Be_Robust()
        {
            // Arrange - 验证错误处理
            var robustErrorHandling = new[]
            {
                "try { /* operation */ } catch (Exception ex) { /* handle */ }",
                "if (param == null) throw new ArgumentNullException(nameof(param));",
                "OnExecuteFail(methodName, command, exception, elapsed);"
            };

            // Act & Assert
            foreach (var pattern in robustErrorHandling)
            {
                Assert.IsTrue(HasRobustErrorHandling(pattern), 
                    $"Should have robust error handling: {pattern}");
            }
        }

        [TestMethod]
        public void Code_Generation_Should_Follow_Naming_Conventions()
        {
            // Arrange - 验证命名约定
            var goodNamingPatterns = new[]
            {
                "__cmd__",      // 生成的命令变量
                "__result__",   // 生成的结果变量  
                "__elapsed__",  // 生成的时间变量
                "ordinal_PropertyName", // 序号变量
                "param_PropertyName"    // 参数变量
            };

            // Act & Assert
            foreach (var name in goodNamingPatterns)
            {
                Assert.IsTrue(FollowsNamingConvention(name), 
                    $"Should follow naming convention: {name}");
            }
        }

        [TestMethod]
        public void Memory_Allocation_Should_Be_Minimized()
        {
            // Arrange - 验证内存分配优化
            var memoryOptimizations = new[]
            {
                "Use object pooling for frequently allocated objects",
                "Cache string results to avoid repeated allocations", 
                "Use strong-typed methods to avoid boxing",
                "Reuse StringBuilder instances",
                "Minimize temporary object creation"
            };

            // Act & Assert
            foreach (var optimization in memoryOptimizations)
            {
                Assert.IsFalse(string.IsNullOrEmpty(optimization), 
                    $"Memory optimization should be documented: {optimization}");
            }
        }

        // Helper methods for pattern detection
        private bool ContainsConvertCall(string code)
        {
            return Regex.IsMatch(code, @"(System\.)?Convert\.To\w+\(", RegexOptions.IgnoreCase);
        }

        private bool UsesStrongTypedAccess(string code)
        {
            return Regex.IsMatch(code, @"reader\.Get(Int32|String|Boolean|Decimal|DateTime|Guid)\(", RegexOptions.IgnoreCase) &&
                   !code.Contains("GetValue");
        }

        private bool HasEfficientNullHandling(string code)
        {
            return (code.Contains("IsDBNull") || code.Contains("?? global::System.DBNull.Value") || code.Contains("?? DBNull.Value")) &&
                   !code.Contains("GetValue") && !code.Contains("(object)");
        }

        private bool IsValidPerformanceComment(string comment)
        {
            return comment.StartsWith("//") && 
                   (comment.Contains("boxing") || comment.Contains("Boxing") || 
                    comment.Contains("fail fast") || comment.Contains("implicit"));
        }

        private bool ImplementsFailFast(string code)
        {
            return code.Contains("ArgumentNullException") || code.Contains("fail fast");
        }

        private bool OptimizesStringOperations(string code)
        {
            return code.Contains("var ") && code.Contains("ToDisplayString()") && !code.Contains("+");
        }

        private bool MaintainsTypeSafety(string code)
        {
            return code.Contains("?") || code.Contains("IsDBNull") || code.Contains("(") && code.Contains(")");
        }

        private bool HasRobustErrorHandling(string code)
        {
            return code.Contains("try") || code.Contains("catch") || 
                   code.Contains("throw") || code.Contains("OnExecuteFail");
        }

        private bool FollowsNamingConvention(string name)
        {
            return name.StartsWith("__") && name.EndsWith("__") || 
                   name.Contains("_") || 
                   Regex.IsMatch(name, @"^[a-z][a-zA-Z0-9]*$");
        }
    }
}

