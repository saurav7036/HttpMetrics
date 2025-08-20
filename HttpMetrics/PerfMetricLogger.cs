namespace HttpMetrics
{
    internal static class PerfMetricLogger
    {
        public static void LogPerfMetricData(PerformanceMetricData performanceMetricData)
        {
            var apilog = new ApiLog()
            {
                CorrelationId = performanceMetricData.CorrelationId,
                ApplicationName = "chatops_perf",
                Verb = performanceMetricData.HttpMethod,
                Api = performanceMetricData.ApiName,
                Url = performanceMetricData.Url.ToString(),
                IsSuccessful = performanceMetricData.IsSuccess,
                TimeTakenInMs = performanceMetricData.Duration
            };

            apilog.SetValue("category", "performance_listner");
            Logger.WriteLogAsync(apilog);
        }
    }
}
