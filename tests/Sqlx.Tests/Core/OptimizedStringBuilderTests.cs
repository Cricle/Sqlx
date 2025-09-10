using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System.Text;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class OptimizedStringBuilderTests
    {
        [TestMethod]
        public void Create_WithDefaultCapacity_ShouldCreateStringBuilderWithOptimizedCapacity()
        {
            // Act
            var sb = OptimizedStringBuilder.Create();

            // Assert
            Assert.IsNotNull(sb);
            Assert.IsInstanceOfType(sb, typeof(StringBuilder));
            // Capacity should be at least 256 (minimum) and power of 2
            Assert.IsTrue(sb.Capacity >= 256);
            Assert.IsTrue(IsPowerOfTwo(sb.Capacity));
        }

        [TestMethod]
        public void Create_WithSpecificCapacity_ShouldCreateStringBuilderWithPowerOfTwoCapacity()
        {
            // Arrange
            var estimatedLength = 500;

            // Act
            var sb = OptimizedStringBuilder.Create(estimatedLength);

            // Assert
            Assert.IsNotNull(sb);
            Assert.IsTrue(sb.Capacity >= estimatedLength);
            Assert.IsTrue(IsPowerOfTwo(sb.Capacity));
        }

        [TestMethod]
        public void Create_WithSmallCapacity_ShouldUseMinimumCapacity()
        {
            // Arrange
            var smallCapacity = 100;

            // Act
            var sb = OptimizedStringBuilder.Create(smallCapacity);

            // Assert
            Assert.IsNotNull(sb);
            Assert.IsTrue(sb.Capacity >= 256); // Minimum capacity
            Assert.IsTrue(IsPowerOfTwo(sb.Capacity));
        }

        [TestMethod]
        public void AppendJoin_WithEmptyArray_ShouldReturnOriginalStringBuilder()
        {
            // Arrange
            var sb = new StringBuilder("initial");
            var values = new string[0];

            // Act
            var result = sb.AppendJoin(",", values);

            // Assert
            Assert.AreSame(sb, result);
            Assert.AreEqual("initial", sb.ToString());
        }

        [TestMethod]
        public void AppendJoin_WithSingleValue_ShouldAppendWithoutSeparator()
        {
            // Arrange
            var sb = new StringBuilder();
            var values = new[] { "value1" };

            // Act
            sb.AppendJoin(",", values);

            // Assert
            Assert.AreEqual("value1", sb.ToString());
        }

        [TestMethod]
        public void AppendJoin_WithMultipleValues_ShouldAppendWithSeparator()
        {
            // Arrange
            var sb = new StringBuilder();
            var values = new[] { "value1", "value2", "value3" };

            // Act
            sb.AppendJoin(",", values);

            // Assert
            Assert.AreEqual("value1,value2,value3", sb.ToString());
        }

        [TestMethod]
        public void AppendIf_WithTrueCondition_ShouldAppendTrueValue()
        {
            // Arrange
            var sb = new StringBuilder();

            // Act
            sb.AppendIf(true, "true_value", "false_value");

            // Assert
            Assert.AreEqual("true_value", sb.ToString());
        }

        [TestMethod]
        public void AppendIf_WithFalseConditionAndFalseValue_ShouldAppendFalseValue()
        {
            // Arrange
            var sb = new StringBuilder();

            // Act
            sb.AppendIf(false, "true_value", "false_value");

            // Assert
            Assert.AreEqual("false_value", sb.ToString());
        }

        [TestMethod]
        public void AppendIf_WithFalseConditionAndNullFalseValue_ShouldNotAppend()
        {
            // Arrange
            var sb = new StringBuilder("initial");

            // Act
            sb.AppendIf(false, "true_value", null);

            // Assert
            Assert.AreEqual("initial", sb.ToString());
        }

        [TestMethod]
        public void AppendLineIf_WithTrueCondition_ShouldAppendLine()
        {
            // Arrange
            var sb = new StringBuilder();

            // Act
            sb.AppendLineIf(true, "test_line");

            // Assert
            Assert.AreEqual("test_line" + System.Environment.NewLine, sb.ToString());
        }

        [TestMethod]
        public void AppendLineIf_WithFalseCondition_ShouldNotAppend()
        {
            // Arrange
            var sb = new StringBuilder("initial");

            // Act
            sb.AppendLineIf(false, "test_line");

            // Assert
            Assert.AreEqual("initial", sb.ToString());
        }

        [TestMethod]
        public void AppendTemplate_WithParameters_ShouldFormatCorrectly()
        {
            // Arrange
            var sb = new StringBuilder();

            // Act
            sb.AppendTemplate("Hello {0}, you have {1} messages", "John", 5);

            // Assert
            Assert.AreEqual("Hello John, you have 5 messages", sb.ToString());
        }

        [TestMethod]
        public void AppendTemplate_WithNoParameters_ShouldAppendTemplate()
        {
            // Arrange
            var sb = new StringBuilder();

            // Act
            sb.AppendTemplate("Hello World");

            // Assert
            Assert.AreEqual("Hello World", sb.ToString());
        }

        [TestMethod]
        public void ChainedOperations_ShouldWorkCorrectly()
        {
            // Arrange
            var sb = OptimizedStringBuilder.Create();
            var values = new[] { "A", "B", "C" };

            // Act
            sb.AppendIf(true, "Start: ")
              .AppendJoin(", ", values)
              .AppendLineIf(true, "")
              .AppendTemplate("Count: {0}", values.Length);

            // Assert
            var expected = "Start: A, B, C" + System.Environment.NewLine + "Count: 3";
            Assert.AreEqual(expected, sb.ToString());
        }

        /// <summary>
        /// Helper method to check if a number is a power of two.
        /// </summary>
        private static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
}
