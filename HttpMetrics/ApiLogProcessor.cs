using HttpMetrics.Interfaces;
using OpenTelemetry;
using System.Diagnostics;

namespace HttpMetrics
{
    internal sealed class ApiLogProcessor : BaseProcessor<Activity>
    {
        private readonly MetricOption _opt;
        private readonly IMetricBuffer _buffer;


        public ApiLogProcessor(MetricOption opt, IMetricBuffer buffer)
        {
            _opt = opt;
            _buffer = buffer;
        }

        public override void OnEnd(Activity a)
        {
            // Only HttpClient spans
            if (a.Kind != ActivityKind.Client) return;

            // 1) URL (prefer url.full; fall back to http.url; as last resort, reconstruct)
            var url = GetUrl(a);
            if (url is null) return;

            // 2) Allowed hosts
            if (_opt.AllowedHosts?.Length > 0 &&
                !Array.Exists(_opt.AllowedHosts, h => string.Equals(h, url.Host, StringComparison.OrdinalIgnoreCase)))
                return;

            // 3) Route match (log only for mapped routes)
            var route = RouteMatcher.TryMatch(_opt.Routes, url);
            if (route is null) return;

            // 4) Method + success
            var method = GetMethod(a) ?? "GET";
            var isSuccess = GetIsSuccess(a);

            // 5) Build and forward
            var data = new PerformanceMetricData
            {
                Url = url,
                HttpMethod = method,
                Duration = a.Duration.TotalMilliseconds,
                ApiName = route.Name,
                IsSuccess = isSuccess,
                CorrelationId = a.TraceId.ToString(),
                StartTime = a.StartTimeUtc,
            };
            _buffer.Add(data);
            PerfMetricLogger.LogPerfMetricData(data);
        }

        private static Uri? GetUrl(Activity a)
        {
            var s = a.GetTagItem("url.full") as string;
            return Uri.TryCreate(s, UriKind.Absolute, out var u) ? u : null;
        }

        private static string? GetMethod(Activity a)
        {
            return a.GetTagItem("http.request.method") as string;
        }

        private static bool GetIsSuccess(Activity a)
        {
            var statusObj = a.GetTagItem("http.response.status_code")
                          ?? a.GetTagItem("http.status_code");

            if (statusObj is int sc) return sc >= 200 && sc < 300;
            if (statusObj is string s && int.TryParse(s, out var sc2)) return sc2 >= 200 && sc2 < 300;

            // Fallback to activity status when no status_code tag is present
            return a.Status != ActivityStatusCode.Error;
        }
    }
}
