using HttpMetrics;
namespace HttpMetricsTests
{
    public class MetricBufferTests
    {
        [Fact]
        public void Add_And_SnapshotAndClear_ShouldReturnMetrics()
        {
            var buffer = new MetricBuffer();

            buffer.Add(new PerformanceMetricData
            {
                ApiName = "test",
                Url = new Uri("https://example.com/test"),
                HttpMethod = "GET",
                Duration = 100,
                CorrelationId = Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow
            });

            var snapshot = buffer.SnapshotAndClear();

            Assert.Single(snapshot.Metrics);
            Assert.Equal(1, snapshot.TotalCaptured);
        }

        [Fact]
        public void SnapshotAndClear_ShouldEmptyQueue()
        {
            var buffer = new MetricBuffer();

            buffer.Add(new PerformanceMetricData { StartTime = DateTime.UtcNow });
            buffer.Add(new PerformanceMetricData { StartTime = DateTime.UtcNow });

            var snapshot1 = buffer.SnapshotAndClear();
            var snapshot2 = buffer.SnapshotAndClear();

            Assert.Equal(2, snapshot1.TotalCaptured);
            Assert.Empty(snapshot2.Metrics);
        }
    }
}