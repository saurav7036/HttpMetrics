namespace HttpMetrics;

public class DefaultMetricSnapshotContext : IMetricSnapshotContext
{
    public void Add(PerformanceMetricData m) => MetricSnapshotContext.Add(m);

    public List<PerformanceMetricData> SnapshotAndClear(out int total, out int dropped) =>
        MetricSnapshotContext.SnapshotAndClear(out total, out dropped);
}
