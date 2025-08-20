namespace HttpMetrics
{
    internal static class MetricSnapshotContext
    {
        private static readonly AsyncLocal<List<PerformanceMetricData>> _current = new();

        public static void Add(PerformanceMetricData metricData)
        {
            if (_current.Value == null)
                _current.Value = new List<PerformanceMetricData>();

            _current.Value.Add(metricData);
        }

        public static List<PerformanceMetricData> SnapshotAndClear()
        {
            var snapshot = _current.Value ?? new List<PerformanceMetricData>();
            _current.Value = null;
            return snapshot;
        }
    }
}
