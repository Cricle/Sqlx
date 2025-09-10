using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class StringInterpolationTests
    {
        [TestMethod]
        public void Format_WithNullOrEmptyFormat_ShouldReturnEmpty()
        {
            // Act & Assert
            Assert.AreEqual(string.Empty, StringInterpolation.Format(null!));
            Assert.AreEqual(string.Empty, StringInterpolation.Format(string.Empty));
        }

        [TestMethod]
        public void Format_WithNoArguments_ShouldReturnOriginalFormat()
        {
            // Arrange
            var format = "Hello World";

            // Act
            var result = StringInterpolation.Format(format);

            // Assert
            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public void Format_WithNullArguments_ShouldReturnOriginalFormat()
        {
            // Arrange
            var format = "Hello World";

            // Act
            var result = StringInterpolation.Format(format, null, null, null);

            // Assert
            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public void Format_WithSingleArgument_ShouldReplaceCorrectly()
        {
            // Arrange
            var format = "Hello {0}";

            // Act
            var result = StringInterpolation.Format(format, "World");

            // Assert
            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public void Format_WithMultipleArguments_ShouldReplaceCorrectly()
        {
            // Arrange
            var format = "Hello {0}, welcome to {1} on {2}";

            // Act
            var result = StringInterpolation.Format(format, "John", "Sqlx", "Monday");

            // Assert
            Assert.AreEqual("Hello John, welcome to Sqlx on Monday", result);
        }

        [TestMethod]
        public void Format_WithRepeatedArguments_ShouldReplaceAll()
        {
            // Arrange
            var format = "{0} loves {0} and {0} is great";

            // Act
            var result = StringInterpolation.Format(format, "Sqlx");

            // Assert
            Assert.AreEqual("Sqlx loves Sqlx and Sqlx is great", result);
        }

        [TestMethod]
        public void Format_WithOutOfRangeIndex_ShouldIgnoreInvalidPlaceholders()
        {
            // Arrange
            var format = "Hello {0}, welcome to {5}";

            // Act
            var result = StringInterpolation.Format(format, "John");

            // Assert
            Assert.AreEqual("Hello John, welcome to ", result);
        }

        [TestMethod]
        public void Format_WithInvalidPlaceholder_ShouldKeepOriginalText()
        {
            // Arrange
            var format = "Hello {abc} and {0}";

            // Act
            var result = StringInterpolation.Format(format, "World");

            // Assert
            Assert.AreEqual("Hello {abc} and World", result);
        }

        [TestMethod]
        public void Format_WithSingleBrace_ShouldKeepBrace()
        {
            // Arrange
            var format = "Hello { and }";

            // Act
            var result = StringInterpolation.Format(format);

            // Assert
            Assert.AreEqual("Hello { and }", result);
        }

        [TestMethod]
        public void Format_WithEmptyPlaceholder_ShouldKeepOriginalText()
        {
            // Arrange
            var format = "Hello {} and {0}";

            // Act
            var result = StringInterpolation.Format(format, "World");

            // Assert
            Assert.AreEqual("Hello {} and World", result);
        }

        [TestMethod]
        public void Format_WithMixedValidAndInvalidPlaceholders_ShouldReplaceValidOnes()
        {
            // Arrange
            var format = "{0} has {invalid} and {1} items";

            // Act
            var result = StringInterpolation.Format(format, "John", "5");

            // Assert
            Assert.AreEqual("John has {invalid} and 5 items", result);
        }

        [TestMethod]
        public void Format_WithAllThreeArguments_ShouldReplaceAll()
        {
            // Arrange
            var format = "{0}-{1}-{2}";

            // Act
            var result = StringInterpolation.Format(format, "A", "B", "C");

            // Assert
            Assert.AreEqual("A-B-C", result);
        }

        [TestMethod]
        public void Format_WithComplexString_ShouldFormatCorrectly()
        {
            // Arrange
            var format = "INSERT INTO {0} (Name, Email) VALUES ('{1}', '{2}')";

            // Act
            var result = StringInterpolation.Format(format, "users", "John Doe", "john@example.com");

            // Assert
            Assert.AreEqual("INSERT INTO users (Name, Email) VALUES ('John Doe', 'john@example.com')", result);
        }

        [TestMethod]
        public void Format_WithNullArgument_ShouldIgnoreNullArgument()
        {
            // Arrange
            var format = "Hello {0} and {1}";

            // Act
            var result = StringInterpolation.Format(format, "World", null);

            // Assert
            Assert.AreEqual("Hello World and ", result);
        }

        [TestMethod]
        public void Format_WithTextAfterLastPlaceholder_ShouldIncludeTrailingText()
        {
            // Arrange
            var format = "Hello {0} from Sqlx!";

            // Act
            var result = StringInterpolation.Format(format, "World");

            // Assert
            Assert.AreEqual("Hello World from Sqlx!", result);
        }

        [TestMethod]
        public void Format_WithNoPlaceholders_ShouldReturnOriginalText()
        {
            // Arrange
            var format = "This is a simple string without placeholders";

            // Act
            var result = StringInterpolation.Format(format, "unused", "args", "here");

            // Assert
            Assert.AreEqual("This is a simple string without placeholders", result);
        }
    }
}
