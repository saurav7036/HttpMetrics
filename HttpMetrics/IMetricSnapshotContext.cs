namespace HttpMetrics;

public interface IMetricSnapshotContext
{
    void Add(PerformanceMetricData m);
    List<PerformanceMetricData> SnapshotAndClear(out int total, out int dropped);
}
