using OpenTelemetry;
using System.Diagnostics;

namespace HttpMetrics
{
    internal sealed class ApiLogProcessor : BaseProcessor<Activity>
    {
        private readonly MetricOption _opt;

        public ApiLogProcessor(MetricOption opt) => _opt = opt;

        public override void OnEnd(Activity a)
        {
            // Only HttpClient spans
            if (a.Kind != ActivityKind.Client) return;

            // Url
            var urlStr = a.GetTagItem("http.url") as string;
            if (string.IsNullOrEmpty(urlStr) || !Uri.TryCreate(urlStr, UriKind.Absolute, out var url)) return;

            // Allowed hosts (array-safe check)
            if (_opt.AllowedHosts?.Length > 0 &&
                !Array.Exists(_opt.AllowedHosts, h => string.Equals(h, url.Host, StringComparison.OrdinalIgnoreCase)))
                return;

            // Route match (shared helper you added earlier)
            var route = RouteMatcher.TryMatch(_opt.Routes, url);
            if (route is null) return; // your “only log when mapped” rule

            var data = new PerformanceMetricData
            {
                Url = url,
                HttpMethod = (a.GetTagItem("http.method") as string) ?? "GET",
                Duration = a.Duration.TotalMilliseconds,
                ApiName = route.Name,
                IsSuccess = GetIsSuccess(a),
                CorrelationId = a.TraceId.ToString(),
            };

            MetricSnapshotContext.Add(data); 
            PerfMetricLogger.LogPerfMetricData(data);
        }

        private static bool GetIsSuccess(Activity a)
        {
            // Prefer http.status_code if present
            var statusCodeObj = a.GetTagItem("http.status_code");
            if (statusCodeObj is int sc) return sc >= 200 && sc < 300;
            if (statusCodeObj is string s && int.TryParse(s, out var sc2)) return sc2 >= 200 && sc2 < 300;

            // fallback to activity status
            return a.Status != ActivityStatusCode.Error;
        }
    }
}
