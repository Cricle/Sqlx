using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel.DataAnnotations;

namespace Sqlx.Tests.Core;

[TestClass]
public class ValidationHelperTests
{
    [TestMethod]
    public void ValidateObject_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => ValidationHelper.ValidateObject(null!, "payload"));
    }

    [TestMethod]
    public void ValidateObject_ValidInstance_DoesNotThrow()
    {
        ValidationHelper.ValidateObject(new ValidatedPayload { Name = "Alpha", Count = 2 }, "payload");
    }

    [TestMethod]
    public void ValidateObject_InvalidInstance_ThrowsValidationException()
    {
        var ex = Assert.ThrowsException<ValidationException>(() =>
            ValidationHelper.ValidateObject(new ValidatedPayload { Name = "", Count = 0 }, "payload"));

        Assert.IsFalse(string.IsNullOrWhiteSpace(ex.Message));
    }

    [TestMethod]
    public void ValidateValue_WithoutAttributes_DoesNothing()
    {
        ValidationHelper.ValidateValue(null, "value");
    }

    [TestMethod]
    public void ValidateValue_InvalidValue_ThrowsValidationException()
    {
        var ex = Assert.ThrowsException<ValidationException>(() =>
            ValidationHelper.ValidateValue("", "name", new RequiredAttribute(), new MinLengthAttribute(2)));

        Assert.IsFalse(string.IsNullOrWhiteSpace(ex.Message));
    }

    [TestMethod]
    public void ValidateValue_ValidValue_DoesNotThrow()
    {
        ValidationHelper.ValidateValue("ab", "name", new RequiredAttribute(), new MinLengthAttribute(2));
    }

    private sealed class ValidatedPayload
    {
        [Required]
        [MinLength(1)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 10)]
        public int Count { get; set; }
    }
}
