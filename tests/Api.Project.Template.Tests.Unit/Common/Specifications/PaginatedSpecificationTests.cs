using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Common.Specifications;
using Ardalis.Specification;

namespace Api.Project.Template.Tests.Unit.Common.Specifications;

[Trait("Category", "PaginatedSpecification")]
public class PaginatedSpecificationTests
{
    private sealed class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Score { get; set; }
        public int? NullableScore { get; set; }
        public DateOnly Date { get; set; }
        public DateOnly? NullableDate { get; set; }
        public int DoubleScore => Score * 2;
    }

    private sealed class TestSpec : PaginatedSpecification<Item>
    {
        public TestSpec(int pageNumber = 1, int pageSize = 10) : base(pageNumber, pageSize) { }

        public void Sort(string sortBy, SortDirection direction, Dictionary<string, string>? mappings = null)
            => ApplySorting(sortBy, direction, mappings ?? []);

        public void AndFilter(Dictionary<string, Filter> filters, Dictionary<string, string>? mappings = null)
            => ApplyAndFilters(filters, mappings ?? []);

        public void OrFilter(Dictionary<string, Filter> filters, Dictionary<string, string>? mappings = null)
            => ApplyOrFilters(filters, mappings ?? []);
    }

    [Fact]
    public void Constructor_StoresPageNumberAndPageSize()
    {
        // Arrange & Act
        var spec = new TestSpec(3, 25);

        // Assert
        Assert.Equal(3, spec.PageNumber);
        Assert.Equal(25, spec.PageSize);
    }

    [Fact]
    [Trait("Method", "ApplySorting")]
    public void ApplySorting_Ascending_OrdersResultsAscending()
    {
        // Arrange
        var spec = new TestSpec();
        spec.Sort("Score", SortDirection.Asc);
        var items = new[] { new Item { Score = 30 }, new Item { Score = 10 }, new Item { Score = 20 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(10, result[0].Score);
        Assert.Equal(20, result[1].Score);
        Assert.Equal(30, result[2].Score);
    }

    [Fact]
    [Trait("Method", "ApplySorting")]
    public void ApplySorting_Descending_OrdersResultsDescending()
    {
        // Arrange
        var spec = new TestSpec();
        spec.Sort("Score", SortDirection.Desc);
        var items = new[] { new Item { Score = 10 }, new Item { Score = 30 }, new Item { Score = 20 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(30, result[0].Score);
        Assert.Equal(20, result[1].Score);
        Assert.Equal(10, result[2].Score);
    }

    [Fact]
    [Trait("Method", "ApplySorting")]
    public void ApplySorting_PropertyNameCaseInsensitive_OrdersCorrectly()
    {
        // Arrange
        var spec = new TestSpec();
        spec.Sort("score", SortDirection.Asc);
        var items = new[] { new Item { Score = 30 }, new Item { Score = 10 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(10, result[0].Score);
        Assert.Equal(30, result[1].Score);
    }

    [Fact]
    [Trait("Method", "ApplySorting")]
    public void ApplySorting_WithPropertyMapping_OrdersByMappedProperty()
    {
        // Arrange
        var spec = new TestSpec();
        spec.Sort("points", SortDirection.Asc, new Dictionary<string, string> { ["points"] = "Score" });
        var items = new[] { new Item { Score = 30 }, new Item { Score = 10 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(10, result[0].Score);
        Assert.Equal(30, result[1].Score);
    }

    [Fact]
    [Trait("Method", "ApplySorting")]
    public void ApplySorting_UnknownProperty_ThrowsInvalidSpecificationException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act & Assert
        var ex = Assert.Throws<InvalidSpecificationException>(() => spec.Sort("NonExistent", SortDirection.Asc));
        Assert.Equal("NonExistent", ex.PropertyName);
    }

    [Fact]
    [Trait("Method", "ApplySorting")]
    public void ApplySorting_ComputedProperty_ThrowsInvalidSpecificationException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act & Assert
        Assert.Throws<InvalidSpecificationException>(() => spec.Sort("DoubleScore", SortDirection.Asc));
    }

    [Fact]
    [Trait("Method", "ApplySorting")]
    public void ApplySorting_DuplicateSortValues_OrdersByIdAsTieBreaker()
    {
        // Arrange
        var spec = new TestSpec();
        spec.Sort("Score", SortDirection.Desc);
        var items = new[]
        {
            new Item { Id = 3, Score = 10 },
            new Item { Id = 1, Score = 10 },
            new Item { Id = 2, Score = 20 }
        };

        // Act
        var result = spec.Evaluate(items).Select(x => x.Id).ToList();

        // Assert
        Assert.Equal([2, 1, 3], result);
    }

    [Fact]
    [Trait("Method", "ApplySorting")]
    public void ApplySorting_ById_DoesNotAddTieBreaker()
    {
        // Arrange
        var spec = new TestSpec();

        // Act
        spec.Sort("id", SortDirection.Asc);

        // Assert
        Assert.Single(spec.OrderExpressions);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Eq")]
    public void ApplyAndFilters_Eq_MatchesEqualValue()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Score"] = new Filter { Operator = FilterOperator.Eq, Value = "42" }
        });
        var items = new[] { new Item { Score = 42 }, new Item { Score = 99 } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal(42, result.Single().Score);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Ne")]
    public void ApplyAndFilters_Ne_ExcludesEqualValue()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Score"] = new Filter { Operator = FilterOperator.Ne, Value = "42" }
        });
        var items = new[] { new Item { Score = 42 }, new Item { Score = 99 } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal(99, result.Single().Score);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Contains")]
    public void ApplyAndFilters_Contains_MatchesSubstring()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Name"] = new Filter { Operator = FilterOperator.Contains, Value = "oo" }
        });
        var items = new[] { new Item { Name = "foobar" }, new Item { Name = "hello" } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal("foobar", result.Single().Name);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "StartsWith")]
    public void ApplyAndFilters_StartsWith_MatchesPrefix()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Name"] = new Filter { Operator = FilterOperator.StartsWith, Value = "foo" }
        });
        var items = new[] { new Item { Name = "foobar" }, new Item { Name = "barfoo" } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal("foobar", result.Single().Name);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "EndsWith")]
    public void ApplyAndFilters_EndsWith_MatchesSuffix()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Name"] = new Filter { Operator = FilterOperator.EndsWith, Value = "bar" }
        });
        var items = new[] { new Item { Name = "foobar" }, new Item { Name = "barfoo" } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal("foobar", result.Single().Name);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Gt")]
    public void ApplyAndFilters_Gt_MatchesStrictlyGreaterValues()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Score"] = new Filter { Operator = FilterOperator.Gt, Value = "10" }
        });
        var items = new[] { new Item { Score = 9 }, new Item { Score = 10 }, new Item { Score = 11 } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal(11, result.Single().Score);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Gte")]
    public void ApplyAndFilters_Gte_MatchesGreaterOrEqualValues()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Score"] = new Filter { Operator = FilterOperator.Gte, Value = "10" }
        });
        var items = new[] { new Item { Score = 9 }, new Item { Score = 10 }, new Item { Score = 11 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, x => x.Score == 9);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Lt")]
    public void ApplyAndFilters_Lt_MatchesStrictlyLesserValues()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Score"] = new Filter { Operator = FilterOperator.Lt, Value = "10" }
        });
        var items = new[] { new Item { Score = 9 }, new Item { Score = 10 }, new Item { Score = 11 } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal(9, result.Single().Score);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Lte")]
    public void ApplyAndFilters_Lte_MatchesLesserOrEqualValues()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Score"] = new Filter { Operator = FilterOperator.Lte, Value = "10" }
        });
        var items = new[] { new Item { Score = 9 }, new Item { Score = 10 }, new Item { Score = 11 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, x => x.Score == 11);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Between")]
    public void ApplyAndFilters_Between_MatchesInclusiveRange()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Score"] = new Filter { Operator = FilterOperator.Between, ValueFrom = "5", ValueTo = "15" }
        });
        var items = new[] { new Item { Score = 4 }, new Item { Score = 5 }, new Item { Score = 10 }, new Item { Score = 15 }, new Item { Score = 16 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, x => Assert.InRange(x.Score, 5, 15));
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Eq")]
    public void ApplyAndFilters_DateOnly_ConvertsAndMatchesCorrectly()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Date"] = new Filter { Operator = FilterOperator.Eq, Value = "2025-06-15" }
        });
        var items = new[] { new Item { Date = new DateOnly(2025, 6, 15) }, new Item { Date = new DateOnly(2025, 6, 16) } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateOnly(2025, 6, 15), result.Single().Date);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Eq")]
    public void ApplyAndFilters_NullableProperty_MatchesWithCoalesce()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["NullableScore"] = new Filter { Operator = FilterOperator.Eq, Value = "0" }
        });
        var items = new[] { new Item { NullableScore = null }, new Item { NullableScore = 0 }, new Item { NullableScore = 5 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(2, result.Count);  // null coalesces to 0
        Assert.DoesNotContain(result, x => x.NullableScore == 5);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    public void ApplyAndFilters_MultipleFilters_AllConditionsMustMatch()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["Score"] = new Filter { Operator = FilterOperator.Gte, Value = "5" },
            ["Name"]  = new Filter { Operator = FilterOperator.Contains, Value = "foo" }
        });
        var items = new[]
        {
            new Item { Score = 10, Name = "foobar" },  // both match
            new Item { Score = 10, Name = "baz" },      // Name fails
            new Item { Score = 1,  Name = "foobar" }    // Score fails
        };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal("foobar", result.Single().Name);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    public void ApplyAndFilters_WithPropertyMapping_FiltersOnMappedProperty()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(
            new Dictionary<string, Filter>
            {
                ["points"] = new Filter { Operator = FilterOperator.Eq, Value = "42" }
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["points"] = "Score" });
        var items = new[] { new Item { Score = 42 }, new Item { Score = 0 } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal(42, result.Single().Score);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    public void ApplyAndFilters_NullValueForNonBetweenOperator_ThrowsInvalidSpecificationException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act & Assert
        Assert.Throws<InvalidSpecificationException>(() =>
            spec.AndFilter(new Dictionary<string, Filter>
            {
                ["Score"] = new Filter { Operator = FilterOperator.Eq, Value = null }
            }));
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    public void ApplyAndFilters_BetweenWithMissingRangeValues_ThrowsInvalidSpecificationException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act & Assert
        Assert.Throws<InvalidSpecificationException>(() =>
            spec.AndFilter(new Dictionary<string, Filter>
            {
                ["Score"] = new Filter { Operator = FilterOperator.Between }
            }));
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    public void ApplyAndFilters_UnknownProperty_ThrowsInvalidSpecificationException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act & Assert
        var ex = Assert.Throws<InvalidSpecificationException>(() =>
            spec.AndFilter(new Dictionary<string, Filter>
            {
                ["Missing"] = new Filter { Operator = FilterOperator.Eq, Value = "1" }
            }));
        Assert.Equal("Missing", ex.PropertyName);
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    public void ApplyAndFilters_ComputedProperty_ThrowsInvalidSpecificationException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act & Assert
        Assert.Throws<InvalidSpecificationException>(() =>
            spec.AndFilter(new Dictionary<string, Filter>
            {
                ["DoubleScore"] = new Filter { Operator = FilterOperator.Eq, Value = "2" }
            }));
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    public void ApplyAndFilters_UnconvertibleValue_ThrowsInvalidSpecificationException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act & Assert
        Assert.Throws<InvalidSpecificationException>(() =>
            spec.AndFilter(new Dictionary<string, Filter>
            {
                ["Score"] = new Filter { Operator = FilterOperator.Eq, Value = "abc" }
            }));
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Contains")]
    public void ApplyAndFilters_ContainsOnNumericProperty_ThrowsInvalidSpecificationException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act & Assert
        Assert.Throws<InvalidSpecificationException>(() =>
            spec.AndFilter(new Dictionary<string, Filter>
            {
                ["Score"] = new Filter { Operator = FilterOperator.Contains, Value = "1" }
            }));
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Gt")]
    public void ApplyAndFilters_GtOnStringProperty_ThrowsInvalidSpecificationException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act & Assert
        Assert.Throws<InvalidSpecificationException>(() =>
            spec.AndFilter(new Dictionary<string, Filter>
            {
                ["Name"] = new Filter { Operator = FilterOperator.Gt, Value = "a" }
            }));
    }

    [Fact]
    [Trait("Method", "ApplyAndFilters")]
    [Trait("Operator", "Eq")]
    public void ApplyAndFilters_NullableDateOnly_MatchesEqualValue()
    {
        // Arrange
        var spec = new TestSpec();
        spec.AndFilter(new Dictionary<string, Filter>
        {
            ["NullableDate"] = new Filter { Operator = FilterOperator.Eq, Value = "2025-06-15" }
        });
        var items = new[]
        {
            new Item { NullableDate = new DateOnly(2025, 6, 15) },
            new Item { NullableDate = null },
            new Item { NullableDate = new DateOnly(2025, 6, 16) }
        };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateOnly(2025, 6, 15), result.Single().NullableDate);
    }

    [Fact]
    [Trait("Method", "ApplyOrFilters")]
    public void ApplyOrFilters_MultipleFilters_MatchesWhenEitherConditionIsTrue()
    {
        // Arrange
        var spec = new TestSpec();
        spec.OrFilter(new Dictionary<string, Filter>
        {
            ["Score"] = new Filter { Operator = FilterOperator.Eq, Value = "5" },
            ["Name"]  = new Filter { Operator = FilterOperator.Eq, Value = "hello" }
        });
        var items = new[]
        {
            new Item { Score = 5, Name = "other" },   // Score matches
            new Item { Score = 0, Name = "hello" },   // Name matches
            new Item { Score = 5, Name = "hello" },   // Both match
            new Item { Score = 0, Name = "other" }    // Neither matches
        };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, x => x.Score == 0 && x.Name == "other");
    }

    [Fact]
    [Trait("Method", "ApplyOrFilters")]
    public void ApplyOrFilters_WithPropertyMapping_FiltersOnMappedProperty()
    {
        // Arrange
        var spec = new TestSpec();
        spec.OrFilter(
            new Dictionary<string, Filter>
            {
                ["points"] = new Filter { Operator = FilterOperator.Eq, Value = "7" }
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["points"] = "Score" });
        var items = new[] { new Item { Score = 7 }, new Item { Score = 0 } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal(7, result.Single().Score);
    }
}
