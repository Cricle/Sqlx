using Xunit;
using Sqlx;
using System.Collections.Generic;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for PagedResult class
    /// </summary>
    public class PagedResult_Tests
    {
        [Fact]
        public void PagedResult_DefaultConstructor_InitializesProperties()
        {
            var result = new PagedResult<string>();
            
            Assert.NotNull(result.Items);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.PageNumber);
            Assert.Equal(0, result.PageSize);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public void PagedResult_SetItems_UpdatesProperty()
        {
            var result = new PagedResult<string>
            {
                Items = new List<string> { "item1", "item2", "item3" }
            };
            
            Assert.Equal(3, result.Items.Count);
            Assert.Equal("item1", result.Items[0]);
        }

        [Fact]
        public void PagedResult_SetPageNumber_UpdatesProperty()
        {
            var result = new PagedResult<string> { PageNumber = 2 };
            
            Assert.Equal(2, result.PageNumber);
        }

        [Fact]
        public void PagedResult_SetPageSize_UpdatesProperty()
        {
            var result = new PagedResult<string> { PageSize = 10 };
            
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public void PagedResult_SetTotalCount_UpdatesProperty()
        {
            var result = new PagedResult<string> { TotalCount = 100 };
            
            Assert.Equal(100, result.TotalCount);
        }

        [Fact]
        public void TotalPages_WithZeroPageSize_ReturnsZero()
        {
            var result = new PagedResult<string>
            {
                TotalCount = 100,
                PageSize = 0
            };
            
            Assert.Equal(0, result.TotalPages);
        }

        [Fact]
        public void TotalPages_WithExactDivision_ReturnsCorrectValue()
        {
            var result = new PagedResult<string>
            {
                TotalCount = 100,
                PageSize = 10
            };
            
            Assert.Equal(10, result.TotalPages);
        }

        [Fact]
        public void TotalPages_WithRemainder_RoundsUp()
        {
            var result = new PagedResult<string>
            {
                TotalCount = 105,
                PageSize = 10
            };
            
            Assert.Equal(11, result.TotalPages);
        }

        [Fact]
        public void TotalPages_WithOneItem_ReturnsOne()
        {
            var result = new PagedResult<string>
            {
                TotalCount = 1,
                PageSize = 10
            };
            
            Assert.Equal(1, result.TotalPages);
        }

        [Fact]
        public void HasPrevious_OnFirstPage_ReturnsFalse()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 100
            };
            
            Assert.False(result.HasPrevious);
        }

        [Fact]
        public void HasPrevious_OnSecondPage_ReturnsTrue()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 100
            };
            
            Assert.True(result.HasPrevious);
        }

        [Fact]
        public void HasNext_OnLastPage_ReturnsFalse()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 10,
                PageSize = 10,
                TotalCount = 100
            };
            
            Assert.False(result.HasNext);
        }

        [Fact]
        public void HasNext_OnFirstPage_ReturnsTrue()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 100
            };
            
            Assert.True(result.HasNext);
        }

        [Fact]
        public void HasNext_OnMiddlePage_ReturnsTrue()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 5,
                PageSize = 10,
                TotalCount = 100
            };
            
            Assert.True(result.HasNext);
        }

        [Fact]
        public void FirstItemIndex_WithZeroTotalCount_ReturnsZero()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 0
            };
            
            Assert.Equal(0, result.FirstItemIndex);
        }

        [Fact]
        public void FirstItemIndex_OnFirstPage_ReturnsOne()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 100
            };
            
            Assert.Equal(1, result.FirstItemIndex);
        }

        [Fact]
        public void FirstItemIndex_OnSecondPage_ReturnsEleven()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 100
            };
            
            Assert.Equal(11, result.FirstItemIndex);
        }

        [Fact]
        public void FirstItemIndex_OnThirdPage_ReturnsTwentyOne()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 3,
                PageSize = 10,
                TotalCount = 100
            };
            
            Assert.Equal(21, result.FirstItemIndex);
        }

        [Fact]
        public void LastItemIndex_OnFirstPage_ReturnsTen()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 100
            };
            
            Assert.Equal(10, result.LastItemIndex);
        }

        [Fact]
        public void LastItemIndex_OnLastPage_ReturnsTotalCount()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 10,
                PageSize = 10,
                TotalCount = 100
            };
            
            Assert.Equal(100, result.LastItemIndex);
        }

        [Fact]
        public void LastItemIndex_OnPartialLastPage_ReturnsCorrectValue()
        {
            var result = new PagedResult<string>
            {
                PageNumber = 11,
                PageSize = 10,
                TotalCount = 105
            };
            
            Assert.Equal(105, result.LastItemIndex);
        }

        [Fact]
        public void PagedResult_CompleteScenario_AllPropertiesWork()
        {
            var result = new PagedResult<string>
            {
                Items = new List<string> { "item1", "item2", "item3" },
                PageNumber = 2,
                PageSize = 3,
                TotalCount = 10
            };
            
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(2, result.PageNumber);
            Assert.Equal(3, result.PageSize);
            Assert.Equal(10, result.TotalCount);
            Assert.Equal(4, result.TotalPages); // 10 / 3 = 3.33 -> 4
            Assert.True(result.HasPrevious);
            Assert.True(result.HasNext);
            Assert.Equal(4, result.FirstItemIndex); // (2-1) * 3 + 1 = 4
            Assert.Equal(6, result.LastItemIndex); // min(2 * 3, 10) = 6
        }
    }
}
