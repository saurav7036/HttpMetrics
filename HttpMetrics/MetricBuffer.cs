using HttpMetrics.Interfaces;
using HttpMetrics.Models;
using System.Collections.Concurrent;

namespace HttpMetrics
{
    public sealed class MetricBuffer : IMetricBuffer
    {
        private readonly ConcurrentQueue<PerformanceMetricData> _queue = new();


        public void Add(PerformanceMetricData m)
        {
            _queue.Enqueue(m);
        }


        public MetricBufferSnapshot SnapshotAndClear()
        {
            int total = 0;
            var picked = new List<PerformanceMetricData>();


            while (_queue.TryDequeue(out var item))
            {
                total++;
                picked.Add(item);
            }


            picked.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));


            return new MetricBufferSnapshot
            {
                Metrics = picked,
                TotalCaptured = total
            };
        }
    }
}
