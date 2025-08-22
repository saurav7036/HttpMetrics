using HttpMetrics;
using Xunit;

[Collection("Env collection")]
public class RouteMatcherTests
{
    [Fact]
    public void TryMatch_ReturnsExactRoute()
    {
        var routes = new[] { new Route { Template = "/api/values", Name = "Values" } };
        var result = RouteMatcher.TryMatch(routes, new Uri("https://example.com/api/values"));
        Assert.NotNull(result);
        Assert.Equal("Values", result!.Name);
    }

    [Fact]
    public void TryMatch_MatchesParameterSegment()
    {
        var routes = new[] { new Route { Template = "/api/{id}", Name = "ById" } };
        var result = RouteMatcher.TryMatch(routes, new Uri("https://example.com/api/123"));
        Assert.NotNull(result);
        Assert.Equal("ById", result!.Name);
    }

    [Fact]
    public void TryMatch_ReturnsNull_WhenNoMatch()
    {
        var routes = new[] { new Route { Template = "/api/values", Name = "Values" } };
        var result = RouteMatcher.TryMatch(routes, new Uri("https://example.com/api/other"));
        Assert.Null(result);
    }
}
