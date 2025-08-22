using HttpMetrics;
using Xunit;

[Collection("Env collection")]
public class MetricSnapshotContextTests
{
    private readonly DefaultMetricSnapshotContext _ctx = new();

    [Fact]
    public void RingBuffer_RespectsMaxItemsAndDrainLimit()
    {
        for (int i = 0; i < 5; i++)
        {
            _ctx.Add(new PerformanceMetricData
            {
                Url = new Uri($"https://example.com/{i}"),
                HttpMethod = "GET",
                Duration = i,
                ApiName = i.ToString(),
                IsSuccess = true,
                CorrelationId = i.ToString(),
                StartTime = DateTime.UtcNow.AddMilliseconds(i)
            });
        }

        var snapshot = _ctx.SnapshotAndClear(out var total, out var dropped);

        Assert.Equal(3, total);
        Assert.Equal(1, dropped);
        Assert.Equal(2, snapshot.Count);
        Assert.Equal("2", snapshot[0].ApiName);
        Assert.Equal("3", snapshot[1].ApiName);
    }
}
