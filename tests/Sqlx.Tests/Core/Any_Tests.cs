using Xunit;
using Sqlx;
using System;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for the Any placeholder class
    /// </summary>
    public class Any_Tests
    {
        [Fact]
        public void Value_Generic_ReturnsDefault()
        {
            var result = Any.Value<int>();
            Assert.Equal(0, result);
        }

        [Fact]
        public void Value_Generic_WithName_ReturnsDefault()
        {
            var result = Any.Value<string>("param1");
            Assert.Null(result);
        }

        [Fact]
        public void String_ReturnsDefault()
        {
            var result = Any.String();
            Assert.Null(result);
        }

        [Fact]
        public void String_WithName_ReturnsDefault()
        {
            var result = Any.String("param1");
            Assert.Null(result);
        }

        [Fact]
        public void Int_ReturnsDefault()
        {
            var result = Any.Int();
            Assert.Equal(0, result);
        }

        [Fact]
        public void Int_WithName_ReturnsDefault()
        {
            var result = Any.Int("param1");
            Assert.Equal(0, result);
        }

        [Fact]
        public void Bool_ReturnsDefault()
        {
            var result = Any.Bool();
            Assert.False(result);
        }

        [Fact]
        public void Bool_WithName_ReturnsDefault()
        {
            var result = Any.Bool("param1");
            Assert.False(result);
        }

        [Fact]
        public void DateTime_ReturnsDefault()
        {
            var result = Any.DateTime();
            Assert.Equal(default(DateTime), result);
        }

        [Fact]
        public void DateTime_WithName_ReturnsDefault()
        {
            var result = Any.DateTime("param1");
            Assert.Equal(default(DateTime), result);
        }

        [Fact]
        public void Guid_ReturnsDefault()
        {
            var result = Any.Guid();
            Assert.Equal(default(Guid), result);
        }

        [Fact]
        public void Guid_WithName_ReturnsDefault()
        {
            var result = Any.Guid("param1");
            Assert.Equal(default(Guid), result);
        }
    }
}
