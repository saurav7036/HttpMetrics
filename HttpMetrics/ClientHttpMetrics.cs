using Amazon.Lambda.ApplicationLoadBalancerEvents;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HttpMetrics
{
    public static class ClientHttpMetrics
    {
        private static TracerProvider? _tracerProvider;
        private static int _started;

        public static void Start(MetricOption metricOption)
        {
            if (Interlocked.Exchange(ref _started, 1) == 1)
                return;

            if (metricOption is null)
                return;

            _tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddHttpClientInstrumentation(o =>
                {
                    o.RecordException = true;

                    // Gate: only trace calls we care about (host + route match)
                    o.FilterHttpRequestMessage = req =>
                    {
                        var uri = req.RequestUri;
                        if (uri is null) return false;

                        if (metricOption.AllowedHosts?.Length > 0 &&
                            !Array.Exists(metricOption.AllowedHosts,
                                h => string.Equals(h, uri.Host, StringComparison.OrdinalIgnoreCase)))
                            return false;

                        return RouteMatcher.TryMatch(metricOption.Routes, uri) is not null;
                    };

                    // Tag request info (reliable place to add custom tags)
                    o.EnrichWithHttpRequestMessage = (activity, req) =>
                    {
                        var uri = req.RequestUri;
                        if (uri is null) return;

                        activity.SetTag("url.full", uri.ToString());
                        activity.SetTag("http.request.method", req.Method.Method);

                        var match = RouteMatcher.TryMatch(metricOption.Routes, uri);
                        if (match is not null && !string.IsNullOrWhiteSpace(match.Name))
                            activity.SetTag("api.name", match.Name);
                    };

                    // Tag response info (status code)
                    o.EnrichWithHttpResponseMessage = (activity, resp) =>
                    {
                        // Some OTel versions already set this; safe to set again
                        activity.SetTag("http.response.status_code", (int)resp.StatusCode);
                    };

                    // Tag exceptions if thrown during send
                    o.EnrichWithException = (activity, ex) =>
                    {
                        activity.SetTag("exception.type", ex.GetType().FullName);
                        activity.SetTag("exception.message", ex.Message);
                        activity.SetTag("exception.stacktrace", ex.StackTrace);
                    };
                })
                // Your custom processor that converts Activity -> PerformanceMetricData
                .AddProcessor(new ApiLogProcessor(metricOption))
                .Build();

            AppDomain.CurrentDomain.ProcessExit += (_, __) =>
            {
                try { _tracerProvider?.Dispose(); } catch { /* ignore */ }
            };
        }

        public static void StartFromEnvironment() =>
            Start(MetricOptionLoader.FromEnvironment());

        /// <summary>
        /// Appends a "clientMetrics" property to the existing JSON response body.
        /// If the body is not valid JSON, it is returned unchanged.
        /// </summary>
        public static ApplicationLoadBalancerResponse WrapResponse(ApplicationLoadBalancerResponse response)
        {
            if (response is null || string.IsNullOrEmpty(response.Body))
                return response;

            if (JsonNode.Parse(response.Body) is not JsonObject node)
                return response;

            var metrics = MetricSnapshotContext.SnapshotAndClear(out var total, out var dropped);

            node["clientMetrics"] = JsonSerializer.SerializeToNode(metrics);
            node["clientMetricsInfo"] = new JsonObject
            {
                ["totalCaptured"] = total,       // total drained from queue
                ["returned"] = metrics.Count,    // returned in this response
                ["dropped"] = dropped            // not returned due to cap
            };

            response.Body = node.ToJsonString();
            return response;
        }
    }
}
