namespace HttpMetrics.Models
{
    public record class MetricBufferSnapshot
    {
        public required List<PerformanceMetricData> Metrics { get; init; }
        public int TotalCaptured { get; init; }
        public int Dropped { get; init; }
    }
}
