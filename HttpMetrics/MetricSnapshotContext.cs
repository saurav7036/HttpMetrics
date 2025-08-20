using System.Collections.Concurrent;

namespace HttpMetrics
{
    internal static class MetricSnapshotContext
    {
        private static readonly ConcurrentQueue<PerformanceMetricData> Q = new();

        // Max items we keep in memory across the whole process (ring buffer)
        private static readonly int MaxItems =
            int.TryParse(Environment.GetEnvironmentVariable("METRICS_MAX_ITEMS"), out var m)
                ? Math.Clamp(m, 1, 100_000)
                : 2048;

        // Max items we return in the response (per invocation)
        private static readonly int DrainLimit =
            int.TryParse(Environment.GetEnvironmentVariable("METRICS_DRAIN_LIMIT"), out var d)
                ? Math.Clamp(d, 1, MaxItems)
                : 256;

        public static void Add(PerformanceMetricData m)
        {
            Q.Enqueue(m);
            // Best-effort trimming to prevent unbounded growth
            while (Q.Count > MaxItems && Q.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Drains the queue completely so nothing carries over,
        /// but only returns up to DrainLimit items.
        /// </summary>
        public static List<PerformanceMetricData> SnapshotAndClear(out int total, out int dropped)
        {
            total = 0;
            dropped = 0;

            var picked = new List<PerformanceMetricData>(DrainLimit);

            while (Q.TryDequeue(out var item))
            {
                total++;
                if (picked.Count < DrainLimit)
                    picked.Add(item);
                else
                    dropped++;
            }

            // If you added StartedUtc in your model, keep chronological order:
            picked.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

            return picked;
        }
    }
}
