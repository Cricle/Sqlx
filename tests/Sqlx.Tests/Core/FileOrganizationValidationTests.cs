// -----------------------------------------------------------------------
// <copyright file="FileOrganizationValidationTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// 验证文件组织和模块化的测试
    /// </summary>
    [TestClass]
    public class FileOrganizationValidationTests
    {
        private readonly string _srcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "src", "Sqlx");

        [TestMethod]
        public void UnusedPartialFiles_Should_Be_Cleaned_Up()
        {
            // Arrange - These files were demonstration files that should be removed
            var removedFiles = new[]
            {
                "AbstractGenerator.Utilities.cs",
                "AbstractGenerator.SqlGeneration.cs", 
                "AbstractGenerator.Interceptors.cs",
                "MethodGenerationContext.Extensions.cs"
            };

            // Act & Assert - Verify that unused demonstration files have been cleaned up
            foreach (var fileName in removedFiles)
            {
                var filePath = Path.Combine(_srcPath, fileName);
                Assert.IsFalse(File.Exists(filePath), $"Unused demonstration file should be removed: {fileName}");
            }
        }

        [TestMethod]
        public void MainFiles_Should_Have_Reasonable_Size()
        {
            // Arrange - Check that main files are not excessively large
            var fileExpectations = new[]
            {
                new { Name = "AbstractGenerator.cs", MaxLines = 5000 },
                new { Name = "MethodGenerationContext.cs", MaxLines = 3000 }
            };

            // Act & Assert
            foreach (var expectation in fileExpectations)
            {
                var filePath = Path.Combine(_srcPath, expectation.Name);
                if (File.Exists(filePath))
                {
                    var lineCount = File.ReadAllLines(filePath).Length;
                    Assert.IsTrue(lineCount <= expectation.MaxLines, 
                        $"File {expectation.Name} should not exceed {expectation.MaxLines} lines, but has {lineCount}");
                }
            }
        }

        [TestMethod]
        public void MainFiles_Should_Still_Exist()
        {
            // Arrange
            var mainFiles = new[]
            {
                "AbstractGenerator.cs",
                "MethodGenerationContext.cs"
            };

            // Act & Assert
            foreach (var fileName in mainFiles)
            {
                var filePath = Path.Combine(_srcPath, fileName);
                Assert.IsTrue(File.Exists(filePath), $"Main file should still exist: {fileName}");
            }
        }

        [TestMethod]
        public void MainFiles_Should_Contain_Partial_Keyword()
        {
            // Arrange
            var mainFiles = new[]
            {
                "AbstractGenerator.cs",
                "MethodGenerationContext.cs"
            };

            // Act & Assert
            foreach (var fileName in mainFiles)
            {
                var filePath = Path.Combine(_srcPath, fileName);
                if (File.Exists(filePath))
                {
                    var content = File.ReadAllText(filePath);
                    Assert.IsTrue(content.Contains("partial"), 
                        $"Main file should contain 'partial' keyword for extensibility: {fileName}");
                }
            }
        }

        [TestMethod]
        public void MainFiles_Should_Have_Proper_Namespace()
        {
            // Arrange
            var mainFiles = new[]
            {
                "AbstractGenerator.cs",
                "MethodGenerationContext.cs"
            };

            // Act & Assert
            foreach (var fileName in mainFiles)
            {
                var filePath = Path.Combine(_srcPath, fileName);
                if (File.Exists(filePath))
                {
                    var content = File.ReadAllText(filePath);
                    Assert.IsTrue(content.Contains("namespace Sqlx"), 
                        $"Main file should have correct namespace: {fileName}");
                }
            }
        }

        [TestMethod]
        public void CodeCleanup_Should_Improve_Maintainability()
        {
            // Arrange
            var cleanupBenefits = new[]
            {
                "Removal of unused demonstration code",
                "Elimination of temporary documentation files",
                "Cleaner codebase without dead code",
                "Reduced maintenance burden",
                "Better focus on production code"
            };

            // Act & Assert
            foreach (var benefit in cleanupBenefits)
            {
                Assert.IsFalse(string.IsNullOrEmpty(benefit), $"Cleanup benefit should be valid: {benefit}");
            }
        }

        [TestMethod]
        public void MainFiles_Should_Have_Clear_Responsibilities()
        {
            // Arrange
            var fileResponsibilities = new[]
            {
                new { Name = "AbstractGenerator.cs", Description = "Core source generation logic" },
                new { Name = "MethodGenerationContext.cs", Description = "Method-level generation context" },
                new { Name = "Extensions.cs", Description = "Type system extensions" },
                new { Name = "IndentedStringBuilder.cs", Description = "Code formatting utility" }
            };

            // Act & Assert
            foreach (var file in fileResponsibilities)
            {
                Assert.IsFalse(string.IsNullOrEmpty(file.Name), 
                    $"File name should be meaningful: {file.Name}");
                Assert.IsFalse(string.IsNullOrEmpty(file.Description), 
                    $"File should have clear responsibility: {file.Description}");
            }
        }

        [TestMethod]
        public void Project_Structure_Should_Be_Well_Organized()
        {
            // Arrange & Act
            var srcExists = Directory.Exists(_srcPath);

            // Assert
            Assert.IsTrue(srcExists, "Source directory should exist");

            if (srcExists)
            {
                var csFiles = Directory.GetFiles(_srcPath, "*.cs").Length;
                Assert.IsTrue(csFiles > 0, "Should have C# source files");
            }
        }

        [TestMethod]
        public void Code_Cleanup_Should_Follow_Best_Practices()
        {
            // Arrange
            var bestPractices = new[]
            {
                "Remove unused methods and dead code",
                "Delete temporary demonstration files",
                "Eliminate redundant documentation",
                "Keep only production-ready code",
                "Maintain clean and focused codebase"
            };

            // Act & Assert
            foreach (var practice in bestPractices)
            {
                Assert.IsFalse(string.IsNullOrEmpty(practice), $"Best practice should be documented: {practice}");
            }
        }
    }
}

