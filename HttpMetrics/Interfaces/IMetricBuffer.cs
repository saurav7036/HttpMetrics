using HttpMetrics.Models;

namespace HttpMetrics.Interfaces
{
    public interface IMetricBuffer
    {
        MetricBufferSnapshot SnapshotAndClear();
        void Add(PerformanceMetricData m);
    }
}
