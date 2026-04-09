using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Common.Specifications;
using Ardalis.Specification;

namespace Api.Project.Template.Tests.Unit.Common.Specifications;

[Trait("Category", "FilterSpecification")]
public class FilterSpecificationTests
{
    private sealed class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Score { get; set; }
    }

    private sealed class MappedFilterSpec : FilterSpecification<Item>
    {
        private static readonly Dictionary<string, string> Mappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["points"] = "Score"
        };

        public MappedFilterSpec(PaginationRequest request)
            : base(request, Mappings) { }
    }

    [Fact]
    public void Constructor_SetsPageNumberAndPageSizeFromRequest()
    {
        // Arrange
        var request = new PaginationRequest { Page = 3, Size = 25 };

        // Act
        var spec = new FilterSpecification<Item>(request);

        // Assert
        Assert.Equal(3, spec.PageNumber);
        Assert.Equal(25, spec.PageSize);
    }

    [Fact]
    [Trait("Method", "Filters")]
    public void Filters_WhenNull_ReturnsAllItems()
    {
        // Arrange
        var request = new PaginationRequest { Filters = null };
        var spec = new FilterSpecification<Item>(request);
        var items = new[] { new Item { Score = 1 }, new Item { Score = 2 } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    [Trait("Method", "Filters")]
    public void Filters_WhenEmpty_ReturnsAllItems()
    {
        // Arrange
        var request = new PaginationRequest { Filters = [] };
        var spec = new FilterSpecification<Item>(request);
        var items = new[] { new Item { Score = 1 }, new Item { Score = 2 } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    [Trait("Method", "Filters")]
    public void Filters_WithAndOperator_AllConditionsMustMatch()
    {
        // Arrange
        var request = new PaginationRequest
        {
            Operator = LogicalOperator.And,
            Filters = new Dictionary<string, Filter>
            {
                ["Score"] = new Filter { Operator = FilterOperator.Gte, Value = "5" },
                ["Name"]  = new Filter { Operator = FilterOperator.Contains, Value = "foo" }
            }
        };
        var spec = new FilterSpecification<Item>(request);
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
    [Trait("Method", "Filters")]
    public void Filters_WithOrOperator_MatchesWhenEitherConditionIsTrue()
    {
        // Arrange
        var request = new PaginationRequest
        {
            Operator = LogicalOperator.Or,
            Filters = new Dictionary<string, Filter>
            {
                ["Score"] = new Filter { Operator = FilterOperator.Eq, Value = "5" },
                ["Name"]  = new Filter { Operator = FilterOperator.Eq, Value = "hello" }
            }
        };
        var spec = new FilterSpecification<Item>(request);
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
    [Trait("Method", "Sorting")]
    public void Sort_WhenSortByIsSet_OrdersResultsAscending()
    {
        // Arrange
        var request = new PaginationRequest { SortBy = "Score", Direction = SortDirection.Asc };
        var spec = new FilterSpecification<Item>(request);
        var items = new[] { new Item { Score = 30 }, new Item { Score = 10 }, new Item { Score = 20 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(10, result[0].Score);
        Assert.Equal(20, result[1].Score);
        Assert.Equal(30, result[2].Score);
    }

    [Fact]
    [Trait("Method", "Sorting")]
    public void Sort_WhenDirectionIsDesc_OrdersResultsDescending()
    {
        // Arrange
        var request = new PaginationRequest { SortBy = "Score", Direction = SortDirection.Desc };
        var spec = new FilterSpecification<Item>(request);
        var items = new[] { new Item { Score = 10 }, new Item { Score = 30 }, new Item { Score = 20 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(30, result[0].Score);
        Assert.Equal(20, result[1].Score);
        Assert.Equal(10, result[2].Score);
    }

    [Fact]
    [Trait("Method", "Sorting")]
    public void Sort_WhenSortByIsNullAndDefaultSortByIsSet_OrdersByDefaultAscending()
    {
        // Arrange
        var request = new PaginationRequest { SortBy = null };
        var spec = new FilterSpecification<Item>(request, defaultSortBy: "Score");
        var items = new[] { new Item { Score = 30 }, new Item { Score = 10 }, new Item { Score = 20 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(10, result[0].Score);
        Assert.Equal(20, result[1].Score);
        Assert.Equal(30, result[2].Score);
    }

    [Fact]
    [Trait("Method", "Sorting")]
    public void Sort_WhenDefaultSortDirectionIsDesc_OrdersByDefaultDescending()
    {
        // Arrange
        var request = new PaginationRequest { SortBy = null };
        var spec = new FilterSpecification<Item>(request, defaultSortBy: "Score", defaultSortDirection: SortDirection.Desc);
        var items = new[] { new Item { Score = 10 }, new Item { Score = 30 }, new Item { Score = 20 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(30, result[0].Score);
        Assert.Equal(20, result[1].Score);
        Assert.Equal(10, result[2].Score);
    }

    [Fact]
    [Trait("Method", "Sorting")]
    public void Sort_WhenBothSortByAndDefaultAreSet_SortByTakesPrecedence()
    {
        // Arrange
        var request = new PaginationRequest { SortBy = "Score", Direction = SortDirection.Desc };
        var spec = new FilterSpecification<Item>(request, defaultSortBy: "Name", defaultSortDirection: SortDirection.Asc);
        var items = new[] { new Item { Score = 10, Name = "c" }, new Item { Score = 30, Name = "a" }, new Item { Score = 20, Name = "b" } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(30, result[0].Score);
        Assert.Equal(20, result[1].Score);
        Assert.Equal(10, result[2].Score);
    }

    [Fact]
    [Trait("Method", "Sorting")]
    public void Sort_WhenNeitherSortByNorDefaultIsSet_AddsNoOrderExpressions()
    {
        // Arrange
        var request = new PaginationRequest { SortBy = null };

        // Act
        var spec = new FilterSpecification<Item>(request);

        // Assert
        Assert.Empty(spec.OrderExpressions);
    }

    [Fact]
    [Trait("Method", "PropertyMappings")]
    public void PropertyMappings_FiltersOnMappedPropertyName()
    {
        // Arrange
        var request = new PaginationRequest
        {
            Operator = LogicalOperator.And,
            Filters = new Dictionary<string, Filter>
            {
                ["points"] = new Filter { Operator = FilterOperator.Eq, Value = "42" }
            }
        };
        var spec = new MappedFilterSpec(request);
        var items = new[] { new Item { Score = 42 }, new Item { Score = 0 } };

        // Act
        var result = spec.Evaluate(items);

        // Assert
        Assert.Single(result);
        Assert.Equal(42, result.Single().Score);
    }

    [Fact]
    [Trait("Method", "PropertyMappings")]
    public void PropertyMappings_SortsOnMappedPropertyName()
    {
        // Arrange
        var request = new PaginationRequest { SortBy = "points" };
        var spec = new MappedFilterSpec(request);
        var items = new[] { new Item { Score = 30 }, new Item { Score = 10 } };

        // Act
        var result = spec.Evaluate(items).ToList();

        // Assert
        Assert.Equal(10, result[0].Score);
        Assert.Equal(30, result[1].Score);
    }
}
